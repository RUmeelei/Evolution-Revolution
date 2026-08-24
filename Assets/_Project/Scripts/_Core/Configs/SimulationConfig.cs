using UnityEngine;

namespace ER
{
    namespace Configs
    {

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
    }
}