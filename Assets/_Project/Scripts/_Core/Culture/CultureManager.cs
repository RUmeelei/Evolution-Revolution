using System;
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
            public bool EnableLogging;

            private int NextCultureId = 1;

            public List<CultureData> Cultures = new List<CultureData>();
            private Dictionary<string, CultureData> CulturesDictionary = new Dictionary<string, CultureData>();

            public List<CultureTrait> CultureTraitTemplates = new List<CultureTrait>();
            private Dictionary<string, CultureTrait> CultureTraitTemplatesDictionary = new Dictionary<string, CultureTrait>();

            private SimulationManager simulationManager;

            public event Action<float> OnCultureTick;

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

                simulationManager.OnSlowTick += ProcessCultureTick;

                var startCulture = CreateCulture(
                    cultureColor: new Color(50f / 255f, 50f / 255f, 50f / 255f),
                    cultureName: "Rosskans",
                    cultureDescription: "A strong man united nomadic tribes and created a unique culture."
                );

                startCulture.AddTrait("NomadicNation");
                startCulture.AddTrait("BarbarianNation");
            }

            void OnDestroy()
            {
                if (simulationManager != null) simulationManager.OnSlowTick -= ProcessCultureTick;
            }

            public CultureData CreateCulture(Color cultureColor, string cultureName = "Nomads", string cultureDescription = "A strong man united nomadic tribes and created a unique culture.")
            {
                int numericId = NextCultureId++;
                string cultureId = $"CUL_{numericId:D4}";

                CultureData culture = new CultureData()
                {
                    CultureNumericId = numericId,
                    CultureId = cultureId,

                    CultureName = cultureName,
                    CultureDescription = cultureDescription,

                    CultureIdentity = 100f,
                    CultureDefeated = false,

                    CultureColor = cultureColor,
                };

                Cultures.Add(culture);

                CulturesDictionary[cultureId] = culture;

                NextCultureId = Cultures.Count > 0 ? Cultures.Max(c => c.CultureNumericId) + 1 : 1;

                Debug.Log($"Created {culture.CultureName} culture with id {culture.CultureId}");

                return culture;
            }

            public CultureData GetCulture(string id)
            {
                CulturesDictionary.TryGetValue(id, out var culture);
                
                return culture;
            }

            public CultureData GetCultureByName(string name)
            {
                return Cultures.FirstOrDefault(c => c.CultureName == name);
            }

            public List<CultureData> GetActiveCultures()
            {
                return Cultures.Where(c => !c.CultureDefeated).ToList();
            }

            public void DefeatCulture(string id)
            {
                var culture = GetCulture(id);

                if (culture == null || culture.CultureDefeated) return;

                culture.CultureDefeated = true;

                Debug.Log($"{culture.CultureName} has been defeated");
            }

            public CultureTrait CreateTraitTemplate(string traitId, string traitName, string traitDescription)
            {
                if (CultureTraitTemplatesDictionary.ContainsKey(traitId))
                {
                    Debug.LogWarning($"Trait {traitId} already exists");

                    return CultureTraitTemplatesDictionary[traitId];
                }

                var trait = ScriptableObject.CreateInstance<CultureTrait>();

                trait.TraitId = traitId;

                trait.TraitName = traitName;
                trait.Description = traitDescription;

                CultureTraitTemplates.Add(trait);
                CultureTraitTemplatesDictionary[traitId] = trait;

                Debug.Log($"Created trait template: {traitName}");

                return trait;
            }

            public CultureTrait GetTraitTemplate(string id)
            {
                CultureTraitTemplatesDictionary.TryGetValue(id, out var trait);

                return trait;
            }
            
            public void AddCultureTrait(string traitId, string cultureId, float initialInfluence = 0f)
            {
                var template = GetTraitTemplate(traitId);

                var culture = GetCulture(cultureId);

                if (template == null)
                {
                    Debug.LogError($"Trait template {traitId} not found");

                    return;
                }

                if (culture == null)
                {
                    Debug.LogError($"Culture {cultureId} not found");

                    return;
                }

                if (!culture.Traits.ContainsKey(traitId))
                {
                    culture.Traits.Add(traitId, new CultureTraitData(true, initialInfluence));

                    Debug.Log($"Added {template.TraitName} to {culture.CultureName}");
                }
                else
                {
                    Debug.LogWarning($"{culture.CultureName} already has {template.TraitName}");
                }
            }

            public void RemoveCultureTrait(string traitId, string cultureId)
            {
                var culture = GetCulture(cultureId);

                if (culture != null && culture.Traits.Remove(traitId))
                {
                    Debug.Log($"Removed trait {traitId} from {culture.CultureName}");
                }
            }

            public CultureTraitData GetTraitData(string cultureId, string traitId)
            {
                var culture = GetCulture(cultureId);

                if (culture == null) return null;

                culture.Traits.TryGetValue(traitId, out var data);

                return data;
            }

            public void ChangeCultureIdentity(string id, float amount)
            {
                var culture = GetCulture(id);

                if (culture == null || culture.CultureDefeated) return;

                float oldIdentity = culture.CultureIdentity;

                culture.CultureIdentity = Mathf.Clamp(culture.CultureIdentity + amount, 0f, 100f);

                if (EnableLogging && Mathf.Abs(culture.CultureIdentity - oldIdentity) > 0.5f)
                {
                    float change = culture.CultureIdentity - oldIdentity;

                    Debug.Log($"{culture.CultureName}: {(change > 0 ? "+" : "")}{change:F1}% identity (now {culture.CultureIdentity:F1}%)");
                }

                if (culture.CultureIdentity <= 0f) DefeatCulture(id);
            }

            public void ProcessCultureTick(float delta)
            {
                OnCultureTick?.Invoke(delta);

                foreach (var culture in Cultures)
                {
                    if (culture.CultureDefeated) continue;

                    float change = UnityEngine.Random.Range(-10f, 10f);

                    float negativeInfluence = 0f;
                    
                    foreach (var traitEntry in culture.Traits)
                    {
                        string traitId = traitEntry.Key;

                        CultureTraitData traitData = traitEntry.Value;

                        if (!traitData.IsActive) continue;

                        float traitChange = UnityEngine.Random.Range(-0.2f, 0.2f);

                        if (traitEntry.Key == "BarbarianNation")
                        {
                            traitChange += UnityEngine.Random.Range(-0.5f, 3f);
                        }
                        
                        if (traitEntry.Key == "NomadicNation")
                        {
                            traitChange += UnityEngine.Random.Range(-0.5f, 3f);
                        }
                        
                        traitData.UpdateInfluence(delta, traitChange);
                        
                        if (traitData.HasSignificantInfluence(20f))
                        {
                            float penalty = UnityEngine.Random.Range(traitData.Influence * 0.01f, traitData.Influence * 0.1f);

                            negativeInfluence += penalty;
                        }
                    }
                    
                    change -= negativeInfluence;

                    ChangeCultureIdentity(culture.CultureId, change * delta);
                }
            }
        }
    }
}