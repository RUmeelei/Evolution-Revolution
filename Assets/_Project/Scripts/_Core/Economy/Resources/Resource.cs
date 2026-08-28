using UnityEngine;

namespace ER
{
    namespace Resources
    {
        [CreateAssetMenu(fileName = "NewResource", menuName = "ER/Resource")]
        public class CultureTrait : ScriptableObject
        {
            public string ResourceId;
            public string ResourceName;
            public string Description;
    
            [Header("Settings")]
            public float DefaultAmount;
    
            [Header("Visual")]
            public Color ResourceColor = Color.white;
            public Sprite ResourceIcon;
        }
    }
}