using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using ER.Simulation.Time;
using ER.World.Polity;
using ER.World.Inventory;
using ER.World.Grid;
using ER.World.Housing;

namespace ER.World.Population
{
    public class PopulationManager : MonoBehaviour
    {
        public static PopulationManager Instance { get; private set; }


        [Header("Configuration Assets")]
        [SerializeField] private PopLocomotionConfig locomotionConfig;

        [SerializeField] private PopMetabolismConfig metabolismConfig;

        [SerializeField] private PopDemographicsConfig demographicsConfig;


        [Header("Starting Population Parameters")]
        [SerializeField] private int initialTribeSize = 28;

        [SerializeField] private float spawnScatterRadius = 10.0f;


        [Header("System References")]
        [SerializeField] private SimulationClockManager clockManager;

        [SerializeField] private PolityRegionManager polityManager;

        [SerializeField] private InventoryManager inventoryManager;

        [SerializeField] private WorldGridManager gridManager;

        [SerializeField] private HouseholdManager householdManager;


        [Header("Debug Visualizer")]
        [SerializeField] private bool drawPopulationGizmos = true;

        [SerializeField] private bool drawVelocityVectors = false;

        [SerializeField] private bool showVitalsGizmoColor = true;


        private NativeArray<PopData> popsBufferA;

        private NativeArray<PopData> popsBufferB;

        private bool isBufferATarget = true;

        private int activePopulationCount = 0;

        private NativeParallelMultiHashMap<int, int> spatialGrid;

        private NativeList<float2> hearthPositions;

        private int nextPopId = 1;

        private bool isInitialized = false;

        private int roadUpdateCooldown = 0;


        public event Action<int> OnPopulationCountChanged;


        public bool IsInitialized => isInitialized;

        public int TotalPopulationCount => activePopulationCount;

        public int AlivePopulationCount
        {
            get
            {
                int count = 0;

                NativeArray<PopData> pops = ActivePops;

                for (int i = 0; i < activePopulationCount; i++)
                {
                    if (pops[i].IsAlive) count++;
                }

                return count;
            }
        }

        public NativeArray<PopData> ActivePops => isBufferATarget ? popsBufferA : popsBufferB;


        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            InitializeStorage();
        }


        private void Start()
        {
            EnsureInitialized();
        }


        public void EnsureInitialized()
        {
            if (isInitialized)
            {
                return;
            }

            if (clockManager == null) clockManager = SimulationClockManager.Instance;

            if (polityManager == null) polityManager = PolityRegionManager.Instance;

            if (inventoryManager == null) inventoryManager = InventoryManager.Instance;

            if (gridManager == null) gridManager = WorldGridManager.Instance;

            if (householdManager == null) householdManager = HouseholdManager.Instance;

            if (polityManager != null)
            {
                polityManager.EnsureInitialized();
            }

            if (inventoryManager != null && !inventoryManager.IsInitialized)
            {
                inventoryManager.InitializeStorage();
            }

            if (householdManager != null)
            {
                householdManager.EnsureInitialized();
            }

            if (polityManager == null || !polityManager.IsInitialized || polityManager.Anchors.Count == 0)
            {
                return;
            }

            UpdateHearthPositionsCache();

            SpawnInitialTribes();

            DistributeInitialHousing();

            if (inventoryManager != null)
            {
                for (int a = 0; a < polityManager.Anchors.Count; a++)
                {
                    int storageId = polityManager.Anchors[a].StorageInventoryId;

                    if (storageId > 0)
                    {
                        inventoryManager.TryAddItem(storageId, 3, 350, 255, 10);

                        inventoryManager.TryAddItem(storageId, 1, 40, 255, 0);
                    }
                }
            }

            if (clockManager != null)
            {
                clockManager.OnTick += HandleSimulationTick;

                clockManager.OnDayPassed += HandleDemographicsDayPassed;
            }

            isInitialized = true;
        }


        private void InitializeStorage()
        {
            const int maxCapacity = 16384;

            popsBufferA = new NativeArray<PopData>(maxCapacity, Allocator.Persistent);

            popsBufferB = new NativeArray<PopData>(maxCapacity, Allocator.Persistent);

            spatialGrid = new NativeParallelMultiHashMap<int, int>(maxCapacity * 2, Allocator.Persistent);

            hearthPositions = new NativeList<float2>(128, Allocator.Persistent);
        }


