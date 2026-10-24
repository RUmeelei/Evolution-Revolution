using System;
using System.Diagnostics;
using UnityEngine;

namespace ER.Simulation.Time
{
    public class SimulationClockManager : MonoBehaviour
    {
        public static SimulationClockManager Instance { get; private set; }


        [Header("Configuration Asset")]
        [SerializeField] private SimulationTimeConfig timeConfig;


        [Header("Runtime State")]
        [SerializeField] private bool isPaused = false;

        [SerializeField] private float currentSpeedMultiplier = 1.0f;


        private WorldTimeData currentTimeData;

        private float timeAccumulator;

        private readonly Stopwatch tickStopwatch = new Stopwatch();

        private double lastTickExecutionDurationMs;


        public event Action<WorldTimeData> OnTick;

        public event Action<WorldTimeData> OnDayPassed;

        public event Action<WorldTimeData> OnSeasonChanged;

        public event Action<WorldTimeData> OnYearPassed;

        public event Action<bool> OnPauseStateChanged;

        public event Action<float> OnSpeedMultiplierChanged;


        public WorldTimeData CurrentTime => currentTimeData;

        public bool IsPaused => isPaused;

        public float CurrentSpeedMultiplier => currentSpeedMultiplier;

        public double LastTickExecutionDurationMs => lastTickExecutionDurationMs;


        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            currentTimeData = WorldTimeData.CreateInitial();
        }


        private void Update()
        {
            if (isPaused || timeConfig == null)
            {
                return;
            }

            float frameDelta = Mathf.Min(UnityEngine.Time.unscaledDeltaTime, timeConfig.MaxFrameAccumulationTime);

            timeAccumulator += frameDelta * currentSpeedMultiplier;

            float targetTickDuration = timeConfig.BaseSecondsPerTick;

            while (timeAccumulator >= targetTickDuration)
            {
                ExecuteSimulationTick();

                timeAccumulator -= targetTickDuration;
            }
        }


        private void ExecuteSimulationTick()
        {
            tickStopwatch.Restart();

            currentTimeData.AdvanceTick(out bool dayChanged, out bool seasonChanged, out bool yearChanged);

            OnTick?.Invoke(currentTimeData);

            if (dayChanged)
            {
                OnDayPassed?.Invoke(currentTimeData);
            }

            if (seasonChanged)
            {
                OnSeasonChanged?.Invoke(currentTimeData);
            }

            if (yearChanged)
            {
                OnYearPassed?.Invoke(currentTimeData);
            }

            tickStopwatch.Stop();

            lastTickExecutionDurationMs = tickStopwatch.Elapsed.TotalMilliseconds;
        }


        public void SetPaused(bool paused)
        {
            if (isPaused == paused)
            {
                return;
            }

            isPaused = paused;

            timeAccumulator = 0.0f;

            OnPauseStateChanged?.Invoke(isPaused);
        }


        public void TogglePause()
        {
            SetPaused(!isPaused);
        }


        public void SetSpeedMultiplier(float multiplier)
        {
            if (Mathf.Approximately(currentSpeedMultiplier, multiplier) || multiplier <= 0.0f)
            {
                return;
            }

            currentSpeedMultiplier = multiplier;

            OnSpeedMultiplierChanged?.Invoke(currentSpeedMultiplier);
        }


        public void SetLoadedTime(WorldTimeData loadedTime)
        {
            currentTimeData = loadedTime;

            timeAccumulator = 0.0f;
        }
    }
}