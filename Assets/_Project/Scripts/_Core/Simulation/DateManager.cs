using System;
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

            public event Action<float> OnStep;
            public event Action<float> OnCycle;
            public event Action<float> OnTier;

            private float stepAccumulator = 0f;

            void Awake()
            {
                if (transform.parent != null)
                {
                    transform.SetParent(null);
                }

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
                        MakeStep(delta);

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
                        MakeStep(delta);

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
                        MakeStep(delta);

                        stepAccumulator -= 1f;
                    }
                }
            }

            private void MakeStep(float delta)
            {
                OnStep?.Invoke(delta);

                Step++;
                
                if (Step > simulationConfig.StepsPerCycle)
                {
                    OnCycle?.Invoke(delta);

                    Step = 0;

                    Cycle++;

                    if (Cycle > simulationConfig.CyclesPerTier)
                    {
                        OnTier?.Invoke(delta);

                        Cycle = 0;

                        Tier++;
                    }
                }
            }
        }
    }
}