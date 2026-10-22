using UnityEngine;
using System.Collections.Generic;

namespace ER
{
    namespace World
    {
        namespace Tiles
        {
            using Configs;

            public static class WorldMapGenerator
            {
                public static int CurrentSeed { get; set; } = 0;

                private static TileData[] cachedTiles;
                private static int cachedWidth;
                private static int cachedHeight;
                private static int cachedSeed;

                private struct ContinentSeed
                {
                    public Vector2 Center;
                    public float Radius;
                    public Vector2 Dir;
                    public Vector2 Perp;
                }

                public static int EnsureSeed(bool forceNew = false)
                {
                    if (forceNew || CurrentSeed == 0)
                    {
                        CurrentSeed = Random.Range(10000, 999999);
                        InvalidateCache();
                    }

                    return CurrentSeed;
                }

                public static void InvalidateCache()
                {
                    cachedTiles = null;
                    cachedWidth = 0;
                    cachedHeight = 0;
                    cachedSeed = 0;
                }

                public static TileData[] GetOrGenerateWorldData(int width, int height, MainConfig config, int seed = 0)
                {
                    if (seed == 0) seed = EnsureSeed();

                    if (cachedTiles != null && cachedWidth == width && cachedHeight == height && cachedSeed == seed && cachedTiles.Length == width * height)
                    {
                        TileData[] copy = new TileData[cachedTiles.Length];
                        System.Array.Copy(cachedTiles, copy, cachedTiles.Length);
                        return copy;
                    }

                    TileData[] generated = GenerateWorldData(width, height, config, seed);

                    cachedTiles = new TileData[generated.Length];
                    System.Array.Copy(generated, cachedTiles, generated.Length);
                    cachedWidth = width;
                    cachedHeight = height;
                    cachedSeed = seed;

                    return generated;
                }

