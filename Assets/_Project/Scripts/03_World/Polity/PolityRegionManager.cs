using System;
using System.Collections.Generic;
using UnityEngine;
using ER.World.Grid;
using ER.World.Chunks;
using ER.World.Regions;
using ER.World.Visual;
using ER.World.Inventory;
using ER.Core.Cameras;

namespace ER.World.Polity
{
    public class PolityRegionManager : MonoBehaviour
    {
        public static PolityRegionManager Instance { get; private set; }


        [Header("Configuration References")]
        [SerializeField] private WorldGridManager gridManager;

        [SerializeField] private ChunkManager chunkManager;

        [SerializeField] private GeoRegionManager regionManager;

        [SerializeField] private InventoryManager inventoryManager;

        [SerializeField] private TilePaletteConfig paletteConfig;


        [Header("Runtime View Mode")]
        [SerializeField] private WorldMapDisplayMode currentDisplayMode = WorldMapDisplayMode.Biomes;


        private readonly Dictionary<ushort, PolityData> polities = new Dictionary<ushort, PolityData>();

        private readonly List<PolityAnchorData> anchors = new List<PolityAnchorData>();

        private readonly Dictionary<ushort, PolityAnchorData> anchorLookup = new Dictionary<ushort, PolityAnchorData>();

        private ushort nextAnchorId = 1;

        private bool isInitialized = false;


        public bool IsInitialized => isInitialized;

        public IReadOnlyDictionary<ushort, PolityData> Polities => polities;

        public IReadOnlyList<PolityAnchorData> Anchors => anchors;

        public WorldMapDisplayMode CurrentDisplayMode => currentDisplayMode;


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

            if (chunkManager == null) chunkManager = ChunkManager.Instance;

            if (regionManager == null) regionManager = GeoRegionManager.Instance;

            if (inventoryManager == null) inventoryManager = InventoryManager.Instance;

            if (regionManager != null)
            {
                regionManager.EnsureInitialized();
            }

            if (inventoryManager != null && !inventoryManager.IsInitialized)
            {
                inventoryManager.InitializeStorage();
            }

            if (regionManager == null || !regionManager.IsSegmented)
            {
                return;
            }

            InitializePolitySystem();
        }


        private void Update()
        {
            HandleModeSwitchInput();
        }


