using UnityEngine;

namespace ER
{
    namespace Culture
    {
        [CreateAssetMenu(fileName = "NewTrait", menuName = "ER/Culture Trait")]
        public class CultureTrait : ScriptableObject
        {
            public string TraitId;
            public string TraitName;
            public string Description;
    
            [Header("Settings")]
            public float DefaultInfluence = 0f;
    
            [Header("Visual")]
            public Color TraitColor = Color.white;
            public Sprite TraitIcon;
        }
    }
}