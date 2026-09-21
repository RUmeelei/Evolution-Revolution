using System;
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

                public event Action<int, int> OnTileChanged;

                private Dictionary<string, int> tilesByOwner = new Dictionary<string, int>();

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
                    tilesByOwner.Clear();

                    for (int y = 0; y < Height; y++)
                    {
                        for (int x = 0; x < Width; x++)
                        {
                            int i = y * Width + x;
                            
                            Tiles[i] = new TileData
                            {
                                Type = TileType.Grass,

                                Elevation = UnityEngine.Random.Range(0f, 10f),

                                Variation = UnityEngine.Random.Range(0, 15),

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

                public TileData WorldToTile(Vector2 worldPos)
                {
                    int x = Mathf.FloorToInt(worldPos.x / mainConfig.TileSize);
                    int y = Mathf.FloorToInt(worldPos.y / mainConfig.TileSize);

                    x = Mathf.Clamp(x, 0, Width - 1);
                    y = Mathf.Clamp(y, 0, Height - 1);

                    return GetTile(x, y);
                }

                public Vector2Int WorldToTilePos(Vector2 worldPos)
                {
                    int x = Mathf.FloorToInt(worldPos.x / mainConfig.TileSize);
                    int y = Mathf.FloorToInt(worldPos.y / mainConfig.TileSize);

                    x = Mathf.Clamp(x, 0, Width - 1);
                    y = Mathf.Clamp(y, 0, Height - 1);

                    return new Vector2Int(x, y);
                }

                public TileData GetTile(int x, int y) => Tiles[y * Width + x];

                public Vector2 GetTileCenter(int x, int y)
                {
                    return new Vector2(x * mainConfig.TileSize + mainConfig.TileSize / 2f, y * mainConfig.TileSize + mainConfig.TileSize / 2f);
                }

                public Vector2Int GetClosestTilePos(Vector2 worldPos)
                {
                    int x = Mathf.RoundToInt((worldPos.x / mainConfig.TileSize) - 0.5f);
                    int y = Mathf.RoundToInt((worldPos.y / mainConfig.TileSize) - 0.5f);

                    x = Mathf.Clamp(x, 0, Width - 1);
                    y = Mathf.Clamp(y, 0, Height - 1);

                    return new Vector2Int(x, y);
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

                public void NotifyTileChanged(int x, int y)
                {
                    OnTileChanged?.Invoke(x, y);
                        
                    tileVisualManager.RenderDirtyTiles();
                }

                public void SetTileType(int x, int y, TileType newType)
                {
                    if (x < 0 || x >= Width || y < 0 || y >= Height) return;

                    int i = y * Width + x;

                    TileData tile = Tiles[i];

                    tile.Type = newType;

                    Tiles[i] = tile;

                    NotifyTileChanged(x, y);
                }

                public void SetTileElevation(int x, int y, float newElevation)
                {
                    if (x < 0 || x >= Width || y < 0 || y >= Height) return;

                    int i = y * Width + x;

                    TileData tile = Tiles[i];

                    tile.Elevation = newElevation;

                    Tiles[i] = tile;

                    NotifyTileChanged(x, y);
                }

                public void SetTileVariation(int x, int y, int newVariation)
                {
                    if (x < 0 || x >= Width || y < 0 || y >= Height) return;

                    int i = y * Width + x;

                    TileData tile = Tiles[i];

                    tile.Variation = newVariation;

                    Tiles[i] = tile;

                    NotifyTileChanged(x, y);
                }

                public void SetTileOwner(int x, int y, string newOwner)
                {
                    if (x < 0 || x >= Width || y < 0 || y >= Height) return;
                    
                    if (newOwner != "CUL_NONE" && cultureManager.GetCulture(newOwner) == null) return;

                    int i = y * Width + x;

                    TileData tile = Tiles[i];

                    string oldOwner = tile.Owner;
                    
                    if (oldOwner == newOwner) return;
                    
                    UpdateOwnerCount(oldOwner, -1);

                    UpdateOwnerCount(newOwner, +1);

                    tile.Owner = newOwner;

                    Tiles[i] = tile;

                    NotifyTileChanged(x, y);
                }

                private void UpdateOwnerCount(string ownerId, int delta)
                {
                    if (string.IsNullOrEmpty(ownerId)) return;

                    if (!tilesByOwner.TryGetValue(ownerId, out int count))
                    {
                        count = 0;
                    }

                    count += delta;

                    if (count <= 0)
                    {
                        tilesByOwner.Remove(ownerId);
                    }
                    else
                    {
                        tilesByOwner[ownerId] = count;
                    }
                }

                public void UpdateTileData(int x, int y, TileData newData)
                {
                    if (x < 0 || x >= Width || y < 0 || y >= Height) return;

                    int i = y * Width + x;

                    TileData oldTile = Tiles[i];
                    
                    if (oldTile.Owner != newData.Owner)
                    {
                        UpdateOwnerCount(oldTile.Owner, -1);
                        
                        UpdateOwnerCount(newData.Owner, +1);
                    }

                    Tiles[i] = newData;

                    NotifyTileChanged(x, y);
                }

                public Color GetTileOwnerColor(int x, int y)
                {
                    if (x < 0 || x >= Width || y < 0 || y >= Height) return Color.clear;

                    var tile = GetTile(x, y);

                    if (tile.Owner == "CUL_NONE") return Color.clear;

                    var culture = cultureManager.GetCulture(tile.Owner);

                    if (culture == null) return Color.clear;

                    return culture.CultureColor;
                }

                public float GetTileCaptureProgress(int x, int y)
                {
                    if (x < 0 || x >= Width || y < 0 || y >= Height) return 0f;
                
                    return GetTile(x, y).CaptureProgress;
                }

                public Color GetTileCaptureColor(int x, int y)
                {
                    if (x < 0 || x >= Width || y < 0 || y >= Height) return Color.clear;

                    var tile = GetTile(x, y);

                    if (string.IsNullOrEmpty(tile.CapturingCultureId)) return Color.clear;

                    var culture = cultureManager.GetCulture(tile.CapturingCultureId);
                    
                    if (culture == null) return Color.clear;

                    return culture.CultureColor;
                }

                public int GetTileCountForOwner(string ownerId)
                {
                    if (string.IsNullOrEmpty(ownerId)) return 0;

                    tilesByOwner.TryGetValue(ownerId, out int count);

                    return count;
                }
            }
        }
    }
}