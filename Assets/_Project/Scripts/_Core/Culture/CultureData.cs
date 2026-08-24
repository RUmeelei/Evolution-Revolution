using UnityEngine;
using System.Collections.Generic;

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

            public Dictionary<string, CultureTrait> CultureTraits;
        }

        public class CultureTrait
        {
            public string CultureTraitId;

            public string CultureTraitName;
            public string CultureTraitDescription;
        }
    }
}