                public static TileData[] GenerateWorldData(int width, int height, MainConfig config, int seed = 0)
                {
                    if (seed == 0) seed = EnsureSeed();

                    Random.State oldState = Random.state;
                    Random.InitState(seed);

                    TileData[] tiles = new TileData[width * height];

                    float offsetX = Random.Range(100f, 5000f);
                    float offsetY = Random.Range(100f, 5000f);

                    float baseScale = (config != null && config.WorldNoiseScale > 0.001f) ? config.WorldNoiseScale : 0.08f;
                    int continentsCount = config != null ? Mathf.Clamp(config.ContinentalPlatesCount, 1, 8) : 3;
                    int oceanicBasinsCount = config != null ? Mathf.Clamp(config.OceanicPlatesCount, 1, 6) : 3;
                    float ridgeStrength = config != null ? config.MountainRidgeStrength : 2.2f;
                    float falloffStrength = config != null ? config.ContinentFalloffStrength : 0.35f;

                    float waterThresh = config != null ? config.WaterElevationThreshold : 2.5f;
                    float sandThresh = config != null ? config.SandElevationThreshold : 3.2f;
                    float rockThresh = config != null ? config.RockElevationThreshold : 7.2f;
                    int riversCount = config != null ? config.MajorRiversCount : 3;
                    
                    const float NormSpace = 100f;
                    Vector2 normCenter = new Vector2(NormSpace * 0.5f, NormSpace * 0.5f);
                    
                    List<ContinentSeed> continents = new List<ContinentSeed>(continentsCount);
                    float baseAngle = Random.Range(0f, Mathf.PI * 2f);

                    for (int i = 0; i < continentsCount; i++)
                    {
                        float angle = baseAngle + (i * Mathf.PI * 2f / continentsCount) + Random.Range(-0.35f, 0.35f);
                        float distFromCenter = (continentsCount == 1)
                            ? Random.Range(0f, NormSpace * 0.1f)
                            : Random.Range(NormSpace * 0.18f, NormSpace * 0.34f);

                        Vector2 center = normCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distFromCenter;
                        center.x = Mathf.Clamp(center.x, NormSpace * 0.15f, NormSpace * 0.85f);
                        center.y = Mathf.Clamp(center.y, NormSpace * 0.15f, NormSpace * 0.85f);

                        float radius = (NormSpace * Random.Range(0.25f, 0.36f)) / Mathf.Pow(continentsCount, 0.32f);

                        float ridgeAngle = Random.Range(0f, Mathf.PI * 2f);
                        Vector2 dir = new Vector2(Mathf.Cos(ridgeAngle), Mathf.Sin(ridgeAngle));

                        continents.Add(new ContinentSeed
                        {
                            Center = center,
                            Radius = radius,
                            Dir = dir,
                            Perp = new Vector2(-dir.y, dir.x)
                        });
                    }
                    
                    List<Vector2> oceanBasins = new List<Vector2>(oceanicBasinsCount);
                    for (int i = 0; i < oceanicBasinsCount; i++)
                    {
                        float angle = baseAngle + ((i + 0.5f) * Mathf.PI * 2f / oceanicBasinsCount) + Random.Range(-0.4f, 0.4f);
                        float dist = Random.Range(NormSpace * 0.05f, NormSpace * 0.38f);
                        oceanBasins.Add(normCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist);
                    }
                    
                    for (int y = 0; y < height; y++)
                    {
                        float ny = (y / (float)Mathf.Max(1, height - 1)) * NormSpace;
                        float latFactor = Mathf.Abs((ny - normCenter.y) / normCenter.y);

                        for (int x = 0; x < width; x++)
                        {
                            int idx = y * width + x;
                            float nx = (x / (float)Mathf.Max(1, width - 1)) * NormSpace;

                            float warpX = (Mathf.PerlinNoise(nx * baseScale * 0.7f + offsetX, ny * baseScale * 0.7f + offsetY) - 0.5f) * (NormSpace * 0.22f);
                            float warpY = (Mathf.PerlinNoise(nx * baseScale * 0.7f + offsetX + 95f, ny * baseScale * 0.7f + offsetY + 95f) - 0.5f) * (NormSpace * 0.22f);
                            Vector2 samplePos = new Vector2(nx + warpX, ny + warpY);

                            float bestContinentInfluence = 0f;
                            float secondContinentInfluence = 0f;
                            int bestContIndex = 0;

                            for (int c = 0; c < continents.Count; c++)
                            {
                                Vector2 toTile = samplePos - continents[c].Center;

                                float along = Vector2.Dot(toTile, continents[c].Dir) * 0.75f;
                                float across = Vector2.Dot(toTile, continents[c].Perp) * 1.25f;
                                float effectiveDist = Mathf.Sqrt(along * along + across * across);

                                float inf = Mathf.Clamp01(1f - (effectiveDist / continents[c].Radius));
                                inf = Mathf.SmoothStep(0f, 1f, inf);

                                if (inf > bestContinentInfluence)
                                {
                                    secondContinentInfluence = bestContinentInfluence;
                                    bestContinentInfluence = inf;
                                    bestContIndex = c;
                                }
                                else if (inf > secondContinentInfluence)
                                {
                                    secondContinentInfluence = inf;
                                }
                            }

                            float straitDepression = 0f;
                            if (secondContinentInfluence > 0.05f)
                            {
                                float overlap = Mathf.Min(bestContinentInfluence, secondContinentInfluence);
                                straitDepression = overlap * 3.8f;
                            }

                            float basinDepression = 0f;
                            float basinRadius = NormSpace * 0.20f;
                            for (int b = 0; b < oceanBasins.Count; b++)
                            {
                                float d = Vector2.Distance(samplePos, oceanBasins[b]);
                                if (d < basinRadius)
                                {
                                    float bInf = 1f - (d / basinRadius);
                                    basinDepression = Mathf.Max(basinDepression, bInf * bInf * 3.2f);
                                }
                            }

                            float macroNoise = Mathf.PerlinNoise(nx * baseScale + offsetX, ny * baseScale + offsetY);
                            float detailNoise = Mathf.PerlinNoise(nx * baseScale * 2.4f + offsetX + 140f, ny * baseScale * 2.4f + offsetY + 140f);
                            float ridgeNoise = 1f - Mathf.Abs(Mathf.PerlinNoise(nx * baseScale * 1.6f + offsetX + 260f, ny * baseScale * 1.6f + offsetY + 260f) * 2f - 1f);

                            ContinentSeed activeCont = continents[bestContIndex];
                            float distToSpine = Mathf.Abs(Vector2.Dot(samplePos - activeCont.Center, activeCont.Perp));
                            float spineFactor = Mathf.Clamp01(1f - (distToSpine / (activeCont.Radius * 0.35f)));
                            float mountainBoost = bestContinentInfluence * spineFactor * ridgeNoise * ridgeStrength * 1.8f;

                            float baseLandHeight = 0.8f + (bestContinentInfluence * 5.2f);
                            float noiseHeight = ((macroNoise * 0.65f + detailNoise * 0.35f) - 0.45f) * 4.5f;

                            float edgeNormX = (nx - normCenter.x) / normCenter.x;
                            float edgeNormY = (ny - normCenter.y) / normCenter.y;
                            float edgeDist = Mathf.Clamp01(Mathf.Max(Mathf.Abs(edgeNormX), Mathf.Abs(edgeNormY)));
                            float edgeFalloff = Mathf.Pow(edgeDist, 3.5f) * 8.0f * falloffStrength;

                            float elevation = baseLandHeight + noiseHeight + mountainBoost - straitDepression - basinDepression - edgeFalloff;
                            elevation = Mathf.Clamp(elevation, 0f, 10f);

                            float moisture = Mathf.PerlinNoise(nx * baseScale * 1.2f + offsetX + 400f, ny * baseScale * 1.2f + offsetY + 400f);
                            bool isDesertBelt = (latFactor < 0.25f && moisture < 0.46f) || moisture < 0.22f;

                            TileType type = TileType.Grass;

                            if (elevation < waterThresh)
                            {
                                type = TileType.Water;
                            }
                            else if (elevation < sandThresh)
                            {
                                type = TileType.Sand;
                            }
                            else if (elevation >= rockThresh)
                            {
                                type = TileType.Rock;
                            }
                            else if (isDesertBelt && elevation < rockThresh - 0.6f)
                            {
                                type = TileType.Sand;
                            }
                            
                            int deterministicVariation = Mathf.Abs((x * 73856093) ^ (y * 19349663) ^ seed) % 15;

                            tiles[idx] = new TileData
                            {
                                Type = type,
                                Structure = TileStructureType.None,
                                Elevation = elevation,
                                Variation = deterministicVariation,
                                Owner = "CUL_NONE",
                                ResidentCultureId = "CUL_NONE",
                                CaptureProgress = 0f,
                                CapturingCultureId = "",
                                StoredFood = 0f,
                                StoredMaterials = 0f
                            };
                        }
                    }
                    
                    Random.InitState(seed + 777);
                    CarveRivers(tiles, width, height, riversCount, waterThresh, rockThresh);

                    Random.state = oldState;
                    return tiles;
                }

