using UnityEngine;
using System.Collections.Generic;

namespace ER
{
    namespace Politics
    {
        namespace External
        {
            namespace Diplomacy
            {
                using Core;
                using Culture;
    
                public class DiplomacyManager : MonoBehaviour
                {
                    public static DiplomacyManager Instance { get; private set; }
    
                    [Header("Main")]
                    public bool EnableLogging;
    
                    private Dictionary<string, RelationState> Relations = new Dictionary<string, RelationState>();
                    private Dictionary<string, float> RelationsTension = new Dictionary<string, float>();
    
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
                        Relations.Clear();
                        RelationsTension.Clear();
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
                        if (string.IsNullOrEmpty(cultureId1) || string.IsNullOrEmpty(cultureId2)) return RelationState.Peace;
    
                        if (cultureId1 == cultureId2 || cultureId1 == "CUL_NONE" || cultureId2 == "CUL_NONE") return RelationState.Peace;
    
                        string key = GetRelationKey(cultureId1, cultureId2);
    
                        if (Relations.TryGetValue(key, out RelationState state))
                        {
                            return state;
                        }
    
                        return RelationState.Peace;
                    }
    
                    public float GetTension(string cultureId1, string cultureId2)
                    {
                        if (string.IsNullOrEmpty(cultureId1) || string.IsNullOrEmpty(cultureId2) || cultureId1 == cultureId2) return 0f;
    
                        string key = GetRelationKey(cultureId1, cultureId2);
    
                        return RelationsTension.TryGetValue(key, out float tension) ? tension : 0f;
                    }
    
                    public void AddBorderTension(string cultureId1, string cultureId2, float amount)
                    {
                        if (string.IsNullOrEmpty(cultureId1) || string.IsNullOrEmpty(cultureId2)) return;
    
                        if (cultureId1 == "CUL_NONE" || cultureId2 == "CUL_NONE" || cultureId1 == cultureId2) return;
    
                        if (IsAtWar(cultureId1, cultureId2)) return;
    
                        string key = GetRelationKey(cultureId1, cultureId2);
    
                        float currentTension = GetTension(cultureId1, cultureId2);
    
                        currentTension = Mathf.Clamp(currentTension + amount, 0f, 100f);
    
                        RelationsTension[key] = currentTension;
    
                        if (currentTension >= 100f)
                        {
                            DeclareWar(cultureId1, cultureId2);
                        }
                    }
    
                    public bool IsAtWar(string cultureId1, string cultureId2)
                    {
                        return GetRelation(cultureId1, cultureId2) == RelationState.War;
                    }
    
                    public bool HasAnyWar(string cultureId)
                    {
                        if (cultureManager == null) cultureManager = CoreManager.CultureManager;
    
                        if (cultureManager == null) return false;
    
                        var cultures = cultureManager.GetActiveCultures();
    
                        for (int i = 0; i < cultures.Count; i++)
                        {
                            if (cultures[i].CultureId != cultureId && IsAtWar(cultureId, cultures[i].CultureId))
                            {
                                return true;
                            }
                        }
                        return false;
                    }
    
                    public void DeclareWar(string cultureId1, string cultureId2)
                    {
                        if (string.IsNullOrEmpty(cultureId1) || string.IsNullOrEmpty(cultureId2)) return;
    
                        if (cultureId1 == "CUL_NONE" || cultureId2 == "CUL_NONE" || cultureId1 == cultureId2) return;
    
                        string key = GetRelationKey(cultureId1, cultureId2);
    
                        if (Relations.TryGetValue(key, out RelationState current) && current == RelationState.War) return;
    
                        Relations[key] = RelationState.War;
    
                        RelationsTension[key] = 100f;
    
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
    
                        if (!Relations.TryGetValue(key, out RelationState current) || current == RelationState.Peace) return;
    
                        Relations[key] = RelationState.Peace;
    
                        RelationsTension[key] = 25f;
    
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
}