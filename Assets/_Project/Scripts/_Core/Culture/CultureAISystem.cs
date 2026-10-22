using UnityEngine;
using System.Collections.Generic;

namespace ER
{
    namespace Culture
    {
        using Core;
        using Configs;
        using Players;
        using World.Tiles;
        using Unit;
        using Politics.External.Diplomacy;

        public static class CultureAISystem
        {
            private class WarCampaignData
            {
                public float StartTime;
                public float LastFrontlineChangeTime;
                public Dictionary<string, int> GoalTilesByCulture = new Dictionary<string, int>();
                public Dictionary<string, int> CapturedTilesByCulture = new Dictionary<string, int>();
            }

            private static Dictionary<string, WarCampaignData> activeWars = new Dictionary<string, WarCampaignData>();
            private static Dictionary<string, float> truceUntilTimers = new Dictionary<string, float>();

            private static string GetPairKey(string c1, string c2)
            {
                return string.CompareOrdinal(c1, c2) < 0 ? $"{c1}_{c2}" : $"{c2}_{c1}";
            }

            public static bool IsInTruce(string c1, string c2)
            {
                if (string.IsNullOrEmpty(c1) || string.IsNullOrEmpty(c2) || c1 == c2) return false;

                string key = GetPairKey(c1, c2);

                if (truceUntilTimers.TryGetValue(key, out float untilTime))
                {
                    if (Time.time < untilTime) return true;

                    truceUntilTimers.Remove(key);
                }

                return false;
            }

            public static void SetDynamicTruce(CultureData c1, CultureData c2, int totalTilesChangedHands)
            {
                var cfg = CoreManager.MainConfig;
                float baseRecovery = cfg != null ? cfg.BasePeaceRecoverySeconds : 20f;
                float perTileRecovery = cfg != null ? cfg.PeaceRecoveryPerTileLost : 8f;
                
                float rawDuration = baseRecovery + (totalTilesChangedHands * perTileRecovery);

                float stanceFactor1 = GetAmbitionRevanchismFactor(c1);
                float stanceFactor2 = GetAmbitionRevanchismFactor(c2);
                float avgFactor = (stanceFactor1 + stanceFactor2) * 0.5f;

                float finalDuration = Mathf.Max(15f, rawDuration * avgFactor);

                string key = GetPairKey(c1.CultureId, c2.CultureId);
                truceUntilTimers[key] = Time.time + finalDuration;
                activeWars.Remove(key);
            }

            private static float GetAmbitionRevanchismFactor(CultureData culture)
            {
                if (culture == null) return 1.0f;

                float factor = culture.Stance switch
                {
                    CultureStance.Aggressive => 0.65f,
                    CultureStance.Passive => 1.45f,
                    _ => 1.0f
                };

                if (culture.HasTrait("BarbarianNation")) factor *= 0.75f;

                return factor;
            }

            private static int CalculateAmbitionWarGoal(CultureData culture, int selfTiles, int enemyTiles)
            {
                if (culture == null) return 3;
                
                if (culture.Stance == CultureStance.Passive && !culture.HasTrait("BarbarianNation"))
                {
                    return Mathf.Max(1, enemyTiles / 20);
                }

                if (culture.Stance == CultureStance.Defensive && !culture.HasTrait("BarbarianNation"))
                {
                    return Mathf.Clamp(Mathf.RoundToInt(enemyTiles * 0.12f) + 1, 2, 8);
                }
                
                float ambitionRatio = culture.HasTrait("BarbarianNation") ? 0.40f : 0.25f;
                int goal = Mathf.RoundToInt(enemyTiles * ambitionRatio + selfTiles * 0.08f);

                return Mathf.Max(3, goal);
            }