                private static void CarveRivers(TileData[] tiles, int width, int height, int riverCount, float waterThresh, float rockThresh)
                {
                    if (riverCount <= 0) return;

                    List<Vector2Int> peaks = new List<Vector2Int>();

                    for (int y = 3; y < height - 3; y++)
                    {
                        for (int x = 3; x < width - 3; x++)
                        {
                            TileData t = tiles[y * width + x];
                            if (t.Type == TileType.Rock && t.Elevation >= rockThresh)
                            {
                                peaks.Add(new Vector2Int(x, y));
                            }
                        }
                    }

                    if (peaks.Count == 0) return;

                    for (int r = 0; r < riverCount; r++)
                    {
                        int peakIndex = Mathf.FloorToInt(((r + 0.5f) / riverCount) * peaks.Count);
                        peakIndex = Mathf.Clamp(peakIndex, 0, peaks.Count - 1);

                        Vector2Int cur = peaks[peakIndex];
                        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

                        for (int step = 0; step < width + height; step++)
                        {
                            visited.Add(cur);
                            int curIdx = cur.y * width + cur.x;

                            if (step > 0)
                            {
                                TileData ct = tiles[curIdx];
                                ct.Type = TileType.Water;
                                ct.Elevation = Mathf.Min(ct.Elevation, waterThresh - 0.2f);
                                tiles[curIdx] = ct;

                                int[] bx = { -1, 1, 0, 0 };
                                int[] by = { 0, 0, -1, 1 };
                                for (int b = 0; b < 4; b++)
                                {
                                    int nx = cur.x + bx[b];
                                    int ny = cur.y + by[b];
                                    if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                                    {
                                        int bIdx = ny * width + nx;
                                        if (tiles[bIdx].Type == TileType.Sand && tiles[bIdx].Elevation >= waterThresh)
                                        {
                                            tiles[bIdx].Type = TileType.Grass;
                                        }
                                    }
                                }
                            }

                            Vector2Int next = cur;
                            float lowest = 999f;
                            bool hitSea = false;

                            int[] dx = { -1, 1, 0, 0 };
                            int[] dy = { -1, 0, -1, 1 };
                            dx[0] = -1; dy[0] = 0;
                            dx[1] = 1;  dy[1] = 0;
                            dx[2] = 0;  dy[2] = -1;
                            dx[3] = 0;  dy[3] = 1;

                            for (int d = 0; d < 4; d++)
                            {
                                int nx = cur.x + dx[d];
                                int ny = cur.y + dy[d];

                                if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                                {
                                    hitSea = true;
                                    break;
                                }

                                Vector2Int cand = new Vector2Int(nx, ny);
                                if (visited.Contains(cand)) continue;

                                TileData nt = tiles[ny * width + nx];
                                if (nt.Type == TileType.Water && step > 2)
                                {
                                    hitSea = true;
                                    break;
                                }

                                if (nt.Elevation < lowest)
                                {
                                    lowest = nt.Elevation;
                                    next = cand;
                                }
                            }

                            if (hitSea || next == cur) break;
                            cur = next;
                        }
                    }
                }
                
