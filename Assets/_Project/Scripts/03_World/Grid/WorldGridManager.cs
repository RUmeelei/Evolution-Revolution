using System;
using Unity.Collections;
using UnityEngine;

namespace ER.World.Grid
{
    public class WorldGridManager : MonoBehaviour
    {
        public static WorldGridManager Instance { get; private set; }


        [Header("Configuration Asset")]
        [SerializeField] private WorldMapConfig mapConfig;


        [Header("Procedural Generation Parameters")]
        [SerializeField] private int seed = 1337;

        [SerializeField] private float elevationFrequency = 0.004f;

        [SerializeField] private float ridgeFrequencyMultiplier = 2.4f;

        [SerializeField] private float moistureFrequency = 0.003f;

        [SerializeField] private float mountainThreshold = 0.68f;

        [SerializeField] private float hillsThreshold = 0.54f;

        [SerializeField] private float waterShallowThreshold = 0.32f;

        [SerializeField] private float waterDeepThreshold = 0.22f;


        private NativeArray<TileData> tiles;

        private bool isInitialized = false;


        public bool IsInitialized => isInitialized;

        public WorldMapConfig MapConfig => mapConfig;

        public NativeArray<TileData> RawTiles => tiles;


        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            InitializeGrid();
        }


        private void OnDestroy()
        {
            DisposeGrid();
        }


        private void OnApplicationQuit()
        {
            DisposeGrid();
        }


        public void InitializeGrid()
        {
            if (isInitialized || mapConfig == null)
            {
                return;
            }

            int totalTiles = mapConfig.TotalTilesCount;

            tiles = new NativeArray<TileData>(totalTiles, Allocator.Persistent);

            GenerateInitialTerrain();

            isInitialized = true;
        }


        private void DisposeGrid()
        {
            if (!tiles.IsCreated)
            {
                return;
            }

            tiles.Dispose();

            isInitialized = false;
        }


        private void GenerateInitialTerrain()
        {
            int width = mapConfig.MapWidth;

            int height = mapConfig.MapHeight;

            float seedOffsetX = (seed ^ 0x5DEECE66D) % 10000;

            float seedOffsetY = (seed ^ 0xB) % 10000;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = mapConfig.CoordsToIndex(x, y);

                    float baseSampleX = (x + seedOffsetX) * elevationFrequency;

                    float baseSampleY = (y + seedOffsetY) * elevationFrequency;

                    float continentBase = Mathf.PerlinNoise(baseSampleX, baseSampleY);

                    float ridgeSampleX = (x + seedOffsetX + 2345.6f) * (elevationFrequency * ridgeFrequencyMultiplier);

                    float ridgeSampleY = (y + seedOffsetY + 6789.1f) * (elevationFrequency * ridgeFrequencyMultiplier);

                    float rawRidgeNoise = Mathf.PerlinNoise(ridgeSampleX, ridgeSampleY);

                    float sharpRidge = 1.0f - Mathf.Abs(rawRidgeNoise * 2.0f - 1.0f);

                    sharpRidge = sharpRidge * sharpRidge;

                    float elevationValue;

                    if (continentBase < waterShallowThreshold)
                    {
                        elevationValue = continentBase;
                    }
                    else
                    {
                        float landFactor = Mathf.Clamp01((continentBase - waterShallowThreshold) / (1.0f - waterShallowThreshold));

                        elevationValue = Mathf.Clamp01(continentBase + sharpRidge * 0.42f * landFactor);
                    }

                    float moistureSampleX = (x + seedOffsetX + 5000) * moistureFrequency;

                    float moistureSampleY = (y + seedOffsetY + 5000) * moistureFrequency;

                    float moistureValue = Mathf.PerlinNoise(moistureSampleX, moistureSampleY);

                    TileData tile = TileData.CreateDefault();

                    tile.Elevation = (byte)(elevationValue * 255.0f);

                    tile.Moisture = (byte)(moistureValue * 255.0f);

                    if (elevationValue < waterDeepThreshold)
                    {
                        tile.Terrain = TerrainType.WaterDeep;
                        tile.MovementCost = 255;
                        tile.Fertility = 0;
                        tile.ResourceReserve = 0;
                    }
                    else if (elevationValue < waterShallowThreshold)
                    {
                        tile.Terrain = TerrainType.WaterShallow;
                        tile.MovementCost = 120;
                        tile.Fertility = 30;
                        tile.ResourceReserve = 500;
                        tile.SetFlag(TileData.FlagHasRiver, true);
                    }
                    else if (elevationValue > mountainThreshold)
                    {
                        tile.Terrain = TerrainType.Mountains;
                        tile.MovementCost = 240;
                        tile.Fertility = 5;
                        tile.ResourceReserve = 5000;
                    }
                    else if (elevationValue > hillsThreshold)
                    {
                        tile.Terrain = TerrainType.Hills;
                        tile.MovementCost = 45;
                        tile.Fertility = 60;
                        tile.ResourceReserve = 2500;
                    }
                    else
                    {
                        if (moistureValue > 0.65f && elevationValue < 0.40f)
                        {
                            tile.Terrain = TerrainType.Swamp;
                            tile.MovementCost = 70;
                            tile.Fertility = 90;
                            tile.ResourceReserve = 1200;
                        }
                        else
                        {
                            tile.Terrain = TerrainType.Plains;
                            tile.MovementCost = 10;
                            tile.Fertility = 160;
                            tile.ResourceReserve = 1500;
                        }
                    }

                    tile.Biome = DetermineBiome(tile.Terrain, tile.Elevation, tile.Moisture);

                    tiles[index] = tile;
                }
            }
        }


        private BiomeType DetermineBiome(TerrainType terrain, byte elevation, byte moisture)
        {
            if (terrain == TerrainType.WaterDeep || terrain == TerrainType.WaterShallow)
            {
                return BiomeType.TemperatePlains;
            }

            if (elevation > 200)
            {
                return BiomeType.Tundra;
            }

            if (moisture < 60)
            {
                return BiomeType.Steppe;
            }

            if (moisture < 100)
            {
                return BiomeType.Desert;
            }

            if (moisture > 160)
            {
                return elevation > 130 ? BiomeType.Taiga : BiomeType.DenseForest;
            }

            return BiomeType.TemperatePlains;
        }


        public TileData GetTile(int x, int y)
        {
            int index = mapConfig.CoordsToIndex(x, y);

            return tiles[index];
        }


        public TileData GetTile(int index)
        {
            return tiles[index];
        }


        public void SetTile(int x, int y, TileData data)
        {
            int index = mapConfig.CoordsToIndex(x, y);

            tiles[index] = data;
        }


        public void SetTile(int index, TileData data)
        {
            tiles[index] = data;
        }


        public bool TryGetTile(int x, int y, out TileData tile)
        {
            if (!mapConfig.IsCoordsValid(x, y))
            {
                tile = default;
                return false;
            }

            int index = mapConfig.CoordsToIndex(x, y);

            tile = tiles[index];

            return true;
        }
    }
}