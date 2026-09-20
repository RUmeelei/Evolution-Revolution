using UnityEngine;
using System.Collections.Generic;

namespace ER
{
    namespace Politics
    {
        namespace Diplomacy
        {
            using Core;
            using Culture;

            public class DiplomacyManager : MonoBehaviour
            {
                public static DiplomacyManager Instance {get; private set;}

                [Header("Main")]
                public bool EnableLogging;

                private Dictionary<string, RelationState> relations = new Dictionary<string, RelationState>();

                public event System.Action<string, string> OnWarDeclared;
                public event System.Action<string, string> OnPeaceMade;

                private CultureManager cultureManager;

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

                    CoreManager.RegisterDiplomacyManager(this);
                }

                void Start()
                {
                    cultureManager = CoreManager.CultureManager;
                }

                public void Initialize()
                {
                    
                }

                void Update()
                {
                    if (Input.GetKeyDown(KeyCode.F12))
                    {
                        DeclareWarBetweenRandomCultures();
                    }
                
                    if (Input.GetKeyDown(KeyCode.F11))
                    {
                        MakePeaceBetweenAllCultures();
                    }
                }

                public void DeclareWarBetweenRandomCultures()
                {
                    var cultures = cultureManager.GetActiveCultures();

                    if (cultures.Count < 2)
                    {
                        Debug.LogWarning("Not enough cultures to declare war");

                        return;
                    }

                    int index1 = UnityEngine.Random.Range(0, cultures.Count);

                    int index2 = index1;
                    
                    while (index2 == index1)
                    {
                        index2 = UnityEngine.Random.Range(0, cultures.Count);
                    }

                    DeclareWar(cultures[index1].CultureId, cultures[index2].CultureId);
                }

                public void MakePeaceBetweenAllCultures()
                {
                    var cultures = cultureManager.GetActiveCultures();

                    for (int i = 0; i < cultures.Count; i++)
                    {
                        for (int j = i + 1; j < cultures.Count; j++)
                        {
                            MakePeace(cultures[i].CultureId, cultures[j].CultureId);
                        }
                    }
                }

                private string GetRelationKey(string cultureId1, string cultureId2)
                {
                    if (string.Compare(cultureId1, cultureId2) <= 0)
                    {
                        return cultureId1 + "_" + cultureId2;
                    }
                    else
                    {
                        return cultureId2 + "_" + cultureId1;
                    }
                }

                public RelationState GetRelation(string cultureId1, string cultureId2)
                {
                    if (cultureId1 == cultureId2) return RelationState.Peace;

                    string key = GetRelationKey(cultureId1, cultureId2);

                    if (relations.TryGetValue(key, out RelationState state))
                    {
                        return state;
                    }

                    return RelationState.Peace;
                }

                public bool IsAtWar(string cultureId1, string cultureId2)
                {
                    return GetRelation(cultureId1, cultureId2) == RelationState.War;
                }

                public void DeclareWar(string cultureId1, string cultureId2)
                {
                    if (cultureId1 == cultureId2) return;

                    string key = GetRelationKey(cultureId1, cultureId2);
                    
                    if (relations.TryGetValue(key, out RelationState current) && current == RelationState.War) return;

                    relations[key] = RelationState.War;

                    if (EnableLogging)
                    {
                        Debug.Log($"War declared: {cultureId1} vs {cultureId2}");
                    }

                    OnWarDeclared?.Invoke(cultureId1, cultureId2);
                }

                public void MakePeace(string cultureId1, string cultureId2)
                {
                    if (cultureId1 == cultureId2) return;

                    string key = GetRelationKey(cultureId1, cultureId2);
                    
                    if (!relations.TryGetValue(key, out RelationState current) || current == RelationState.Peace) return;

                    relations[key] = RelationState.Peace;

                    if (EnableLogging)
                    {
                        Debug.Log($"Peace made: {cultureId1} vs {cultureId2}");
                    }

                    OnPeaceMade?.Invoke(cultureId1, cultureId2);
                }
            }
        }
    }
}