using UnityEngine;
using System.Collections.Generic;

namespace ER
{
    namespace Unit
    {
        using Configs;
        using Culture;
        using Players;
        using World.Tiles;
        
        public static class UnitAISystem
        {
            public static void ProcessUnit(Unit unit, AIConfig aiConfig, UnitSpatialGrid grid, CultureManager cultureManager, PlayerManager playerManager, TileManager tileManager, float worldWidth, float worldHeight)
            {
                if (unit == null || !unit.IsAlive) return;
                
                var player = playerManager.GetPlayerForCulture(unit.CultureId);

                if (player != null && !player.PlayerAI) return;
                
                if (ShouldFlee(unit, aiConfig))
                {
                    Flee(unit, aiConfig, grid, worldWidth, worldHeight);

                    unit.AIState = UnitAIState.Fleeing;

                    return;
                }
                
                if (unit.CanAttack)
                {
                    Unit enemy = FindEnemyToAttack(unit, aiConfig, grid);

                    if (enemy != null)
                    {
                        Attack(unit, enemy);

                        unit.AIState = UnitAIState.Attacking;

                        return;
                    }
                }
                
                if (unit.AIState == UnitAIState.Fleeing || unit.AIState == UnitAIState.Attacking)
                {
                    Regroup(unit, aiConfig, grid, cultureManager);

                    unit.AIState = UnitAIState.Regrouping;

                    return;
                }

                WanderOrMoveToRally(unit, aiConfig, cultureManager, tileManager, worldWidth, worldHeight);
            }
            
            private static bool ShouldFlee(Unit unit, AIConfig aiConfig)
            {
                return unit.GetCurrentHealthPercent() < aiConfig.FleeHealthThreshold;
            }
            
            private static void Flee(Unit unit, AIConfig aiConfig, UnitSpatialGrid grid,
                float worldWidth, float worldHeight)
            {
                var candidates = grid.GetUnitsInRadius(unit.Position, aiConfig.AllySearchRadius);

                Unit nearestEnemy = UnitCombatSystem.FindNearestEnemy(unit, candidates);

                Vector2 fleeTarget;

                if (nearestEnemy != null)
                {
                    Vector2 awayDirection = (unit.Position - nearestEnemy.Position).normalized;

                    fleeTarget = unit.Position + awayDirection * aiConfig.FleeDistance;
                }
                else
                {
                    unit.Stop();

                    return;
                }
                
                fleeTarget.x = Mathf.Clamp(fleeTarget.x, 0, worldWidth);
                fleeTarget.y = Mathf.Clamp(fleeTarget.y, 0, worldHeight);

                unit.ClearAttackTarget();

                unit.SetTarget(fleeTarget);
            }

            private static Unit FindEnemyToAttack(Unit unit, AIConfig aiConfig, UnitSpatialGrid grid)
            {
                if (unit.AttackTarget != null)
                {
                    if (unit.AttackTarget.IsAlive) return unit.AttackTarget;

                    unit.ClearAttackTarget();
                }
                
                var candidates = grid.GetUnitsInRadius(unit.Position, aiConfig.AllySearchRadius);
                return UnitCombatSystem.FindNearestEnemy(unit, candidates);
            }
            
            private static void Attack(Unit unit, Unit enemy)
            {
                unit.AttackTarget = enemy;

                float distToTarget = Vector2.Distance(unit.Position, enemy.Position);

                if (distToTarget > unit.AttackRange)
                {
                    unit.SetTarget(enemy.Position);
                }
                else
                {
                    unit.Stop();
                }
            }

