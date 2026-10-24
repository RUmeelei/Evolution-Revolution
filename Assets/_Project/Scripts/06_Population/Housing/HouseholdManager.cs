using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using ER.World.Grid;
using ER.World.Polity;
using ER.World.Population;

namespace ER.World.Housing
{
    public class HouseholdManager : MonoBehaviour
    {
        public static HouseholdManager Instance { get; private set; }


        [Header("Starting Housing Setup")]
        [SerializeField] private int startingDwellingsPerCamp = 6;

        [SerializeField] private float minPlacementRadius = 11.0f;

        [SerializeField] private float maxPlacementRadius = 20.0f;


        [Header("System References")]
        [SerializeField] private PolityRegionManager polityManager;

        [SerializeField] private WorldGridManager gridManager;


        [Header("Debug Visualizer")]
        [SerializeField] private bool drawDwellingGizmos = true;


        private NativeList<DwellingData> dwellings;

        private NativeList<HouseholdData> households;

        private int nextDwellingId = 1;

        private int nextHouseholdId = 1;

        private bool isInitialized = false;


        public bool IsInitialized => isInitialized;

        public NativeList<DwellingData> RawDwellings => dwellings;

        public NativeList<HouseholdData> RawHouseholds => households;


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

            if (polityManager == null) polityManager = PolityRegionManager.Instance;

            if (gridManager == null) gridManager = WorldGridManager.Instance;

            if (polityManager != null)
            {
                polityManager.EnsureInitialized();
            }

            if (polityManager == null || !polityManager.IsInitialized || polityManager.Anchors.Count == 0)
            {
                return;
            }

            if (startingDwellingsPerCamp > 0)
            {
                SpawnStartingSettlementDwellings();
            }

            isInitialized = true;
        }


        private void InitializeStorage()
        {
            if (!dwellings.IsCreated)
            {
                dwellings = new NativeList<DwellingData>(1024, Allocator.Persistent);
            }

            if (!households.IsCreated)
            {
                households = new NativeList<HouseholdData>(1024, Allocator.Persistent);
            }
        }


        private void OnDestroy()
        {
            DisposeStorage();
        }


        private void OnApplicationQuit()
        {
            DisposeStorage();
        }


        private void DisposeStorage()
        {
            if (households.IsCreated) households.Dispose();

            if (dwellings.IsCreated) dwellings.Dispose();

            isInitialized = false;
        }


        private void SpawnStartingSettlementDwellings()
        {
            var anchors = polityManager.Anchors;

            float tileSize = gridManager.MapConfig.TileWorldSize;

            for (int a = 0; a < anchors.Count; a++)
            {
                PolityAnchorData anchor = anchors[a];

                float centerX = (anchor.TileX + 0.5f) * tileSize;

                float centerY = (anchor.TileY + 0.5f) * tileSize;

                for (int i = 0; i < startingDwellingsPerCamp; i++)
                {
                    float angle = (i / (float)startingDwellingsPerCamp) * Mathf.PI * 2.0f + 0.25f;

                    float dist = UnityEngine.Random.Range(minPlacementRadius, maxPlacementRadius);

                    float dx = Mathf.Cos(angle) * dist;

                    float dy = Mathf.Sin(angle) * dist;

                    RegisterDwelling(anchor.OwnerPolityId, anchor.AnchorId, DwellingType.DugoutShelter, centerX + dx, centerY + dy, 5, 18.0f);
                }
            }

            Debug.Log($"[HouseholdManager] Successfully established {dwellings.Length} primitive dwellings across settlements.");
        }


        public int RegisterDwelling(
            ushort polityId,
            ushort anchorId,
            DwellingType type,
            float x,
            float y,
            byte capacity,
            float thermalBonus)
        {
            int id = nextDwellingId++;

            DwellingData dwelling = DwellingData.Create(id, polityId, anchorId, type, x, y, capacity, thermalBonus);

            dwellings.Add(dwelling);

            return id;
        }


