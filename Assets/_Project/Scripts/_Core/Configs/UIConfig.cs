using UnityEngine;

namespace ER
{
    namespace Configs
    {
        [CreateAssetMenu(fileName = "UIConfig", menuName = "ER/Configs/UI Config")]
        public class UIConfig : ScriptableObject
        {
            [Header("Main UI Settings")]
            public Color BaseButtonColor;
            public Color HoverButtonColor;

            public Color MainTextColor;
            public Color SecondTextColor;
            public Color ThirdTextColor;

            public Color SuccessColor;
            public Color ErrorColor;
            public Color WarningColor;
        }
    }
}