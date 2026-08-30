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

                [Header("References")]
                [SerializeField] private Camera Cam;

                [Header("Tiles")]
                [SerializeField] private Tilemap BaseTilemap;
                [SerializeField] private TileBase[] GrassTiles;

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
                    for (int y = 0; y < mainConfig.WorldHeight; y++)
                    {
                        for (int x = 0; x < mainConfig.WorldWidth; x++)
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

                    TileBase tileBase = GetTileVariation(GrassTiles, variation);

                    float elevation = Mathf.Clamp01(tile.Elevation / 10f);
                    float brightness = 1f - elevation * 0.5f;

                    brightness = Mathf.Clamp01(brightness);

                    Color tileColor = new Color(brightness, brightness, brightness);

                    BaseTilemap.SetTile(tilePos, tileBase);
                    BaseTilemap.SetColor(tilePos, tileColor);
                }

                private TileBase GetTileVariation(TileBase[] tileBase, int variation = 0)
                {
                    if (tileBase == null || tileBase.Length == 0) return null;

                    if (variation >= 0 && variation < tileBase.Length) return tileBase[variation];

                    return tileBase[0];
                }
            }
        }
    }
}