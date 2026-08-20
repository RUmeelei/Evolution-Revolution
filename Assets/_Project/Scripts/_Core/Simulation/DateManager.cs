using UnityEngine;

namespace ER
{
    namespace Simulation
    {
        using Core;
        using Configs;

        public class DateManager : MonoBehaviour
        {
            public static DateManager Instance {get; private set;}

            [Header("Main")]
            public int Step = 0;
            public int Cycle = 0;
            public int Tier = 0;

            private SimulationManager simulationManager;
            private SimulationConfig simulationConfig;

            private float stepAccumulator = 0f;

            void Awake()
            {
                if (Instance != null && Instance != this)
                {
                    Destroy(gameObject); return;
                }

                Instance = this;

                DontDestroyOnLoad(gameObject);

                CoreManager.RegisterDateManager(this);
            }

            void Start()
            {
                simulationConfig = CoreManager.SimulationConfig;

                simulationManager = CoreManager.SimulationManager;

                if (simulationManager != null)
                {
                    simulationManager.OnTick += OnTickHandler;
                    simulationManager.OnSlowTick += OnSlowTickHandler;
                    simulationManager.OnEpicTick += OnEpicTickHandler;
                }
            }
            
            private void OnTickHandler(float delta)
            {
                if (simulationConfig.DateScaler == 0)
                {
                    stepAccumulator += simulationConfig.BaseAccumulation * delta;

                    while (stepAccumulator >= 1f)
                    {
                        MakeStep();

                        stepAccumulator -= 1f;
                    }
                }
            }
            
            private void OnSlowTickHandler(float delta)
            {
                if (simulationConfig.DateScaler == 1)
                {
                    stepAccumulator += simulationConfig.BaseAccumulation * delta;

                    while (stepAccumulator >= 1f)
                    {
                        MakeStep();

                        stepAccumulator -= 1f;
                    }
                }
            }
            
            private void OnEpicTickHandler(float delta)
            {
                if (simulationConfig.DateScaler == 2)
                {
                    stepAccumulator += simulationConfig.BaseAccumulation * delta;

                    while (stepAccumulator >= 1f)
                    {
                        MakeStep();

                        stepAccumulator -= 1f;
                    }
                }
            }

            private void MakeStep()
            {
                Step++;
                
                if (Step > simulationConfig.StepsPerCycle)
                {
                    Step = 0;
                    Cycle++;

                    if (Cycle > simulationConfig.CyclesPerTier)
                    {
                        Cycle = 0;
                        Tier++;
                    }
                }
            }
        }
    }
}