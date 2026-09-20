using UnityEngine;
using System.Collections.Generic;

namespace ER
{
    namespace Culture
    {
        using World.Tiles;
        using Players;
        using Unit;

        public static class CultureAISystem
        {
            public static void ProcessCulture(CultureData culture, TileManager tileManager, PlayerManager playerManager, UnitManager unitManager)
            {
                if (culture == null || culture.CultureDefeated) return;
                
                var player = playerManager.GetPlayerForCulture(culture.CultureId);

                if (player != null && !player.PlayerAI) return;
                

                Vector2 invasionPoint = FindInvasionPoint(culture, tileManager, unitManager);

                if (invasionPoint != Vector2.zero)
                {
                    culture.RallyPoint = invasionPoint;

                    return;
                }
                
                Vector2 rallyPoint = FindExpansionTarget(culture, tileManager);

                if (rallyPoint != Vector2.zero)
                {
                    culture.RallyPoint = rallyPoint;
                }
                else
                {
                    culture.RallyPoint = tileManager.GetTileCenter(culture.CultureCapital.x, culture.CultureCapital.y);
                }
            }
            
            private static Vector2 FindExpansionTarget(CultureData culture, TileManager tileManager)
            {
                int width = tileManager.WorldWidth;
                int height = tileManager.WorldHeight;
                
                var candidateTiles = new List<Vector2Int>();

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        var tile = tileManager.GetTile(x, y);

                        if (tile.Type == TileType.Water) continue;

                        if (tile.Owner != "CUL_NONE") continue;
                        
                        if (BordersOurTerritory(x, y, culture.CultureId, tileManager))
                        {
                            candidateTiles.Add(new Vector2Int(x, y));
                        }
                    }
                }

                if (candidateTiles.Count == 0) return Vector2.zero;

                Vector2 capital = tileManager.GetTileCenter(culture.CultureCapital.x, culture.CultureCapital.y);
                
                candidateTiles.Sort((a, b) => {float distA = Vector2.Distance(tileManager.GetTileCenter(a.x, a.y), capital); float distB = Vector2.Distance(tileManager.GetTileCenter(b.x, b.y), capital); return distA.CompareTo(distB);});
                
                int clusterSize = Mathf.Min(5, candidateTiles.Count);

                var cluster = candidateTiles.GetRange(0, clusterSize);
                
                Vector2 center = Vector2.zero;

                foreach (var pos in cluster)
                {
                    center += tileManager.GetTileCenter(pos.x, pos.y);
                }

                center /= clusterSize;

                return center;
            }
            
            private static bool BordersOurTerritory(int x, int y, string cultureId, TileManager tileManager)
            {
                int[] dx = { -1, 1, 0, 0 };
                int[] dy = { 0, 0, -1, 1 };

                for (int i = 0; i < 4; i++)
                {
                    int nx = x + dx[i];
                    int ny = y + dy[i];

                    if (nx < 0 || nx >= tileManager.WorldWidth) continue;
                    if (ny < 0 || ny >= tileManager.WorldHeight) continue;

                    var neighbor = tileManager.GetTile(nx, ny);
                    
                    if (neighbor.Owner == cultureId) return true;
                }

                return false;
            }
            
            private static Vector2 FindInvasionPoint(CultureData culture, TileManager tileManager, UnitManager unitManager)
            {
                var allUnits = unitManager.GetAllUnits();

                Vector2 invasionSum = Vector2.zero;

                int invasionCount = 0;

                foreach (var unit in allUnits)
                {
                    if (!unit.IsAlive) continue;
                    
                    if (unit.CultureId == culture.CultureId) continue;
                    
                    Vector2Int tilePos = tileManager.WorldToTilePos(unit.Position);

                    var tile = tileManager.GetTile(tilePos.x, tilePos.y);

                    if (tile.Owner == culture.CultureId)
                    {
                        invasionSum += unit.Position;

                        invasionCount++;
                    }
                }

                if (invasionCount == 0) return Vector2.zero;
                
                return invasionSum / invasionCount;
            }
        }
    }
}