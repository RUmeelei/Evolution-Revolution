using UnityEngine;

namespace ER
{
    namespace World
    {
        namespace Tiles
        {
            using Unit;
            using Configs;

            public static class TileCaptureSystem
            {
                public static void ProcessCapture(TileData tile, int x, int y, Unit unit, TileManager tileManager, MainConfig mainConfig, float delta)
                {
                    if (unit == null || !unit.IsAlive) return;
                    
                    if (tile.Owner == unit.CultureId) return;
                    
                    if (!string.IsNullOrEmpty(tile.CapturingCultureId) && tile.CapturingCultureId != unit.CultureId)
                    {
                        tile.CapturingCultureId = unit.CultureId;

                        tile.CaptureProgress = 0f;
                    }
                    
                    else if (string.IsNullOrEmpty(tile.CapturingCultureId))
                    {
                        tile.CapturingCultureId = unit.CultureId;

                        tile.CaptureProgress = 0f;
                    }
                    
                    tile.CaptureProgress += mainConfig.CaptureRatePerSecond * delta;
                    
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
                    string oldOwner = tile.Owner;
                    string newOwner = tile.CapturingCultureId;

                    tile.Owner = newOwner;

                    tile.CaptureProgress = 0f;

                    tile.CapturingCultureId = "";

                    tileManager.UpdateTileData(x, y, tile);
                }
                
                public static void DecayCapture(TileData tile, int x, int y, TileManager tileManager, MainConfig mainConfig, float decayInterval)
                {
                    if (string.IsNullOrEmpty(tile.CapturingCultureId)) return;

                    // decayInterval - реальное время между вызовами (обычно 0.5 сек)
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