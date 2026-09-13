using UnityEngine;
using System.Collections.Generic;

namespace ER
{
    namespace World
    {
        namespace Tiles
        {
            using Core;
            using Configs;
            using Simulation;
            // using Government;
            using Culture;
            using Resources;
            using Unit;

            public class TileManager : MonoBehaviour
            {
                public static TileManager Instance {get; private set;}

                [Header("Main")]
                public bool EnableLogging;

                private MainConfig mainConfig;

                private CultureManager cultureManager;
                private TileVisualManager tileVisualManager;

                private TileData[] Tiles;

                private int Width;
                private int Height;

                public int WorldWidth => Width;
                public int WorldHeight => Height;

                void Awake()
                {
                    if (transform.parent != null)
                    {
                        transform.SetParent(null);
                    }

                    if (Instance != null && Instance != this)
                    {
                        Destroy(gameObject); return;
                    }

                    Instance = this;

                    DontDestroyOnLoad(gameObject);

                    CoreManager.RegisterTileManager(this);
                }

                void Start()
                {
                    
                }

                void OnDestroy()
                {
                    
                }

                public void Initialize()
                {
                    mainConfig = CoreManager.MainConfig;

                    cultureManager = CoreManager.CultureManager;

                    tileVisualManager = CoreManager.TileVisualManager;

                    Width = WorldCreationData.WorldSize;
                    Height = WorldCreationData.WorldSize;

                    Tiles = new TileData[Width * Height];

                    GenerateWorld();

                    if (cultureManager.GetActiveCultures().Count > 0)
                    {
                        GenerateCultureCapitals();
                    }
                }

                private void GenerateWorld()
                {
                    for (int y = 0; y < Height; y++)
                    {
                        for (int x = 0; x < Width; x++)
                        {
                            int i = y * Width + x;
                            
                            Tiles[i] = new TileData
                            {
                                Type = TileType.Grass,

                                Elevation = Random.Range(0f, 10f),

                                Variation = Random.Range(0, 15),

                                Owner = "CUL_NONE",
                            };
                        }
                    }
                }

                private void GenerateCultureCapitals()
                {
                    foreach (var culture in cultureManager.GetActiveCultures())
                    {
                        int _x = 0;
                        int _y = 0;

                        float bestScore = 0f;

                        for (int y = 0; y < Height; y++)
                        {
                            for (int x = 0; x < Width; x++)
                            {
                                int i = y * Width + x;

                                if (Tiles[i].Owner != "CUL_NONE") continue;

                                float captialScore = 0f;

                                captialScore += Tiles[i].Elevation;

                                if (Tiles[i].Type == TileType.Grass) captialScore += 1;

                                if (bestScore < captialScore)
                                {
                                    bestScore = captialScore;

                                    _x = x;
                                    _y = y;
                                }
                            }
                        }

                        if (_x >= 0 && _y >= 0)
                        {
                            SetTileOwner(_x, _y, culture.CultureId);

                            culture.SetCultureCapital(new Vector2Int(_x, _y));
                        }
                    }
                }

                public TileData GetTile(int x, int y) => Tiles[y * Width + x];

                public Vector2 GetTileCenter(int x, int y)
                {
                    return new Vector2(x * mainConfig.TileSize + mainConfig.TileSize / 2f, y * mainConfig.TileSize + mainConfig.TileSize / 2f);
                }

                public List<TileData> GetNeighboringTiles(int x, int y)
                {
                    int size = WorldCreationData.WorldSize;

                    var tiles = new List<TileData>(4);

                    foreach (var (dx, dy) in new[] {(-1, 0), (1, 0), (0, -1), (0, 1)})
                    {
                        int nx = x + dx;

                        int ny = y + dy;

                        if (nx >= 0 && nx < size && ny >= 0 && ny < size) tiles.Add(GetTile(nx, ny));
                    }

                    return tiles;
                }

                public void SetTileType(int x, int y, TileType newType)
                {
                    if (x < 0 || x >= Width || y < 0 || y >= Height) return;

                    int i = y * Width + x;

                    TileData tile = Tiles[i];

                    tile.Type = newType;

                    Tiles[i] = tile;

                    tileVisualManager.MarkTileDirty(x, y);
                }

                public void SetTileElevation(int x, int y, float newElevation)
                {
                    if (x < 0 || x >= Width || y < 0 || y >= Height) return;

                    int i = y * Width + x;

                    TileData tile = Tiles[i];

                    tile.Elevation = newElevation;

                    Tiles[i] = tile;

                    tileVisualManager.MarkTileDirty(x, y);
                }

                public void SetTileVariation(int x, int y, int newVariation)
                {
                    if (x < 0 || x >= Width || y < 0 || y >= Height) return;

                    int i = y * Width + x;

                    TileData tile = Tiles[i];

                    tile.Variation = newVariation;

                    Tiles[i] = tile;

                    tileVisualManager.MarkTileDirty(x, y);
                }

                public void SetTileOwner(int x, int y, string newOwner)
                {
                    if (x < 0 || x >= Width || y < 0 || y >= Height || cultureManager.GetCulture(newOwner) == null) return;

                    int i = y * Width + x;

                    TileData tile = Tiles[i];

                    tile.Owner = newOwner;

                    Tiles[i] = tile;

                    tileVisualManager.MarkTileDirty(x, y);
                }
            }
        }
    }
}