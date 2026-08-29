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
                [SerializeField] private Camera cam;

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

                    for (int y = 0; y < mainConfig.WorldHeight; y++)
                    {
                        for (int x = 0; x < mainConfig.WorldWidth; x++)
                        {
                            SetTileAt(x, y);
                        }
                    }
                }

                public void Initialize()
                {
                    mainConfig = CoreManager.MainConfig;

                    tileManager = CoreManager.TileManager;
                }

                private void SetTileAt(int x, int y)
                {
                    TileData tile = tileManager.GetTile(x, y);

                    Vector3Int tilePos = new Vector3Int(x, y, 0);

                    int variation = tile.Variation;

                    TileBase tileBase = GetTileVariation(GrassTiles, variation);

                    Color tileColor = Color.white;

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