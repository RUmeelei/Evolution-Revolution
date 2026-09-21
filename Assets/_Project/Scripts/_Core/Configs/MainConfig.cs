using UnityEngine;

namespace ER
{
    namespace Configs
    {
        [CreateAssetMenu(fileName = "MainConfig", menuName = "ER/Configs/Main Config")]
        public class MainConfig : ScriptableObject
        {
            [Header("Economy Loop")]
            public int GatherSeason; // 0 - Per step | 1 - Per cycle | 2 - Per tier

            [Header("World")]
            public float TileSize;

            public string[] CultureNamePrefixes;
            public string[] CultureNameSuffixes;

            [Header("Camera")]
            public float CameraSmoothing;
            public float CameraKeyboardScrollSpeed;
            public float CameraZoomSpeed;
            
            public float CameraMinHeight;
            public float CameraMaxHeight;

            [Header("Cultures")]
            public float WarningIdentityThreshold;
            public float ErrorIdentityThreshold;

            public int MinStartTraits;
            public int MaxStartTraits;

            [Header("Units")]
            public float UnitSmoothSpeed;
            public float UnitAccelerationRate;
            public float UnitDecelerationRate;

            public int UnitVisualInitialSize;

            [Header("Selection")]
            public float SelectionDragThreshold;

            [Header("Tile Capture")]
            [Tooltip("Tile capture speed (% per second)")]
            public float CaptureRatePerSecond;

            [Tooltip("Capture progress decay rate (% per second)")]
            public float CaptureDecayRatePerSecond;

            [Header("Game")]
            [Tooltip("How often (in seconds) to check the conditions")]
            public float CheckInterval;
            public bool SpectatorMode;
        }
    }
}