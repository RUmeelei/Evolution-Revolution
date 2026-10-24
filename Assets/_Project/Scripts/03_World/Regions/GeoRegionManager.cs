using System.Collections.Generic;
using UnityEngine;
using ER.World.Grid;
using ER.World.Chunks;
using ER.World.Visual;
using ER.World.Polity;

namespace ER.World.Regions
{
    public class GeoRegionManager : MonoBehaviour
    {
        public static GeoRegionManager Instance { get; private set; }


        [Header("Configuration")]
        [SerializeField] private WorldGridManager gridManager;

        [SerializeField] private ChunkManager chunkManager;

        [SerializeField] private TilePaletteConfig paletteConfig;

        [SerializeField] private int targetRegionCount = 210;


        [Header("Debug Visualizer")]
        [SerializeField] private bool showRegionBoundariesView = false;

        [SerializeField] private bool drawAdjacencyGraphGizmos = true;


        private GeoRegionData[] regions;

        private readonly Dictionary<ushort, GeoRegionData> regionLookup = new Dictionary<ushort, GeoRegionData>();

        private MacroAdjacencyGraph adjacencyGraph;

        private bool isSegmented = false;


        public bool IsSegmented => isSegmented;

        public IReadOnlyList<GeoRegionData> Regions => regions;

        public MacroAdjacencyGraph AdjacencyGraph => adjacencyGraph;

        public bool ShowRegionBoundariesView => showRegionBoundariesView;


        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }


        private void Start()
        {
            EnsureInitialized();
        }


        public void EnsureInitialized()
        {
            if (isSegmented)
            {
                return;
            }

            if (gridManager == null)
            {
                gridManager = WorldGridManager.Instance;
            }

            if (chunkManager == null)
            {
                chunkManager = ChunkManager.Instance;
            }

            if (gridManager != null && !gridManager.IsInitialized)
            {
                gridManager.InitializeGrid();
            }

            if (gridManager != null && gridManager.IsInitialized)
            {
                ExecuteSegmentation();
            }
        }


        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2))
            {
                ToggleRegionViewMode();
            }
        }


        public void ExecuteSegmentation()
        {
            if (isSegmented || gridManager == null || !gridManager.IsInitialized)
            {
                return;
            }

            regions = GeoRegionSegmentation.GenerateRegions(gridManager.RawTiles, gridManager.MapConfig, targetRegionCount, 30);

            regionLookup.Clear();

            for (int i = 0; i < regions.Length; i++)
            {
                regionLookup[regions[i].RegionId] = regions[i];
            }

            adjacencyGraph = GeoRegionAdjacencyBuilder.BuildAdjacencyGraph(gridManager.RawTiles, gridManager.MapConfig);

            isSegmented = true;
        }


        public bool TryGetRegion(ushort regionId, out GeoRegionData regionData)
        {
            return regionLookup.TryGetValue(regionId, out regionData);
        }


        public bool TryGetRegionAt(int tileX, int tileY, out GeoRegionData regionData)
        {
            if (gridManager != null && gridManager.TryGetTile(tileX, tileY, out TileData tile))
            {
                return TryGetRegion(tile.GeoRegionId, out regionData);
            }

            regionData = default;

            return false;
        }


        public void ToggleRegionViewMode()
        {
            showRegionBoundariesView = !showRegionBoundariesView;

            if (PolityRegionManager.Instance != null)
            {
                WorldMapDisplayMode targetMode = showRegionBoundariesView ? WorldMapDisplayMode.GeoRegions : WorldMapDisplayMode.Biomes;

                PolityRegionManager.Instance.SetDisplayMode(targetMode);

                return;
            }

            if (chunkManager != null && chunkManager.IsInitialized && paletteConfig != null)
            {
                WorldChunk[,] chunks = chunkManager.Chunks;

                int countX = chunks.GetLength(0);

                int countY = chunks.GetLength(1);

                WorldMapDisplayMode mode = showRegionBoundariesView ? WorldMapDisplayMode.GeoRegions : WorldMapDisplayMode.Biomes;

                for (int y = 0; y < countY; y++)
                {
                    for (int x = 0; x < countX; x++)
                    {
                        chunks[x, y].RefreshChunkColors(gridManager.RawTiles, gridManager.MapConfig, paletteConfig, mode);
                    }
                }
            }
        }


        private void OnDrawGizmosSelected()
        {
            if (!drawAdjacencyGraphGizmos || adjacencyGraph == null || !isSegmented || gridManager == null)
            {
                return;
            }

            float tileSize = gridManager.MapConfig.TileWorldSize;

            IReadOnlyList<GeoRegionEdge> edges = adjacencyGraph.Edges;

            for (int i = 0; i < edges.Count; i++)
            {
                GeoRegionEdge edge = edges[i];

                if (!regionLookup.TryGetValue(edge.RegionA, out GeoRegionData regA)) continue;

                if (!regionLookup.TryGetValue(edge.RegionB, out GeoRegionData regB)) continue;

                Vector3 posA = new Vector3(regA.CenterTileX * tileSize, regA.CenterTileY * tileSize, -1.0f);

                Vector3 posB = new Vector3(regB.CenterTileX * tileSize, regB.CenterTileY * tileSize, -1.0f);

                Vector3 passPos = new Vector3(edge.PassTileXA * tileSize, edge.PassTileYA * tileSize, -2.0f);

                Gizmos.color = edge.IsOceanConnection ? new Color(0.2f, 0.6f, 0.9f, 0.35f) : new Color(0.9f, 0.7f, 0.2f, 0.45f);

                Gizmos.DrawLine(posA, passPos);

                Gizmos.DrawLine(passPos, posB);

                Gizmos.color = Color.red;

                Gizmos.DrawWireSphere(passPos, tileSize * 1.5f);
            }
        }
    }
}