            private static WarCampaignData GetOrCreateWarData(CultureData c1, CultureData c2, TileManager tileManager)
            {
                string key = GetPairKey(c1.CultureId, c2.CultureId);

                if (!activeWars.TryGetValue(key, out var war))
                {
                    int tiles1 = tileManager.GetTileCountForOwner(c1.CultureId);
                    int tiles2 = tileManager.GetTileCountForOwner(c2.CultureId);

                    war = new WarCampaignData
                    {
                        StartTime = Time.time,
                        LastFrontlineChangeTime = Time.time
                    };

                    war.GoalTilesByCulture[c1.CultureId] = CalculateAmbitionWarGoal(c1, tiles1, tiles2);
                    war.GoalTilesByCulture[c2.CultureId] = CalculateAmbitionWarGoal(c2, tiles2, tiles1);

                    activeWars[key] = war;
                }

                return war;
            }

            public static void RegisterWarTileCaptured(string attackerId, string defenderId, TileManager tileManager)
            {
                var cm = CoreManager.CultureManager;
                if (cm == null || tileManager == null) return;

                var attacker = cm.GetCulture(attackerId);
                var defender = cm.GetCulture(defenderId);
                if (attacker == null || defender == null) return;

                var war = GetOrCreateWarData(attacker, defender, tileManager);
                
                war.LastFrontlineChangeTime = Time.time;

                war.CapturedTilesByCulture.TryGetValue(attackerId, out int count);
                war.CapturedTilesByCulture[attackerId] = count + 1;
            }

            public static void ProcessCulture(CultureData culture, TileManager tileManager, PlayerManager playerManager, UnitManager unitManager)
            {
                if (culture == null || culture.CultureDefeated) return;

                var player = playerManager.GetPlayerForCulture(culture.CultureId);

                if (player != null && !player.PlayerAI) return;

                int militaryCount = UpdateAIStance(culture, player, unitManager);

                ProcessAIStructures(culture, player, tileManager);

                var diplomacy = DiplomacyManager.Instance;

                if (diplomacy != null && diplomacy.HasAnyWar(culture.CultureId))
                {
                    ProcessWarStrategyAndPeace(culture, player, militaryCount, tileManager, playerManager, unitManager, diplomacy);

                    return;
                }

                Vector2Int rallyTile = FindBorderOutpostOrCapital(culture, tileManager);
                culture.RallyPoint = tileManager.GetTileCenter(rallyTile.x, rallyTile.y);
            }

