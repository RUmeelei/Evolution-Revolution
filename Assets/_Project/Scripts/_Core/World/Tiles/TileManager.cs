using UnityEngine;
using UnityEngine.Tilemaps;
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

                public TileData GetTile(int x, int y) => Tiles[y * Width + x];

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
                    if (x < 0 || x >= Width || y < 0 || y >= Height) return;

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