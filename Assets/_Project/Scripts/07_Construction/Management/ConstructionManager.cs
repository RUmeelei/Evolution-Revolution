using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using ER.World.Grid;
using ER.World.Polity;
using ER.World.Housing;
using ER.World.Inventory;
using ER.World.Chunks;

namespace ER.World.Construction
{
    public class ConstructionManager : MonoBehaviour
    {
        public static ConstructionManager Instance { get; private set; }


        [Header("System References")]
        [SerializeField] private PolityRegionManager polityManager;

        [SerializeField] private WorldGridManager gridManager;

        [SerializeField] private HouseholdManager householdManager;

        [SerializeField] private InventoryManager inventoryManager;

        [SerializeField] private ChunkManager chunkManager;


        [Header("Debug Visualizer")]
        [SerializeField] private bool drawBlueprintGizmos = true;


        private NativeList<BuildingBlueprintData> blueprints;

        private int nextBlueprintId = 1;

        private bool isInitialized = false;


        public bool IsInitialized => isInitialized;

        public NativeList<BuildingBlueprintData> RawBlueprints => blueprints;


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

            if (householdManager == null) householdManager = HouseholdManager.Instance;

            if (inventoryManager == null) inventoryManager = InventoryManager.Instance;

            if (chunkManager == null) chunkManager = ChunkManager.Instance;

            if (polityManager != null)
            {
                polityManager.EnsureInitialized();
            }

            isInitialized = true;
        }


        private void InitializeStorage()
        {
            if (!blueprints.IsCreated)
            {
                blueprints = new NativeList<BuildingBlueprintData>(512, Allocator.Persistent);
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
            if (blueprints.IsCreated)
            {
                blueprints.Dispose();
            }

            isInitialized = false;
        }


        public int PlaceBlueprint(BuildingType type, float worldX, float worldY, ushort polityId, ushort anchorId)
        {
            if (!isInitialized)
            {
                return -1;
            }

            int id = nextBlueprintId++;

            BuildingBlueprintData bp = BuildingBlueprintData.Create(id, polityId, anchorId, type, worldX, worldY);

            blueprints.Add(bp);

            Debug.Log($"[ConstructionManager] Placed blueprint {type} (ID: {id}) at ({worldX:F1}, {worldY:F1}).");

            return id;
        }


        public bool TryGetBlueprint(int bpId, out BuildingBlueprintData bp, out int arrayIndex)
        {
            for (int i = 0; i < blueprints.Length; i++)
            {
                if (blueprints[i].BlueprintId == bpId && blueprints[i].IsActive)
                {
                    bp = blueprints[i];

                    arrayIndex = i;

                    return true;
                }
            }

            bp = default;

            arrayIndex = -1;

            return false;
        }


        public bool DeliverMaterialToBlueprint(int bpId, ushort itemTypeId, uint amount)
        {
            if (!TryGetBlueprint(bpId, out BuildingBlueprintData bp, out int idx))
            {
                return false;
            }

            if (itemTypeId == 1)
            {
                bp.DeliveredWood = Math.Min(bp.RequiredWood, bp.DeliveredWood + amount);
            }
            else if (itemTypeId == 2)
            {
                bp.DeliveredStone = Math.Min(bp.RequiredStone, bp.DeliveredStone + amount);
            }

            if (bp.HasAllMaterials)
            {
                bp.State = BlueprintState.UnderConstruction;
            }

            blueprints[idx] = bp;

            return true;
        }


        public bool ApplyConstructionLabor(int bpId, float laborAmount)
        {
            if (!TryGetBlueprint(bpId, out BuildingBlueprintData bp, out int idx))
            {
                return false;
            }

            if (!bp.HasAllMaterials)
            {
                return false;
            }

            bp.CurrentLaborProgress += laborAmount;

            if (bp.CurrentLaborProgress >= bp.RequiredLabor)
            {
                CompleteBuilding(bp);

                bp.State = BlueprintState.Completed;

                bp.IsActive = false;

                blueprints[idx] = bp;

                return true;
            }

            blueprints[idx] = bp;

            return false;
        }


        private void CompleteBuilding(BuildingBlueprintData bp)
        {
            Debug.Log($"[ConstructionManager] Construction COMPLETED for {bp.TargetBuildingType} at ({bp.WorldPositionX:F1}, {bp.WorldPositionY:F1})!");

            switch (bp.TargetBuildingType)
            {
                case BuildingType.DugoutShelter:
                    if (householdManager != null && householdManager.IsInitialized)
                    {
                        householdManager.RegisterDwelling(
                            bp.PolityId,
                            bp.HomeAnchorId,
                            DwellingType.DugoutShelter,
                            bp.WorldPositionX,
                            bp.WorldPositionY,
                            capacity: 5,
                            thermalBonus: 18.0f);

                        if (Population.PopulationManager.Instance != null)
                        {
                            Population.PopulationManager.Instance.UpdateHearthPositionsCache();
                        }
                    }
                    break;

                case BuildingType.Storehouse:
                    if (polityManager != null && polityManager.TryGetAnchor(bp.HomeAnchorId, out PolityAnchorData anchor))
                    {
                        if (inventoryManager != null)
                        {
                            inventoryManager.CreateInventory(15000.0f, 25000.0f, 80);
                        }
                    }
                    break;
            }
        }


        private void OnDrawGizmos()
        {
            if (!drawBlueprintGizmos || !blueprints.IsCreated || blueprints.Length == 0)
            {
                return;
            }

            for (int i = 0; i < blueprints.Length; i++)
            {
                BuildingBlueprintData bp = blueprints[i];

                if (!bp.IsActive) continue;

                Vector3 pos = new Vector3(bp.WorldPositionX, bp.WorldPositionY, -2.15f);

                if (bp.State == BlueprintState.AwaitingMaterials)
                {
                    Gizmos.color = new Color(1.0f, 0.75f, 0.2f, 0.75f);

                    Gizmos.DrawWireCube(pos, new Vector3(2.8f, 2.8f, 1.6f));
                }
                else if (bp.State == BlueprintState.UnderConstruction)
                {
                    Gizmos.color = new Color(0.2f, 0.9f, 0.3f, 0.85f);

                    Gizmos.DrawWireCube(pos, new Vector3(2.8f, 2.8f, 1.6f));

                    float fill = bp.LaborFillRatio;

                    Gizmos.DrawCube(pos, new Vector3(2.6f * fill, 2.6f * fill, 0.4f));
                }
            }
        }
    }
}