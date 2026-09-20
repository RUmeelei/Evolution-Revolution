using UnityEngine;

namespace ER
{
    namespace Configs
    {
        [CreateAssetMenu(fileName = "CombatConfig", menuName = "ER/Configs/Combat Config")]
        public class CombatConfig : ScriptableObject
        {
            [Header("Attack")]
            public float BaseAttackDamage;
            public float BaseAttackRange;
            public float BaseAttackCooldown;

            [Header("Detection")]
            public float AggroRadius;

            [Header("Adrenaline (universal mechanic)")]
            [Tooltip("Health threshold (0–1) below which adrenaline kicks in")]
            [Range(0f, 1f)]
            public float AdrenalineHealthThreshold;

            [Tooltip("Adrenaline speed multiplier")]
            public float AdrenalineSpeedMultiplier;

            [Tooltip("Minimum speed multiplier based on health (0.5 = an injured character runs at a minimum of 50% speed)")]
            [Range(0f, 1f)]
            public float MinHealthSpeedFactor;

            [Header("Debug")]
            public bool EnableCombatLogging;
        }
    }
}