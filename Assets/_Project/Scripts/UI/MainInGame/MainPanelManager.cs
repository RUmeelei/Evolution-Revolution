using System.Text;
using TMPro;
using UnityEngine;

namespace ER
{
    namespace UI
    {
        namespace InGame
        {
            using Core;
            using Culture;
            using Configs;
            using Players;
            using Unit;
            using World.Tiles;
            using Controls;
            using Politics.External.Diplomacy;

            public class MainPanelManager : MonoBehaviour
            {
                public static MainPanelManager Instance { get; private set; }

                [Header("Main")]
                [SerializeField] private TextMeshProUGUI CultureNameText;
                [SerializeField] private TextMeshProUGUI CultureIdText;
                [SerializeField] private TextMeshProUGUI CultureIdentityText;
                [SerializeField] private TextMeshProUGUI CultureDescriptionText;

                private CultureManager cultureManager;
                private PlayerManager playerManager;
                private UnitManager unitManager;
                private TileManager tileManager;
                private SelectionManager selectionManager;
                private DiplomacyManager diplomacyManager;

                private MainConfig mainConfig;
                private UIConfig uiConfig;

                private float uiUpdateTimer = 0f;
                
                private const float UIUpdateInterval = 0.15f;

                private readonly StringBuilder sb = new StringBuilder(256);

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

                    CoreManager.RegisterMainPanelManager(this);
                }

                void Start()
                {
                    mainConfig = CoreManager.MainConfig;
                    uiConfig = CoreManager.UIConfig;

                    cultureManager = CoreManager.CultureManager;
                    playerManager = CoreManager.PlayerManager;
                    unitManager = CoreManager.UnitManager;
                    tileManager = CoreManager.TileManager;
                    selectionManager = CoreManager.SelectionManager;
                    diplomacyManager = CoreManager.DiplomacyManager;
                }

                void Update()
                {
                    uiUpdateTimer += Time.unscaledDeltaTime;

                    if (uiUpdateTimer >= UIUpdateInterval)
                    {
                        uiUpdateTimer = 0f;

                        UpdateUI();
                    }
                }

                private void UpdateUI()
                {
                    if (cultureManager == null || playerManager == null) return;

                    var player = playerManager.GetPlayer("PLAYER_0001");

                    var culture = player != null && player.PlayerCulture != null ? player.PlayerCulture : cultureManager.GetCulture("CUL_0001");

                    if (culture == null) return;

                    if (mainConfig.SpectatorMode)
                    {
                        CultureNameText.SetText($"Spectator");
                        CultureIdText.SetText($"Spectator");
                        CultureIdentityText.SetText($"Spectator");
                        CultureDescriptionText.SetText($"Spectator");

                        return;
                    }
                    
                    if (CultureNameText != null)
                    {
                        Color color = culture.CultureDefeated ? uiConfig.ErrorColor : uiConfig.MainTextColor;

                        string text = culture.CultureDefeated ? "Defeated Nation" : $"{culture.CultureName}";

                        CultureNameText.SetText(text);

                        CultureNameText.color = color;
                    }
                    
                    if (CultureIdText != null)
                    {
                        Color color = culture.CultureDefeated ? uiConfig.ErrorColor : uiConfig.MainTextColor;

                        string text = $"ID: {culture.CultureId}";

                        CultureIdText.SetText(text);

                        CultureIdText.color = color;
                    }
                    
                    if (CultureIdentityText != null)
                    {
                        Color color = uiConfig.SecondTextColor;

                        float identity = culture.CultureIdentity;

                        if (identity < mainConfig.WarningIdentityThreshold) color = identity < mainConfig.ErrorIdentityThreshold ? uiConfig.ErrorColor : uiConfig.WarningColor;

                        string text = $"Identity: {identity:F1}%";

                        CultureIdentityText.SetText(text);

                        CultureIdentityText.color = color;
                    }
                    
                    if (CultureDescriptionText != null)
                    {
                        CultureDescriptionText.color = uiConfig.MainTextColor;
                        
                        // if (selectionManager != null && selectionManager.SelectedUnits.Count > 0)
                        // {
                        //     if (selectionManager.SelectedUnits.Count == 1)
                        //     {
                        //         var u = selectionManager.SelectedUnits[0];
                        
                        //         var uCulture = cultureManager.GetCulture(u.CultureId);
                        
                        //         string cName = uCulture != null ? uCulture.CultureName : u.CultureId;
                        
                        //         CultureDescriptionText.SetText($"[UNIT] {(u is Human h ? h.Group.ToString() : "Unit")}, Carry: {u.GetInventory()?.Count ?? 0}/3, {u.UnitName} ({cName}, Age: {u.GetAge()})\n" + $"HP: {u.Health:F0}/{u.MaxHealth:F0}  |  Stamina: {u.GetStamina():F0}  |  Morale: {u.GetMorale():F0}%  |  State: {u.AIState}");
                        //     }
                        //     else
                        //     {
                        //         CultureDescriptionText.SetText($"[SELECTED SQUAD] {selectionManager.SelectedUnits.Count} units ready for orders (RMB to Move/Attack, Shift+RMB to set Rally Point).");
                        //     }
                        // }
                        //else if (selectionManager != null && selectionManager.SelectedTiles.Count > 0 && tileManager != null)
                        //{
                        //    Vector2Int tPos = selectionManager.SelectedTiles[0];
                        
                        //    TileData t = tileManager.GetTile(tPos.x, tPos.y);
                        
                        //    var ownerCulture = cultureManager.GetCulture(t.Owner);
                        
                        //    string ownerName = ownerCulture != null ? ownerCulture.CultureName : "Neutral (Unclaimed)";
                        
                        //    CultureDescriptionText.SetText($"[TILE {tPos.x}, {tPos.y}] Terrain: {t.Type}  |  Elevation: {t.Elevation:F1}m\n" + $"Owner: {ownerName}  |  Capture Progress: {t.CaptureProgress:F0}%");
                        //}
                        if (culture.CultureDefeated)
                        {
                            CultureDescriptionText.color = uiConfig.ErrorColor;

                            CultureDescriptionText.SetText("A ruins of defeated nation. The death of civilization.");
                        }
                        else
                        {
                            CultureDescriptionText.SetText(culture.CultureDescription);
                        }
                    }
                }
            }
        }
    }
}