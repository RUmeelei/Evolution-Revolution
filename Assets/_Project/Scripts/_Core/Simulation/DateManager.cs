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
            public bool Gavno;

            private SimulationManager simulationManager;
            private SimulationConfig simulationConfig;

            private int Step = 0;
            private int Cycle = 0;
            private int Tier = 0;

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

            public int GetStep() => Step;

            public int GetCycle() => Cycle;

            public int GetTier() => Tier;
        }
    }
}