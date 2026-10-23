using UnityEngine;

namespace ER
{
    namespace World
    {
        namespace Tiles
        {
            [System.Serializable]
            public struct TileData
            {
                public TileType Type; 

                public TileStructureType Structure;

                public TileRoadType Road;

                public TileCommLineType CommLine;

                public float RoadTraffic;

                public float Elevation;

                public int Variation;
                
                public string DeFactoOwner;
                
                public string DeJureOwner;
                
                public string ResidentCultureId;
                
                public float LocalDevastation;
                
                public string Owner
                {
                    get => DeFactoOwner;
                    set => DeFactoOwner = value;
                }

                public bool IsOccupied =>
                    !string.IsNullOrEmpty(DeFactoOwner) && DeFactoOwner != "CUL_NONE" &&
                    !string.IsNullOrEmpty(DeJureOwner) && DeJureOwner != "CUL_NONE" &&
                    DeFactoOwner != DeJureOwner;
                
                public float CaptureProgress;
                
                public string CapturingCultureId;

                public float StoredFood;

                public float StoredMaterials;

                public float TotalStored => StoredFood + StoredMaterials;

                public float GetInfluenceMultiplier(bool isCapital)
                {
                    float mult = isCapital ? 3.0f : 1.0f;

                    float structMult = Structure switch
                    {
                        TileStructureType.Outpost => 3.0f,
                        TileStructureType.Settlement => 2.5f,
                        TileStructureType.LogisticsHub => 1.25f,
                        _ => 1.0f
                    };

                    float infraMult = Road switch
                    {
                        TileRoadType.DirtPath => 1.05f,
                        TileRoadType.PavedRoad => 1.15f,
                        TileRoadType.Railway => 1.30f,
                        _ => 1.0f
                    };

                    if (CommLine != TileCommLineType.None)
                    {
                        infraMult += (int)CommLine * 0.08f;
                    }

                    return mult * structMult * infraMult;
                }

                public float GetCaptureResistance(bool isCapital)
                {
                    float res = isCapital ? 2.5f : 1.0f;

                    return Structure switch
                    {
                        TileStructureType.Outpost => res * 3.0f,
                        TileStructureType.Settlement => res * 2.0f,
                        TileStructureType.LogisticsHub => res * 1.25f,
                        _ => res
                    };
                }

                public float GetMovementSpeedMultiplier(float waterSpeed = 0.33f, float rockSpeed = 0.67f, float sandSpeed = 0.85f, float dirtBonus = 1.20f, float pavedBonus = 1.45f, float railBonus = 2.0f)
                {
                    if (Type == TileType.Water) return waterSpeed;

                    float terrainFactor = Type switch
                    {
                        TileType.Rock => rockSpeed,
                        TileType.Sand => sandSpeed,
                        _ => 1.0f
                    };

                    return Road switch
                    {
                        TileRoadType.DirtPath => Mathf.Max(terrainFactor * dirtBonus, Mathf.Lerp(terrainFactor, 1.0f, 0.5f) * dirtBonus),
                        TileRoadType.PavedRoad => pavedBonus,
                        TileRoadType.Railway => railBonus,
                        _ => terrainFactor
                    };
                }

                public float GetPathfindingCost()
                {
                    if (Type == TileType.Water) return 5.0f;

                    float baseCost = Type switch
                    {
                        TileType.Rock => 1.8f,
                        TileType.Sand => 1.2f,
                        _ => 1.0f
                    };

                    return Road switch
                    {
                        TileRoadType.DirtPath => baseCost * 0.75f,
                        TileRoadType.PavedRoad => 0.55f,
                        TileRoadType.Railway => 0.35f,
                        _ => baseCost
                    };
                }
            }

            public enum TileType
            {
                None = 0,

                Water = 10,
                Grass = 11,
                Sand = 12,
                Rock = 13,
            }

            public enum TileStructureType
            {
                None = 0,
                Outpost = 1,
                LogisticsHub = 2,
                Settlement = 3,
            }

            public enum TileRoadType
            {
                None = 0,
                DirtPath = 1,
                PavedRoad = 2,
                Railway = 3,
            }

            public enum TileCommLineType
            {
                None = 0,
                Watchtowers = 1,
                TelegraphLine = 2,
                TelephoneLine = 3,
            }
        }
    }
}