        private void OnDestroy()
        {
            if (clockManager != null)
            {
                clockManager.OnTick -= HandleSimulationTick;

                clockManager.OnDayPassed -= HandleDemographicsDayPassed;
            }

            DisposeStorage();
        }


        private void OnApplicationQuit()
        {
            DisposeStorage();
        }


        private void DisposeStorage()
        {
            if (hearthPositions.IsCreated) hearthPositions.Dispose();

            if (spatialGrid.IsCreated) spatialGrid.Dispose();

            if (popsBufferA.IsCreated) popsBufferA.Dispose();

            if (popsBufferB.IsCreated) popsBufferB.Dispose();

            isInitialized = false;
        }


        public void UpdateHearthPositionsCache()
        {
            if (!hearthPositions.IsCreated || polityManager == null || gridManager == null)
            {
                return;
            }

            hearthPositions.Clear();

            var anchors = polityManager.Anchors;

            float tileSize = gridManager.MapConfig.TileWorldSize;

            for (int i = 0; i < anchors.Count; i++)
            {
                if (anchors[i].IsActive)
                {
                    float hx = (anchors[i].TileX + 0.5f) * tileSize;

                    float hy = (anchors[i].TileY + 0.5f) * tileSize;

                    hearthPositions.Add(new float2(hx, hy));
                }
            }

            if (householdManager != null && householdManager.IsInitialized)
            {
                var dwellings = householdManager.RawDwellings;

                for (int d = 0; d < dwellings.Length; d++)
                {
                    if (dwellings[d].IsActive)
                    {
                        hearthPositions.Add(new float2(dwellings[d].WorldPositionX, dwellings[d].WorldPositionY));
                    }
                }
            }
        }


        private void SpawnInitialTribes()
        {
            var anchors = polityManager.Anchors;

            float tileSize = gridManager.MapConfig.TileWorldSize;

            ushort minDays = demographicsConfig != null ? demographicsConfig.StartingAdultMinAgeDays : (ushort)1280;

            ushort maxDays = demographicsConfig != null ? demographicsConfig.StartingAdultMaxAgeDays : (ushort)2240;

            for (int a = 0; a < anchors.Count; a++)
            {
                PolityAnchorData anchor = anchors[a];

                float centerX = (anchor.TileX + 0.5f) * tileSize;

                float centerY = (anchor.TileY + 0.5f) * tileSize;

                int popCount = anchor.OwnerPolityId == 1 ? initialTribeSize : initialTribeSize / 2;

                for (int i = 0; i < popCount; i++)
                {
                    Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * spawnScatterRadius;

                    float popX = centerX + randomOffset.x;

                    float popY = centerY + randomOffset.y;

                    Gender gender = (i % 2 == 0) ? Gender.Male : Gender.Female;

                    ushort ageDays = (ushort)UnityEngine.Random.Range(minDays, maxDays);

                    SpawnSinglePop(anchor.OwnerPolityId, anchor.AnchorId, popX, popY, ageDays, gender);
                }
            }

            OnPopulationCountChanged?.Invoke(activePopulationCount);

            Debug.Log($"[PopulationManager] Initialized {activePopulationCount} adult agents using {demographicsConfig?.name ?? "Default"} parameters.");
        }


        private void DistributeInitialHousing()
        {
            if (householdManager == null || !householdManager.IsInitialized)
            {
                return;
            }

            NativeArray<PopData> pops = isBufferATarget ? popsBufferA : popsBufferB;

            List<int> males = new List<int>();

            List<int> females = new List<int>();

            for (int i = 0; i < activePopulationCount; i++)
            {
                if (pops[i].Gender == Gender.Male && pops[i].LifeStage == PopLifeStage.Adult) males.Add(i);
                else if (pops[i].Gender == Gender.Female && pops[i].LifeStage == PopLifeStage.Adult) females.Add(i);
            }

            int pairs = Mathf.Min(males.Count, females.Count);

            for (int p = 0; p < pairs; p++)
            {
                int mIdx = males[p];

                int fIdx = females[p];

                PopData male = pops[mIdx];

                PopData female = pops[fIdx];

                male.SpousePopId = female.PopId;

                female.SpousePopId = male.PopId;

                householdManager.FormHouseholdForCouple(ref male, ref female);

                pops[mIdx] = male;

                pops[fIdx] = female;
            }

            SyncBuffers(pops);

            UpdateHearthPositionsCache();
        }


