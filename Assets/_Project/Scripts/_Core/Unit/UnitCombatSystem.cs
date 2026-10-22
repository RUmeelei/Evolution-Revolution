using UnityEngine;
using System.Collections.Generic;

namespace ER
{
    namespace Unit
    {
        using Core;
        using World.Tiles;
        using Politics.External.Diplomacy;

        public static class UnitCombatSystem
        {
            public static bool IsEnemy(Unit attacker, Unit target)
            {
                if (attacker == null || target == null) return false;
                if (!target.IsAlive) return false;
                if (attacker.CultureId == target.CultureId) return false;

                var diplomacy = DiplomacyManager.Instance;
                
                if (diplomacy == null) return false;

                return diplomacy.IsAtWar(attacker.CultureId, target.CultureId);
            }

            public static Unit FindNearestEnemy(Unit attacker, List<Unit> candidates)
            {
                if (candidates == null || candidates.Count == 0) return null;

                Unit nearest = null;

                float minDist = float.MaxValue;

                for (int i = 0; i < candidates.Count; i++)
                {
                    var candidate = candidates[i];

                    if (!IsEnemy(attacker, candidate)) continue;

                    float dist = Vector2.Distance(attacker.Position, candidate.Position);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        nearest = candidate;
                    }
                }

                return nearest;
            }

            public static float CalculateDamage(Unit attacker, Unit target)
            {
                float damage = attacker.AttackDamage;
                
                var tileManager = CoreManager.TileManager;

                if (tileManager != null)
                {
                    TileData attackerTile = tileManager.WorldToTile(attacker.Position);
                    TileData targetTile = tileManager.WorldToTile(target.Position);
                    
                    float elevationDiff = attackerTile.Elevation - targetTile.Elevation;

                    float elevationMultiplier = Mathf.Clamp(1f + (elevationDiff * 0.04f), 0.8f, 1.25f);

                    damage *= elevationMultiplier;
                    
                    if (targetTile.Type == TileType.Rock)
                    {
                        damage *= 0.8f;
                    }

                    if (targetTile.Structure == TileStructureType.Outpost && targetTile.Owner == target.CultureId)
                    {
                        damage *= 0.65f;
                    }
                }
                
                var cultureManager = CoreManager.CultureManager;

                if (cultureManager != null)
                {
                    var attackerCulture = cultureManager.GetCulture(attacker.CultureId);

                    if (attackerCulture != null)
                    {
                        float moraleMultiplier = Mathf.Lerp(0.75f, 1.15f, attackerCulture.CultureIdentity / 100f);

                        damage *= moraleMultiplier;
                    }
                }
                
                float effectiveArmor = Mathf.Max(0f, target.Armor - attacker.ArmorPenetration);
                float armorReductionFactor = 100f / (100f + effectiveArmor * 2.5f);

                damage *= armorReductionFactor;

                return Mathf.Max(1f, damage);
            }

            public static void PerformAttack(Unit attacker, Unit target)
            {
                if (attacker == null || target == null) return;
            
                if (!target.IsAlive) return;
            
                if (!attacker.IsAttackReady) return;
            
                float damage = CalculateDamage(attacker, target);
            
                target.TakeDamage(damage);
            
                attacker.MarkAttackPerformed(target);
            
                if (!target.IsAlive)
                {
                    LootTargetInventory(attacker, target);
                }
            }
            
            private static void LootTargetInventory(Unit attacker, Unit target)
            {
                var targetInventory = target.GetInventory();
            
                if (targetInventory == null || targetInventory.Count == 0) return;
            
                var playerManager = Core.CoreManager.PlayerManager;
            
                if (playerManager == null) return;
            
                var attackerPlayer = playerManager.GetPlayerForCulture(attacker.CultureId);

                var gov = CoreManager.GovernmentManager != null ? CoreManager.GovernmentManager.GetGovernment(attacker.CultureId) : null;
                float lootMult = gov != null ? gov.GetLootMultiplier() : 1f;
            
                if (attackerPlayer != null)
                {
                    for (int i = 0; i < targetInventory.Count; i++)
                    {
                        if (targetInventory[i] == "Materials")
                        {
                            attackerPlayer.GatherResource("Materials", 5f * lootMult);
                        }
                        else
                        {
                            attackerPlayer.GatherResource("Fruits", 8f * lootMult);
                        }
                    }
                }
            
                targetInventory.Clear();
            }
        }
    }
}