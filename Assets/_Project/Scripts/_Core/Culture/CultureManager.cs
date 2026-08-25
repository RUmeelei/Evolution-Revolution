using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace ER
{
    namespace Culture
    {
        using Core;
        using Simulation;

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

            private SimulationManager simulationManager;

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
                simulationManager = CoreManager.SimulationManager;

                simulationManager.OnTick += ProcessCultureTick;

                CreateCultureTrait("NomadicNation", "Nomadic Nation", "Nomads");
                CreateCultureTrait("WarriorNation", "Warrior Nation", "Warriors");
                CreateCultureTrait("MerchantNation", "Merchant Nation", "Merchants");
                CreateCultureTrait("ReligiousNation", "Religious Nation", "Religious");
                CreateCultureTrait("BarbarianNation", "Barbarian Nation", "Barbarians");

                CreateCulture(cultureColor : new Color(50, 50, 50), cultureDescription : "A strong man united nomadic tribes and created a unique culture. This culture has basic traits like ``Nomads`` and ``Warriors``.");

                AddCultureTrait("NomadicNation", "CUL_0001");
                AddCultureTrait("WarriorNation", "CUL_0001");
                AddCultureTrait("BarbarianNation", "CUL_0001");
            }

            public CultureData CreateCulture(Color cultureColor, string cultureId = "CUL", string cultureName = "Nomads", string cultureDescription = "A strong man united nomadic tribes and created a unique culture.")
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

            public IEnumerable<CultureTrait> GetCultureTraits(string id)
            {
                CultureData culture = GetCulture(id);

                if (culture == null) yield break;

                foreach (var trait in culture.CultureTraits.Values)
                {
                    yield return trait;
                }
            }

            public CultureTrait GetCultureTrait(string id)
            {
                CultureTraitsDictionary.TryGetValue(id, out var cultureTrait);

                return cultureTrait;
            }

            public void SetCultureIdentity(string id, float amount)
            {
                CultureData _culture = GetCulture(id);

                if (_culture == null) return;

                _culture.CultureIdentity = Mathf.Clamp(amount, 0f, 100f);
            }

            public void ChangeCultureIdentity(string id, float amount)
            {
                CultureData _culture = GetCulture(id);

                if (_culture == null) return;

                _culture.CultureIdentity = Mathf.Clamp(_culture.CultureIdentity + amount, 0f, 100f);
            }

            public void ProcessCultureTick(float delta)
            {
                float _change = Random.Range(-10f, 10f);

                foreach (var culture in Cultures)
                {
                    if (culture.CultureDefeated) continue;
                    
                    if (culture.CultureTraits.ContainsKey("BarbarianNation"))
                    {
                        _change -= Random.Range(1f, 10f);
                    }

                    ChangeCultureIdentity(culture.CultureId, _change * delta);
                }
            }
        }
    }
}