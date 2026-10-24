using UnityEngine;

namespace ER.Simulation.Time
{
    public class SimulationHotkeysDebugger : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SimulationClockManager clockManager;

        [SerializeField] private SimulationTimeConfig timeConfig;


        [Header("Debug Overlay State")]
        [SerializeField] private bool showDebugOverlay = false;


        private float fpsAccumulator = 0.0f;

        private int fpsFramesCount = 0;

        private float currentFps = 0.0f;

        private float currentFrameTimeMs = 0.0f;

        private float fpsUpdateTimer = 0.0f;

        private const float FpsUpdateInterval = 0.25f;


        public bool ShowDebugOverlay => showDebugOverlay;


        private void Start()
        {
            if (clockManager == null)
            {
                clockManager = SimulationClockManager.Instance;
            }
        }


        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F3))
            {
                showDebugOverlay = !showDebugOverlay;
            }

            if (showDebugOverlay)
            {
                CalculatePerformanceMetrics();
            }

            HandleSimulationInput();
        }


        private void CalculatePerformanceMetrics()
        {
            float delta = UnityEngine.Time.unscaledDeltaTime;

            fpsAccumulator += delta;

            fpsFramesCount++;

            fpsUpdateTimer += delta;

            if (fpsUpdateTimer >= FpsUpdateInterval)
            {
                currentFps = fpsFramesCount / fpsAccumulator;

                currentFrameTimeMs = (fpsAccumulator / fpsFramesCount) * 1000.0f;

                fpsAccumulator = 0.0f;

                fpsFramesCount = 0;

                fpsUpdateTimer = 0.0f;
            }
        }


        private void HandleSimulationInput()
        {
            if (clockManager == null || timeConfig == null)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                clockManager.TogglePause();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                clockManager.SetSpeedMultiplier(timeConfig.Speed1);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                clockManager.SetSpeedMultiplier(timeConfig.Speed2);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                clockManager.SetSpeedMultiplier(timeConfig.Speed3);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                clockManager.SetSpeedMultiplier(timeConfig.Speed4);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                clockManager.SetSpeedMultiplier(timeConfig.Speed5);
            }
        }


        private void OnGUI()
        {
            if (!showDebugOverlay || clockManager == null)
            {
                return;
            }

            WorldTimeData time = clockManager.CurrentTime;

            string statusText = clockManager.IsPaused ? "PAUSED" : $"{clockManager.CurrentSpeedMultiplier:0.#}x";

            GUI.Box(new Rect(12, 60, 540, 115), "Simulation Engine Telemetry (F3 to Hide)");

            GUI.Label(new Rect(24, 82, 510, 20), $"Status: {statusText}  |  Controls: [Space] Pause  |  [1..5] Speeds");

            GUI.Label(new Rect(24, 102, 510, 20), $"Date: Year {time.CurrentYear}, {time.CurrentSeason}, Day {time.CurrentDay:00}, Hour {time.CurrentHour:00}:00");

            GUI.Label(new Rect(24, 122, 510, 20), $"Tick: #{time.TotalTicks}  |  Sim Tick: {clockManager.LastTickExecutionDurationMs:F4} ms");

            GUI.Label(new Rect(24, 142, 510, 20), $"Render: {currentFps:0.} FPS ({currentFrameTimeMs:F2} ms/frame)");
        }
    }
}