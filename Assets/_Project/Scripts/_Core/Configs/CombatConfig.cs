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

            [Header("Debug")]
            public bool EnableCombatLogging;
        }
    }
}