        public int SpawnSinglePop(
            ushort polityId,
            ushort homeAnchorId,
            float worldX,
            float worldY,
            ushort ageDays,
            Gender gender)
        {
            if (activePopulationCount >= popsBufferA.Length)
            {
                return -1;
            }

            int inventoryId = 0;

            if (inventoryManager != null && inventoryManager.IsInitialized)
            {
                inventoryId = inventoryManager.CreateInventory(15.0f, 20.0f, 30);
            }

            ulong currentTick = clockManager != null ? clockManager.CurrentTime.TotalTicks : 0;

            int popId = nextPopId++;

            PopData newPop = PopData.Create(
                popId,
                polityId,
                homeAnchorId,
                inventoryId,
                worldX,
                worldY,
                currentTick,
                ageDays,
                gender,
                demographicsConfig);

            float wanderRadius = locomotionConfig != null ? locomotionConfig.WanderRadius : 12.0f;

            Vector2 randomTarget = new Vector2(worldX, worldY) + UnityEngine.Random.insideUnitCircle * wanderRadius;

            newPop.TargetPositionX = randomTarget.x;

            newPop.TargetPositionY = randomTarget.y;

            popsBufferA[activePopulationCount] = newPop;

            popsBufferB[activePopulationCount] = newPop;

            activePopulationCount++;

            OnPopulationCountChanged?.Invoke(activePopulationCount);

            return popId;
        }


        private void HandleSimulationTick(WorldTimeData timeData)
        {
            if (!isInitialized || activePopulationCount == 0 || locomotionConfig == null || gridManager == null)
            {
                return;
            }

            NativeArray<PopData> currentReadBuffer = isBufferATarget ? popsBufferA : popsBufferB;

            NativeArray<PopData> currentWriteBuffer = isBufferATarget ? popsBufferB : popsBufferA;

            AssignAutonomousWander(currentReadBuffer);

            spatialGrid.Clear();

            BuildPopSpatialGridJob gridJob = new BuildPopSpatialGridJob
            {
                Pops = currentReadBuffer,
                SpatialGrid = spatialGrid.AsParallelWriter(),
                CellSize = locomotionConfig.SpatialCellSize
            };

            JobHandle gridHandle = gridJob.Schedule(activePopulationCount, 64);

            float tickDelta = 1.0f;

            WorldMapConfig config = gridManager.MapConfig;

            PopLocomotionJob locomotionJob = new PopLocomotionJob
            {
                ReadPops = currentReadBuffer,
                WritePops = currentWriteBuffer,
                SpatialGrid = spatialGrid,
                Tiles = gridManager.RawTiles,
                MapWidth = config.MapWidth,
                MapHeight = config.MapHeight,
                TileWorldSize = config.TileWorldSize,
                DeltaTime = tickDelta,
                BaseWalkSpeed = locomotionConfig.BaseWalkSpeed,
                Acceleration = locomotionConfig.AccelerationRate,
                Drag = locomotionConfig.DragDamping,
                SeparationRadius = locomotionConfig.SeparationRadius,
                SeparationWeight = locomotionConfig.SeparationWeight,
                ArrivalDistance = locomotionConfig.ArrivalDistance,
                SpatialCellSize = locomotionConfig.SpatialCellSize,
                CurrentTick = timeData.TotalTicks
            };

            JobHandle locomotionHandle = locomotionJob.Schedule(activePopulationCount, 32, gridHandle);

            locomotionHandle.Complete();

            RecordTrafficAndWearPaths(currentWriteBuffer, config);

            if (metabolismConfig != null)
            {
                PopMetabolismJob metabolismJob = new PopMetabolismJob
                {
                    Pops = currentWriteBuffer,
                    Tiles = gridManager.RawTiles,
                    HearthPositions = hearthPositions.AsArray(),
                    CurrentTick = timeData.TotalTicks,
                    MapWidth = config.MapWidth,
                    MapHeight = config.MapHeight,
                    TileWorldSize = config.TileWorldSize,
                    AmbientBaseTemperature = timeData.AmbientTemperature,
                    IdleCalorieBurn = metabolismConfig.IdleCalorieBurn,
                    MovingCalorieBurn = metabolismConfig.MovingCalorieBurn,
                    WorkingCalorieBurn = metabolismConfig.WorkingCalorieBurn,
                    ShiveringCalorieCost = metabolismConfig.ShiveringCalorieCost,
                    BaseHydrationDecay = metabolismConfig.BaseHydrationDecay,
                    WorkHydrationMultiplier = metabolismConfig.WorkHydrationMultiplier,
                    NominalBodyTemp = metabolismConfig.NominalBodyTemp,
                    BaseComfortTemp = metabolismConfig.BaseComfortTemp,
                    ActiveWarmingRate = metabolismConfig.ActiveWarmingRate,
                    PassiveCoolingRate = metabolismConfig.PassiveCoolingRate,
                    WorkHeatBoost = metabolismConfig.WorkMetabolicHeatBoost,
                    HearthRadius = metabolismConfig.HearthHeatRadius,
                    HearthHeatBonus = metabolismConfig.HearthMaxHeatBonus,
                    HypothermiaCritTemp = metabolismConfig.HypothermiaCriticalTemp,
                    StarvationDamage = metabolismConfig.StarvationDamage,
                    DehydrationDamage = metabolismConfig.DehydrationDamage,
                    HypothermiaDamage = metabolismConfig.HypothermiaDamage,
                    NaturalHealingRate = metabolismConfig.NaturalHealingRate
                };

                JobHandle metabolismHandle = metabolismJob.Schedule(activePopulationCount, 64);

                metabolismHandle.Complete();

                ProcessSustenanceAndFeeding(currentWriteBuffer);
            }

            isBufferATarget = !isBufferATarget;
        }


