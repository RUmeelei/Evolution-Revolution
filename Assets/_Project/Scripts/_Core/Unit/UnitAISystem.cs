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
        using Politics.External.Diplomacy;
        
        public static class UnitAISystem
        {
            public static void ProcessUnit(Unit unit, AIConfig aiConfig, CombatConfig combatConfig, UnitSpatialGrid grid, CultureManager cultureManager, PlayerManager playerManager, TileManager tileManager, float worldWidth, float worldHeight)
            {
                if (unit == null || !unit.IsAlive) return;
                
                if (unit.HasPlayerOrder)
                {
                    ExecutePlayerOrder(unit);

                    return;
                }

                bool isProducer = unit is Human h && h.Group == PopulationGroup.Producers;

                if (isProducer)
                {
                    if (ShouldFlee(unit, aiConfig))
                    {
                        Flee(unit, aiConfig, grid, worldWidth, worldHeight);
                        unit.AIState = UnitAIState.Fleeing;
                        return;
                    }

                    ProcessProducerLogistics(unit, aiConfig, cultureManager, playerManager, tileManager);

                    return;
                }
                
                ProcessMilitaryTactics(unit, aiConfig, combatConfig, grid, cultureManager, tileManager, worldWidth, worldHeight);
            }

            private static void ProcessMilitaryTactics(Unit unit, AIConfig aiConfig, CombatConfig combatConfig, UnitSpatialGrid grid, CultureManager cultureManager, TileManager tileManager, float worldWidth, float worldHeight)
            {
                float scanRadius = combatConfig != null && combatConfig.AggroRadius > 0.5f ? combatConfig.AggroRadius : 4.5f;
                var nearbyUnits = grid.GetUnitsInRadius(unit.Position, scanRadius);

                int alliesCount = 0;
                int enemiesCount = 0;
                int mySlotIndex = 0;

                Unit squadLeader = unit;
                Unit nearestEnemy = null;
                float nearestEnemyDist = float.MaxValue;

                for (int i = 0; i < nearbyUnits.Count; i++)
                {
                    var other = nearbyUnits[i];

                    if (!other.IsAlive) continue;

                    if (other.CultureId == unit.CultureId)
                    {
                        if (other is Human oh && oh.Group == PopulationGroup.Military)
                        {
                            alliesCount++;
                            
                            if (string.CompareOrdinal(other.UnitId, squadLeader.UnitId) < 0)
                            {
                                squadLeader = other;
                            }

                            if (string.CompareOrdinal(other.UnitId, unit.UnitId) < 0)
                            {
                                mySlotIndex++;
                            }
                        }
                    }
                    else if (UnitCombatSystem.IsEnemy(unit, other))
                    {
                        enemiesCount++;

                        float d = Vector2.Distance(unit.Position, other.Position);

                        if (d < nearestEnemyDist)
                        {
                            nearestEnemyDist = d;
                            nearestEnemy = other;
                        }
                    }
                }

                var doctrine = TacticalDoctrine.GetOrCreateDoctrine(unit.CultureId);
                var activeRule = doctrine.Evaluate(unit, alliesCount, enemiesCount, nearestEnemyDist);
                
                if (activeRule.Action == TacticalAction.RetreatToSafety && nearestEnemy != null)
                {
                    RetreatToStronghold(unit, nearestEnemy, cultureManager, tileManager, worldWidth, worldHeight);
                    unit.AIState = UnitAIState.Fleeing;
                    return;
                }
                
                if (nearestEnemy != null && unit.CanAttack)
                {
                    unit.AttackTarget = nearestEnemy;
                    unit.AIState = UnitAIState.Attacking;

                    Vector2 dirToEnemy = (nearestEnemy.Position - squadLeader.Position).normalized;
                    Vector2 formationOffset = TacticalDoctrine.GetFormationSlotOffset(mySlotIndex, alliesCount, dirToEnemy, activeRule.Formation);

                    bool isRangedWeapon = unit is Human hum && (hum.Equipment.Weapon == WeaponType.Bow || hum.Equipment.Weapon == WeaponType.Crossbow);

                    if (activeRule.Action == TacticalAction.SkirmishKeepDistance && isRangedWeapon && nearestEnemyDist < unit.AttackRange * 0.7f)
                    {
                        Vector2 kitePos = unit.Position - dirToEnemy * 1.5f + formationOffset * 0.3f;
                        kitePos.x = Mathf.Clamp(kitePos.x, 0.5f, worldWidth - 0.5f);
                        kitePos.y = Mathf.Clamp(kitePos.y, 0.5f, worldHeight - 0.5f);

                        unit.SetTarget(kitePos);
                    }
                    else if (nearestEnemyDist > unit.AttackRange)
                    {
                        Vector2 combatSlotPos = nearestEnemy.Position - (dirToEnemy * (unit.AttackRange * 0.8f)) + formationOffset * 0.5f;
                        combatSlotPos.x = Mathf.Clamp(combatSlotPos.x, 0.5f, worldWidth - 0.5f);
                        combatSlotPos.y = Mathf.Clamp(combatSlotPos.y, 0.5f, worldHeight - 0.5f);

                        unit.SetTarget(combatSlotPos);
                    }
                    else
                    {
                        unit.Stop();
                    }

                    return;
                }

                unit.ClearAttackTarget();

                if (!unit.IsAutonomous) return;
                
                WanderOrMoveToRally(unit, squadLeader, mySlotIndex, alliesCount, activeRule.Formation, aiConfig, cultureManager, tileManager, grid, worldWidth, worldHeight);
            }

            private static void RetreatToStronghold(Unit unit, Unit nearestEnemy, CultureManager cultureManager, TileManager tileManager, float worldWidth, float worldHeight)
            {
                unit.ClearAttackTarget();

                var culture = cultureManager.GetCulture(unit.CultureId);

                if (culture != null)
                {
                    Vector2 capPos = tileManager.GetTileCenter(culture.CultureCapital.x, culture.CultureCapital.y);

                    if (Vector2.Distance(unit.Position, capPos) > 2.5f)
                    {
                        unit.SetTarget(capPos);
                        return;
                    }
                }

                Vector2 awayDir = (unit.Position - nearestEnemy.Position).normalized;
                Vector2 fleePos = unit.Position + awayDir * 4f;

                fleePos.x = Mathf.Clamp(fleePos.x, 0.5f, worldWidth - 0.5f);
                fleePos.y = Mathf.Clamp(fleePos.y, 0.5f, worldHeight - 0.5f);

                unit.SetTarget(fleePos);
            }

            private static bool ShouldFlee(Unit unit, AIConfig aiConfig)
            {
                return unit.GetCurrentHealthPercent() < aiConfig.FleeHealthThreshold;
            }

            private static void Flee(Unit unit, AIConfig aiConfig, UnitSpatialGrid grid, float worldWidth, float worldHeight)
            {
                var candidates = grid.GetUnitsInRadius(unit.Position, aiConfig.AllySearchRadius);
                
                Unit nearestEnemy = UnitCombatSystem.FindNearestEnemy(unit, candidates);

                if (nearestEnemy != null)
                {
                    Vector2 awayDirection = (unit.Position - nearestEnemy.Position).normalized;
                    Vector2 fleeTarget = unit.Position + awayDirection * aiConfig.FleeDistance;

                    fleeTarget.x = Mathf.Clamp(fleeTarget.x, 0.5f, worldWidth - 0.5f);
                    fleeTarget.y = Mathf.Clamp(fleeTarget.y, 0.5f, worldHeight - 0.5f);

                    unit.ClearAttackTarget();
                    unit.SetTarget(fleeTarget);
                }
                else
                {
                    unit.Stop();
                }
            }

            private static void WanderOrMoveToRally(Unit unit, Unit squadLeader, int slotIndex, int alliesCount, FormationType marchFormation, AIConfig aiConfig, CultureManager cultureManager, TileManager tileManager, UnitSpatialGrid grid, float worldWidth, float worldHeight)
            {
                Vector2Int currentTilePos = tileManager.GetClosestTilePos(unit.Position);
                var currentTile = tileManager.GetTile(currentTilePos.x, currentTilePos.y);

                if (CanUnitCaptureTile(unit, currentTile))
                {
                    Vector2 tileCenter = tileManager.GetTileCenter(currentTilePos.x, currentTilePos.y);

                    if (Vector2.Distance(unit.Position, tileCenter) > 0.15f)
                    {
                        unit.SetTarget(tileCenter);
                    }
                    else
                    {
                        unit.Stop();
                    }

                    return;
                }

                if (unit.HasTarget && Vector2.Distance(unit.Position, unit.TargetPosition) > 0.25f)
                {
                    return;
                }

                if (unit.DecisionTimer > 0) return;

                unit.DecisionTimer = (aiConfig.DecisionInterval > 0 ? aiConfig.DecisionInterval : 2f) + Random.Range(0f, 0.5f);

                var culture = cultureManager.GetCulture(unit.CultureId);
                if (culture == null) return;

                bool atWar = DiplomacyManager.Instance != null && DiplomacyManager.Instance.HasAnyWar(unit.CultureId);
                
                Vector2 captureTarget = FindCapturableTile(unit, tileManager, grid, 10);

                if (captureTarget == Vector2.zero)
                {
                    captureTarget = FindCapturableTile(unit, tileManager, grid, 24);
                }
                
                if (captureTarget == Vector2.zero && atWar)
                {
                    captureTarget = FindEnemyFrontlineAcrossMap(unit, cultureManager, tileManager);
                }

                if (captureTarget != Vector2.zero)
                {
                    Vector2 marchDir = (captureTarget - squadLeader.Position).normalized;
                    Vector2 slotOffset = TacticalDoctrine.GetFormationSlotOffset(slotIndex, alliesCount, marchDir, marchFormation);

                    Vector2 targetWithFormation = captureTarget + slotOffset * 0.4f;
                    targetWithFormation.x = Mathf.Clamp(targetWithFormation.x, 0.5f, worldWidth - 0.5f);
                    targetWithFormation.y = Mathf.Clamp(targetWithFormation.y, 0.5f, worldHeight - 0.5f);

                    unit.SetTarget(targetWithFormation);
                    unit.AIState = UnitAIState.Wandering;
                    return;
                }
                
                Vector2 rally = culture.RallyPoint != Vector2.zero
                    ? culture.RallyPoint
                    : tileManager.GetTileCenter(culture.CultureCapital.x, culture.CultureCapital.y);

                float maxWander = aiConfig != null && aiConfig.WanderRadius > 1.5f ? aiConfig.WanderRadius : 4.0f;
                Vector2 dest = rally + Random.insideUnitCircle * Random.Range(1.0f, maxWander);

                dest.x = Mathf.Clamp(dest.x, 0.5f, worldWidth - 0.5f);
                dest.y = Mathf.Clamp(dest.y, 0.5f, worldHeight - 0.5f);

                Vector2Int destTilePos = tileManager.WorldToTilePos(dest);

                if (tileManager.GetTile(destTilePos.x, destTilePos.y).Type != TileType.Water)
                {
                    unit.SetTarget(dest);
                    unit.AIState = UnitAIState.Wandering;
                }
            }

            private static Vector2 FindEnemyFrontlineAcrossMap(Unit unit, CultureManager cultureManager, TileManager tileManager)
            {
                var diplomacy = DiplomacyManager.Instance;
                if (diplomacy == null) return Vector2.zero;

                float bestDist = float.MaxValue;
                Vector2 bestPos = Vector2.zero;

                foreach (var other in cultureManager.GetActiveCultures())
                {
                    if (other.CultureId == unit.CultureId) continue;

                    if (diplomacy.IsAtWar(unit.CultureId, other.CultureId))
                    {
                        Vector2 enemyCapPos = tileManager.GetTileCenter(other.CultureCapital.x, other.CultureCapital.y);
                        float d = Vector2.Distance(unit.Position, enemyCapPos);

                        if (d < bestDist)
                        {
                            bestDist = d;
                            bestPos = enemyCapPos;
                        }
                    }
                }

                return bestPos;
            }

            private static void ProcessProducerLogistics(Unit unit, AIConfig aiConfig, CultureManager cultureManager, PlayerManager playerManager, TileManager tileManager)
            {
                var cfg = Core.CoreManager.MainConfig;

                var culture = cultureManager.GetCulture(unit.CultureId);

                if (culture == null) return;

                var inventory = unit.GetInventory();

                if (inventory == null) return;

                Vector2 capitalPos = tileManager.GetTileCenter(culture.CultureCapital.x, culture.CultureCapital.y);
                
                bool carryingHubConvoy = inventory.Count > 0 && inventory[0].StartsWith("Hub_");

                if (carryingHubConvoy || inventory.Count >= cfg.UnitMaxCarryCapacity)
                {
                    Vector2Int targetDropTile = carryingHubConvoy ? culture.CultureCapital : FindNearestDropOffTile(unit.Position, culture, tileManager);

                    Vector2 dropPos = tileManager.GetTileCenter(targetDropTile.x, targetDropTile.y);

                    if (Vector2.Distance(unit.Position, dropPos) <= 0.65f)
                    {
                        unit.Stop();

                        DepositInventoryToTarget(unit, inventory, targetDropTile, culture, playerManager, tileManager);

                        unit.DecisionTimer = 0.5f;
                    }
                    else
                    {
                        unit.SetTarget(dropPos);

                        unit.AIState = UnitAIState.Wandering;
                    }

                    return;
                }

                if (unit.HasTarget && Vector2.Distance(unit.Position, unit.TargetPosition) > 0.25f)
                {
                    return;
                }

                if (unit.DecisionTimer > 0) return;

                unit.DecisionTimer = (aiConfig.DecisionInterval > 0 ? aiConfig.DecisionInterval : 2f) + Random.Range(0f, 0.5f);
                
                if (inventory.Count == 0 && TryFindFullLogisticsHub(unit.Position, culture.CultureId, tileManager, out Vector2Int fullHubPos))
                {
                    Vector2 hubWorldPos = tileManager.GetTileCenter(fullHubPos.x, fullHubPos.y);

                    if (Vector2.Distance(unit.Position, hubWorldPos) <= 0.75f)
                    {
                        tileManager.ExtractFromHub(fullHubPos.x, fullHubPos.y, out float food, out float mat);

                        if (food > 0f) unit.AddItem($"Hub_Food_{food:F0}");
                        if (mat > 0f) unit.AddItem($"Hub_Mat_{mat:F0}");

                        unit.SetTarget(capitalPos);
                    }
                    else
                    {
                        unit.SetTarget(hubWorldPos);
                    }

                    unit.AIState = UnitAIState.Wandering;

                    return;
                }

                Vector2Int currentTilePos = tileManager.GetClosestTilePos(unit.Position);

                TileData currentTile = tileManager.GetTile(currentTilePos.x, currentTilePos.y);

                if (currentTile.Owner == unit.CultureId && currentTile.Type != TileType.Water)
                {
                    string harvestedItem = "Fruits";

                    if (currentTile.Type == TileType.Rock)
                    {
                        harvestedItem = Random.value < 0.6f ? "Stone" : "Iron";
                    }
                    else if (currentTile.Type == TileType.Grass)
                    {
                        var p = playerManager.GetPlayerForCulture(unit.CultureId);
                        bool needFoodUrgently = p != null && p.GetFood() < 80f;
                    
                        harvestedItem = (needFoodUrgently || Random.value < 0.70f) ? "Fruits" : "Wood";
                    }

                    unit.AddItem(harvestedItem);
                }

                Vector2 workTarget = FindOwnedTileToWork(unit, culture, tileManager);

                if (workTarget != Vector2.zero)
                {
                    unit.SetTarget(workTarget);

                    unit.AIState = UnitAIState.Wandering;
                }
            }

            private static Vector2Int FindNearestDropOffTile(Vector2 unitPos, CultureData culture, TileManager tileManager)
            {
                var cfg = Core.CoreManager.MainConfig;

                Vector2Int bestTile = culture.CultureCapital;

                float bestDist = Vector2.Distance(unitPos, tileManager.GetTileCenter(bestTile.x, bestTile.y));

                foreach (var pos in tileManager.GetStructureTiles())
                {
                    TileData t = tileManager.GetTile(pos.x, pos.y);

                    if (t.Owner == culture.CultureId && t.Structure == TileStructureType.LogisticsHub && t.TotalStored < cfg.HubMaxCapacity)
                    {
                        float d = Vector2.Distance(unitPos, tileManager.GetTileCenter(pos.x, pos.y));

                        if (d < bestDist)
                        {
                            bestDist = d;
                            bestTile = pos;
                        }
                    }
                }

                return bestTile;
            }

            private static bool TryFindFullLogisticsHub(Vector2 unitPos, string cultureId, TileManager tileManager, out Vector2Int hubPos)
            {
                var cfg = Core.CoreManager.MainConfig;

                hubPos = Vector2Int.zero;
                float bestDist = float.MaxValue;
                bool found = false;

                foreach (var pos in tileManager.GetStructureTiles())
                {
                    TileData t = tileManager.GetTile(pos.x, pos.y);

                    if (t.Owner == cultureId && t.Structure == TileStructureType.LogisticsHub && t.TotalStored >= cfg.HubMaxCapacity * 0.75f)
                    {
                        float d = Vector2.Distance(unitPos, tileManager.GetTileCenter(pos.x, pos.y));

                        if (d < bestDist)
                        {
                            bestDist = d;
                            hubPos = pos;
                            found = true;
                        }
                    }
                }

                return found;
            }

            private static void DepositInventoryToTarget(Unit unit, List<string> inventory, Vector2Int targetTilePos, CultureData culture, PlayerManager playerManager, TileManager tileManager)
            {
                var cfg = Core.CoreManager.MainConfig;

                float totalFood = 0f;
                float totalWood = 0f;
                float totalStone = 0f;
                float totalIron = 0f;

                for (int i = 0; i < inventory.Count; i++)
                {
                    string item = inventory[i];

                    if (item.StartsWith("Hub_Food_"))
                    {
                        if (float.TryParse(item.Substring(9), out float val)) totalFood += val;
                    }
                    else if (item.StartsWith("Hub_Mat_"))
                    {
                        if (float.TryParse(item.Substring(8), out float val)) totalIron += val;
                    }
                    else if (item == "Wood")
                    {
                        totalWood += cfg != null ? cfg.HarvestWoodAmount : 6f;
                    }
                    else if (item == "Stone")
                    {
                        totalStone += cfg != null ? cfg.HarvestStoneAmount : 5f;
                    }
                    else if (item == "Iron" || item == "Materials")
                    {
                        totalIron += cfg != null ? cfg.HarvestIronAmount : 4f;
                    }
                    else
                    {
                        totalFood += cfg != null ? cfg.HarvestFoodAmount : 8f;
                    }
                }

                var player = playerManager.GetPlayerForCulture(unit.CultureId);

                if (player != null)
                {
                    if (totalWood > 0f) player.GatherResource("Wood", totalWood);
                    if (totalStone > 0f) player.GatherResource("Stone", totalStone);
                }

                TileData dropTile = tileManager.GetTile(targetTilePos.x, targetTilePos.y);
                
                bool isCapital = targetTilePos == culture.CultureCapital;

                float maxHubCap = cfg != null ? cfg.HubMaxCapacity : 40f;

                if (!isCapital && dropTile.Structure == TileStructureType.LogisticsHub && dropTile.TotalStored < maxHubCap)
                {
                    tileManager.DepositToHub(targetTilePos.x, targetTilePos.y, totalFood, totalIron);
                }
                else if (player != null)
                {
                    if (totalFood > 0f) player.GatherResource("Fruits", totalFood);
                    if (totalIron > 0f) player.GatherResource("Iron", totalIron);
                }

                inventory.Clear();
            }

            private static Vector2 FindOwnedTileToWork(Unit unit, CultureData culture, TileManager tileManager)
            {
                Vector2Int centerTile = FindNearestDropOffTile(unit.Position, culture, tileManager);

                int searchRadius = 8;

                List<Vector2> validWorkTiles = new List<Vector2>();

                for (int dy = -searchRadius; dy <= searchRadius; dy++)
                {
                    for (int dx = -searchRadius; dx <= searchRadius; dx++)
                    {
                        int nx = centerTile.x + dx;
                        int ny = centerTile.y + dy;

                        if (nx < 0 || nx >= tileManager.WorldWidth || ny < 0 || ny >= tileManager.WorldHeight) continue;

                        var tile = tileManager.GetTile(nx, ny);

                        if (tile.Owner != unit.CultureId || tile.Type == TileType.Water) continue;

                        validWorkTiles.Add(tileManager.GetTileCenter(nx, ny));
                    }
                }

                if (validWorkTiles.Count == 0) return Vector2.zero;

                Vector2 chosenTile = validWorkTiles[Random.Range(0, validWorkTiles.Count)];

                return chosenTile + Random.insideUnitCircle * 0.3f;
            }

            private static bool CanUnitCaptureTile(Unit unit, TileData tile)
            {
                if (tile.Type == TileType.Water) return false;

                if (tile.Owner == unit.CultureId) return false;

                if (string.IsNullOrEmpty(tile.Owner) || tile.Owner == "CUL_NONE") return true;

                var diplomacy = DiplomacyManager.Instance;

                return diplomacy != null && diplomacy.IsAtWar(unit.CultureId, tile.Owner);
            }

            private static Vector2 FindCapturableTile(Unit unit, TileManager tileManager, UnitSpatialGrid grid, int searchRadius)
            {
                Vector2Int unitTilePos = tileManager.GetClosestTilePos(unit.Position);

                float bestScore = float.MaxValue;

                Vector2 bestTarget = Vector2.zero;

                var nearbyUnits = grid.GetUnitsInRadius(unit.Position, searchRadius * 1.5f);

                for (int dy = -searchRadius; dy <= searchRadius; dy++)
                {
                    for (int dx = -searchRadius; dx <= searchRadius; dx++)
                    {
                        int nx = unitTilePos.x + dx;
                        int ny = unitTilePos.y + dy;

                        if (nx < 0 || nx >= tileManager.WorldWidth || ny < 0 || ny >= tileManager.WorldHeight) continue;

                        var tile = tileManager.GetTile(nx, ny);

                        if (!CanUnitCaptureTile(unit, tile)) continue;

                        if (!string.IsNullOrEmpty(tile.CapturingCultureId) && tile.CapturingCultureId != unit.CultureId && tile.CaptureProgress > 25f)
                        {
                            if (DiplomacyManager.Instance == null || !DiplomacyManager.Instance.IsAtWar(unit.CultureId, tile.CapturingCultureId))
                            {
                                continue;
                            }
                        }

                        Vector2 tileCenter = tileManager.GetTileCenter(nx, ny);

                        bool claimedByAlly = false;

                        for (int i = 0; i < nearbyUnits.Count; i++)
                        {
                            var other = nearbyUnits[i];

                            if (other == unit || !other.IsAlive || other.CultureId != unit.CultureId) continue;

                            if (Vector2.Distance(other.Position, tileCenter) < 0.6f || (other.HasTarget && Vector2.Distance(other.TargetPosition, tileCenter) < 0.6f))
                            {
                                claimedByAlly = true;

                                break;
                            }
                        }

                        if (claimedByAlly) continue;

                        float score = Vector2.Distance(unit.Position, tileCenter) + Random.Range(0f, 1.5f);
                        
                        if (!string.IsNullOrEmpty(tile.Owner) && tile.Owner != "CUL_NONE")
                        {
                            score -= 4.0f;

                            if (tile.Structure != TileStructureType.None) score -= 8.0f;
                        }

                        if (score < bestScore)
                        {
                            bestScore = score;

                            bestTarget = tileCenter;
                        }
                    }
                }

                return bestTarget;
            }

            private static void ExecutePlayerOrder(Unit unit)
            {
                switch (unit.OrderType)
                {
                    case Unit.PlayerOrderType.MoveTo:
                        float dist = Vector2.Distance(unit.Position, unit.OrderTargetPosition);

                        if (dist < 0.1f)
                        {
                            unit.ClearPlayerOrder();
                            unit.Stop();

                            unit.AIState = UnitAIState.Idle;

                            return;
                        }

                        unit.SetTarget(unit.OrderTargetPosition);

                        break;

                    case Unit.PlayerOrderType.AttackUnit:
                        var target = unit.OrderTargetUnit;

                        if (target == null || !target.IsAlive)
                        {
                            unit.ClearAttackTarget();
                            unit.ClearPlayerOrder();

                            unit.Stop();

                            unit.AIState = UnitAIState.Idle;

                            return;
                        }

                        if (unit.CultureId != target.CultureId && DiplomacyManager.Instance != null)
                        {
                            if (!DiplomacyManager.Instance.IsAtWar(unit.CultureId, target.CultureId))
                            {
                                DiplomacyManager.Instance.DeclareWar(unit.CultureId, target.CultureId);
                            }
                        }

                        unit.AttackTarget = target;

                        if (Vector2.Distance(unit.Position, target.Position) <= unit.AttackRange)
                        {
                            unit.Stop();
                            UnitCombatSystem.PerformAttack(unit, target);
                        }
                        else
                        {
                            unit.SetTarget(target.Position);
                        }

                        unit.AIState = UnitAIState.Attacking;

                        break;
                }
            }
        }
    }
}