        private void HandleModeSwitchInput()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                SetDisplayMode(WorldMapDisplayMode.Biomes);
            }
            else if (Input.GetKeyDown(KeyCode.F2))
            {
                SetDisplayMode(WorldMapDisplayMode.GeoRegions);
            }
            else if (Input.GetKeyDown(KeyCode.F4))
            {
                SetDisplayMode(WorldMapDisplayMode.PoliticalDeFacto);
            }
            else if (Input.GetKeyDown(KeyCode.F5))
            {
                SetDisplayMode(WorldMapDisplayMode.PoliticalDeJure);
            }
        }


        public void InitializePolitySystem()
        {
            if (isInitialized)
            {
                return;
            }

            if (regionManager == null || !regionManager.IsSegmented || regionManager.Regions == null || regionManager.Regions.Count == 0)
            {
                return;
            }

            RegisterDefaultPolities();

            EstablishInitialAnchors();

            if (anchors.Count == 0)
            {
                return;
            }

            RecalculateTerritorialControl();

            isInitialized = true;

            RefreshVisuals();

            FocusCameraOnPlayerSettlement();

            Debug.Log($"[PolityRegionManager] Successfully established {anchors.Count} settlements safely on continental landmass.");
        }


        private void RegisterDefaultPolities()
        {
            polities.Clear();

            PolityData playerTribe = PolityData.Create(
                1,
                "Северный Род (Игрок)",
                new Color32(50, 120, 210, 255),
                12,
                true);

            PolityData rivalTribeA = PolityData.Create(
                2,
                "Степные Кочевники",
                new Color32(210, 75, 60, 255),
                35,
                false);

            PolityData rivalTribeB = PolityData.Create(
                3,
                "Речной Союз",
                new Color32(65, 175, 95, 255),
                60,
                false);

            polities.Add(playerTribe.PolityId, playerTribe);

            polities.Add(rivalTribeA.PolityId, rivalTribeA);

            polities.Add(rivalTribeB.PolityId, rivalTribeB);
        }


        private void EstablishInitialAnchors()
        {
            anchors.Clear();

            anchorLookup.Clear();

            List<GeoRegionData> candidateRegions = FilterViableLandRegions();

            List<Vector2Int> occupiedTiles = new List<Vector2Int>(polities.Count);

            int pIndex = 0;

            foreach (var kvp in polities)
            {
                ushort polityId = kvp.Key;

                Vector2Int anchorTile = SelectBestAnchorLocation(candidateRegions, occupiedTiles, pIndex, polities.Count);

                occupiedTiles.Add(anchorTile);

                int tileIdx = gridManager.MapConfig.CoordsToIndex(anchorTile.x, anchorTile.y);

                ushort geoRegionId = gridManager.RawTiles[tileIdx].GeoRegionId;

                int storageId = 0;

                if (inventoryManager != null)
                {
                    storageId = inventoryManager.CreateInventory(10000.0f, 15000.0f, 60);
                }

                PolityAnchorData anchor = PolityAnchorData.Create(
                    nextAnchorId++,
                    PolityAnchorType.TribalHearth,
                    polityId,
                    geoRegionId,
                    storageId,
                    anchorTile.x,
                    anchorTile.y,
                    radius: 4);

                anchors.Add(anchor);

                anchorLookup.Add(anchor.AnchorId, anchor);

                pIndex++;
            }
        }


        private List<GeoRegionData> FilterViableLandRegions()
        {
            List<GeoRegionData> viable = new List<GeoRegionData>();

            if (regionManager == null || regionManager.Regions == null)
            {
                return viable;
            }

            var allRegions = regionManager.Regions;

            for (int i = 0; i < allRegions.Count; i++)
            {
                GeoRegionData reg = allRegions[i];

                if (reg.IsOceanRegion || reg.RegionId == 0)
                {
                    continue;
                }

                viable.Add(reg);
            }

            return viable;
        }


        private Vector2Int SelectBestAnchorLocation(
            List<GeoRegionData> candidateRegions,
            List<Vector2Int> existingAnchors,
            int polityIndex,
            int totalPolities)
        {
            WorldMapConfig config = gridManager.MapConfig;

            float minSeparationTiles = config.MapWidth * 0.12f;

            if (candidateRegions.Count > 0)
            {
                int regionStep = Mathf.Max(1, candidateRegions.Count / totalPolities);

                int startOffset = (polityIndex * regionStep + (polityIndex * 7)) % candidateRegions.Count;

                for (int attempt = 0; attempt < candidateRegions.Count; attempt++)
                {
                    int rIdx = (startOffset + attempt) % candidateRegions.Count;

                    GeoRegionData candidateReg = candidateRegions[rIdx];

                    if (TryFindBestHabitableTileInRegion(candidateReg, out Vector2Int bestTile))
                    {
                        bool farEnough = true;

                        for (int a = 0; a < existingAnchors.Count; a++)
                        {
                            if (Vector2Int.Distance(bestTile, existingAnchors[a]) < minSeparationTiles)
                            {
                                farEnough = false;

                                break;
                            }
                        }

                        if (farEnough || attempt > candidateRegions.Count / 2)
                        {
                            return bestTile;
                        }
                    }
                }
            }

            return FindGlobalFallbackLandTile(existingAnchors);
        }


        private bool TryFindBestHabitableTileInRegion(GeoRegionData region, out Vector2Int resultTile)
        {
            WorldMapConfig config = gridManager.MapConfig;

            var tiles = gridManager.RawTiles;

            int startX = Mathf.Clamp(region.CenterTileX, 15, config.MapWidth - 16);

            int startY = Mathf.Clamp(region.CenterTileY, 15, config.MapHeight - 16);

            Vector2Int bestTile = new Vector2Int(startX, startY);

            float bestScore = float.MinValue;

            bool found = false;

            int maxRadius = 48;

            for (int r = 0; r <= maxRadius; r++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (r > 0 && Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue;

                        int tx = startX + dx;

                        int ty = startY + dy;

                        if (!config.IsCoordsValid(tx, ty)) continue;

                        int idx = config.CoordsToIndex(tx, ty);

                        TileData tile = tiles[idx];

                        if (tile.GeoRegionId != region.RegionId) continue;

                        if (!IsTileHabitable(tx, ty, config)) continue;

                        float score = EvaluateTileSettlementScore(tx, ty, config, tiles);

                        score -= (dx * dx + dy * dy) * 0.008f;

                        if (score > bestScore)
                        {
                            bestScore = score;

                            bestTile = new Vector2Int(tx, ty);

                            found = true;
                        }
                    }
                }

                if (found && bestScore >= 45.0f)
                {
                    break;
                }
            }

            resultTile = bestTile;

            return found;
        }


        private float EvaluateTileSettlementScore(
            int tx,
            int ty,
            WorldMapConfig config,
            Unity.Collections.NativeArray<TileData> tiles)
        {
            int idx = config.CoordsToIndex(tx, ty);

            TileData centerTile = tiles[idx];

            float score = 0.0f;

            if (centerTile.Terrain == TerrainType.Plains) score += 40.0f;
            else if (centerTile.Terrain == TerrainType.Hills) score += 25.0f;
            else return -1000.0f;

            if (centerTile.Biome == BiomeType.DenseForest || centerTile.Biome == BiomeType.Taiga) score += 15.0f;
            else if (centerTile.Biome == BiomeType.Steppe) score += 10.0f;

            int habitableNeighbors = 0;

            bool hasWaterNeighbor = false;

            for (int ny = -2; ny <= 2; ny++)
            {
                for (int nx = -2; nx <= 2; nx++)
                {
                    if (nx == 0 && ny == 0) continue;

                    int cx = tx + nx;

                    int cy = ty + ny;

                    if (!config.IsCoordsValid(cx, cy)) continue;

                    int nIdx = config.CoordsToIndex(cx, cy);

                    TileData nTile = tiles[nIdx];

                    if (nTile.Terrain == TerrainType.Plains || nTile.Terrain == TerrainType.Hills)
                    {
                        habitableNeighbors++;
                    }
                    else if (nTile.Terrain == TerrainType.WaterShallow)
                    {
                        hasWaterNeighbor = true;
                    }
                }
            }

            score += habitableNeighbors * 2.5f;

            if (hasWaterNeighbor) score += 20.0f;

            return score;
        }


        private Vector2Int FindGlobalFallbackLandTile(List<Vector2Int> existingAnchors)
        {
            WorldMapConfig config = gridManager.MapConfig;

            var tiles = gridManager.RawTiles;

            int cx = config.MapWidth / 2;

            int cy = config.MapHeight / 2;

            Vector2Int fallback = new Vector2Int(cx, cy);

            float bestScore = float.MinValue;

            for (int step = 15; step < config.MapWidth / 2; step += 18)
            {
                for (int signY = -1; signY <= 1; signY += 2)
                {
                    for (int signX = -1; signX <= 1; signX += 2)
                    {
                        int tx = cx + step * signX;

                        int ty = cy + step * signY;

                        if (!config.IsCoordsValid(tx, ty)) continue;

                        if (IsTileHabitable(tx, ty, config))
                        {
                            float score = EvaluateTileSettlementScore(tx, ty, config, tiles);

                            for (int a = 0; a < existingAnchors.Count; a++)
                            {
                                score += Vector2Int.Distance(new Vector2Int(tx, ty), existingAnchors[a]) * 0.1f;
                            }

                            if (score > bestScore)
                            {
                                bestScore = score;

                                fallback = new Vector2Int(tx, ty);
                            }
                        }
                    }
                }

                if (bestScore > 35.0f)
                {
                    return fallback;
                }
            }

            return fallback;
        }


        private bool IsTileHabitable(int tileX, int tileY, WorldMapConfig config)
        {
            if (!config.IsCoordsValid(tileX, tileY)) return false;

            int idx = config.CoordsToIndex(tileX, tileY);

            TileData tile = gridManager.RawTiles[idx];

            return tile.Terrain != TerrainType.WaterDeep &&
                   tile.Terrain != TerrainType.WaterShallow &&
                   tile.Terrain != TerrainType.Mountains;
        }


        private void FocusCameraOnPlayerSettlement()
        {
            if (anchors.Count == 0 || gridManager == null)
            {
                return;
            }

            StrategicCameraController cam = StrategicCameraController.Instance;

            if (cam == null)
            {
                cam = FindFirstObjectByType<StrategicCameraController>();
            }

            if (cam != null)
            {
                PolityAnchorData playerAnchor = anchors[0];

                float tileSize = gridManager.MapConfig.TileWorldSize;

                float hqX = (playerAnchor.TileX + 0.5f) * tileSize;

                float hqY = (playerAnchor.TileY + 0.5f) * tileSize;

                cam.FocusOnCoordinates(hqX, hqY, 160.0f);
            }
        }


        public bool TryGetAnchor(ushort anchorId, out PolityAnchorData anchor)
        {
            return anchorLookup.TryGetValue(anchorId, out anchor);
        }


        public void RecalculateTerritorialControl()
        {
            if (gridManager == null || !gridManager.IsInitialized)
            {
                return;
            }

            var tiles = gridManager.RawTiles;

            int totalTiles = tiles.Length;

            for (int i = 0; i < totalTiles; i++)
            {
                TileData tile = tiles[i];

                tile.DeFactoPolityId = 0;

                tile.DeJurePolityId = 0;

                tiles[i] = tile;
            }

            WorldMapConfig config = gridManager.MapConfig;

            for (int a = 0; a < anchors.Count; a++)
            {
                PolityAnchorData anchor = anchors[a];

                if (!anchor.IsActive)
                {
                    continue;
                }

                ProjectAnchorInfluence(anchor, tiles, config);
            }
        }


        private void ProjectAnchorInfluence(
            PolityAnchorData anchor,
            Unity.Collections.NativeArray<TileData> tiles,
            WorldMapConfig config)
        {
            int centerX = anchor.TileX;

            int centerY = anchor.TileY;

            int radius = anchor.InfluenceRadius;

            int radiusSqr = radius * radius;

            for (int dy = -radius; dy <= radius; dy++)
            {
                int targetY = centerY + dy;

                if (targetY < 0 || targetY >= config.MapHeight)
                {
                    continue;
                }

                for (int dx = -radius; dx <= radius; dx++)
                {
                    int targetX = centerX + dx;

                    if (targetX < 0 || targetX >= config.MapWidth)
                    {
                        continue;
                    }

                    if (dx * dx + dy * dy > radiusSqr)
                    {
                        continue;
                    }

                    int tileIndex = config.CoordsToIndex(targetX, targetY);

                    TileData tile = tiles[tileIndex];

                    if (tile.Terrain == TerrainType.Mountains || tile.Terrain == TerrainType.WaterDeep)
                    {
                        continue;
                    }

                    tile.DeFactoPolityId = anchor.OwnerPolityId;

                    tile.DeJurePolityId = anchor.OwnerPolityId;

                    tiles[tileIndex] = tile;
                }
            }
        }


        public void PaintTileControl(int centerX, int centerY, ushort polityId, int brushRadius, bool setDeFacto = true, bool setDeJure = true)
        {
            if (gridManager == null || !gridManager.IsInitialized)
            {
                return;
            }

            var tiles = gridManager.RawTiles;

            WorldMapConfig config = gridManager.MapConfig;

            int radiusSqr = brushRadius * brushRadius;

            for (int dy = -brushRadius; dy <= brushRadius; dy++)
            {
                int y = centerY + dy;

                if (y < 0 || y >= config.MapHeight) continue;

                for (int dx = -brushRadius; dx <= brushRadius; dx++)
                {
                    int x = centerX + dx;

                    if (x < 0 || x >= config.MapWidth) continue;

                    if (dx * dx + dy * dy > radiusSqr) continue;

                    int index = config.CoordsToIndex(x, y);

                    TileData tile = tiles[index];

                    if (tile.Terrain == TerrainType.Mountains || tile.Terrain == TerrainType.WaterDeep)
                    {
                        continue;
                    }

                    if (setDeFacto) tile.DeFactoPolityId = polityId;

                    if (setDeJure) tile.DeJurePolityId = polityId;

                    tiles[index] = tile;
                }
            }

            RefreshVisuals();
        }


        public void FillRegionControl(ushort geoRegionId, ushort polityId, bool setDeFacto = true, bool setDeJure = true)
        {
            if (gridManager == null || !gridManager.IsInitialized || geoRegionId == 0)
            {
                return;
            }

            var tiles = gridManager.RawTiles;

            int totalTiles = tiles.Length;

            for (int i = 0; i < totalTiles; i++)
            {
                TileData tile = tiles[i];

                if (tile.GeoRegionId == geoRegionId)
                {
                    if (tile.Terrain != TerrainType.Mountains && tile.Terrain != TerrainType.WaterDeep)
                    {
                        if (setDeFacto) tile.DeFactoPolityId = polityId;

                        if (setDeJure) tile.DeJurePolityId = polityId;

                        tiles[i] = tile;
                    }
                }
            }

            RefreshVisuals();
        }


        public void SetDisplayMode(WorldMapDisplayMode mode)
        {
            currentDisplayMode = mode;

            RefreshVisuals();
        }


        public void RefreshVisuals()
        {
            if (chunkManager == null || !chunkManager.IsInitialized || paletteConfig == null)
            {
                return;
            }

            WorldChunk[,] chunks = chunkManager.Chunks;

            int countX = chunks.GetLength(0);

            int countY = chunks.GetLength(1);

            for (int y = 0; y < countY; y++)
            {
                for (int x = 0; x < countX; x++)
                {
                    chunks[x, y].RefreshChunkColors(gridManager.RawTiles, gridManager.MapConfig, paletteConfig, currentDisplayMode, polities);
                }
            }
        }


        private void OnDrawGizmos()
        {
            if (anchors == null || gridManager == null || !gridManager.IsInitialized)
            {
                return;
            }

            float tileSize = gridManager.MapConfig.TileWorldSize;

            for (int i = 0; i < anchors.Count; i++)
            {
                PolityAnchorData anchor = anchors[i];

                Vector3 pos = new Vector3((anchor.TileX + 0.5f) * tileSize, (anchor.TileY + 0.5f) * tileSize, -2.0f);

                if (polities.TryGetValue(anchor.OwnerPolityId, out PolityData polity))
                {
                    Gizmos.color = (Color)polity.MapColor;
                }
                else
                {
                    Gizmos.color = Color.white;
                }

                Gizmos.DrawSphere(pos, tileSize * 0.4f);

                Gizmos.color = Color.white;

                Gizmos.DrawWireSphere(pos, anchor.InfluenceRadius * tileSize);
            }
        }
    }
}