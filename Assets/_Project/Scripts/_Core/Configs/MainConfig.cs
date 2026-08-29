using UnityEngine;

namespace ER
{
    namespace Configs
    {
        [CreateAssetMenu(fileName = "MainConfig", menuName = "ER/Configs/Main Config")]
        public class MainConfig : ScriptableObject
        {
            [Header("Thresholds")]
            public float WarningIdentityThreshold;
            public float ErrorIdentityThreshold;

            [Header("Economy Loop")]
            public int GatherSeason; // 0 - Per step | 1 - Per cycle | 2 - Per tier

            [Header("World")]
            public int WorldWidth;
            public int WorldHeight;

            public float TileSize;
        }
    }
}