            private static void ProcessWarStrategyAndPeace(CultureData culture, PlayerData player, int myMilitaryCount, TileManager tileManager, PlayerManager playerManager, UnitManager unitManager, DiplomacyManager diplomacy)
            {
                var cfg = CoreManager.MainConfig;
                var allCultures = CoreManager.CultureManager.GetActiveCultures();

                CultureData primaryEnemy = null;
                float closestDist = float.MaxValue;

                foreach (var other in allCultures)
                {
                    if (other.CultureId == culture.CultureId) continue;

                    if (diplomacy.IsAtWar(culture.CultureId, other.CultureId))
                    {
                        if (IsInTruce(culture.CultureId, other.CultureId))
                        {
                            diplomacy.MakePeace(culture.CultureId, other.CultureId);
                            continue;
                        }

                        float d = Vector2Int.Distance(culture.CultureCapital, other.CultureCapital);

                        if (d < closestDist)
                        {
                            closestDist = d;
                            primaryEnemy = other;
                        }
                    }
                }

                if (primaryEnemy == null) return;

                var warData = GetOrCreateWarData(culture, primaryEnemy, tileManager);

                int myCaptured = warData.CapturedTilesByCulture.TryGetValue(culture.CultureId, out int mc) ? mc : 0;
                int enemyCaptured = warData.CapturedTilesByCulture.TryGetValue(primaryEnemy.CultureId, out int ec) ? ec : 0;

                int myGoal = warData.GoalTilesByCulture.TryGetValue(culture.CultureId, out int mg) ? mg : 4;
                int enemyGoal = warData.GoalTilesByCulture.TryGetValue(primaryEnemy.CultureId, out int eg) ? eg : 4;

                float foodThreshold = cfg != null ? cfg.WarExhaustionFoodThreshold : 25f;
                float identityThreshold = cfg != null ? cfg.WarExhaustionIdentityThreshold : 55f;
                float stalemateTimeout = cfg != null ? cfg.WarStalemateTimeout : 35f;

                int enemyMilitaryCount = CountMilitaryUnits(primaryEnemy.CultureId, unitManager);
                var enemyPlayer = playerManager.GetPlayerForCulture(primaryEnemy.CultureId);

                bool myExhausted = (player != null && player.GetFood() < foodThreshold) || myMilitaryCount <= 1 || culture.CultureIdentity < identityThreshold;
                bool enemyExhausted = (enemyPlayer != null && enemyPlayer.GetFood() < foodThreshold) || enemyMilitaryCount <= 1 || primaryEnemy.CultureIdentity < identityThreshold;

                bool myGoalAchieved = myCaptured >= myGoal;
                bool enemyGoalAchieved = enemyCaptured >= enemyGoal;
                
                bool isFrontlineStalemate = (Time.time - warData.LastFrontlineChangeTime) >= stalemateTimeout;
                int totalTilesChangedHands = myCaptured + enemyCaptured;

                if (myGoalAchieved || enemyExhausted)
                {
                    PayWarReparations(enemyPlayer, player);
                    ConcludePeaceWithTruce(culture, primaryEnemy, totalTilesChangedHands, tileManager, diplomacy, $"[{culture.CultureName}] achieved war ambition against [{primaryEnemy.CultureName}] (Captured {myCaptured}/{myGoal} tiles)!");
                    return;
                }

                if (enemyGoalAchieved || myExhausted)
                {
                    PayWarReparations(player, enemyPlayer);
                    ConcludePeaceWithTruce(culture, primaryEnemy, totalTilesChangedHands, tileManager, diplomacy, $"[{culture.CultureName}] capitulated to [{primaryEnemy.CultureName}] and paid tribute.");
                    return;
                }

                if (isFrontlineStalemate)
                {
                    ConcludePeaceWithTruce(culture, primaryEnemy, totalTilesChangedHands, tileManager, diplomacy, $"White peace signed between [{culture.CultureName}] and [{primaryEnemy.CultureName}] due to frontline stalemate.");
                    return;
                }

                Vector2Int targetTile = SelectStrategicWarTarget(culture, primaryEnemy, tileManager);
                culture.RallyPoint = tileManager.GetTileCenter(targetTile.x, targetTile.y);
            }

            private static void PayWarReparations(PlayerData loser, PlayerData winner)
            {
                if (loser == null || winner == null) return;

                float foodTribute = Mathf.Floor(loser.GetFood() * 0.25f);
                float woodTribute = Mathf.Floor(loser.GetWood() * 0.25f);
                float ironTribute = Mathf.Floor(loser.GetIron() * 0.25f);

                if (foodTribute > 0f)
                {
                    loser.SpendResource("Fruits", foodTribute);
                    winner.GatherResource("Fruits", foodTribute);
                }

                if (woodTribute > 0f)
                {
                    loser.SpendResource("Wood", woodTribute);
                    winner.GatherResource("Wood", woodTribute);
                }

                if (ironTribute > 0f)
                {
                    loser.SpendResource("Iron", ironTribute);
                    winner.GatherResource("Iron", ironTribute);
                }
            }

            private static void ConcludePeaceWithTruce(CultureData c1, CultureData c2, int totalTilesChangedHands, TileManager tileManager, DiplomacyManager diplomacy, string logMessage)
            {
                SetDynamicTruce(c1, c2, totalTilesChangedHands);
                diplomacy.MakePeace(c1.CultureId, c2.CultureId);

                c1.RallyPoint = tileManager.GetTileCenter(c1.CultureCapital.x, c1.CultureCapital.y);
                c2.RallyPoint = tileManager.GetTileCenter(c2.CultureCapital.x, c2.CultureCapital.y);

                if (CoreManager.CultureManager != null && CoreManager.CultureManager.EnableLogging)
                {
                    Debug.Log(logMessage);
                }
            }

