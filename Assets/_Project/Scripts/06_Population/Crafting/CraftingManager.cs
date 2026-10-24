using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using ER.World.Grid;
using ER.World.Polity;
using ER.World.Inventory;
using ER.Simulation.Time;

namespace ER.World.Crafting
{
    public class CraftingManager : MonoBehaviour
    {
        public static CraftingManager Instance { get; private set; }


        [Header("Starting Crafting Infrastructure")]
        [SerializeField] private int tanningRacksPerCamp = 3;

        [SerializeField] private int knappingStationsPerCamp = 2;

        [SerializeField] private float stationPlacementRadius = 15.0f;


        [Header("System References")]
        [SerializeField] private SimulationClockManager clockManager;

        [SerializeField] private PolityRegionManager polityManager;

        [SerializeField] private WorldGridManager gridManager;

        [SerializeField] private InventoryManager inventoryManager;


        [Header("Debug Visualizer")]
        [SerializeField] private bool drawStationGizmos = true;


        private NativeList<CraftingStationData> stations;

        private readonly List<CraftingRecipeData> recipes = new List<CraftingRecipeData>();

        private int nextStationId = 1;

        private bool isInitialized = false;


        public bool IsInitialized => isInitialized;

        public NativeList<CraftingStationData> RawStations => stations;


        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            InitializeStorage();

            RegisterDefaultRecipes();
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

            if (gridManager == null) gridManager = WorldGridManager.Instance;

            if (inventoryManager == null) inventoryManager = InventoryManager.Instance;

            if (polityManager != null)
            {
                polityManager.EnsureInitialized();
            }

            if (polityManager == null || !polityManager.IsInitialized || polityManager.Anchors.Count == 0)
            {
                return;
            }

            SpawnSettlementStations();

            if (clockManager != null)
            {
                clockManager.OnDayPassed += HandleDailyProductionCycle;
            }

            isInitialized = true;
        }


        private void InitializeStorage()
        {
            if (!stations.IsCreated)
            {
                stations = new NativeList<CraftingStationData>(256, Allocator.Persistent);
            }
        }


        private void RegisterDefaultRecipes()
        {
            recipes.Clear();

            recipes.Add(CraftingRecipeData.Create(1, CraftingStationType.TanningRack, 5, 2, 6, 2, 10.0f));

            recipes.Add(CraftingRecipeData.Create(2, CraftingStationType.TanningRack, 6, 3, 7, 1, 15.0f));
        }


        private void OnDestroy()
        {
            if (clockManager != null)
            {
                clockManager.OnDayPassed -= HandleDailyProductionCycle;
            }

            DisposeStorage();
        }


        private void OnApplicationQuit()
        {
            DisposeStorage();
        }


        private void DisposeStorage()
        {
            if (stations.IsCreated)
            {
                stations.Dispose();
            }

            isInitialized = false;
        }


        private void SpawnSettlementStations()
        {
            stations.Clear();

            var anchors = polityManager.Anchors;

            float tileSize = gridManager.MapConfig.TileWorldSize;

            for (int a = 0; a < anchors.Count; a++)
            {
                PolityAnchorData anchor = anchors[a];

                float centerX = (anchor.TileX + 0.5f) * tileSize;

                float centerY = (anchor.TileY + 0.5f) * tileSize;

                for (int t = 0; t < tanningRacksPerCamp; t++)
                {
                    float angle = (t / (float)tanningRacksPerCamp) * Mathf.PI + 0.35f;

                    float sx = centerX + Mathf.Cos(angle) * stationPlacementRadius;

                    float sy = centerY + Mathf.Sin(angle) * stationPlacementRadius;

                    RegisterStation(anchor.OwnerPolityId, anchor.AnchorId, CraftingStationType.TanningRack, sx, sy);
                }

                for (int k = 0; k < knappingStationsPerCamp; k++)
                {
                    float angle = (k / (float)knappingStationsPerCamp) * Mathf.PI + Mathf.PI + 0.35f;

                    float sx = centerX + Mathf.Cos(angle) * (stationPlacementRadius * 0.8f);

                    float sy = centerY + Mathf.Sin(angle) * (stationPlacementRadius * 0.8f);

                    RegisterStation(anchor.OwnerPolityId, anchor.AnchorId, CraftingStationType.FlintKnappingStation, sx, sy);
                }
            }

            Debug.Log($"[CraftingManager] Successfully established {stations.Length} crafting stations across settlements.");
        }


