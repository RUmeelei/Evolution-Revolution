using UnityEngine;

namespace ER
{
    namespace Configs
    {
        [CreateAssetMenu(fileName = "AIConfig", menuName = "ER/Configs/AI Config")]
        public class AIConfig : ScriptableObject
        {
            [Header("Main AI Settings")]
            public bool IsAIEnabled;

            [Header("Decision Making")]
            [Tooltip("How often (in seconds) does the unit re-evaluate the situation?")]
            public float DecisionInterval;

            [Header("Fleeing")]
            [Tooltip("Health threshold (0–1) below which the unit flees")]
            [Range(0f, 1f)]
            public float FleeHealthThreshold;

            [Tooltip("How far the unit runs away from the enemy")]
            public float FleeDistance;

            [Header("Grouping")]
            [Tooltip("Search radius for allies to regroup with")]
            public float AllySearchRadius;

            [Header("Wandering")]
            [Tooltip("Wandering radius around one's position")]
            public float WanderRadius;
        }
    }
}