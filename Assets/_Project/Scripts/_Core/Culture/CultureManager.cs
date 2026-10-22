using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace ER
{
    namespace Culture
    {
        using Core;
        using Configs;
        using Simulation;
        using Players;
        using World.Tiles;
        using Unit;
        using Politics.External.Diplomacy;

        public class CultureManager : MonoBehaviour
        {
            public static CultureManager Instance { get; private set; }

            [Header("Main")]
            public bool EnableLogging;

            private int NextCultureId = 1;

            [SerializeField] private List<CultureTrait> CultureTraitsList;

            public List<CultureData> Cultures = new List<CultureData>();
            private Dictionary<string, CultureData> CulturesDictionary = new Dictionary<string, CultureData>();

            public List<CultureTrait> CultureTraitTemplates = new List<CultureTrait>();
            private Dictionary<string, CultureTrait> CultureTraitTemplatesDictionary = new Dictionary<string, CultureTrait>();

            private SimulationManager simulationManager;
            private PlayerManager playerManager;
            private TileManager tileManager;
            private UnitManager unitManager;

            private float CultureAITimer = 0f;
            private const float CultureAIInterval = 2f;

            private MainConfig mainConfig;

            public event Action<float> OnCultureTick;
            public event System.Action<string> OnCultureDefeated;

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
                playerManager = CoreManager.PlayerManager;

                if (playerManager != null)
                {
                    playerManager.OnPlayerTick += ProcessCultureTick;
                }

                tileManager = CoreManager.TileManager;
                unitManager = CoreManager.UnitManager;
            }

            void OnDestroy()
            {
                if (playerManager != null) playerManager.OnPlayerTick -= ProcessCultureTick;
            }

            public void Initialize()
            {
                mainConfig = CoreManager.MainConfig;
                LoadTraitsFromResources();
            }

            private void LoadTraitsFromResources()
            {
                if (CultureTraitsList == null) return;

                foreach (var trait in CultureTraitsList)
                {
                    if (trait != null && !CultureTraitTemplatesDictionary.ContainsKey(trait.TraitId))
                    {
                        CultureTraitTemplates.Add(trait);
                        CultureTraitTemplatesDictionary.Add(trait.TraitId, trait);
                    }
                }
            }

            public CultureData CreateCulture(Color cultureColor, string cultureName = "Nomads", string cultureDescription = "A strong man united nomadic tribes and created a unique culture.")
            {
                int numericId = NextCultureId++;
                string cultureId = $"CUL_{numericId:D4}";

                CultureData culture = new CultureData()
                {
                    CultureNumericId = numericId,
                    CultureId = cultureId,
                    CultureName = string.IsNullOrEmpty(cultureName) ? $"Culture {numericId}" : cultureName,
                    CultureDescription = cultureDescription,
                    CultureIdentity = 100f,
                    CultureDefeated = false,
                    CultureColor = cultureColor,
                    Stance = CultureStance.Passive,
                };

                Cultures.Add(culture);
                CulturesDictionary[cultureId] = culture;

                NextCultureId = Cultures.Count > 0 ? Cultures.Max(c => c.CultureNumericId) + 1 : 1;

                Debug.Log($"Created {culture.CultureName} culture with id {culture.CultureId}");

                return culture;
            }

            public CultureData GetCulture(string id)
            {
                if (string.IsNullOrEmpty(id)) return null;
                CulturesDictionary.TryGetValue(id, out var culture);
                return culture;
            }

            public CultureData GetCultureByName(string name)
            {
                return Cultures.FirstOrDefault(c => c.CultureName == name);
            }

            public PlayerData GetPlayerForCulture(string cultureId)
            {
                return playerManager != null ? playerManager.GetPlayerForCulture(cultureId) : null;
            }

            public List<CultureData> GetActiveCultures()
            {
                return Cultures.Where(c => !c.CultureDefeated).ToList();
            }

            public Dictionary<string, CultureData> GetAllCultures() => CulturesDictionary;

            public void DefeatCulture(string id)
            {
                var culture = GetCulture(id);

                if (culture == null || culture.CultureDefeated) return;

                culture.CultureDefeated = true;

                culture.CultureIdentity = 0;

                Debug.Log($"{culture.CultureName} has been defeated");

                OnCultureDefeated?.Invoke(id);
            }

            public CultureTrait CreateTraitTemplate(string traitId, string traitName, string traitDescription)
            {
                if (CultureTraitTemplatesDictionary.ContainsKey(traitId))
                {
                    return CultureTraitTemplatesDictionary[traitId];
                }

                var trait = ScriptableObject.CreateInstance<CultureTrait>();
                trait.TraitId = traitId;
                trait.TraitName = traitName;
                trait.Description = traitDescription;

                CultureTraitTemplates.Add(trait);
                CultureTraitTemplatesDictionary[traitId] = trait;

                return trait;
            }

            public CultureTrait GetTraitTemplate(string id)
            {
                CultureTraitTemplatesDictionary.TryGetValue(id, out var trait);
                return trait;
            }

            public void AddCultureTrait(string traitId, string cultureId, float initialInfluence = -1f)
            {
                var template = GetTraitTemplate(traitId);
                var culture = GetCulture(cultureId);

                if (template == null || culture == null) return;

                float startInfluence = initialInfluence >= 0f ? initialInfluence : template.DefaultInfluence;

                if (!culture.CultureTraits.ContainsKey(traitId))
                {
                    culture.CultureTraits.Add(traitId, new CultureTraitData(true, startInfluence));

                    Debug.Log($"Added {template.TraitName} to {culture.CultureName}");
                }
            }

            public void RemoveCultureTrait(string traitId, string cultureId)
            {
                var culture = GetCulture(cultureId);

                if (culture != null && culture.CultureTraits.Remove(traitId))
                {
                    Debug.Log($"Removed trait {traitId} from {culture.CultureName}");
                }
            }

            public void AddRandomTraits(string cultureId)
            {
                var culture = GetCulture(cultureId);

                if (culture == null || CultureTraitTemplates == null || CultureTraitTemplates.Count == 0) return;

                int traitCount = UnityEngine.Random.Range(mainConfig.MinStartTraits, mainConfig.MaxStartTraits);

                var shuffled = CultureTraitTemplates.OrderBy(x => UnityEngine.Random.value).Take(traitCount);

                foreach (var trait in shuffled)
                {
                    culture.AddTrait(trait.TraitId, trait.DefaultInfluence);
                }
            }

            public CultureTraitData GetTraitData(string cultureId, string traitId)
            {
                var culture = GetCulture(cultureId);

                if (culture == null) return null;

                culture.CultureTraits.TryGetValue(traitId, out var data);

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
                if (tileManager != null) tileManager.ProcessRegionalInfluence(delta);

                OnCultureTick?.Invoke(delta);

                CultureAITimer += delta;

                if (CultureAITimer >= CultureAIInterval)
                {
                    CultureAITimer = 0f;

                    ProcessCultureAI();
                }

                foreach (var culture in Cultures)
                {
                    if (culture.CultureDefeated) continue;

                    var units = unitManager != null ? unitManager.GetUnitsByCulture(culture.CultureId) : null;
                    int unitCount = units != null ? units.Count : 0;
                    int culturalTiles = tileManager.GetTileCountForCulture(culture.CultureId);
                    int stateTiles = tileManager.GetTileCountForOwner(culture.CultureId);

                    if (culture.CultureIdentity <= 0f || (unitCount == 0 && culturalTiles == 0))
                    {
                        DefeatCulture(culture.CultureId);

                        continue;
                    }
                    
                    Vector2Int oldCapPos = culture.CultureCapital;
                    TileData capitalTile = tileManager.GetTile(oldCapPos.x, oldCapPos.y);

                    bool hasValidCapital = true;

                    if (capitalTile.Owner != culture.CultureId)
                    {
                        var newCapital = tileManager.FindNewCultureCapital(culture.CultureId);

                        if (newCapital.HasValue)
                        {
                            Vector2 oldCapitalCenter = tileManager.GetTileCenter(oldCapPos.x, oldCapPos.y);
                            culture.SetCultureCapital(newCapital.Value);

                            tileManager.NotifyTileChanged(oldCapPos.x, oldCapPos.y);
                            tileManager.NotifyTileChanged(newCapital.Value.x, newCapital.Value.y);

                            if (Vector2.Distance(culture.RallyPoint, oldCapitalCenter) < 2f)
                            {
                                culture.RallyPoint = tileManager.GetTileCenter(newCapital.Value.x, newCapital.Value.y);
                            }
                            
                            ChangeCultureIdentity(culture.CultureId, -5f);
                        }
                        else
                        {
                            hasValidCapital = false;
                        }
                    }
                    
                    float targetIdentity = CalculateTargetIdentity(culture, units, unitCount, culturalTiles, hasValidCapital, delta);
                    
                    float diff = targetIdentity - culture.CultureIdentity;

                    if (Mathf.Abs(diff) > 0.05f)
                    {
                        float convergenceRate = diff > 0f ? 0.06f : 0.05f;
                        float maxStepPerSec = diff > 0f ? 1.2f : 1.5f;

                        float speed = Mathf.Clamp(diff * convergenceRate, -maxStepPerSec, maxStepPerSec);

                        ChangeCultureIdentity(culture.CultureId, speed * delta);
                    }
                }
            }

            private float CalculateTargetIdentity(CultureData culture, List<Unit> units, int unitCount, int tileCount, bool hasValidCapital, float delta)
            {
                var gov = CoreManager.GovernmentManager != null ? CoreManager.GovernmentManager.GetGovernment(culture.CultureId) : null;
                bool isMarauders = gov != null && gov.Type == Politics.Internal.Government.OrganizationType.Marauders;
                
                if (unitCount == 0) return 0f;
                
                if (tileCount == 0 && !isMarauders) return 0f;
                
                float target = 80f;

                if (!hasValidCapital && !isMarauders)
                {
                    target -= 35f;
                }
                
                int settlementsCount = 0;
                int supportStructuresCount = 0;

                if (tileManager != null)
                {
                    foreach (var pos in tileManager.GetStructureTiles())
                    {
                        TileData st = tileManager.GetTile(pos.x, pos.y);
                        if (st.Owner != culture.CultureId) continue;

                        if (st.Structure == TileStructureType.Settlement) settlementsCount++;
                        else if (st.Structure != TileStructureType.None) supportStructuresCount++;
                    }
                }

                int stateTiles = tileManager.GetTileCountForOwner(culture.CultureId);
                int supportedTiles = 35 + (settlementsCount * 25) + (supportStructuresCount * 8);
                
                target += Mathf.Min(12f, settlementsCount * 4f);

                if (stateTiles > supportedTiles)
                {
                    float overextension = stateTiles - supportedTiles;
                    target -= Mathf.Min(25f, overextension * 0.8f);
                }
                
                int unassimilatedTiles = Mathf.Max(0, stateTiles - tileCount);

                if (unassimilatedTiles > 0)
                {
                    target -= Mathf.Min(15f, unassimilatedTiles * 0.6f);
                }
                
                var player = GetPlayerForCulture(culture.CultureId);
                if (player != null)
                {
                    float food = player.GetFood();
                    float comfortableReserve = Mathf.Max(20f, unitCount * 5f);

                    if (food <= 1f)
                    {
                        target -= 40f;
                    }
                    else if (food < comfortableReserve * 0.5f)
                    {
                        target -= 12f;
                    }
                    else if (food >= comfortableReserve)
                    {
                        target += 12f;
                    }
                }
                
                bool isAtWar = DiplomacyManager.Instance != null && DiplomacyManager.Instance.HasAnyWar(culture.CultureId);

                int militaryCount = 0;
                if (units != null)
                {
                    for (int i = 0; i < units.Count; i++)
                    {
                        if (units[i].IsAlive && units[i] is Human h && h.Group == PopulationGroup.Military) militaryCount++;
                    }
                }

                float militaryRatio = unitCount > 0 ? (float)militaryCount / unitCount : 0f;

                if (isAtWar)
                {
                    target -= 10f;
                }

                if (militaryRatio > 0.6f)
                {
                    target -= (militaryRatio - 0.6f) * 25f;
                }
                
                foreach (var traitEntry in culture.CultureTraits)
                {
                    string traitId = traitEntry.Key;
                    CultureTraitData traitData = traitEntry.Value;

                    if (!traitData.IsActive) continue;

                    traitData.UpdateInfluence(delta, 0.35f);
                    float factor = traitData.Influence / 100f;

                    if (traitId == "BarbarianNation")
                    {
                        target += isAtWar ? (12f * factor) : (-8f * factor);
                    }
                    else if (traitId == "NomadicNation")
                    {
                        target += (tileCount > 40) ? (-15f * factor) : (8f * factor);
                    }
                }

                return Mathf.Clamp(target, 0f, 100f);
            }

            private void ProcessCultureAI()
            {
                if (tileManager == null) tileManager = CoreManager.TileManager;

                if (unitManager == null) unitManager = CoreManager.UnitManager;

                if (tileManager == null || unitManager == null) return;

                foreach (var culture in Cultures)
                {
                    CultureAISystem.ProcessCulture(culture, tileManager, playerManager, unitManager);
                }
            }
        }
    }
}