            private static Vector2Int SelectStrategicWarTarget(CultureData myCulture, CultureData enemyCulture, TileManager tileManager)
            {
                Vector2Int bestTarget = enemyCulture.CultureCapital;
                float bestScore = Vector2Int.Distance(myCulture.CultureCapital, enemyCulture.CultureCapital);

                foreach (var pos in tileManager.GetStructureTiles())
                {
                    TileData t = tileManager.GetTile(pos.x, pos.y);

                    if (t.Owner != enemyCulture.CultureId) continue;

                    float dist = Vector2Int.Distance(myCulture.CultureCapital, pos);
                    float score = dist - 4f;

                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestTarget = pos;
                    }
                }

                return bestTarget;
            }

            private static Vector2Int FindBorderOutpostOrCapital(CultureData culture, TileManager tileManager)
            {
                foreach (var pos in tileManager.GetStructureTiles())
                {
                    TileData t = tileManager.GetTile(pos.x, pos.y);

                    if (t.Owner == culture.CultureId && t.Structure == TileStructureType.Outpost)
                    {
                        return pos;
                    }
                }

                return culture.CultureCapital;
            }

            private static int CountMilitaryUnits(string cultureId, UnitManager unitManager)
            {
                var units = unitManager.GetUnitsByCulture(cultureId);
                int count = 0;

                for (int i = 0; i < units.Count; i++)
                {
                    if (units[i].IsAlive && units[i] is Human h && h.Group == PopulationGroup.Military)
                    {
                        count++;
                    }
                }

                return count;
            }

            private static int UpdateAIStance(CultureData culture, PlayerData player, UnitManager unitManager)
            {
                var units = unitManager.GetUnitsByCulture(culture.CultureId);

                int aliveCount = 0;
                int militaryCount = 0;
                int producerCount = 0;

                Human candidateToMobilize = null;
                Human candidateToDemobilize = null;

                foreach (var u in units) 
                {
                    if (!u.IsAlive) continue;

                    aliveCount++;

                    if (u is Human h)
                    {
                        if (h.Group == PopulationGroup.Military)
                        {
                            militaryCount++;
                            candidateToDemobilize = h;
                        }
                        else if (h.Group == PopulationGroup.Producers)
                        {
                            producerCount++;
                            candidateToMobilize = h;
                        }
                    }
                }

                bool atWar = DiplomacyManager.Instance != null && DiplomacyManager.Instance.HasAnyWar(culture.CultureId);

                if (player != null)
                {
                    float food = player.GetFood();
                    float iron = player.GetIron();
                    float mobCost = CoreManager.MainConfig != null ? CoreManager.MainConfig.UnitMobilizationCost : 10f;
                    
                    int desiredMilitary = atWar
                        ? Mathf.Max(2, Mathf.RoundToInt(aliveCount * 0.45f))
                        : Mathf.Max(1, Mathf.RoundToInt(aliveCount * 0.25f));

                    if (militaryCount < desiredMilitary && producerCount > 3 && iron >= mobCost && food > 45f)
                    {
                        if (candidateToMobilize != null)
                        {
                            unitManager.MobilizeHuman(candidateToMobilize);
                            militaryCount++;
                        }
                    }
                    else if ((!atWar && militaryCount > desiredMilitary) || (food < 30f && militaryCount > 1))
                    {
                        if (candidateToDemobilize != null)
                        {
                            unitManager.DemobilizeHuman(candidateToDemobilize);
                            militaryCount--;
                        }
                    }
                }
                
                if (militaryCount >= 6)
                {
                    culture.Stance = CultureStance.Aggressive;
                }
                else if (militaryCount >= 3)
                {
                    culture.Stance = CultureStance.Defensive;
                }
                else
                {
                    culture.Stance = CultureStance.Passive;
                }

                return militaryCount;
            }

