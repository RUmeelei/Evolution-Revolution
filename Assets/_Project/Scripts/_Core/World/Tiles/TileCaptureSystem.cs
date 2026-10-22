using UnityEngine;

namespace ER
{
    namespace World
    {
        namespace Tiles
        {
            using Core;
            using Unit;
            using Configs;
            using Culture;
            using Politics.External.Diplomacy;

            public static class TileCaptureSystem
            {
                public static void ProcessCapture(TileData tile, int x, int y, Unit unit, TileManager tileManager, MainConfig mainConfig, float delta)
                {
                    if (unit == null || !unit.IsAlive) return;
                    
                    if (tile.Owner == unit.CultureId) return;

                    var diplomacy = DiplomacyManager.Instance;

                    if (!string.IsNullOrEmpty(tile.Owner) && tile.Owner != "CUL_NONE")
                    {
                        if (CultureAISystem.IsInTruce(unit.CultureId, tile.Owner)) return;

                        bool atWar = diplomacy != null && diplomacy.IsAtWar(unit.CultureId, tile.Owner);

                        if (!atWar)
                        {
                            if (unit.HasPlayerOrder && diplomacy != null)
                            {
                                diplomacy.AddBorderTension(unit.CultureId, tile.Owner, 12f * delta);
                            }

                            return;
                        }
                    }

                    bool isOwnerCapital = false;
                    var cultureManager = CoreManager.CultureManager;

                    if (cultureManager != null && tile.Owner != "CUL_NONE")
                    {
                        var ownerCulture = cultureManager.GetCulture(tile.Owner);
                        if (ownerCulture != null && ownerCulture.CultureCapital.x == x && ownerCulture.CultureCapital.y == y)
                        {
                            isOwnerCapital = true;
                        }
                    }

                    float resistance = tile.GetCaptureResistance(isOwnerCapital);
                    float rate = (mainConfig.CaptureRatePerSecond / resistance) * delta;

                    ApplyInfluence(tile, x, y, unit.CultureId, rate, tileManager, isDirectUnitCapture: true);
                }

                public static void ApplyInfluence(TileData tile, int x, int y, string influencingCultureId, float amount, TileManager tileManager, bool isDirectUnitCapture = false)
                {
                    if (string.IsNullOrEmpty(influencingCultureId) || tile.Owner == influencingCultureId || amount <= 0.001f) return;

                    if (!string.IsNullOrEmpty(tile.Owner) && tile.Owner != "CUL_NONE")
                    {
                        if (CultureAISystem.IsInTruce(influencingCultureId, tile.Owner)) return;
                    }

                    var diplomacy = DiplomacyManager.Instance;

                    if (string.IsNullOrEmpty(tile.CapturingCultureId))
                    {
                        tile.CapturingCultureId = influencingCultureId;
                        tile.CaptureProgress = amount;
                    }
                    else if (tile.CapturingCultureId == influencingCultureId)
                    {
                        tile.CaptureProgress += amount;
                    }
                    else
                    {
                        if (diplomacy != null && !CultureAISystem.IsInTruce(influencingCultureId, tile.CapturingCultureId))
                        {
                            float tensionFactor = isDirectUnitCapture ? 1.2f : 0.25f;
                            diplomacy.AddBorderTension(influencingCultureId, tile.CapturingCultureId, tensionFactor * amount);
                        }

                        tile.CaptureProgress -= amount;

                        if (tile.CaptureProgress <= 0f)
                        {
                            tile.CaptureProgress = 0f;
                            tile.CapturingCultureId = "";
                        }
                    }

                    if (tile.CaptureProgress >= 100f)
                    {
                        CompleteCapture(x, y, tile, tileManager);
                    }
                    else
                    {
                        tileManager.UpdateTileData(x, y, tile);
                    }
                }
                
                private static void CompleteCapture(int x, int y, TileData tile, TileManager tileManager)
                {
                    string previousOwner = tile.Owner;
                    string newOwner = tile.CapturingCultureId;

                    if (!string.IsNullOrEmpty(previousOwner) && previousOwner != "CUL_NONE" && previousOwner != newOwner)
                    {
                        CultureAISystem.RegisterWarTileCaptured(newOwner, previousOwner, tileManager);
                    }

                    if (tile.Structure == TileStructureType.LogisticsHub && tile.TotalStored > 0f)
                    {
                        var playerManager = CoreManager.PlayerManager;
                        var winnerPlayer = playerManager != null ? playerManager.GetPlayerForCulture(newOwner) : null;

                        if (winnerPlayer != null)
                        {
                            winnerPlayer.GatherResource("Fruits", tile.StoredFood);
                            winnerPlayer.GatherResource("Iron", tile.StoredMaterials);
                        }

                        tile.StoredFood = 0f;
                        tile.StoredMaterials = 0f;
                    }
                    
                    if (string.IsNullOrEmpty(tile.ResidentCultureId) || tile.ResidentCultureId == "CUL_NONE")
                    {
                        tile.ResidentCultureId = newOwner;
                    }

                    tile.Owner = newOwner;
                    tile.CaptureProgress = 0f;
                    tile.CapturingCultureId = "";

                    tileManager.UpdateTileData(x, y, tile);
                }
                
                public static void DecayCapture(TileData tile, int x, int y, TileManager tileManager, MainConfig mainConfig, float decayInterval)
                {
                    if (string.IsNullOrEmpty(tile.CapturingCultureId)) return;

                    tile.CaptureProgress -= mainConfig.CaptureDecayRatePerSecond * decayInterval;

                    if (tile.CaptureProgress <= 0f)
                    {
                        tile.CaptureProgress = 0f;
                        tile.CapturingCultureId = "";
                    }

                    tileManager.UpdateTileData(x, y, tile);
                }
            }
        }
    }
}