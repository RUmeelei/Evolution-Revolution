using System;
using UnityEngine;

namespace ER
{
    namespace Culture
    {
        [System.Serializable]
        public class CultureTraitData
        {
            public bool IsActive = true;

            public float Influence = 0f;

            public float TimeActive = 0f;

            public CultureTraitData(){}

            public CultureTraitData(bool initialActive = true, float initialInfluence = 0f)
            {
                IsActive = initialActive;

                Influence = initialInfluence;
            }

            public void AddInfluence(float amount)
            {
                Influence = Mathf.Clamp(Influence + amount, 0f, 100f);
            }

            public void UpdateInfluence(float delta, float baseChange = 0f)
            {
                TimeActive += delta;

                Influence += baseChange * delta;

                Influence = Mathf.Clamp(Influence, 0f, 100f);
            }

            public bool HasSignificantInfluence(float threshold = 20f)
            {
                return IsActive && Influence >= threshold;
            }

            public override string ToString()
            {
                return $"Influence: {Influence:F1}%, Active: {IsActive}";
            }
        }

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