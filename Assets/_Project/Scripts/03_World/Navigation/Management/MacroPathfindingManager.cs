using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using ER.World.Grid;
using ER.World.Regions;

namespace ER.World.Navigation
{
    public class MacroPathfindingManager : MonoBehaviour
    {
        public static MacroPathfindingManager Instance { get; private set; }


        [Header("System References")]
        [SerializeField] private WorldGridManager gridManager;

        [SerializeField] private GeoRegionManager regionManager;


        [Header("Pass Detection Calibration")]
        [SerializeField] private int borderSamplingStep = 2;

        [SerializeField] private float elevationSlopePenalty = 1.6f;


        [Header("Debug Visualizer")]
        [SerializeField] private bool drawMacroGraphGizmos = true;

        [SerializeField] private bool drawTestedPathGizmo = false;


        private readonly MacroNavGraph graph = new MacroNavGraph();

        private MacroPathResult lastDebugPath;

        private int nextNodeId = 1;

        private int nextEdgeId = 1;

        private bool isInitialized = false;


        public bool IsInitialized => isInitialized;

        public MacroNavGraph Graph => graph;


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
            if (isInitialized)
            {
                return;
            }

            if (gridManager == null) gridManager = WorldGridManager.Instance;

            if (regionManager == null) regionManager = GeoRegionManager.Instance;

            if (regionManager != null)
            {
                regionManager.EnsureInitialized();
            }

            if (gridManager == null || !gridManager.IsInitialized || regionManager == null || !regionManager.IsSegmented)
            {
                return;
            }

            BakeMacroNavGraph();

            isInitialized = true;
        }


        public void BakeMacroNavGraph()
        {
            graph.Clear();

            nextNodeId = 1;

            nextEdgeId = 1;

            var regions = regionManager.Regions;

            float tileSize = gridManager.MapConfig.TileWorldSize;

            var tiles = gridManager.RawTiles;

            WorldMapConfig config = gridManager.MapConfig;

            Dictionary<ushort, int> centerNodeMap = new Dictionary<ushort, int>();

            for (int i = 0; i < regions.Count; i++)
            {
                GeoRegionData reg = regions[i];

                if (reg.IsOceanRegion || reg.RegionId == 0) continue;

                int tIdx = config.CoordsToIndex(reg.CenterTileX, reg.CenterTileY);

                float elev = tiles[tIdx].Elevation;

                float wx = (reg.CenterTileX + 0.5f) * tileSize;

                float wy = (reg.CenterTileY + 0.5f) * tileSize;

                int cNodeId = nextNodeId++;

                MacroNavNode centerNode = MacroNavNode.Create(cNodeId, reg.RegionId, 0, MacroNodeType.RegionCenter, wx, wy, elev);

                graph.AddNode(centerNode);

                centerNodeMap[reg.RegionId] = cNodeId;
            }

            Dictionary<long, BorderPassCandidate> borderPasses = DetectMountainPassPortals(regions, config, tiles, tileSize);

            foreach (var kvp in borderPasses)
            {
                BorderPassCandidate cand = kvp.Value;

                int passNodeId = nextNodeId++;

                MacroNavNode passNode = MacroNavNode.Create(
                    passNodeId,
                    cand.RegionA,
                    cand.RegionB,
                    MacroNodeType.MountainPass,
                    cand.WorldPos.x,
                    cand.WorldPos.y,
                    cand.Elevation);

                graph.AddNode(passNode);

                if (centerNodeMap.TryGetValue(cand.RegionA, out int centerA))
                {
                    ConnectBidirectional(centerA, passNodeId);
                }

                if (centerNodeMap.TryGetValue(cand.RegionB, out int centerB))
                {
                    ConnectBidirectional(centerB, passNodeId);
                }
            }

            Debug.Log($"[MacroPathfindingManager] Successfully baked MacroNavGraph with {graph.NodeCount} nodes across {regions.Count} valleys.");
        }


        private Dictionary<long, BorderPassCandidate> DetectMountainPassPortals(
            IReadOnlyList<GeoRegionData> regions,
            WorldMapConfig config,
            NativeArray<TileData> tiles,
            float tileSize)
        {
            Dictionary<long, BorderPassCandidate> passes = new Dictionary<long, BorderPassCandidate>();

            int width = config.MapWidth;

            int height = config.MapHeight;

            for (int y = 5; y < height - 5; y += borderSamplingStep)
            {
                for (int x = 5; x < width - 5; x += borderSamplingStep)
                {
                    int idx = y * width + x;

                    TileData tCurr = tiles[idx];

                    if (tCurr.Terrain == TerrainType.WaterDeep || tCurr.GeoRegionId == 0) continue;

                    CheckBorderCandidate(x + 1, y, tCurr, config, tiles, tileSize, passes);

                    CheckBorderCandidate(x, y + 1, tCurr, config, tiles, tileSize, passes);
                }
            }

            return passes;
        }