        public bool TryAssignHomelessToDwelling(int dwellingId, ushort anchorId)
        {
            if (!dwellings.IsCreated || dwellingId <= 0)
            {
                return false;
            }

            int dwellingIndex = -1;

            for (int i = 0; i < dwellings.Length; i++)
            {
                if (dwellings[i].DwellingId == dwellingId && dwellings[i].IsActive)
                {
                    dwellingIndex = i;
                    break;
                }
            }

            if (dwellingIndex < 0)
            {
                return false;
            }

            DwellingData dwelling = dwellings[dwellingIndex];

            if (dwelling.OccupantCount >= dwelling.Capacity)
            {
                return false;
            }

            bool assignedAny = false;

            for (int h = 0; h < households.Length; h++)
            {
                HouseholdData hh = households[h];

                if (hh.HomeAnchorId == anchorId && hh.DwellingId <= 0)
                {
                    byte neededBeds = (byte)(2 + hh.ChildrenCount);

                    if (dwelling.OccupantCount + neededBeds <= dwelling.Capacity || dwelling.OccupantCount == 0)
                    {
                        hh.DwellingId = dwellingId;

                        households[h] = hh;

                        dwelling.OccupantCount = (byte)Mathf.Min(dwelling.Capacity, dwelling.OccupantCount + neededBeds);

                        assignedAny = true;

                        UpdateHouseholdPopsDwelling(hh.HouseholdId, dwellingId, dwelling.WorldPositionX, dwelling.WorldPositionY);

                        Debug.Log($"[HouseholdManager] Household #{hh.HouseholdId} moved into Dwelling #{dwellingId}!");

                        if (dwelling.OccupantCount >= dwelling.Capacity)
                        {
                            break;
                        }
                    }
                }
            }

            if (dwelling.OccupantCount < dwelling.Capacity && PopulationManager.Instance != null && PopulationManager.Instance.IsInitialized)
            {
                NativeArray<PopData> pops = PopulationManager.Instance.ActivePops;

                int popCount = PopulationManager.Instance.TotalPopulationCount;

                for (int p = 0; p < popCount; p++)
                {
                    PopData pop = pops[p];

                    if (pop.IsAlive && pop.HomeAnchorId == anchorId && pop.DwellingId <= 0)
                    {
                        pop.DwellingId = dwellingId;

                        float ox = UnityEngine.Random.Range(-1.5f, 1.5f);

                        float oy = UnityEngine.Random.Range(-1.5f, 1.5f);

                        pop.TargetPositionX = dwelling.WorldPositionX + ox;

                        pop.TargetPositionY = dwelling.WorldPositionY + oy;

                        pop.ActivityState = PopActivityState.Moving;

                        pops[p] = pop;

                        dwelling.OccupantCount++;

                        assignedAny = true;

                        if (dwelling.OccupantCount >= dwelling.Capacity)
                        {
                            break;
                        }
                    }
                }
            }

            dwellings[dwellingIndex] = dwelling;

            return assignedAny;
        }


        private void UpdateHouseholdPopsDwelling(int householdId, int dwellingId, float homeX, float homeY)
        {
            if (householdId <= 0 || PopulationManager.Instance == null || !PopulationManager.Instance.IsInitialized)
            {
                return;
            }

            NativeArray<PopData> pops = PopulationManager.Instance.ActivePops;

            int count = PopulationManager.Instance.TotalPopulationCount;

            for (int i = 0; i < count; i++)
            {
                PopData p = pops[i];

                if (p.IsAlive && p.HouseholdId == householdId)
                {
                    p.DwellingId = dwellingId;

                    float ox = UnityEngine.Random.Range(-1.5f, 1.5f);

                    float oy = UnityEngine.Random.Range(-1.5f, 1.5f);

                    p.TargetPositionX = homeX + ox;

                    p.TargetPositionY = homeY + oy;

                    p.ActivityState = PopActivityState.Moving;

                    pops[i] = p;
                }
            }
        }


