using UnityEngine;

namespace ER
{
    namespace Unit
    {
        [CreateAssetMenu(fileName = "CultureSprites", menuName = "ER/Unit/Culture Sprites")]
        public class CultureSprites : ScriptableObject
        {
            public UnitEthnicity Ethnicity;

            public UnitSpriteSet Human;
        }
    }
}