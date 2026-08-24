using UnityEngine;

namespace ER
{
    namespace Configs
    {
        [CreateAssetMenu(fileName = "UIConfig", menuName = "Game/Configs/UI Config")]
        public class UIConfig : ScriptableObject
        {
            [Header("Main UI Settings")]
            public Color BaseButtonColor;
            public Color HoverButtonColor;
        }
    }
}