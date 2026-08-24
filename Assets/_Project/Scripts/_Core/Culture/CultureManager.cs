using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace ER
{
    namespace Culture
    {
        using Core;

        public class CultureManager : MonoBehaviour
        {
            public static CultureManager Instance {get; private set;}

            [Header("Main")]
            public bool Govno;

            private int NextCultureId = 1;

            public List<CultureData> Cultures = new List<CultureData>();
            private Dictionary<string, CultureData> CulturesDictionary = new Dictionary<string, CultureData>();

            void Awake()
            {
                if (transform.parent != null)
                {
                    transform.SetParent(null);
                }

                if (Instance != null && Instance != this)
                {
                    Destroy(gameObject); return;
                }

                Instance = this;

                DontDestroyOnLoad(gameObject);

                CoreManager.RegisterCultureManager(this);
            }

            void Start()
            {
                CreateCulture(cultureColor : new Color(50, 50, 50));
            }

            public CultureData CreateCulture(Color cultureColor, string cultureId = "CUL", string cultureName = "Nomads", string cultureDescription = "Nomad tribed united and created united culture.")
            {
                int numericId = NextCultureId++;

                CultureData culture = new CultureData()
                {
                    CultureNumericId = numericId,
                    CultureId = $"{cultureId}_{numericId:D4}",

                    CultureName = cultureName,
                    CultureDescription =cultureDescription,

                    CultureIdentity = 100f,
                    CultureDefeated = false,

                    CultureColor = cultureColor,
                };

                Cultures.Add(culture);
                CulturesDictionary[culture.CultureId] = culture;

                NextCultureId = Cultures.Count() + 1;

                return culture;
            }

            private void GenerateRandomCulture()
            {
                
            }

            public CultureData GetCulture(string id)
            {
                CulturesDictionary.TryGetValue(id, out var culture);

                return culture;
            }
        }
    }
}