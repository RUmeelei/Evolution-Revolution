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

                var culture = cultureManager.GetCulture(unit.CultureId);

                if (culture == null) return;
                
                if (unit.HasPlayerOrder)
                {
                    ExecutePlayerOrder(unit);

                    return;
                }
                
                if (ShouldFlee(unit, aiConfig))
                {
                    Flee(unit, aiConfig, grid, worldWidth, worldHeight);

                    unit.AIState = UnitAIState.Fleeing;

                    return;
                }
                
                switch (culture.Stance)
                {
                    case CultureStance.Aggressive:
                        AggressiveBehavior(unit, aiConfig, grid, cultureManager, tileManager, worldWidth, worldHeight);

                        break;
                    case CultureStance.Defensive:
                        DefensiveBehavior(unit, aiConfig, grid, cultureManager, tileManager, worldWidth, worldHeight);

                        break;
                    case CultureStance.Passive:
                        PassiveBehavior(unit, aiConfig, grid, cultureManager, tileManager, worldWidth, worldHeight);

                        break;
                }
            }
            
            private static void AggressiveBehavior(Unit unit, AIConfig aiConfig, UnitSpatialGrid grid, CultureManager cultureManager, TileManager tileManager, float worldWidth, float worldHeight)
            {
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
                
                WanderOrMoveToRally(unit, aiConfig, cultureManager, tileManager, grid, worldWidth, worldHeight);
            }
            
            private static void DefensiveBehavior(Unit unit, AIConfig aiConfig, UnitSpatialGrid grid, CultureManager cultureManager, TileManager tileManager, float worldWidth, float worldHeight)
            {
                if (unit.CanAttack)
                {
                    Unit enemy = FindEnemyOnOurTerritory(unit, aiConfig, grid, tileManager);

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
                
                MoveToRallyWithoutCapture(unit, aiConfig, cultureManager, tileManager, worldWidth, worldHeight);
            }
            
            private static void PassiveBehavior(Unit unit, AIConfig aiConfig, UnitSpatialGrid grid, CultureManager cultureManager, TileManager tileManager, float worldWidth, float worldHeight)
            {
                if (unit.AIState == UnitAIState.Fleeing || unit.AIState == UnitAIState.Attacking)
                {
                    Regroup(unit, aiConfig, grid, cultureManager);
                    unit.AIState = UnitAIState.Regrouping;
                    return;
                }
                
                MoveToRallyWithoutCapture(unit, aiConfig, cultureManager, tileManager, worldWidth, worldHeight);
            }

            private static bool ShouldFlee(Unit unit, AIConfig aiConfig)
            {
                return unit.GetCurrentHealthPercent() < aiConfig.FleeHealthThreshold;
            }
            
            private static void Flee(Unit unit, AIConfig aiConfig, UnitSpatialGrid grid, float worldWidth, float worldHeight)
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
            
            private static Unit FindEnemyOnOurTerritory(Unit unit, AIConfig aiConfig, UnitSpatialGrid grid, TileManager tileManager)
            {
                if (unit.AttackTarget != null)
                {
                    if (unit.AttackTarget.IsAlive)
                    {
                        Vector2Int enemyTilePos = tileManager.WorldToTilePos(unit.AttackTarget.Position);

                        var enemyTile = tileManager.GetTile(enemyTilePos.x, enemyTilePos.y);
                        
                        if (enemyTile.Owner == unit.CultureId)
                        {
                            return unit.AttackTarget;
                        }
                        else
                        {
                            unit.ClearAttackTarget();
                        }
                    }
                    else
                    {
                        unit.ClearAttackTarget();
                    }
                }
                
                var candidates = grid.GetUnitsInRadius(unit.Position, aiConfig.AllySearchRadius);
                
                Unit nearestOnOurTerritory = null;

                float minDist = float.MaxValue;

                for (int i = 0; i < candidates.Count; i++)
                {
                    var candidate = candidates[i];
                    
                    if (!UnitCombatSystem.IsEnemy(unit, candidate)) continue;

                    Vector2Int candidateTilePos = tileManager.WorldToTilePos(candidate.Position);

                    var candidateTile = tileManager.GetTile(candidateTilePos.x, candidateTilePos.y);
                    
                    if (candidateTile.Owner != unit.CultureId) continue;

                    float dist = Vector2.Distance(unit.Position, candidate.Position);
                    if (dist < minDist)
                    {
                        minDist = dist;

                        nearestOnOurTerritory = candidate;
                    }
                }

                return nearestOnOurTerritory;
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
            
            private static void WanderOrMoveToRally(Unit unit, AIConfig aiConfig, CultureManager cultureManager, TileManager tileManager, UnitSpatialGrid grid, float worldWidth, float worldHeight)
            {
                Vector2Int currentTilePos = tileManager.GetClosestTilePos(unit.Position);

                var currentTile = tileManager.GetTile(currentTilePos.x, currentTilePos.y);

                if (currentTile.CapturingCultureId == unit.CultureId && currentTile.CaptureProgress > 0f)
                {
                    Vector2 tileCenter = tileManager.GetTileCenter(currentTilePos.x, currentTilePos.y);

                    float distToCenter = Vector2.Distance(unit.Position, tileCenter);
                    
                    if (distToCenter < 0.1f)
                    {
                        unit.Stop();

                        return;
                    }
                }

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
                        Vector2 captureTarget = FindNeutralTileToCapture(unit, tileManager, grid);

                        if (captureTarget != Vector2.zero)
                        {
                            unit.SetTarget(captureTarget);

                            unit.AIState = UnitAIState.Wandering;
                        }
                        else
                        {
                            Vector2 target = rallyPoint + new Vector2(Random.Range(-2f, 2f), Random.Range(-2f, 2f));

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
            
            private static void MoveToRallyWithoutCapture(Unit unit, AIConfig aiConfig, CultureManager cultureManager, TileManager tileManager, float worldWidth, float worldHeight)
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
                        Vector2 target = unit.Position + new Vector2(Random.Range(-aiConfig.WanderRadius, aiConfig.WanderRadius), Random.Range(-aiConfig.WanderRadius, aiConfig.WanderRadius));

                        target.x = Mathf.Clamp(target.x, 0, worldWidth);
                        target.y = Mathf.Clamp(target.y, 0, worldHeight);

                        unit.SetTarget(target);

                        unit.AIState = UnitAIState.Wandering;
                    }
                }
                else
                {
                    unit.Stop();
                }
            }
            
            private static Vector2 FindNeutralTileToCapture(Unit unit, TileManager tileManager, UnitSpatialGrid grid)
            {
                Vector2Int unitTilePos = tileManager.GetClosestTilePos(unit.Position);
                
                int searchRadius = 10;

                var validTiles = new List<Vector2>();
            
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
                        
                        if (!string.IsNullOrEmpty(tile.CapturingCultureId) && tile.CapturingCultureId != unit.CultureId)
                        {
                            continue;
                        }
            
                        Vector2 tileCenter = tileManager.GetTileCenter(nx, ny);
                        
                        var unitsOnTile = grid.GetUnitsInRadius(tileCenter, 1f);
                        
                        bool hasAllyOnTile = false;

                        for (int i = 0; i < unitsOnTile.Count; i++)
                        {
                            var u = unitsOnTile[i];

                            if (u == unit) continue;

                            if (!u.IsAlive) continue;

                            if (u.CultureId != unit.CultureId) continue;
                            
                            hasAllyOnTile = true;

                            break;
                        }
                        
                        if (hasAllyOnTile) continue;
            
                        validTiles.Add(tileCenter);
                    }
                }
            
                if (validTiles.Count == 0) return Vector2.zero;
            
                validTiles.Sort((a, b) =>
                {
                    float distA = Vector2.Distance(unit.Position, a);
                    float distB = Vector2.Distance(unit.Position, b);

                    return distA.CompareTo(distB);
                });
            
                int poolSize = Mathf.Min(3, validTiles.Count);

                int randomIndex = Random.Range(0, poolSize);
            
                return validTiles[randomIndex];
            }
            
            private static void ExecutePlayerOrder(Unit unit)
            {
                switch (unit.OrderType)
                {
                    case Unit.PlayerOrderType.MoveTo:
                        ExecuteMoveOrder(unit);

                        break;

                    case Unit.PlayerOrderType.AttackUnit:
                        ExecuteAttackOrder(unit);

                        break;

                    default:
                        unit.ClearPlayerOrder();

                        break;
                }
            }
            
            private static void ExecuteMoveOrder(Unit unit)
            {
                float distToTarget = Vector2.Distance(unit.Position, unit.OrderTargetPosition);
                
                if (distToTarget < 0.1f && unit.Speed < 0.1f)
                {
                    unit.ClearPlayerOrder();

                    unit.AIState = UnitAIState.Idle;

                    return;
                }
                
                unit.SetTarget(unit.OrderTargetPosition);

                unit.AIState = UnitAIState.Wandering; 
            }
            
            private static void ExecuteAttackOrder(Unit unit)
            {
                var target = unit.OrderTargetUnit;
                
                if (target == null || !target.IsAlive)
                {
                    unit.ClearAttackTarget();

                    unit.ClearPlayerOrder();

                    unit.AIState = UnitAIState.Idle;

                    return;
                }
                
                unit.OrderTargetPosition = target.Position;

                unit.AttackTarget = target;

                float distToTarget = Vector2.Distance(unit.Position, target.Position);

                if (distToTarget <= unit.AttackRange)
                {
                    unit.Stop();

                    UnitCombatSystem.PerformAttack(unit, target);
                }
                else
                {
                    unit.SetTarget(target.Position);
                }

                unit.AIState = UnitAIState.Attacking;
            }
        }
    }
}