        private void CheckBorderCandidate(
            int nx,
            int ny,
            TileData tCurr,
            WorldMapConfig config,
            NativeArray<TileData> tiles,
            float tileSize,
            Dictionary<long, BorderPassCandidate> passes)
        {
            if (!config.IsCoordsValid(nx, ny)) return;

            int nIdx = config.CoordsToIndex(nx, ny);

            TileData tNext = tiles[nIdx];

            if (tNext.Terrain == TerrainType.WaterDeep || tNext.GeoRegionId == 0) return;

            if (tCurr.GeoRegionId == tNext.GeoRegionId) return;

            if (tCurr.Terrain == TerrainType.Mountains && tNext.Terrain == TerrainType.Mountains)
            {
                if (tCurr.Elevation > 210 && tNext.Elevation > 210) return;
            }

            ushort rA = Math.Min(tCurr.GeoRegionId, tNext.GeoRegionId);

            ushort rB = Math.Max(tCurr.GeoRegionId, tNext.GeoRegionId);

            long pairKey = ((long)rA << 32) | rB;

            float avgElev = (tCurr.Elevation + tNext.Elevation) * 0.5f;

            float wx = (nx + 0.5f) * tileSize;

            float wy = (ny + 0.5f) * tileSize;

            if (!passes.TryGetValue(pairKey, out BorderPassCandidate existing) || avgElev < existing.Elevation)
            {
                passes[pairKey] = new BorderPassCandidate
                {
                    RegionA = rA,
                    RegionB = rB,
                    WorldPos = new Vector2(wx, wy),
                    Elevation = avgElev
                };
            }
        }


        private void ConnectBidirectional(int nodeAId, int nodeBId)
        {
            MacroNavNode a = graph.Nodes[nodeAId];

            MacroNavNode b = graph.Nodes[nodeBId];

            float dist = math.distance(new float2(a.WorldPositionX, a.WorldPositionY), new float2(b.WorldPositionX, b.WorldPositionY));

            float elevDiff = Mathf.Abs(a.Elevation - b.Elevation);

            float weight = dist + (elevDiff * elevationSlopePenalty * 15.0f);

            int e1 = nextEdgeId++;

            int e2 = nextEdgeId++;

            graph.AddEdge(MacroNavEdge.Create(e1, nodeAId, nodeBId, dist, weight));

            graph.AddEdge(MacroNavEdge.Create(e2, nodeBId, nodeAId, dist, weight));
        }


        public MacroPathResult FindMacroPathBetweenRegions(ushort startRegionId, ushort targetRegionId)
        {
            if (!isInitialized)
            {
                return default;
            }

            if (!graph.TryGetRegionCenterNodeId(startRegionId, out int startNode) ||
                !graph.TryGetRegionCenterNodeId(targetRegionId, out int targetNode))
            {
                return default;
            }

            MacroPathResult path = graph.FindPath(startNode, targetNode);

            lastDebugPath = path;

            return path;
        }


        public MacroPathResult FindMacroPathBetweenPositions(Vector2 startPos, Vector2 targetPos)
        {
            if (!isInitialized || gridManager == null)
            {
                return default;
            }

            WorldMapConfig config = gridManager.MapConfig;

            int sx = Mathf.FloorToInt(startPos.x / config.TileWorldSize);

            int sy = Mathf.FloorToInt(startPos.y / config.TileWorldSize);

            int tx = Mathf.FloorToInt(targetPos.x / config.TileWorldSize);

            int ty = Mathf.FloorToInt(targetPos.y / config.TileWorldSize);

            if (!config.IsCoordsValid(sx, sy) || !config.IsCoordsValid(tx, ty))
            {
                return default;
            }

            ushort regStart = gridManager.RawTiles[config.CoordsToIndex(sx, sy)].GeoRegionId;

            ushort regTarget = gridManager.RawTiles[config.CoordsToIndex(tx, ty)].GeoRegionId;

            return FindMacroPathBetweenRegions(regStart, regTarget);
        }


        private void OnDrawGizmos()
        {
            if (!drawMacroGraphGizmos || !isInitialized || graph.NodeCount == 0)
            {
                return;
            }

            foreach (var kvp in graph.Nodes)
            {
                MacroNavNode n = kvp.Value;

                Vector3 pos = new Vector3(n.WorldPositionX, n.WorldPositionY, -2.8f);

                if (n.NodeType == MacroNodeType.RegionCenter)
                {
                    Gizmos.color = new Color(0.2f, 0.8f, 1.0f, 0.7f);

                    Gizmos.DrawSphere(pos, 8.0f);
                }
                else
                {
                    Gizmos.color = new Color(1.0f, 0.8f, 0.2f, 0.85f);

                    Gizmos.DrawCube(pos, new Vector3(6.0f, 6.0f, 6.0f));
                }
            }

            if (drawTestedPathGizmo && lastDebugPath.Success && lastDebugPath.Waypoints != null)
            {
                Gizmos.color = Color.magenta;

                for (int i = 0; i < lastDebugPath.Waypoints.Length - 1; i++)
                {
                    Vector3 p1 = new Vector3(lastDebugPath.Waypoints[i].x, lastDebugPath.Waypoints[i].y, -3.2f);

                    Vector3 p2 = new Vector3(lastDebugPath.Waypoints[i + 1].x, lastDebugPath.Waypoints[i + 1].y, -3.2f);

                    Gizmos.DrawLine(p1, p2);

                    Gizmos.DrawSphere(p1, 5.0f);
                }
            }
        }


        private struct BorderPassCandidate
        {
            public ushort RegionA;

            public ushort RegionB;

            public Vector2 WorldPos;

            public float Elevation;
        }
    }
}