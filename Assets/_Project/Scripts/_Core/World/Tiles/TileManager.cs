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

                    Width = WorldCreationData.WorldSize;
                    Height = WorldCreationData.WorldSize;

                    Tiles = new TileData[Width * Height];

                    GenerateWorld();
                }

                private void GenerateWorld()
                {
                    List<CultureData> activeCultures = cultureManager.GetActiveCultures();

                    for (int y = 0; y < Height; y++)
                    {
                        for (int x = 0; x < Width; x++)
                        {
                            int i = y * Width + x;

                            CultureData randomCulture = null;

                            int randomId = Random.Range(0, activeCultures.Count);

                            if (activeCultures.Count > 0 && activeCultures[randomId] != null) randomCulture = activeCultures[randomId];
                            else randomId = Random.Range(0, activeCultures.Count);

                            activeCultures[randomId] = null;

                            string cultureId = randomCulture != null ? randomCulture.CultureId : null;
                            
                            Tiles[i] = new TileData
                            {
                                Type = TileType.Grass,

                                Elevation = Random.Range(0f, 10f),

                                Variation = Random.Range(0, 15),

                                Owner = cultureId,
                            };
                        }
                    }
                }

                public TileData GetTile(int x, int y) => Tiles[y * Width + x];
            }
        }
    }
}