using System;
using UnityEngine;

namespace ER
{
    namespace Simulation
    {
        using Core;
        using Configs;

        public class SimulationManager : MonoBehaviour
        {
            public static SimulationManager Instance {get; private set;}

            [Header("Main")]
            public bool IsSimulationRunning = false;
            public int SimulationSpeed = 1;

            private int Tick;

            private SimulationConfig simulationConfig;

            public event Action<float> OnTick;
            public event Action<float> OnSlowTick;
            public event Action<float> OnEpicTick;

            private float TickTimer = 0f;
            private float SlowTickTimer = 0f;
            private float EpicTickTimer = 0f;

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

                CoreManager.RegisterSimulationManager(this);
            }

            void Start()
            {
                simulationConfig = CoreManager.SimulationConfig;
            }

            void Update()
            {
                if (!IsSimulationRunning) return;

                float tickInterval = 1f / Mathf.Max(1, simulationConfig.MaxTicksPerSecond);

                int maxTicksThisFrame = Mathf.CeilToInt(simulationConfig.MaxTicksPerSecond * 0.1f);
                int ticksThisFrame = 0;

                TickTimer += Time.deltaTime;

                while (TickTimer >= tickInterval && ticksThisFrame < maxTicksThisFrame)
                {
                    float effectiveDelta = tickInterval * SimulationSpeed;

                    ProcessTick(effectiveDelta);

                    ProcessSlowTick(effectiveDelta);

                    ProcessEpicTick(effectiveDelta);

                    TickTimer -= tickInterval;

                    ticksThisFrame++;
                }

                if (ticksThisFrame >= maxTicksThisFrame) TickTimer = 0f;
            }

            public void ProcessTick(float delta)
            {
                OnTick?.Invoke(delta);

                Tick++;
            }

            public void ProcessSlowTick(float delta)
            {
                SlowTickTimer += delta;

                if (SlowTickTimer >= simulationConfig.SlowTickInterval)
                {
                    OnSlowTick?.Invoke(delta);

                    SlowTickTimer = 0f;
                }
            }

            public void ProcessEpicTick(float delta)
            {
                EpicTickTimer += delta;

                if (EpicTickTimer >= simulationConfig.EpicTickInterval)
                {
                    OnEpicTick?.Invoke(delta);

                    EpicTickTimer = 0f;
                }
            }

            public void Pause()
            {
                IsSimulationRunning = false;
            }

            public void Unpause()
            {
                IsSimulationRunning = true;
            }

            public void SetSimulationSpeed(int speed)
            {
                SimulationSpeed = Mathf.Min(simulationConfig.MaxSimulationSpeed, speed);
            }
        }
    }
}