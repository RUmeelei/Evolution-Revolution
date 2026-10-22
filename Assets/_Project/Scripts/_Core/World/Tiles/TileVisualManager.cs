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
            using Culture;
            using Controls;

            public class TileVisualManager : MonoBehaviour
            {
                public static TileVisualManager Instance {get; private set;}

                [Header("Main")]
                public bool EnableLogging;
                public bool EnableElevationDisplay;
                public int MapMode = 1;

                [Header("References")]
                [SerializeField] private Camera Cam;

                [Header("Tiles")]
                [SerializeField] private Tilemap BaseTilemap;
                [SerializeField] private Tilemap OwnerTilemap;
                [SerializeField] private Tilemap StrategicTilemap;
                [SerializeField] private Tilemap StructsTilemap;
                [SerializeField] private Tilemap WTFTilemap;

                [SerializeField] private TileBase[] WaterTiles;
                [SerializeField] private TileBase[] GrassTiles;
                [SerializeField] private TileBase[] SandTiles;
                [SerializeField] private TileBase[] RockTiles;
                [SerializeField] private TileBase[] BlankTiles;

                [Header("Structures Visual")]
                [SerializeField] private TileBase CapitalStructureTile;
                [SerializeField] private TileBase OutpostStructureTile;
                [SerializeField] private TileBase HubStructureTile;
                [SerializeField] private TileBase SettlementStructureTile;

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
                    if (Cam == null)
                    {
                        Cam = Camera.main;
                    }
                }

                void OnDestroy()
                {
                    if (tileManager != null) tileManager.OnTileChanged -= MarkTileDirty;
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
                    cultureManager = CoreManager.CultureManager;
                    selectionManager = CoreManager.SelectionManager;
                    tileManager = CoreManager.TileManager;

                    tileManager.OnTileChanged += MarkTileDirty;

                    RenderWorld();
                }

                private void RenderWorld()
                {
                    if (tileManager == null) return;

                    BaseTilemap.ClearAllTiles();
                    OwnerTilemap.ClearAllTiles();
                    StructsTilemap.ClearAllTiles();

                    if (StrategicTilemap != null) StrategicTilemap.ClearAllTiles();

                    int width = tileManager.WorldWidth;
                    int height = tileManager.WorldHeight;

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            SetTileAt(x, y);
                        }
                    }
                }

                public void RenderDirtyTiles()
                {
                    if (DirtyTiles.Count == 0 || tileManager == null) return;

                    foreach (var pos in DirtyTiles)
                    {
                        SetTileAt(pos.x, pos.y);
                    }

                    DirtyTiles.Clear();
                }

                private bool IsCultureCapital(int x, int y, string ownerId)
                {
                    if (string.IsNullOrEmpty(ownerId) || ownerId == "CUL_NONE" || cultureManager == null) return false;

                    var culture = cultureManager.GetCulture(ownerId);

                    return culture != null && !culture.CultureDefeated && culture.CultureCapital.x == x && culture.CultureCapital.y == y;
                }

                private void SetTileAt(int x, int y)
                {
                    TileData tile = tileManager.GetTile(x, y);

                    Vector3Int tilePos = new Vector3Int(x, y, 0);

                    int variation = tile.Variation;

                    TileBase tileBase = tile.Type switch
                    {
                        TileType.Water => GetTileVariation(WaterTiles, variation),
                        TileType.Grass => GetTileVariation(GrassTiles, variation),
                        TileType.Sand => GetTileVariation(SandTiles, variation),
                        TileType.Rock => GetTileVariation(RockTiles, variation),
                        _ => GetTileVariation(BlankTiles, variation)
                    };

                    float elevation = Mathf.Clamp01(tile.Elevation / 10f);
                    float brightness = EnableElevationDisplay ? Mathf.Clamp01(1f - elevation * 0.5f) : 1f;

                    bool isSelected = selectionManager != null && selectionManager.SelectedTiles != null && selectionManager.SelectedTiles.Contains(new Vector2Int(x, y));
                    bool isCapital = IsCultureCapital(x, y, tile.Owner);

                    Color baseTint = new Color(brightness, brightness, brightness);

                    if (isCapital)
                    {
                        baseTint = new Color(1f, 0.92f, 0.45f);
                    }
                    else if (tile.Structure == TileStructureType.Settlement)
                    {
                        baseTint = new Color(1f, 0.78f, 0.4f);
                    }
                    else if (tile.Structure == TileStructureType.LogisticsHub)
                    {
                        baseTint = new Color(0.55f, 0.9f, 1f);
                    }
                    else if (tile.Structure == TileStructureType.Outpost)
                    {
                        baseTint = new Color(0.8f, 0.65f, 1f);
                    }
                    else if (tile.Road == TileRoadType.Railway)
                    {
                        baseTint = Color.Lerp(baseTint, new Color(0.35f, 0.38f, 0.42f), 0.55f);
                    }
                    else if (tile.Road == TileRoadType.PavedRoad)
                    {
                        baseTint = Color.Lerp(baseTint, new Color(0.68f, 0.66f, 0.60f), 0.45f);
                    }
                    else if (tile.Road == TileRoadType.DirtPath)
                    {
                        baseTint = Color.Lerp(baseTint, new Color(0.62f, 0.50f, 0.32f), 0.38f);
                    }

                    Color tileColor = isSelected ? Color.green : baseTint;

                    BaseTilemap.SetTile(tilePos, tileBase);
                    BaseTilemap.SetTileFlags(tilePos, TileFlags.None);
                    BaseTilemap.SetColor(tilePos, tileColor);

                    RenderStructureOverlay(tile, tilePos, isCapital, variation);

                    if (MapMode == 1)
                    {
                        Color layerColor = Color.white;
                        float layerAlpha = 0f;
                    
                        bool hasOwner = tile.Owner != "CUL_NONE";
                        bool isBeingCaptured = !string.IsNullOrEmpty(tile.CapturingCultureId) && tile.CaptureProgress > 0f;
                    
                        if (hasOwner && cultureManager != null)
                        {
                            var ownerCulture = cultureManager.GetCulture(tile.Owner);
                    
                            if (ownerCulture != null)
                            {
                                layerColor = isCapital ? Color.Lerp(ownerCulture.CultureColor, Color.yellow, 0.35f) : ownerCulture.CultureColor;
                                layerAlpha = 0.6f;
                            }
                        }
                    
                        if (isBeingCaptured && cultureManager != null)
                        {
                            var capturingCulture = cultureManager.GetCulture(tile.CapturingCultureId);
                    
                            if (capturingCulture != null)
                            {
                                float progress01 = Mathf.Clamp01(tile.CaptureProgress / 100f);
                                float smoothProgress = Mathf.SmoothStep(0f, 1f, progress01);
                    
                                if (hasOwner)
                                {
                                    layerColor = Color.Lerp(layerColor, capturingCulture.CultureColor, smoothProgress);
                                    layerAlpha = Mathf.Lerp(0.6f, 0.75f, smoothProgress);
                                }
                                else
                                {
                                    layerColor = capturingCulture.CultureColor;
                                    layerAlpha = Mathf.Lerp(0f, 0.6f, smoothProgress);
                                }
                            }
                        }
                    
                        layerColor.a = layerAlpha;
                    
                        TileBase ownerTileBase = GetTileVariation(BlankTiles, variation);
                    
                        OwnerTilemap.SetTile(tilePos, ownerTileBase);
                        OwnerTilemap.SetTileFlags(tilePos, TileFlags.None);
                        OwnerTilemap.SetColor(tilePos, layerColor);
                    }
                }

                private void RenderStructureOverlay(TileData tile, Vector3Int tilePos, bool isCapital, int variation)
                {
                    if (StrategicTilemap == null) return;

                    if (!isCapital && tile.Structure == TileStructureType.None)
                    {
                        StrategicTilemap.SetTile(tilePos, null);

                        return;
                    }

                    TileBase fallbackTile = GetTileVariation(BlankTiles, variation);
                    TileBase chosenTile = null;
                    Color structColor = Color.white;

                    if (isCapital)
                    {
                        chosenTile = CapitalStructureTile != null ? CapitalStructureTile : fallbackTile;
                        structColor = new Color(1f, 0.85f, 0.1f, 0.45f);
                    }
                    else
                    {
                        switch (tile.Structure)
                        {
                            case TileStructureType.Outpost:
                                chosenTile = OutpostStructureTile != null ? OutpostStructureTile : fallbackTile;
                                structColor = new Color(0.6f, 0.2f, 0.9f, 0.45f);
                                break;

                            case TileStructureType.LogisticsHub:
                                chosenTile = HubStructureTile != null ? HubStructureTile : fallbackTile;
                                structColor = new Color(0.2f, 0.85f, 1f, 0.45f);
                                break;

                            case TileStructureType.Settlement:
                                chosenTile = SettlementStructureTile != null ? SettlementStructureTile : fallbackTile;
                                structColor = new Color(1f, 0.55f, 0.15f, 0.45f);
                                break;
                        }
                    }

                    StrategicTilemap.SetTile(tilePos, chosenTile);
                    StrategicTilemap.SetTileFlags(tilePos, TileFlags.None);
                    StrategicTilemap.SetColor(tilePos, structColor);
                }

                private TileBase GetTileVariation(TileBase[] tileBase, int variation = 0)
                {
                    if (tileBase == null || tileBase.Length == 0) return null;

                    if (variation >= 0 && variation < tileBase.Length) return tileBase[variation];

                    return tileBase.Length > 0 ? tileBase[0] : BlankTiles[0];
                }

                public void MarkTileDirty(int x, int y)
                {
                    if (tileManager == null) return;

                    if (x < 0 || y < 0 || x >= tileManager.WorldWidth || y >= tileManager.WorldHeight) return;

                    DirtyTiles.Add(new Vector2Int(x, y));
                }
            }
        }
    }
}