        private void RecordTrafficAndWearPaths(NativeArray<PopData> pops, WorldMapConfig config)
        {
            var tiles = gridManager.RawTiles;

            float tileSize = config.TileWorldSize;

            bool roadsChanged = false;

            for (int i = 0; i < activePopulationCount; i++)
            {
                PopData pop = pops[i];

                if (!pop.IsAlive || pop.ActivityState != PopActivityState.Moving) continue;

                int tx = Mathf.FloorToInt(pop.WorldPositionX / tileSize);

                int ty = Mathf.FloorToInt(pop.WorldPositionY / tileSize);

                if (!config.IsCoordsValid(tx, ty)) continue;

                int idx = config.CoordsToIndex(tx, ty);

                TileData t = tiles[idx];

                if (!t.HasFlag(TileData.FlagHasRoad))
                {
                    if (UnityEngine.Random.value < 0.002f)
                    {
                        t.SetFlag(TileData.FlagHasRoad, true);

                        tiles[idx] = t;

                        roadsChanged = true;
                    }
                }
            }

            roadUpdateCooldown++;

            if (roadsChanged && roadUpdateCooldown >= 300 && polityManager != null)
            {
                roadUpdateCooldown = 0;

                polityManager.RefreshVisuals();
            }
        }


        private void HandleDemographicsDayPassed(WorldTimeData timeData)
        {
            if (!isInitialized || activePopulationCount == 0 || demographicsConfig == null)
            {
                return;
            }

            NativeArray<PopData> activePops = isBufferATarget ? popsBufferA : popsBufferB;

            PopDemographicsJob demographicsJob = new PopDemographicsJob
            {
                Pops = activePops,
                InfantMaxDays = demographicsConfig.InfantMaxDays,
                ChildMaxDays = demographicsConfig.ChildMaxDays,
                YouthMaxDays = demographicsConfig.YouthMaxDays,
                AdultMaxDays = demographicsConfig.AdultMaxDays,
                ElderBaseAgeDays = demographicsConfig.ElderBaseMortalityAgeDays,
                BaseSenescenceChance = demographicsConfig.BaseSenescenceDailyChance,
                SenescenceCurveMultiplier = demographicsConfig.SenescenceAgeCurveMultiplier
            };

            JobHandle handle = demographicsJob.Schedule(activePopulationCount, 64);

            handle.Complete();

            ProcessBirthDeliveries(activePops);

            ProcessClanMatrimonyAndConception(activePops);

            SyncBuffers(activePops);
        }


