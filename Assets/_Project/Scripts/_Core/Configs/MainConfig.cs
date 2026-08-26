using UnityEngine;

namespace ER
{
    namespace Configs
    {
        [CreateAssetMenu(fileName = "MainConfig", menuName = "ER/Configs/Main Config")]
        public class MainConfig : ScriptableObject
        {
            [Header("Thresholds for UI Settings")]
            public float WarningIdentityThreshold;
            public float ErrorIdentityThreshold;
        }
    }
}