using UnityEngine;

namespace ER
{
    namespace Unit
    {
        public enum UnitSpriteDirection
        {
            Up,
            Right,
            Down,
            Left,
        }

        public enum UnitEthnicity
        {
            European,
            Asian,
            African,
            MiddleEast,
            Native,
        }

        [System.Serializable]
        public class UnitSpriteSet
        {
            public Sprite Up;
            public Sprite Right;
            public Sprite Down;
            public Sprite Left;

            public Sprite UpBlink;
            public Sprite RightBlink;
            public Sprite DownBlink;
            public Sprite LeftBlink;
        }
    }
}