using System;
using UnityEngine;
using System.Collections.Generic;

namespace ER
{
    namespace Politics
    {
        namespace Internal
        {
            namespace Government
            {
                using Core;
                using Configs;
                using Culture;
                using Players;
                using World.Tiles;
                using Unit;
    
                public class GovernmentManager : MonoBehaviour
                {
                    public static GovernmentManager Instance {get; private set;}
    
                    [Header("Main")]
                    public bool EnableLogging;
    
                    private Dictionary<string, GovernmentData> GovernmentsByCulture = new Dictionary<string, GovernmentData>();
    
                    private CultureManager cultureManager;
                    private PlayerManager playerManager;
                    private TileManager tileManager;
                    private UnitManager unitManager;
    
                    private MainConfig mainConfig;
    
                    public event Action<string, OrganizationType> OnGovernmentChanged;
                    public event Action<string> OnGovernmentCollapsed;
    
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
    
                        CoreManager.RegisterGovernmentManager(this);
                    }
    
                    void Start()
                    {
                        mainConfig = CoreManager.MainConfig;
    
                        cultureManager = CoreManager.CultureManager;
                        playerManager = CoreManager.PlayerManager;
                        tileManager = CoreManager.TileManager;
                        unitManager = CoreManager.UnitManager;
    
                        if (cultureManager != null)
                        {
                            cultureManager.OnCultureTick += ProcessGovernmentTick;
                        }
                    }
    
                    void OnDestroy()
                    {
                        if (cultureManager != null)
                        {
                            cultureManager.OnCultureTick -= ProcessGovernmentTick;
                        }
                    }
    
                    public void Initialize()
                    {
                        GovernmentsByCulture.Clear();
    
                        mainConfig = CoreManager.MainConfig;
    
                        cultureManager = CoreManager.CultureManager;
                        playerManager = CoreManager.PlayerManager;
                        tileManager = CoreManager.TileManager;
                        unitManager = CoreManager.UnitManager;
    
                        if (cultureManager != null)
                        {
                            foreach (var culture in cultureManager.GetActiveCultures())
                            {
                                GetOrCreateGovernment(culture);
                            }
                        }
                    }
    
                    public GovernmentData GetOrCreateGovernment(CultureData culture)
                    {
                        if (culture == null) return null;
    
                        if (!GovernmentsByCulture.TryGetValue(culture.CultureId, out var gov))
                        {
                            gov = new GovernmentData(culture.CultureId, culture.CultureName, OrganizationType.Tribe);
    
                            GovernmentsByCulture[culture.CultureId] = gov;
                        }
    
                        return gov;
                    }
    
                    public GovernmentData GetGovernment(string cultureId)
                    {
                        if (string.IsNullOrEmpty(cultureId)) return null;
    
                        if (GovernmentsByCulture.TryGetValue(cultureId, out var gov))
                        {
                            return gov;
                        }
    
                        var culture = cultureManager != null ? cultureManager.GetCulture(cultureId) : null;
    
                        return culture != null ? GetOrCreateGovernment(culture) : null;
                    }
    
                    public static float GetReformCost(OrganizationType targetType)
                    {
                        return targetType switch
                        {
                            OrganizationType.State => 40f,
                            OrganizationType.Corporation => 35f,
                            OrganizationType.Marauders => 15f,
                            _ => 0f
                        };
                    }
    
                    public bool CanReformGovernment(string cultureId, OrganizationType targetType, out string reason)
                    {
                        reason = "Ready";
    
                        var gov = GetGovernment(cultureId);
    
                        if (gov == null)
                        {
                            reason = "No culture";
                            return false;
                        }
    
                        if (gov.Type == targetType)
                        {
                            reason = "Already active";
                            return false;
                        }
    
                        var player = playerManager != null ? playerManager.GetPlayerForCulture(cultureId) : null;
                        float cost = GetReformCost(targetType);
    
                        if (player != null && player.GetIron() < cost)
                        {
                            reason = $"Need {cost:F0} Mat";
                            return false;
                        }
    
                        int tiles = tileManager != null ? tileManager.GetTileCountForOwner(cultureId) : 0;
    
                        if (targetType == OrganizationType.State)
                        {
                            bool hasSettlement = HasStructure(cultureId, TileStructureType.Settlement);
    
                            if (tiles < 10 && !hasSettlement)
                            {
                                reason = "Need 10+ Tiles or Settlement";
                                return false;
                            }
                        }
                        else if (targetType == OrganizationType.Corporation)
                        {
                            bool hasHub = HasStructure(cultureId, TileStructureType.LogisticsHub);
    
                            if (!hasHub)
                            {
                                reason = "Need Logistics Hub";
                                return false;
                            }
                        }
    
                        return true;
                    }
    