            private static void ProcessAIStructures(CultureData culture, PlayerData player, TileManager tileManager)
            {
                if (player == null || tileManager == null) return;

                if (Random.value > 0.35f) return;

                int ownedTiles = tileManager.GetTileCountForOwner(culture.CultureId);

                if (ownedTiles < 10) return;

                int settlementsCount = 0;
                int hubsCount = 0;
                int outpostsCount = 0;

                foreach (var pos in tileManager.GetStructureTiles())
                {
                    TileData st = tileManager.GetTile(pos.x, pos.y);

                    if (st.Owner != culture.CultureId) continue;

                    if (st.Structure == TileStructureType.Settlement) settlementsCount++;
                    else if (st.Structure == TileStructureType.LogisticsHub) hubsCount++;
                    else if (st.Structure == TileStructureType.Outpost) outpostsCount++;
                }

                if (settlementsCount < ownedTiles / 22 && tileManager.CanAffordStructure(TileStructureType.Settlement, player))
                {
                    if (TryFindBuildTileForAI(culture, tileManager, false, out Vector2Int buildPos))
                    {
                        tileManager.TryBuildStructure(buildPos.x, buildPos.y, TileStructureType.Settlement, player);
                        return;
                    }
                }

                if (hubsCount < ownedTiles / 15 && tileManager.CanAffordStructure(TileStructureType.LogisticsHub, player))
                {
                    if (TryFindBuildTileForAI(culture, tileManager, false, out Vector2Int buildPos))
                    {
                        tileManager.TryBuildStructure(buildPos.x, buildPos.y, TileStructureType.LogisticsHub, player);
                        return;
                    }
                }

                if (outpostsCount < ownedTiles / 16 && tileManager.CanAffordStructure(TileStructureType.Outpost, player))
                {
                    if (TryFindBuildTileForAI(culture, tileManager, true, out Vector2Int borderPos))
                    {
                        tileManager.TryBuildStructure(borderPos.x, borderPos.y, TileStructureType.Outpost, player);
                    }
                }
                
                if (tileManager.CanAffordRoad(TileRoadType.PavedRoad, player))
                {
                    foreach (var roadPos in tileManager.GetRoadTiles())
                    {
                        TileData rt = tileManager.GetTile(roadPos.x, roadPos.y);
                        if (rt.Owner == culture.CultureId && rt.Road == TileRoadType.DirtPath)
                        {
                            tileManager.TryBuildRoad(roadPos.x, roadPos.y, TileRoadType.PavedRoad, player);
                            break;
                        }
                    }
                }
            }

            private static bool TryFindBuildTileForAI(CultureData culture, TileManager tileManager, bool preferBorder, out Vector2Int chosenPos)
            {
                chosenPos = Vector2Int.zero;
                int searchRadius = 15;

                int capX = culture.CultureCapital.x;
                int capY = culture.CultureCapital.y;

                int minX = Mathf.Max(1, capX - searchRadius);
                int maxX = Mathf.Min(tileManager.WorldWidth - 2, capX + searchRadius);
                int minY = Mathf.Max(1, capY - searchRadius);
                int maxY = Mathf.Min(tileManager.WorldHeight - 2, capY + searchRadius);

                for (int attempt = 0; attempt < 20; attempt++)
                {
                    int x = Random.Range(minX, maxX + 1);
                    int y = Random.Range(minY, maxY + 1);

                    if (x == capX && y == capY) continue;

                    TileData tile = tileManager.GetTile(x, y);

                    if (tile.Owner != culture.CultureId || tile.Type == TileType.Water || tile.Structure != TileStructureType.None) continue;

                    float distToCap = Vector2Int.Distance(new Vector2Int(x, y), culture.CultureCapital);

                    if (distToCap < 2.5f) continue;

                    bool isBorderTile = false;
                    var neighbors = tileManager.GetNeighboringTiles(x, y);

                    foreach (var n in neighbors)
                    {
                        if (n.Owner != culture.CultureId && n.Type != TileType.Water)
                        {
                            isBorderTile = true;
                            break;
                        }
                    }

                    if (preferBorder == isBorderTile)
                    {
                        chosenPos = new Vector2Int(x, y);
                        return true;
                    }
                }

                return false;
            }
        }
    }
}