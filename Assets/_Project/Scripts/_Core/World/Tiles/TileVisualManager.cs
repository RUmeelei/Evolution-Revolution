using UnityEngine;
using UnityEngine.Tilemaps;

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
                [SerializeField] private TileBase[] WaterTiles;
                [SerializeField] private TileBase[] GrassTiles;
                [SerializeField] private TileBase[] SandTiles;
                [SerializeField] private TileBase[] RockTiles;
                [SerializeField] private TileBase[] BlankTiles;

                private CultureManager cultureManager;
                private TileManager tileManager;

                private MainConfig mainConfig;

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
                }

                void OnDestroy()
                {
                    
                }

                void Update()
                {
                    if (tileManager == null) return;

                    RenderWorld();
                }

                public void Initialize()
                {
                    mainConfig = CoreManager.MainConfig;

                    tileManager = CoreManager.TileManager;
                }

                private void RenderWorld()
                {
                    for (int y = 0; y < WorldCreationData.WorldSize; y++)
                    {
                        for (int x = 0; x < WorldCreationData.WorldSize; x++)
                        {
                            SetTileAt(x, y);
                        }
                    }
                }

                private void SetTileAt(int x, int y)
                {
                    TileData tile = tileManager.GetTile(x, y);

                    Vector3Int tilePos = new Vector3Int(x, y, 0);

                    int variation = tile.Variation;

                    TileBase tileBase = GetTileVariation(BlankTiles, variation);

                    Color tileColor = Color.white;

                    if (MapMode == 0)
                    {
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

                        tileColor = new Color(brightness, brightness, brightness);
                    }
                    else if (MapMode == 1)
                    {
                        tileBase = GetTileVariation(BlankTiles, variation);

                        tileColor = tile.Owner != null ? cultureManager.GetCulture(tile.Owner).CultureColor : Color.white;
                    }

                    BaseTilemap.SetTile(tilePos, tileBase);
                    BaseTilemap.SetColor(tilePos, tileColor);
                }

                private TileBase GetTileVariation(TileBase[] tileBase, int variation = 0)
                {
                    if (tileBase == null || tileBase.Length == 0) return null;

                    if (variation >= 0 && variation < tileBase.Length) return tileBase[variation];

                    return tileBase.Length > 0 ? tileBase[0] : BlankTiles[0];
                }
            }
        }
    }
}