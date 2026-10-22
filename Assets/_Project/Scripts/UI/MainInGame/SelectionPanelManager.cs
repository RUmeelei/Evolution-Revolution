using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

            public class SelectionPanelManager : MonoBehaviour
            {
                public static SelectionPanelManager Instance {get; private set;}

                [Header("Main")]
                [SerializeField] private GameObject SelectionPanel;
                [SerializeField] private GameObject UnitInteractionPanel;
                [SerializeField] private GameObject TilesInteractionPanel;

                [SerializeField] private TextMeshProUGUI ObjectNameText;
                [SerializeField] private TextMeshProUGUI ObjectIdText;
                [SerializeField] private TextMeshProUGUI ObjectCultureText;
                [SerializeField] private TextMeshProUGUI ObjectDescriptionText;

                [SerializeField] private Button UnitMobilizeButton;
                [SerializeField] private Button UnitAutonomyButton;

                [SerializeField] private Image unitMobilizeBtnImage;
                
                [SerializeField] private TextMeshProUGUI unitMobilizeBtnText;
                [SerializeField] private TextMeshProUGUI unitAutonomyBtnText;

                [SerializeField] private Sprite[] unitMobilizeBtnSprites;

                private Image mobilizeButtonBg;
                private Image autonomyButtonBg;

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

                    CoreManager.RegisterSelectionPanelManager(this);
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

                    if (UnitMobilizeButton != null)
                    {
                        UnitMobilizeButton.onClick.AddListener(UnitMobilizeButtonHandler);

                        mobilizeButtonBg = UnitMobilizeButton.GetComponent<Image>();
                    }

                    if (UnitAutonomyButton != null)
                    {
                        UnitAutonomyButton.onClick.AddListener(UnitAutonomyButtonHandler);

                        autonomyButtonBg = UnitAutonomyButton.GetComponent<Image>();
                    }
                }

                void Update()
                {
                    uiUpdateTimer += Time.unscaledDeltaTime;

                    if (uiUpdateTimer >= UIUpdateInterval)
                    {
                        uiUpdateTimer = 0f;

                        UpdateUI();
                    }
                    
                    if (selectionManager != null && selectionManager.SelectedTiles.Count > 0)
                    {
                        if (Input.GetKeyDown(KeyCode.Z)) BuildStructureOnSelectedTile(TileStructureType.Outpost);
                        else if (Input.GetKeyDown(KeyCode.X)) BuildStructureOnSelectedTile(TileStructureType.LogisticsHub);
                        else if (Input.GetKeyDown(KeyCode.V)) BuildStructureOnSelectedTile(TileStructureType.Settlement);
                        else if (Input.GetKeyDown(KeyCode.R)) UpgradeRoadOnSelectedTile();
                        else if (Input.GetKeyDown(KeyCode.T)) UpgradeCommLineOnSelectedTile();
                    }
                }

                public void UpdateUI()
                {
                    if (cultureManager == null || playerManager == null || selectionManager == null || unitManager == null || tileManager == null) return;

                    if (selectionManager.SelectedUnits.Count > 0)
                    {
                        var firstUnit = selectionManager.SelectedUnits[0];

                        if (firstUnit == null) return;

                        var unitOwnerPlayer = cultureManager.GetPlayerForCulture(firstUnit.CultureId);
                        bool isOwnedByHumanPlayer = unitOwnerPlayer != null && !unitOwnerPlayer.PlayerAI;

                        SelectionPanel.SetActive(true);

                        UnitInteractionPanel.SetActive(isOwnedByHumanPlayer);
                        TilesInteractionPanel.SetActive(false);

                        if (isOwnedByHumanPlayer && UnitMobilizeButton != null && unitMobilizeBtnImage != null)
                        {
                            bool mobilize = false;
                            bool autonomy = false;

                            foreach (var u in selectionManager.SelectedUnits)
                            {
                                if (u == null) continue;

                                if (mobilize && autonomy) break;

                                if (u is Human h && h.Group == PopulationGroup.Producers && !mobilize)
                                {
                                    mobilize = true;
                                } 

                                if (!u.IsAutonomous && !autonomy)
                                {
                                    autonomy = true;
                                }
                            }

                            if (mobilize)
                            {
                                if (mobilizeButtonBg != null) mobilizeButtonBg.color = uiConfig.BaseButtonColor;

                                unitMobilizeBtnText.SetText("Mobilize");
                                unitMobilizeBtnText.color = (unitOwnerPlayer.GetIron() >= selectionManager.SelectedUnits.Count * mainConfig.UnitMobilizationCost) ? uiConfig.AdditionalTextColor : uiConfig.WarningColor;

                                if (unitMobilizeBtnSprites.Length > 0) unitMobilizeBtnImage.sprite = unitMobilizeBtnSprites[0];
                            } 
                            else
                            {
                                if (mobilizeButtonBg != null) mobilizeButtonBg.color = uiConfig.HoverButtonColor;

                                unitMobilizeBtnText.SetText("Demobilize");
                                unitMobilizeBtnText.color = uiConfig.AdditionalTextColor;

                                if (unitMobilizeBtnSprites.Length > 1) unitMobilizeBtnImage.sprite = unitMobilizeBtnSprites[1];
                            } 

                            if (autonomy)
                            {
                                if (autonomyButtonBg != null) autonomyButtonBg.color = uiConfig.BaseButtonColor;

                                unitAutonomyBtnText.SetText("Enable Autonomy");
                            } 
                            else
                            {
                                if (autonomyButtonBg != null) autonomyButtonBg.color = uiConfig.HoverButtonColor;

                                unitAutonomyBtnText.SetText("Disable Autonomy");
                            }
                        }

                        if (selectionManager.SelectedUnits.Count == 1)
                        {
                            var u = selectionManager.SelectedUnits[0];
                            var uCulture = cultureManager.GetCulture(u.CultureId);
                            string cName = uCulture != null ? uCulture.CultureName : u.CultureId;
                            string equipInfo = u is Human h ? $"{h.Group} [{h.Equipment.Weapon} / {h.Equipment.Chestplate} Armor]" : "Unit";

                            ObjectNameText.SetText(u.UnitName);
                            ObjectIdText.SetText(u.UnitId);
                            ObjectCultureText.SetText(cName);
                            ObjectDescriptionText.SetText($"[UNIT] {(equipInfo)}, Carry: {u.GetInventory()?.Count ?? 0}/{mainConfig.UnitMaxCarryCapacity}, (Age: {u.GetAge()})\n" + $"HP: {u.Health:F0}/{u.MaxHealth:F0} | Stamina: {u.GetStamina():F0} | Morale: {u.GetMorale():F0}% | State: {u.AIState}");
                        }
                        else
                        {
                            var u = selectionManager.SelectedUnits[0];
                            var uCulture = cultureManager.GetCulture(u.CultureId);
                            string cName = uCulture != null ? uCulture.CultureName : u.CultureId;
                            
                            ObjectNameText.SetText($"[SELECTED SQUAD] {selectionManager.SelectedUnits.Count}");
                            ObjectIdText.SetText("(RMB to Move/Attack).");
                            ObjectCultureText.SetText(cName);
                            ObjectDescriptionText.SetText("Units ready for orders");
                        }
                    }
                    else if (selectionManager.SelectedTiles.Count > 0)
                    {
                        SelectionPanel.SetActive(true);

                        UnitInteractionPanel.SetActive(false);
                        TilesInteractionPanel.SetActive(true);
                        
                        Vector2Int tPos = selectionManager.SelectedTiles[0];
                        TileData t = tileManager.GetTile(tPos.x, tPos.y);

                        var ownerCulture = cultureManager.GetCulture(t.Owner);
                        string ownerName = ownerCulture != null ? ownerCulture.CultureName : "Unclaimed";
                        string structInfo = ownerCulture != null && tPos == ownerCulture.CultureCapital ? "Capital Settlement" : t.Structure != TileStructureType.None ? $"{t.Structure}" : "None)";
                        string roadInfo = $"Road: {t.Road} ({t.RoadTraffic:F0}%) [R] | Comm: {t.CommLine} [T]";
                        string storageInfo = t.Structure == TileStructureType.LogisticsHub ? $" | Hub Storage: {t.TotalStored:F0}/{mainConfig.HubMaxCapacity:F0}" : "";

                        var residentCulture = cultureManager.GetCulture(t.ResidentCultureId);
                        string resCultureName = residentCulture != null ? residentCulture.CultureName : ownerName;

                        ObjectNameText.SetText($"[TILE {tPos.x}, {tPos.y}]");
                        ObjectIdText.SetText($"{ownerName}");
                        ObjectCultureText.SetText(ownerName != resCultureName ? $"{resCultureName}" : "");
                        ObjectDescriptionText.SetText($"Structure: {structInfo}{storageInfo} {roadInfo}\n" + $"Elevation: {t.Elevation:F1}m | Capture Progress: {t.CaptureProgress:F0}% | Terrain: {t.Type}");
                    }
                    else
                    {
                        SelectionPanel.SetActive(false);

                        UnitInteractionPanel.SetActive(false);
                        TilesInteractionPanel.SetActive(false);
                    }
                }

                private void UnitMobilizeButtonHandler()
                {
                    if (selectionManager == null || unitManager == null || cultureManager == null) return;

                    bool mobilize = false;

                    foreach (var u in selectionManager.SelectedUnits)
                    {
                        if (u is Human h && h.Group == PopulationGroup.Producers && !mobilize) mobilize = true;
                    }

                    foreach (var u in selectionManager.SelectedUnits)
                    {
                        if (u is not Human human) continue;

                        var player = cultureManager.GetPlayerForCulture(human.CultureId);

                        if (player == null || player.PlayerAI) continue;

                        if (mobilize) unitManager.MobilizeHuman(human);
                        else unitManager.DemobilizeHuman(human);
                    }

                    UpdateUI();
                }

                private void UnitAutonomyButtonHandler()
                {
                    if (selectionManager == null || unitManager == null || cultureManager == null) return;

                    bool autonomy = false;

                    foreach (var u in selectionManager.SelectedUnits)
                    {
                        if (u != null && !u.IsAutonomous && !autonomy) autonomy = true;
                    }

                    foreach (var u in selectionManager.SelectedUnits)
                    {
                        if (u == null) continue;

                        var player = cultureManager.GetPlayerForCulture(u.CultureId);

                        if (player == null || player.PlayerAI) continue;

                        u.SetAutonomous(autonomy);
                    }

                    UpdateUI();
                }

                public void BuildOutpostButtonHandler() => BuildStructureOnSelectedTile(TileStructureType.Outpost);
                public void BuildHubButtonHandler() => BuildStructureOnSelectedTile(TileStructureType.LogisticsHub);
                public void BuildSettlementButtonHandler() => BuildStructureOnSelectedTile(TileStructureType.Settlement);

                public void UpgradeRoadOnSelectedTile()
                {
                    if (selectionManager == null || selectionManager.SelectedTiles.Count == 0 || tileManager == null || playerManager == null) return;
                    Vector2Int tPos = selectionManager.SelectedTiles[0];
                    TileData t = tileManager.GetTile(tPos.x, tPos.y);

                    TileRoadType nextRoad = t.Road < TileRoadType.PavedRoad ? TileRoadType.PavedRoad : TileRoadType.Railway;
                    if (tileManager.TryBuildRoad(tPos.x, tPos.y, nextRoad, playerManager.NonAIPlayer)) UpdateUI();
                }

                public void UpgradeCommLineOnSelectedTile()
                {
                    if (selectionManager == null || selectionManager.SelectedTiles.Count == 0 || tileManager == null || playerManager == null) return;
                    Vector2Int tPos = selectionManager.SelectedTiles[0];
                    TileData t = tileManager.GetTile(tPos.x, tPos.y);

                    TileCommLineType nextComm = t.CommLine switch
                    {
                        TileCommLineType.None => TileCommLineType.Watchtowers,
                        TileCommLineType.Watchtowers => TileCommLineType.TelegraphLine,
                        _ => TileCommLineType.TelephoneLine
                    };

                    if (tileManager.TryBuildCommLine(tPos.x, tPos.y, nextComm, playerManager.NonAIPlayer)) UpdateUI();
                }

                private void BuildStructureOnSelectedTile(TileStructureType structureType)
                {
                    if (selectionManager == null || selectionManager.SelectedTiles.Count == 0 || tileManager == null || playerManager == null) return;

                    Vector2Int tPos = selectionManager.SelectedTiles[0];

                    if (tileManager.TryBuildStructure(tPos.x, tPos.y, structureType, playerManager.NonAIPlayer))
                    {
                        UpdateUI();
                    }
                }
            }
        }
    }
}