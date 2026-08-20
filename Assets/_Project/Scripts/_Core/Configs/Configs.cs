using UnityEngine;

namespace ER
{
    namespace Configs
    {
        [CreateAssetMenu(fileName = "MainConfig", menuName = "Game/Configs/Main Config")]
        public class MainConfig : ScriptableObject
        {
            [Header("Main Simulation Settings")]
            public int MaxSimulationSpeed;
            public int MaxTicksPerSecond;

            public float SlowTickInterval;
            public float EpicTickInterval;
        }

        [CreateAssetMenu(fileName = "AIConfig", menuName = "Game/Configs/AI Config")]
        public class AIConfig : ScriptableObject
        {
            [Header("Main AI Settings")]
            public bool IsAIEnabled;
        }
    }
}