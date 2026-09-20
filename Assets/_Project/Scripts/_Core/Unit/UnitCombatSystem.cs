using UnityEngine;
using System.Collections.Generic;

namespace ER
{
    namespace Unit
    {
        public static class UnitCombatSystem
        {
            public static bool IsEnemy(Unit attacker, Unit target)
            {
                if (attacker == null || target == null) return false;

                if (!target.IsAlive) return false;
                
                return attacker.CultureId != target.CultureId;
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

                return damage;
            }
            
            public static void PerformAttack(Unit attacker, Unit target)
            {
                if (attacker == null || target == null) return;

                if (!target.IsAlive) return;

                if (!attacker.IsAttackReady) return;

                float damage = CalculateDamage(attacker, target);
                
                target.TakeDamage(damage);

                attacker.MarkAttackPerformed(target);
            }
        }
    }
}