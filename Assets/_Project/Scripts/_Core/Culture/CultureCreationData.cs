using UnityEngine;
using System.Collections.Generic;

namespace ER
{
    namespace Culture
    {
        using Unit;
            
        public static class CultureCreationData
        {
            public static string Name;
            public static string Description;

            public static Color Color;

            public static List<CultureTrait> CultureTraits = new List<CultureTrait>();

            public static UnitEthnicity CultureEthnicity;
        }
    }
}