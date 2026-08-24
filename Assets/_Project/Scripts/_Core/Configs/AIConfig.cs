using UnityEngine;

namespace ER
{
    namespace Configs
    {
        [CreateAssetMenu(fileName = "AIConfig", menuName = "Game/Configs/AI Config")]
        public class AIConfig : ScriptableObject
        {
            [Header("Main AI Settings")]
            public bool IsAIEnabled;
        }
    }
}