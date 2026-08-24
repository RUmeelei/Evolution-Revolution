using UnityEngine;

namespace ER
{
    namespace Culture
    {
        public class CultureData
        {
            public int CultureNumericId;
            public string CultureId;

            public string CultureName;
            public string CultureDescription;

            public float CultureIdentity;
            public bool CultureDefeated;

            public Color CultureColor;

            public CultureTrait[] CultureTraits;
        }

        public class CultureTrait
        {
            public int CultureTraitId;

            public string CultureTraitName;
            public string CultureTraitDescription;

            public float CultureTraitInfluence;
        }
    }
}