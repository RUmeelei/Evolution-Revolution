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
            using Controls;

            public class TileVisualManager : MonoBehaviour
            {
                public static TileVisualManager Instance {get; private set;}

                [Header("Main")]
                public bool EnableLogging;
                public bool EnableElevationDisplay;
                public int MapMode;

                [Header("References")]
                [SerializeField] private Camera Cam;

                [Header("Tiles")]
                [SerializeField] private Tilemap BaseTilemap;
                [SerializeField] private Tilemap OwnerTilemap;
                [SerializeField] private Tilemap StrategicTilemap;
                [SerializeField] private Tilemap WTFTilemap;

                [SerializeField] private TileBase[] WaterTiles;
                [SerializeField] private TileBase[] GrassTiles;
                [SerializeField] private TileBase[] SandTiles;
                [SerializeField] private TileBase[] RockTiles;
                [SerializeField] private TileBase[] BlankTiles;

                private CultureManager cultureManager;
                private TileManager tileManager;
                private SelectionManager selectionManager;

                private MainConfig mainConfig;

                private HashSet<Vector2Int> DirtyTiles = new HashSet<Vector2Int>();

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

                    CoreManager.RegisterTileVisualManager(this);
                }

                void Start()
                {
                    cultureManager = CoreManager.CultureManager;

                    selectionManager = CoreManager.SelectionManager;
                }

                void OnDestroy()
                {
                    
                }

                void Update()
                {
                    if (tileManager == null) return;

                    if (DirtyTiles.Count > 0)
                    {
                        RenderDirtyTiles();
                    }

                    if (Input.GetKeyDown(KeyCode.M))
                    {
                        MapMode = MapMode == 0 ? 1 : 0;

                        RenderWorld();
                    }
                }

                public void Initialize()
                {
                    mainConfig = CoreManager.MainConfig;

                    tileManager = CoreManager.TileManager;

                    RenderWorld();
                }

                private void RenderWorld()
                {
                    if (tileManager == null) return;
                    
                    BaseTilemap.ClearAllTiles();
                    OwnerTilemap.ClearAllTiles();
                    // StrategicTilemap.ClearAllTiles();
                    // WTFTilemap.ClearAllTiles();
                    
                    for (int y = 0; y < WorldCreationData.WorldSize; y++)
                    {
                        for (int x = 0; x < WorldCreationData.WorldSize; x++)
                        {
                            SetTileAt(x, y);
                        }
                    }
                }

                private void RenderVisibleWorld()
                {
                    if (tileManager == null) return;

                    float halfH = Cam.orthographicSize;
                    float halfW = halfH * Cam.aspect;

                    Vector3 camPos = Cam.transform.position;

                    int minX = Mathf.Max(0, Mathf.FloorToInt((camPos.x - halfW) / mainConfig.TileSize));
                    int maxX = Mathf.Min(WorldCreationData.WorldSize - 1, Mathf.CeilToInt((camPos.x + halfW) / mainConfig.TileSize));

                    int minY = Mathf.Max(0, Mathf.FloorToInt((camPos.y - halfH) / mainConfig.TileSize));
                    int maxY = Mathf.Min(WorldCreationData.WorldSize - 1, Mathf.CeilToInt((camPos.y + halfH) / mainConfig.TileSize));

                    for (int y = minY; y <= maxY; y++)
                    {
                        for (int x = minX; x <= maxX; x++)
                        {
                            SetTileAt(x, y);
                        }
                    }
                }

                private void RenderDirtyTiles()
                {
                    if (DirtyTiles.Count == 0 || tileManager == null) return;

                    foreach (var pos in DirtyTiles)
                    {
                        SetTileAt(pos.x, pos.y);
                    }

                    DirtyTiles.Clear();
                }

                private void SetTileAt(int x, int y)
                {
                    TileData tile = tileManager.GetTile(x, y);

                    Vector3Int tilePos = new Vector3Int(x, y, 0);

                    int variation = tile.Variation;

                    TileBase tileBase = GetTileVariation(BlankTiles, variation);

                    Color tileColor = Color.white;

                    switch (tile.Type)
                    {
                        case TileType.Water:
                            tileBase = GetTileVariation(WaterTiles, variation);
     
                        break;
     
                        case TileType.Grass:
                            tileBase = GetTileVariation(GrassTiles, variation);
     
                        break;
     
                        case TileType.Sand:
                            tileBase = GetTileVariation(SandTiles, variation);
     
                        break;
     
                        case TileType.Rock:
                            tileBase = GetTileVariation(RockTiles, variation);
     
                        break;
     
                        default :
                            tileBase = GetTileVariation(BlankTiles, variation);
     
                        break;
                    }
     
                    float elevation = Mathf.Clamp01(tile.Elevation / 10f);
                    float brightness = EnableElevationDisplay ? 1f - elevation * 0.5f : 1f;
     
                    brightness = Mathf.Clamp01(brightness);
     
                    tileColor = selectionManager.SelectedTiles.Contains(new Vector2Int(x, y)) ? Color.green : new Color(brightness, brightness, brightness);
     
                    BaseTilemap.SetTile(tilePos, tileBase);
                    BaseTilemap.SetColor(tilePos, tileColor);

                    if (MapMode == 1)
                    {
                        tileBase = GetTileVariation(BlankTiles, variation);

                        tileColor = tile.Owner != "CUL_NONE" ? cultureManager.GetCulture(tile.Owner).CultureColor : Color.white;

                        tileColor.a = tile.Owner != "CUL_NONE" ? 0.8f : 0.2f;
     
                        OwnerTilemap.SetTile(tilePos, tileBase);
                        OwnerTilemap.SetColor(tilePos, tileColor);
                    }
                }

                private TileBase GetTileVariation(TileBase[] tileBase, int variation = 0)
                {
                    if (tileBase == null || tileBase.Length == 0) return null;

                    if (variation >= 0 && variation < tileBase.Length) return tileBase[variation];

                    return tileBase.Length > 0 ? tileBase[0] : BlankTiles[0];
                }

                public void MarkTileDirty(int x, int y)
                {
                    if (x < 0 || y < 0 || x >= WorldCreationData.WorldSize || y >= WorldCreationData.WorldSize) return;

                    DirtyTiles.Add(new Vector2Int(x, y));
                }
            }
        }
    }
}