        private void ProcessBirthDeliveries(NativeArray<PopData> pops)
        {
            int currentCount = activePopulationCount;

            ushort pregnancyLimit = demographicsConfig != null ? demographicsConfig.PregnancyDurationDays : (ushort)48;

            ushort postPartumLimit = demographicsConfig != null ? demographicsConfig.PostPartumCooldownDays : (ushort)16;

            for (int i = 0; i < currentCount; i++)
            {
                PopData mother = pops[i];

                if (!mother.IsAlive || !mother.IsPregnant)
                {
                    continue;
                }

                if (mother.PregnancyDays >= pregnancyLimit)
                {
                    mother.IsPregnant = false;

                    mother.PregnancyDays = 0;

                    mother.MatingCooldownDays = postPartumLimit;

                    pops[i] = mother;

                    Gender newbornGender = UnityEngine.Random.value > 0.5f ? Gender.Male : Gender.Female;

                    float childX = mother.WorldPositionX + UnityEngine.Random.Range(-1.2f, 1.2f);

                    float childY = mother.WorldPositionY + UnityEngine.Random.Range(-1.2f, 1.2f);

                    int childId = SpawnSinglePop(mother.PolityId, mother.HomeAnchorId, childX, childY, 0, newbornGender);

                    if (childId > 0 && householdManager != null)
                    {
                        NativeArray<PopData> updatedPops = isBufferATarget ? popsBufferA : popsBufferB;

                        int childIndex = activePopulationCount - 1;

                        PopData child = updatedPops[childIndex];

                        householdManager.AssignNewbornToHousehold(ref mother, ref child);

                        updatedPops[childIndex] = child;

                        updatedPops[i] = mother;
                    }

                    Debug.Log($"[PopulationManager] Baby born in Household {mother.HouseholdId} (Polity {mother.PolityId})! Total alive pop: {AlivePopulationCount}");
                }
            }
        }


        private void ProcessClanMatrimonyAndConception(NativeArray<PopData> pops)
        {
            int count = activePopulationCount;

            float minHealth = demographicsConfig != null ? demographicsConfig.ConceptionMinHealth : 180.0f;

            float minCal = demographicsConfig != null ? demographicsConfig.ConceptionMinCalories : 1200.0f;

            float minTemp = demographicsConfig != null ? demographicsConfig.ConceptionMinBodyTemp : 35.5f;

            float prob = demographicsConfig != null ? demographicsConfig.DailyConceptionProbability : 0.04f;

            for (int i = 0; i < count; i++)
            {
                PopData female = pops[i];

                if (!female.IsAlive || female.Gender != Gender.Female || female.LifeStage != PopLifeStage.Adult)
                {
                    continue;
                }

                if (female.IsPregnant || female.MatingCooldownDays > 0)
                {
                    continue;
                }

                if (female.Health < minHealth || female.CurrentCalories < minCal || female.BodyTemperature < minTemp)
                {
                    continue;
                }

                if (householdManager != null && !householdManager.HasHousingCapacity(female.DwellingId))
                {
                    if (UnityEngine.Random.value > 0.10f)
                    {
                        continue;
                    }
                }

                bool hasPartnerNearby = false;

                if (female.SpousePopId > 0)
                {
                    for (int j = 0; j < count; j++)
                    {
                        if (pops[j].PopId == female.SpousePopId && pops[j].IsAlive)
                        {
                            hasPartnerNearby = true;
                            break;
                        }
                    }
                }

                if (hasPartnerNearby && UnityEngine.Random.value < prob)
                {
                    female.IsPregnant = true;

                    female.PregnancyDays = 0;

                    pops[i] = female;

                    Debug.Log($"[PopulationManager] Conception occurred in Household {female.HouseholdId}!");
                }
            }
        }


        private void SyncBuffers(NativeArray<PopData> source)
        {
            if (isBufferATarget)
            {
                popsBufferB.CopyFrom(source);
            }
            else
            {
                popsBufferA.CopyFrom(source);
            }
        }