        public int RegisterStation(ushort polityId, ushort anchorId, CraftingStationType type, float x, float y)
        {
            int id = nextStationId++;

            CraftingStationData station = CraftingStationData.Create(id, polityId, anchorId, type, x, y);

            stations.Add(station);

            return id;
        }


        private void HandleDailyProductionCycle(WorldTimeData timeData)
        {
            if (!isInitialized || inventoryManager == null || polityManager == null)
            {
                return;
            }

            var anchors = polityManager.Anchors;

            for (int a = 0; a < anchors.Count; a++)
            {
                int storageId = anchors[a].StorageInventoryId;

                if (storageId <= 0) continue;

                for (int r = 0; r < recipes.Count; r++)
                {
                    ProcessRecipeBatch(storageId, recipes[r], anchors[a].AnchorId);
                }
            }
        }


        private void ProcessRecipeBatch(int storageId, CraftingRecipeData recipe, ushort anchorId)
        {
            if (!HasStationAvailable(anchorId, recipe.RequiredStation))
            {
                return;
            }

            uint countA = inventoryManager.GetItemCount(storageId, recipe.InputItemA);

            if (countA < recipe.InputCountA)
            {
                return;
            }

            if (recipe.InputItemB > 0)
            {
                uint countB = inventoryManager.GetItemCount(storageId, recipe.InputItemB);

                if (countB < recipe.InputCountB)
                {
                    return;
                }
            }

            inventoryManager.ExecuteTransfer(storageId, 0, recipe.InputItemA, recipe.InputCountA, out uint removedA);

            if (removedA >= recipe.InputCountA)
            {
                if (recipe.InputItemB > 0)
                {
                    inventoryManager.ExecuteTransfer(storageId, 0, recipe.InputItemB, recipe.InputCountB, out _);
                }

                inventoryManager.TryAddItem(storageId, recipe.OutputItem, recipe.OutputCount);

                if (recipe.OutputItem == 7)
                {
                    Debug.Log($"[CraftingManager] Produced Warm Fur Parka in Settlement {anchorId}!");
                }
            }
        }


        public bool HasStationAvailable(ushort anchorId, CraftingStationType type)
        {
            for (int i = 0; i < stations.Length; i++)
            {
                if (stations[i].HomeAnchorId == anchorId && stations[i].StationType == type && stations[i].IsActive)
                {
                    return true;
                }
            }

            return false;
        }


        public bool TryGetNearestStationPosition(float2 popPos, CraftingStationType type, out float2 stationPos)
        {
            float minDist = float.MaxValue;

            stationPos = float2.zero;

            bool found = false;

            for (int i = 0; i < stations.Length; i++)
            {
                if (stations[i].StationType == type && stations[i].IsActive)
                {
                    float dist = math.distance(popPos, new float2(stations[i].WorldPositionX, stations[i].WorldPositionY));

                    if (dist < minDist)
                    {
                        minDist = dist;

                        stationPos = new float2(stations[i].WorldPositionX, stations[i].WorldPositionY);

                        found = true;
                    }
                }
            }

            return found;
        }


        private void OnDrawGizmos()
        {
            if (!drawStationGizmos || !stations.IsCreated || stations.Length == 0)
            {
                return;
            }

            for (int i = 0; i < stations.Length; i++)
            {
                CraftingStationData s = stations[i];

                if (!s.IsActive) continue;

                Vector3 pos = new Vector3(s.WorldPositionX, s.WorldPositionY, -2.2f);

                if (s.StationType == CraftingStationType.TanningRack)
                {
                    Gizmos.color = new Color(0.85f, 0.5f, 0.2f);

                    Gizmos.DrawCube(pos, new Vector3(1.8f, 0.4f, 1.2f));
                }
                else if (s.StationType == CraftingStationType.FlintKnappingStation)
                {
                    Gizmos.color = new Color(0.45f, 0.55f, 0.65f);

                    Gizmos.DrawSphere(pos, 0.75f);
                }
            }
        }
    }
}