            private static void Regroup(Unit unit, AIConfig aiConfig, UnitSpatialGrid grid, CultureManager cultureManager)
            {
                var candidates = grid.GetUnitsInRadius(unit.Position, aiConfig.AllySearchRadius);

                Unit nearestAlly = null;

                float minDist = float.MaxValue;

                for (int i = 0; i < candidates.Count; i++)
                {
                    var candidate = candidates[i];

                    if (candidate == unit || !candidate.IsAlive) continue;

                    if (candidate.CultureId != unit.CultureId) continue;

                    float dist = Vector2.Distance(unit.Position, candidate.Position);

                    if (dist < minDist)
                    {
                        minDist = dist;

                        nearestAlly = candidate;
                    }
                }

                if (nearestAlly != null && minDist > 3f)
                {
                    unit.SetTarget(nearestAlly.Position);
                }
                else
                {
                    unit.AIState = UnitAIState.Wandering;
                }
            }

            private static void WanderOrMoveToRally(Unit unit, AIConfig aiConfig, CultureManager cultureManager, TileManager tileManager, float worldWidth, float worldHeight)
            {
                if (Random.value > 0.1f) return;

                var culture = cultureManager.GetCulture(unit.CultureId);

                if (culture == null) return;

                Vector2 rallyPoint = culture.RallyPoint;

                if (rallyPoint != Vector2.zero)
                {
                    float distToRally = Vector2.Distance(unit.Position, rallyPoint);

                    if (distToRally > aiConfig.WanderRadius)
                    {
                        Vector2 offset = new Vector2(Random.Range(-2f, 2f), Random.Range(-2f, 2f));

                        Vector2 target = rallyPoint + offset;

                        target.x = Mathf.Clamp(target.x, 0, worldWidth);
                        target.y = Mathf.Clamp(target.y, 0, worldHeight);

                        unit.SetTarget(target);

                        unit.AIState = UnitAIState.Wandering;
                    }
                    else
                    {
                        Vector2 captureTarget = FindNeutralTileToCapture(unit, tileManager);

                        if (captureTarget != Vector2.zero)
                        {
                            unit.SetTarget(captureTarget);

                            unit.AIState = UnitAIState.Wandering;
                        }
                        else
                        {
                            Vector2 target = unit.Position + new Vector2(Random.Range(-aiConfig.WanderRadius, aiConfig.WanderRadius), Random.Range(-aiConfig.WanderRadius, aiConfig.WanderRadius));

                            target.x = Mathf.Clamp(target.x, 0, worldWidth);
                            target.y = Mathf.Clamp(target.y, 0, worldHeight);

                            unit.SetTarget(target);

                            unit.AIState = UnitAIState.Wandering;
                        }
                    }
                }
                else
                {
                    Vector2 target = unit.Position + new Vector2(Random.Range(-aiConfig.WanderRadius, aiConfig.WanderRadius), Random.Range(-aiConfig.WanderRadius, aiConfig.WanderRadius));

                    target.x = Mathf.Clamp(target.x, 0, worldWidth);
                    target.y = Mathf.Clamp(target.y, 0, worldHeight);

                    unit.SetTarget(target);

                    unit.AIState = UnitAIState.Wandering;
                }
            }
            
            private static Vector2 FindNeutralTileToCapture(Unit unit, TileManager tileManager)
            {
                Vector2Int unitTilePos = tileManager.WorldToTilePos(unit.Position);
                
                int searchRadius = 3;

                Vector2 bestTarget = Vector2.zero;
                float bestDist = float.MaxValue;

                for (int dy = -searchRadius; dy <= searchRadius; dy++)
                {
                    for (int dx = -searchRadius; dx <= searchRadius; dx++)
                    {
                        int nx = unitTilePos.x + dx;
                        int ny = unitTilePos.y + dy;

                        if (nx < 0 || nx >= tileManager.WorldWidth) continue;
                        if (ny < 0 || ny >= tileManager.WorldHeight) continue;

                        var tile = tileManager.GetTile(nx, ny);
                        
                        if (tile.Type == TileType.Water) continue;

                        if (tile.Owner != "CUL_NONE") continue;

                        Vector2 tileCenter = tileManager.GetTileCenter(nx, ny);

                        float dist = Vector2.Distance(unit.Position, tileCenter);

                        if (dist < bestDist)
                        {
                            bestDist = dist;

                            bestTarget = tileCenter;
                        }
                    }
                }

                return bestTarget;
            }
        }
    }
}