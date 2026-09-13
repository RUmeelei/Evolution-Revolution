using UnityEngine;
using System.Collections.Generic;

namespace ER
{
    namespace Culture
    {
        [System.Serializable]
        public class CultureData
        {
            public int CultureNumericId;
            public string CultureId;

            public string CultureName;
            public string CultureDescription;

            public float CultureIdentity;
            public bool CultureDefeated;

            public Color CultureColor;

            public Vector2Int CultureCapital;

            public Dictionary<string, CultureTraitData> CultureTraits = new Dictionary<string, CultureTraitData>();

            public bool HasTrait(string traitId)
            {
                return CultureTraits.ContainsKey(traitId);
            }

            public bool IsTraitActive(string traitId)
            {
                return CultureTraits.TryGetValue(traitId, out var data) && data.IsActive;
            }

            public float GetTraitInfluence(string traitId)
            {
                return CultureTraits.TryGetValue(traitId, out var data) ? data.Influence : 0f;
            }

            public void AddTrait(string traitId, float initialInfluence = 0f)
            {
                if (!CultureTraits.ContainsKey(traitId))
                {
                    CultureTraits.Add(traitId, new CultureTraitData(true, initialInfluence));
                }
            }

            public bool RemoveTrait(string traitId)
            {
                return CultureTraits.Remove(traitId);
            }

            public float GetTotalNegativeInfluence()
            {
                float total = 0f;

                foreach (var trait in CultureTraits.Values)
                {
                    if (trait.HasSignificantInfluence(10f)) total += trait.Influence;
                }
                
                return total;
            }

            public void SetCultureCapital(Vector2Int pos)
            {
                if (pos == null) return;
                
                CultureCapital = pos;
            }

            public override string ToString()
            {
                return $"{CultureName} (ID: {CultureId}) - Identity: {CultureIdentity:F1}%, CultureTraits: {CultureTraits.Count}";
            }
        }
    }
}