        private void ProcessSustenanceAndFeeding(NativeArray<PopData> pops)
        {
            if (inventoryManager == null || polityManager == null)
            {
                return;
            }

            for (int i = 0; i < activePopulationCount; i++)
            {
                PopData pop = pops[i];

                if (!pop.IsAlive)
                {
                    continue;
                }

                if (pop.CurrentCalories < (metabolismConfig != null ? metabolismConfig.HungerThreshold : 1000.0f))
                {
                    if (inventoryManager.GetItemCount(pop.PersonalInventoryId, 3) > 0)
                    {
                        inventoryManager.ExecuteTransfer(pop.PersonalInventoryId, 0, 3, 1, out _);

                        pop.CurrentCalories = math.min(pop.MaxCalories, pop.CurrentCalories + 450.0f);

                        pops[i] = pop;
                    }
                    else if (inventoryManager.GetItemCount(pop.PersonalInventoryId, 8) > 0)
                    {
                        inventoryManager.ExecuteTransfer(pop.PersonalInventoryId, 0, 8, 1, out _);

                        pop.CurrentCalories = math.min(pop.MaxCalories, pop.CurrentCalories + 550.0f);

                        pops[i] = pop;
                    }
                    else if (inventoryManager.GetItemCount(pop.PersonalInventoryId, 4) > 0)
                    {
                        inventoryManager.ExecuteTransfer(pop.PersonalInventoryId, 0, 4, 1, out _);

                        pop.CurrentCalories = math.min(pop.MaxCalories, pop.CurrentCalories + 650.0f);

                        pops[i] = pop;
                    }
                    else if (polityManager.TryGetAnchor(pop.HomeAnchorId, out PolityAnchorData anchor) && anchor.StorageInventoryId > 0)
                    {
                        float distToHearth = math.distance(
                            new float2(pop.WorldPositionX, pop.WorldPositionY),
                            new float2((anchor.TileX + 0.5f) * gridManager.MapConfig.TileWorldSize, (anchor.TileY + 0.5f) * gridManager.MapConfig.TileWorldSize));

                        if (distToHearth <= (metabolismConfig != null ? metabolismConfig.HearthHeatRadius : 24.0f))
                        {
                            if (inventoryManager.GetItemCount(anchor.StorageInventoryId, 3) > 0)
                            {
                                inventoryManager.ExecuteTransfer(anchor.StorageInventoryId, 0, 3, 1, out _);

                                pop.CurrentCalories = math.min(pop.MaxCalories, pop.CurrentCalories + 450.0f);

                                pops[i] = pop;
                            }
                            else if (inventoryManager.GetItemCount(anchor.StorageInventoryId, 8) > 0)
                            {
                                inventoryManager.ExecuteTransfer(anchor.StorageInventoryId, 0, 8, 1, out _);

                                pop.CurrentCalories = math.min(pop.MaxCalories, pop.CurrentCalories + 550.0f);

                                pops[i] = pop;
                            }
                            else if (inventoryManager.GetItemCount(anchor.StorageInventoryId, 4) > 0)
                            {
                                inventoryManager.ExecuteTransfer(anchor.StorageInventoryId, 0, 4, 1, out _);

                                pop.CurrentCalories = math.min(pop.MaxCalories, pop.CurrentCalories + 650.0f);

                                pops[i] = pop;
                            }
                        }
                    }
                }

                if (pop.WarmthInsulation < 180 && polityManager.TryGetAnchor(pop.HomeAnchorId, out PolityAnchorData homeAnchor))
                {
                    int storageId = homeAnchor.StorageInventoryId;

                    if (storageId > 0 && inventoryManager.GetItemCount(storageId, 7) > 0)
                    {
                        inventoryManager.ExecuteTransfer(storageId, 0, 7, 1, out uint equipped);

                        if (equipped > 0)
                        {
                            pop.WarmthInsulation = 180;

                            pops[i] = pop;

                            Debug.Log($"[PopulationManager] Pop {pop.PopId} equipped Warm Fur Parka! Insulation increased to 180.");
                        }
                    }
                }

                if (pop.CurrentHydration < 40.0f)
                {
                    pop.CurrentHydration = math.min(100.0f, pop.CurrentHydration + 30.0f);

                    pops[i] = pop;
                }
            }
        }