        public int FormHouseholdForCouple(ref PopData male, ref PopData female)
        {
            int assignedDwellingId = FindVacantDwelling(female.HomeAnchorId);

            int hId = nextHouseholdId++;

            HouseholdData household = HouseholdData.Create(
                hId,
                female.PolityId,
                female.HomeAnchorId,
                assignedDwellingId,
                male.PopId,
                female.PopId);

            households.Add(household);

            male.HouseholdId = hId;

            male.DwellingId = assignedDwellingId;

            female.HouseholdId = hId;

            female.DwellingId = assignedDwellingId;

            if (assignedDwellingId > 0)
            {
                IncrementDwellingOccupancy(assignedDwellingId, 2);

                if (TryGetDwellingPosition(assignedDwellingId, out float2 pos))
                {
                    male.TargetPositionX = pos.x + 0.8f;

                    male.TargetPositionY = pos.y + 0.8f;

                    male.ActivityState = PopActivityState.Moving;

                    female.TargetPositionX = pos.x - 0.8f;

                    female.TargetPositionY = pos.y - 0.8f;

                    female.ActivityState = PopActivityState.Moving;
                }
            }

            return hId;
        }


        public void AssignNewbornToHousehold(ref PopData mother, ref PopData newborn)
        {
            newborn.HouseholdId = mother.HouseholdId;

            newborn.DwellingId = mother.DwellingId;

            if (mother.HouseholdId > 0)
            {
                for (int i = 0; i < households.Length; i++)
                {
                    if (households[i].HouseholdId == mother.HouseholdId)
                    {
                        HouseholdData hh = households[i];

                        hh.ChildrenCount++;

                        households[i] = hh;

                        break;
                    }
                }
            }

            if (mother.DwellingId > 0)
            {
                IncrementDwellingOccupancy(mother.DwellingId, 1);
            }
        }


        public int FindVacantDwelling(ushort anchorId)
        {
            for (int i = 0; i < dwellings.Length; i++)
            {
                DwellingData d = dwellings[i];

                if (d.HomeAnchorId == anchorId && d.IsActive && d.OccupantCount < d.Capacity)
                {
                    return d.DwellingId;
                }
            }

            return 0;
        }


        public bool HasHousingCapacity(int dwellingId)
        {
            if (dwellingId <= 0) return false;

            for (int i = 0; i < dwellings.Length; i++)
            {
                if (dwellings[i].DwellingId == dwellingId)
                {
                    return dwellings[i].OccupantCount < dwellings[i].Capacity;
                }
            }

            return false;
        }


        private void IncrementDwellingOccupancy(int dwellingId, byte count)
        {
            for (int i = 0; i < dwellings.Length; i++)
            {
                if (dwellings[i].DwellingId == dwellingId)
                {
                    DwellingData d = dwellings[i];

                    d.OccupantCount = (byte)Mathf.Min(d.Capacity, d.OccupantCount + count);

                    dwellings[i] = d;

                    return;
                }
            }
        }


        public bool TryGetDwellingPosition(int dwellingId, out float2 position)
        {
            for (int i = 0; i < dwellings.Length; i++)
            {
                if (dwellings[i].DwellingId == dwellingId)
                {
                    position = new float2(dwellings[i].WorldPositionX, dwellings[i].WorldPositionY);

                    return true;
                }
            }

            position = float2.zero;

            return false;
        }


        public void GetHousingStats(ushort anchorId, out int totalDwellings, out int totalBeds, out int occupiedBeds)
        {
            totalDwellings = 0;

            totalBeds = 0;

            occupiedBeds = 0;

            if (!dwellings.IsCreated) return;

            for (int i = 0; i < dwellings.Length; i++)
            {
                DwellingData d = dwellings[i];

                if (d.HomeAnchorId == anchorId && d.IsActive)
                {
                    totalDwellings++;

                    totalBeds += d.Capacity;

                    occupiedBeds += d.OccupantCount;
                }
            }
        }


        private void OnDrawGizmos()
        {
            if (!drawDwellingGizmos || !dwellings.IsCreated || dwellings.Length == 0)
            {
                return;
            }

            for (int i = 0; i < dwellings.Length; i++)
            {
                DwellingData d = dwellings[i];

                if (!d.IsActive) continue;

                Vector3 pos = new Vector3(d.WorldPositionX, d.WorldPositionY, -2.1f);

                Gizmos.color = new Color(0.45f, 0.32f, 0.18f);

                Gizmos.DrawCube(pos, new Vector3(2.6f, 2.6f, 1.4f));

                Gizmos.color = new Color(0.9f, 0.6f, 0.2f, 0.25f);

                Gizmos.DrawWireSphere(pos, 3.2f);
            }
        }
    }
}