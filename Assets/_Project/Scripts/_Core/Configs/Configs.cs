using UnityEngine;

namespace ER
{
    namespace Configs
    {
        [CreateAssetMenu(fileName = "MainConfig", menuName = "Game/Configs/Main Config")]
        public class MainConfig : ScriptableObject
        {
        }

        [CreateAssetMenu(fileName = "SimulationConfig", menuName = "Game/Configs/Simulation Config")]
        public class SimulationConfig : ScriptableObject
        {
            [Header("Main Simulation Settings")]
            public int MaxSimulationSpeed;
            public int MaxTicksPerSecond;

            public float SlowTickInterval; // In seconds
            public float EpicTickInterval; // In seconds

            [Header("Date Interpreter Settings")]
            public int DateScaler; // 0 - Per tick | 1 - Per slow tick | 2 - Per epic tick

            public float BaseAccumulation;

            public int StepsPerCycle;
            public int CyclesPerTier;
        }

        [CreateAssetMenu(fileName = "AIConfig", menuName = "Game/Configs/AI Config")]
        public class AIConfig : ScriptableObject
        {
            [Header("Main AI Settings")]
            public bool IsAIEnabled;
        }

        [CreateAssetMenu(fileName = "UIConfig", menuName = "Game/Configs/UI Config")]
        public class UIConfig : ScriptableObject
        {
            [Header("Main UI Settings")]
            public Color BaseButtonColor;
            public Color HoverButtonColor;
        }
    }
}