        private void AssignAutonomousWander(NativeArray<PopData> pops)
        {
            float tileSize = gridManager.MapConfig.TileWorldSize;

            for (int i = 0; i < activePopulationCount; i++)
            {
                PopData pop = pops[i];

                if (!pop.IsAlive)
                {
                    continue;
                }

                if (pop.ActivityState == PopActivityState.Idle && UnityEngine.Random.value < 0.08f)
                {
                    if (inventoryManager != null && inventoryManager.TryGetHeader(pop.PersonalInventoryId, out InventoryHeaderData header))
                    {
                        if (header.CurrentWeightKg >= 12.0f)
                        {
                            continue;
                        }
                    }

                    Vector2 centerPos;

                    float wanderDist = 2.5f;

                    if (householdManager != null && householdManager.TryGetDwellingPosition(pop.DwellingId, out float2 dPos))
                    {
                        centerPos = new Vector2(dPos.x, dPos.y);
                    }
                    else
                    {
                        centerPos = ResolveAnchorPosition(pop.HomeAnchorId, tileSize);

                        wanderDist = locomotionConfig.WanderRadius * 0.45f;
                    }

                    Vector2 wanderTarget = centerPos + UnityEngine.Random.insideUnitCircle * wanderDist;

                    pop.TargetPositionX = wanderTarget.x;

                    pop.TargetPositionY = wanderTarget.y;

                    pop.ActivityState = PopActivityState.Moving;

                    pops[i] = pop;
                }
            }
        }


        private Vector2 ResolveAnchorPosition(ushort anchorId, float tileSize)
        {
            if (polityManager == null) return Vector2.zero;

            var anchors = polityManager.Anchors;

            for (int i = 0; i < anchors.Count; i++)
            {
                if (anchors[i].AnchorId == anchorId)
                {
                    return new Vector2((anchors[i].TileX + 0.5f) * tileSize, (anchors[i].TileY + 0.5f) * tileSize);
                }
            }

            return Vector2.zero;
        }


        private void OnDrawGizmos()
        {
            if (!drawPopulationGizmos || activePopulationCount == 0)
            {
                return;
            }

            NativeArray<PopData> active = ActivePops;

            if (!active.IsCreated)
            {
                return;
            }

            for (int i = 0; i < activePopulationCount; i++)
            {
                PopData pop = active[i];

                if (!pop.IsAlive)
                {
                    if (!pop.IsBuried && pop.WorldPositionX != 0.0f && pop.WorldPositionY != 0.0f)
                    {
                        Vector3 corpsePos = new Vector3(pop.WorldPositionX, pop.WorldPositionY, -3.0f);

                        Gizmos.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

                        Gizmos.DrawCube(corpsePos, new Vector3(1.2f, 0.5f, 0.5f));
                    }

                    continue;
                }

                Vector3 pos = new Vector3(pop.WorldPositionX, pop.WorldPositionY, -3.0f);

                if (showVitalsGizmoColor)
                {
                    if (pop.CurrentCalories <= 0.0f)
                    {
                        Gizmos.color = new Color(1.0f, 0.2f, 0.0f);
                    }
                    else if (pop.BodyTemperature < 32.0f)
                    {
                        Gizmos.color = Color.cyan;
                    }
                    else if (pop.IsPregnant)
                    {
                        Gizmos.color = new Color(1.0f, 0.4f, 0.8f);
                    }
                    else if (pop.LifeStage == PopLifeStage.Infant)
                    {
                        Gizmos.color = Color.white;
                    }
                    else if (pop.CurrentCalories < (metabolismConfig != null ? metabolismConfig.HungerThreshold : 1000.0f))
                    {
                        Gizmos.color = Color.yellow;
                    }
                    else
                    {
                        Gizmos.color = pop.Gender == Gender.Male ? new Color(0.3f, 0.7f, 1.0f) : new Color(0.9f, 0.5f, 0.8f);
                    }
                }
                else
                {
                    Gizmos.color = pop.PolityId == 1 ? Color.blue : (pop.PolityId == 2 ? Color.red : Color.green);
                }

                float scale = pop.LifeStage == PopLifeStage.Infant ? 0.45f : (pop.LifeStage == PopLifeStage.Child ? 0.65f : 0.85f);

                Gizmos.DrawSphere(pos, scale);

                float speedSqr = pop.VelocityX * pop.VelocityX + pop.VelocityY * pop.VelocityY;

                if (drawVelocityVectors && speedSqr > 0.05f)
                {
                    Gizmos.color = Color.yellow;

                    Vector3 velTarget = pos + new Vector3(pop.VelocityX, pop.VelocityY, 0.0f) * 1.5f;

                    Gizmos.DrawLine(pos, velTarget);
                }
            }
        }
    }
}