                    public bool TryReformGovernment(string cultureId, OrganizationType targetType)
                    {
                        if (!CanReformGovernment(cultureId, targetType, out _)) return false;
    
                        var culture = cultureManager.GetCulture(cultureId);
                        var gov = GetOrCreateGovernment(culture);
                        var player = playerManager != null ? playerManager.GetPlayerForCulture(cultureId) : null;
    
                        float cost = GetReformCost(targetType);
    
                        if (player != null && cost > 0f)
                        {
                            player.SpendResource("Materials", cost);
                        }
    
                        gov.Type = targetType;
                        gov.UpdateOrganizationName(culture.CultureName);
    
                        if (EnableLogging)
                        {
                            Debug.Log($"[{culture.CultureName}] Reformed into {gov.OrganizationName} ({targetType})");
                        }
    
                        OnGovernmentChanged?.Invoke(cultureId, targetType);
    
                        return true;
                    }
    
                    private bool HasStructure(string cultureId, TileStructureType type)
                    {
                        if (tileManager == null) return false;
    
                        foreach (var pos in tileManager.GetStructureTiles())
                        {
                            TileData t = tileManager.GetTile(pos.x, pos.y);
    
                            if (t.Owner == cultureId && t.Structure == type) return true;
                        }
    
                        return false;
                    }
    
                    private void ProcessGovernmentTick(float delta)
                    {
                        if (cultureManager == null || tileManager == null) return;
    
                        foreach (var culture in cultureManager.GetActiveCultures())
                        {
                            var gov = GetOrCreateGovernment(culture);
    
                            int tiles = tileManager.GetTileCountForOwner(culture.CultureId);
                            
                            if (gov.Type == OrganizationType.State && tiles < 3 && !HasStructure(culture.CultureId, TileStructureType.Settlement))
                            {
                                CollapseGovernment(culture, gov);
                            }
                            else if (gov.Type == OrganizationType.Corporation && tiles < 2 && !HasStructure(culture.CultureId, TileStructureType.LogisticsHub))
                            {
                                CollapseGovernment(culture, gov);
                            }
    
                            var player = playerManager != null ? playerManager.GetPlayerForCulture(culture.CultureId) : null;
    
                            if (player != null && player.PlayerAI)
                            {
                                ProcessAIGovernment(culture, gov);
                            }
                        }
                    }
    
                    private void CollapseGovernment(CultureData culture, GovernmentData gov)
                    {
                        OrganizationType fallbackType = culture.HasTrait("BarbarianNation") || culture.HasTrait("NomadicNation") ? OrganizationType.Marauders : OrganizationType.Tribe;
    
                        gov.Type = fallbackType;
                        gov.UpdateOrganizationName(culture.CultureName);
                        
                        cultureManager.ChangeCultureIdentity(culture.CultureId, -15f);
    
                        if (EnableLogging)
                        {
                            Debug.Log($"[{culture.CultureName}] State collapsed! People reorganized into {gov.OrganizationName}");
                        }
    
                        OnGovernmentCollapsed?.Invoke(culture.CultureId);
                        OnGovernmentChanged?.Invoke(culture.CultureId, fallbackType);
                    }
    
                    private void ProcessAIGovernment(CultureData culture, GovernmentData gov)
                    {
                        if (gov.Type != OrganizationType.Tribe) return;
    
                        if (culture.HasTrait("BarbarianNation") && CanReformGovernment(culture.CultureId, OrganizationType.Marauders, out _))
                        {
                            TryReformGovernment(culture.CultureId, OrganizationType.Marauders);
    
                            return;
                        }
    
                        if (CanReformGovernment(culture.CultureId, OrganizationType.State, out _))
                        {
                            TryReformGovernment(culture.CultureId, OrganizationType.State);
    
                            return;
                        }
    
                        if (CanReformGovernment(culture.CultureId, OrganizationType.Corporation, out _))
                        {
                            TryReformGovernment(culture.CultureId, OrganizationType.Corporation);
                        }
                    }
                }
            }
        }
    }
}