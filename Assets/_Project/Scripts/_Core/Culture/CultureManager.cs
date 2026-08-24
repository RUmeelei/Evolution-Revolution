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

            public List<CultureTrait> CultureTraits = new List<CultureTrait>();
            private Dictionary<string, CultureTrait> CultureTraitsDictionary = new Dictionary<string, CultureTrait>();

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
                CreateCultureTrait("NomadicNation", "Nomadic Nation", "Nomads");
                CreateCultureTrait("WarriorNation", "Warrior Nation", "Warriors");
                CreateCultureTrait("MerchantNation", "Merchant Nation", "Merchants");
                CreateCultureTrait("ReligiousNation", "Religious Nation", "Religious");
                CreateCultureTrait("BarbarianNation", "Barbarian Nation", "Barbarians");

                CreateCulture(cultureColor : new Color(50, 50, 50));

                AddCultureTrait("NomadicNation", "CUL_0001");
                AddCultureTrait("WarriorNation", "CUL_0001");
                AddCultureTrait("BarbarianNation", "CUL_0001");
            }

            public CultureData CreateCulture(Color cultureColor, string cultureId = "CUL", string cultureName = "Nomads", string cultureDescription = "A strong man united nomadic tribes and created a unique culture. This culture has basic traits like ``Nomads`` and ``Warriors``.")
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

                    CultureTraits = new Dictionary<string, CultureTrait>(),
                };

                Cultures.Add(culture);
                CulturesDictionary[culture.CultureId] = culture;

                NextCultureId = Cultures.Max(c => c.CultureNumericId) + 1;

                Debug.Log($"Created {culture.CultureName} culture with id {culture.CultureId}.");

                return culture;
            }

            public CultureTrait CreateCultureTrait(string cultureTraitId, string cultureTraitName, string cultureTraitDescription)
            {
                CultureTrait cultureTrait = new CultureTrait()
                {
                    CultureTraitId = cultureTraitId,

                    CultureTraitName = cultureTraitName,
                    CultureTraitDescription = cultureTraitDescription,
                };

                CultureTraits.Add(cultureTrait);
                CultureTraitsDictionary[cultureTrait.CultureTraitId] = cultureTrait;

                Debug.Log($"Created {cultureTrait.CultureTraitName} culture trait with id {cultureTrait.CultureTraitId}.");

                return cultureTrait;
            }

            public void AddCultureTrait(string cultureTraitId, string cultureId)
            {
                CultureTrait _cultureTrait = GetCultureTrait(cultureTraitId);

                CultureData _culture = GetCulture(cultureId);

                if (_cultureTrait == null || _culture == null) return;

                if (!_culture.CultureTraits.ContainsKey(cultureTraitId)) 
                {
                    _culture.CultureTraits.Add(cultureTraitId, _cultureTrait);

                    Debug.Log($"Added {_cultureTrait.CultureTraitName} trait to {_culture.CultureName} culture.");
                }
            }

            private void GenerateRandomCulture()
            {
                
            }

            public CultureData GetCulture(string id)
            {
                CulturesDictionary.TryGetValue(id, out var culture);

                return culture;
            }

            public CultureTrait GetCultureTrait(string id)
            {
                CultureTraitsDictionary.TryGetValue(id, out var cultureTrait);

                return cultureTrait;
            }
        }
    }
}