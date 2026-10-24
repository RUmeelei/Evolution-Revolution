using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using ER.World.Grid;
using ER.World.Polity;
using ER.Simulation.Time;

namespace ER.World.Resources
{
    public class WorldResourceManager : MonoBehaviour
    {
        public static WorldResourceManager Instance { get; private set; }


        [Header("Spawn Configuration")]
        [SerializeField] private int nodesPerSettlement = 40;

        [SerializeField] private float minSpawnRadius = 8.0f;

        [SerializeField] private float maxSpawnRadius = 38.0f;


        [Header("References")]
        [SerializeField] private WorldGridManager gridManager;

        [SerializeField] private PolityRegionManager polityManager;

        [SerializeField] private SimulationClockManager clockManager;


        [Header("Debug Visualizer")]
        [SerializeField] private bool drawResourceGizmos = true;


        private NativeList<WorldResourceNodeData> resourceNodes;

        private int nextNodeId = 1;

        private bool isInitialized = false;


        public bool IsInitialized => isInitialized;

        public NativeList<WorldResourceNodeData> RawNodes => resourceNodes;


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

            if (gridManager == null) gridManager = WorldGridManager.Instance;

            if (polityManager == null) polityManager = PolityRegionManager.Instance;

            if (clockManager == null) clockManager = SimulationClockManager.Instance;

            if (polityManager != null)
            {
                polityManager.EnsureInitialized();
            }

            if (polityManager == null || !polityManager.IsInitialized || polityManager.Anchors.Count == 0)
            {
                return;
            }

            SpawnInitialResources();

            if (clockManager != null)
            {
                clockManager.OnDayPassed += HandleDayPassed;
            }

            isInitialized = true;
        }


        private void InitializeStorage()
        {
            if (!resourceNodes.IsCreated)
            {
                resourceNodes = new NativeList<WorldResourceNodeData>(2048, Allocator.Persistent);
            }
        }


        private void OnDestroy()
        {
            if (clockManager != null)
            {
                clockManager.OnDayPassed -= HandleDayPassed;
            }

            DisposeStorage();
        }


        private void OnApplicationQuit()
        {
            DisposeStorage();
        }


        private void DisposeStorage()
        {
            if (resourceNodes.IsCreated)
            {
                resourceNodes.Dispose();
            }

            isInitialized = false;
        }


        private void SpawnInitialResources()
        {
            var anchors = polityManager.Anchors;

            float tileSize = gridManager.MapConfig.TileWorldSize;

            for (int a = 0; a < anchors.Count; a++)
            {
                PolityAnchorData anchor = anchors[a];

                float centerX = (anchor.TileX + 0.5f) * tileSize;

                float centerY = (anchor.TileY + 0.5f) * tileSize;

                for (int i = 0; i < nodesPerSettlement; i++)
                {
                    Vector2 offset = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(minSpawnRadius, maxSpawnRadius);

                    float posX = centerX + offset.x;

                    float posY = centerY + offset.y;

                    if (!ValidatePlacement(posX, posY))
                    {
                        continue;
                    }

                    int roll = UnityEngine.Random.Range(0, 100);

                    if (roll < 45)
                    {
                        SpawnNode(ResourceNodeType.FallenTree, 1, 0, posX, posY, (uint)UnityEngine.Random.Range(20, 50), 1.0f);
                    }
                    else if (roll < 75)
                    {
                        SpawnNode(ResourceNodeType.FlintDeposit, 2, 0, posX, posY, (uint)UnityEngine.Random.Range(15, 35), 1.5f);
                    }
                    else
                    {
                        SpawnNode(ResourceNodeType.BerryBush, 3, 0, posX, posY, (uint)UnityEngine.Random.Range(40, 80), 0.5f);
                    }
                }
            }

            Debug.Log($"[WorldResourceManager] Successfully spawned {resourceNodes.Length} resource nodes in the wild.");
        }


        private bool ValidatePlacement(float worldX, float worldY)
        {
            if (gridManager == null || !gridManager.IsInitialized) return false;

            WorldMapConfig config = gridManager.MapConfig;

            int tx = Mathf.FloorToInt(worldX / config.TileWorldSize);

            int ty = Mathf.FloorToInt(worldY / config.TileWorldSize);

            if (!config.IsCoordsValid(tx, ty)) return false;

            TileData tile = gridManager.GetTile(tx, ty);

            return tile.Terrain != TerrainType.WaterDeep && tile.Terrain != TerrainType.Mountains;
        }


        public int SpawnNode(ResourceNodeType type, ushort itemId, ushort toolId, float x, float y, uint yieldAmount, float hardness)
        {
            int id = nextNodeId++;

            WorldResourceNodeData node = WorldResourceNodeData.Create(id, type, itemId, toolId, x, y, yieldAmount, hardness);

            resourceNodes.Add(node);

            return id;
        }


        public bool TryGetNode(int nodeId, out WorldResourceNodeData node, out int arrayIndex)
        {
            for (int i = 0; i < resourceNodes.Length; i++)
            {
                if (resourceNodes[i].NodeId == nodeId && !resourceNodes[i].IsDepleted)
                {
                    node = resourceNodes[i];

                    arrayIndex = i;

                    return true;
                }
            }

            node = default;

            arrayIndex = -1;

            return false;
        }


        public void SetNode(int index, WorldResourceNodeData node)
        {
            if (index >= 0 && index < resourceNodes.Length)
            {
                resourceNodes[index] = node;
            }
        }


        private void HandleDayPassed(WorldTimeData timeData)
        {
            if (!resourceNodes.IsCreated || timeData.CurrentSeason == Season.Winter)
            {
                return;
            }

            for (int i = 0; i < resourceNodes.Length; i++)
            {
                WorldResourceNodeData node = resourceNodes[i];

                if (node.NodeType == ResourceNodeType.BerryBush)
                {
                    if (node.IsDepleted)
                    {
                        node.IsDepleted = false;

                        node.RemainingYield = 10;
                    }
                    else if (node.RemainingYield < 80)
                    {
                        node.RemainingYield = System.Math.Min(80u, node.RemainingYield + 4u);
                    }

                    resourceNodes[i] = node;
                }
            }
        }


        private void OnDrawGizmos()
        {
            if (!drawResourceGizmos || !resourceNodes.IsCreated || resourceNodes.Length == 0)
            {
                return;
            }

            for (int i = 0; i < resourceNodes.Length; i++)
            {
                WorldResourceNodeData node = resourceNodes[i];

                if (node.IsDepleted) continue;

                Vector3 pos = new Vector3(node.WorldPositionX, node.WorldPositionY, -2.2f);

                switch (node.NodeType)
                {
                    case ResourceNodeType.FallenTree:
                        Gizmos.color = new Color(0.6f, 0.4f, 0.2f);
                        Gizmos.DrawCube(pos, new Vector3(1.4f, 0.6f, 0.6f));
                        break;

                    case ResourceNodeType.FlintDeposit:
                        Gizmos.color = new Color(0.5f, 0.55f, 0.6f);
                        Gizmos.DrawSphere(pos, 0.7f);
                        break;

                    case ResourceNodeType.BerryBush:
                        Gizmos.color = new Color(0.85f, 0.15f, 0.35f);
                        Gizmos.DrawSphere(pos, 0.6f);
                        break;
                }
            }
        }
    }
}