using System;
using UnityEngine;

namespace ER
{
    namespace World
    {
        namespace Tiles
        {
            public enum WorldObjectType
            {
                Camp = 0,
                TownHall = 1,
                Warehouse = 2,
                Fortress = 3,
                Mine = 4, 
                Workshop = 5,
            }

            [Serializable]
            public class WorldAnchorObject
            {
                public string ObjectId;
                public WorldObjectType ObjectType;

                public Vector2 Position;
                public float FootprintRadius = 0.18f;

                public string DeFactoOwnerId = "CUL_NONE";
                public string DeJureOwnerId = "CUL_NONE";

                public bool IsUnderConstruction = true;
                public float RequiredWood;
                public float RequiredStone;
                public float DeliveredWood;
                public float DeliveredStone;
                public float BuildProgress;

                public float Integrity = 100f;
                public float ControlPower = 0f;

                public string CapturingOwnerId = "";
                public float CaptureProgress = 0f;

                public float StoredFood;
                public float StoredWood;
                public float StoredStone;
                public float StoredIron;

                public bool IsOperational => !IsUnderConstruction && Integrity > 15f;

                public int GetMaxControlRadiusInTiles()
                {
                    if (!IsOperational) return 0;

                    int baseRadius = ObjectType switch
                    {
                        WorldObjectType.Camp => 2,
                        WorldObjectType.TownHall => 4,
                        WorldObjectType.Fortress => 3,
                        WorldObjectType.Mine => 2,
                        WorldObjectType.Warehouse => 2,
                        _ => 1
                    };

                    float powerFactor = Mathf.Clamp01(ControlPower / 50f);
                    return Mathf.Max(1, Mathf.RoundToInt(baseRadius * powerFactor));
                }

                public bool NeedsConstructionMaterials()
                {
                    return IsUnderConstruction && (DeliveredWood < RequiredWood || DeliveredStone < RequiredStone);
                }

                public void DeliverMaterial(string item, float amount)
                {
                    if (!IsUnderConstruction) return;

                    if (item == "Wood")
                    {
                        DeliveredWood = Mathf.Min(RequiredWood, DeliveredWood + amount);
                    }
                    else if (item == "Stone")
                    {
                        DeliveredStone = Mathf.Min(RequiredStone, DeliveredStone + amount);
                    }
                }

                public void AdvanceConstruction(float workAmount)
                {
                    if (!IsUnderConstruction) return;

                    float matRatio = (RequiredWood + RequiredStone) > 0.01f
                        ? (DeliveredWood + DeliveredStone) / (RequiredWood + RequiredStone)
                        : 1f;

                    float maxAllowedProgress = Mathf.Max(25f, matRatio * 100f);
                    BuildProgress = Mathf.Min(maxAllowedProgress, BuildProgress + workAmount);

                    if (BuildProgress >= 100f)
                    {
                        IsUnderConstruction = false;
                        Integrity = 100f;
                        ControlPower = 75f;
                    }
                }
            }
        }
    }
}