                public static Texture2D GeneratePreviewTexture(int size, MainConfig config, int seed)
                {
                    int actualSize = Mathf.Max(10, size);
                    TileData[] exactTiles = GetOrGenerateWorldData(actualSize, actualSize, config, seed);

                    Texture2D tex = new Texture2D(actualSize, actualSize, TextureFormat.RGBA32, false);
                    tex.filterMode = FilterMode.Point;
                    tex.wrapMode = TextureWrapMode.Clamp;

                    Color[] pixels = new Color[actualSize * actualSize];

                    for (int i = 0; i < exactTiles.Length; i++)
                    {
                        TileData t = exactTiles[i];
                        float elev01 = Mathf.Clamp01(t.Elevation / 10f);
                        float shade = Mathf.Lerp(0.75f, 1.1f, elev01);

                        Color baseCol = t.Type switch
                        {
                            TileType.Water => new Color(0.14f, 0.36f, 0.68f),
                            TileType.Sand => new Color(0.76f, 0.70f, 0.44f),
                            TileType.Rock => new Color(0.46f, 0.46f, 0.48f),
                            _ => new Color(0.26f, 0.54f, 0.16f)
                        };

                        pixels[i] = new Color(baseCol.r * shade, baseCol.g * shade, baseCol.b * shade, 1f);
                    }

                    tex.SetPixels(pixels);
                    tex.Apply();

                    return tex;
                }
            }
        }
    }
}