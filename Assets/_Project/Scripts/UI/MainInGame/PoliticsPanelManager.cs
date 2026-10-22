using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

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
            using World.Tiles;
            using Controls;
            using Politics.Internal.Government;
            using Politics.External.Diplomacy;

            public class PoliticsPanelManager : MonoBehaviour
            {
                public static PoliticsPanelManager Instance {get; private set;}

                [Header("Main")]
                [SerializeField] private GameObject PoliticsPanel;
                [SerializeField] private GameObject ResourcesPanel;
                [SerializeField] private Button ClosePanelButton;

                [Header("Internal Politics UI")]
                [SerializeField] private TextMeshProUGUI OrganizationTitleText;
                [SerializeField] private TextMeshProUGUI InternalInfoText;

                [SerializeField] private Button FormTribeButton;
                [SerializeField] private Button FormStateButton;
                [SerializeField] private Button FormCorporationButton;
                [SerializeField] private Button FormMaraudersButton;
                [SerializeField] private Button CycleStanceButton;

                [Header("External Politics UI")]
                [SerializeField] private TextMeshProUGUI ForeignCultureNameText;
                [SerializeField] private TextMeshProUGUI ForeignRelationInfoText;

                [SerializeField] private Button NextForeignCultureButton;
                [SerializeField] private Button ToggleWarPeaceButton;
                [SerializeField] private TextMeshProUGUI ToggleWarPeaceBtnText;

                private CultureManager cultureManager;
                private PlayerManager playerManager;
                private TileManager tileManager;
                private DiplomacyManager diplomacyManager;
                private GovernmentManager governmentManager;
                private SelectionManager selectionManager;

                private MainConfig mainConfig;
                private UIConfig uiConfig;

                private int selectedForeignIndex = 0;

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

                    CoreManager.RegisterPoliticsPanelManager(this);
                }

                void Start()
                {
                    mainConfig = CoreManager.MainConfig;
                    uiConfig = CoreManager.UIConfig;

                    cultureManager = CoreManager.CultureManager;
                    playerManager = CoreManager.PlayerManager;
                    tileManager = CoreManager.TileManager;
                    diplomacyManager = CoreManager.DiplomacyManager;
                    governmentManager = CoreManager.GovernmentManager;
                    selectionManager = CoreManager.SelectionManager;

                    if (ClosePanelButton != null) ClosePanelButton.onClick.AddListener(ClosePanelButtonHandler);

                    if (FormTribeButton != null) FormTribeButton.onClick.AddListener(() => TryReformPlayer(OrganizationType.Tribe));
                    if (FormStateButton != null) FormStateButton.onClick.AddListener(() => TryReformPlayer(OrganizationType.State));
                    if (FormCorporationButton != null) FormCorporationButton.onClick.AddListener(() => TryReformPlayer(OrganizationType.Corporation));
                    if (FormMaraudersButton != null) FormMaraudersButton.onClick.AddListener(() => TryReformPlayer(OrganizationType.Marauders));

                    if (CycleStanceButton != null) CycleStanceButton.onClick.AddListener(CycleStanceButtonHandler);

                    if (NextForeignCultureButton != null) NextForeignCultureButton.onClick.AddListener(NextForeignCultureButtonHandler);
                    if (ToggleWarPeaceButton != null) ToggleWarPeaceButton.onClick.AddListener(ToggleWarPeaceButtonHandler);
                }

                void Update()
                {
                    uiUpdateTimer += Time.unscaledDeltaTime;

                    if (uiUpdateTimer >= UIUpdateInterval)
                    {
                        uiUpdateTimer = 0f;

                        UpdateUI();
                    }

                    if (Input.GetKeyUp(KeyCode.P))
                    {
                        if (PoliticsPanel != null)
                        {
                            PoliticsPanel.SetActive(!PoliticsPanel.activeInHierarchy);

                            if (PoliticsPanel.activeInHierarchy && ResourcesPanel != null) ResourcesPanel.SetActive(false);

                            UpdateUI();
                        }
                    }
                }

                private CultureData GetPlayerCulture()
                {
                    if (playerManager != null && playerManager.NonAIPlayer != null && playerManager.NonAIPlayer.PlayerCulture != null)
                    {
                        return playerManager.NonAIPlayer.PlayerCulture;
                    }

                    return cultureManager != null ? cultureManager.GetCulture("CUL_0001") : null;
                }

                private List<CultureData> GetForeignCultures(string ownCultureId)
                {
                    var list = new List<CultureData>();

                    if (cultureManager == null) return list;

                    foreach (var c in cultureManager.GetActiveCultures())
                    {
                        if (c.CultureId != ownCultureId)
                        {
                            list.Add(c);
                        }
                    }

                    return list;
                }

                public void UpdateUI()
                {
                    if (PoliticsPanel == null || !PoliticsPanel.activeInHierarchy) return;

                    if (cultureManager == null || governmentManager == null || diplomacyManager == null || selectionManager == null) return;

                    var playerCulture = GetPlayerCulture();

                    if (playerCulture == null) return;

                    var gov = governmentManager.GetOrCreateGovernment(playerCulture);

                    // INTERNAL

                    if (OrganizationTitleText != null)
                    {
                        OrganizationTitleText.SetText($"{gov.OrganizationName} [{gov.Type}]");
                    }

                    if (InternalInfoText != null)
                    {
                        governmentManager.CanReformGovernment(playerCulture.CultureId, OrganizationType.State, out string stateReq);
                        governmentManager.CanReformGovernment(playerCulture.CultureId, OrganizationType.Corporation, out string corpReq);

                        InternalInfoText.SetText(
                            $"Stance: {playerCulture.Stance} | Identity: {playerCulture.CultureIdentity:F1}%\n" +
                            $"Bonuses: Influence x{gov.GetInfluenceMultiplier():F2} | Logistics x{gov.GetLogisticsYieldMultiplier():F2} | Loot x{gov.GetLootMultiplier():F1}\n"
                        );
                    }

                    // EXTERNAL

                    var foreignCultures = GetForeignCultures(playerCulture.CultureId);

                    if (foreignCultures.Count == 0)
                    {
                        if (ForeignCultureNameText != null) ForeignCultureNameText.SetText("No Foreign Cultures");
                        if (ForeignRelationInfoText != null) ForeignRelationInfoText.SetText("You are the only surviving culture in the world.");
                        if (ToggleWarPeaceButton != null) ToggleWarPeaceButton.gameObject.SetActive(false);

                        return;
                    }

                    if (ToggleWarPeaceButton != null) ToggleWarPeaceButton.gameObject.SetActive(true);

                    if (selectedForeignIndex >= foreignCultures.Count) selectedForeignIndex = 0;

                    var targetCulture = (selectionManager.SelectedTiles.Count > 0 && tileManager.GetTile(selectionManager.SelectedTiles[0].x, selectionManager.SelectedTiles[0].y).Owner != playerCulture.CultureId) ? cultureManager.GetCulture(tileManager.GetTile(selectionManager.SelectedTiles[0].x, selectionManager.SelectedTiles[0].y).Owner) : foreignCultures[selectedForeignIndex];
                    bool atWar = playerCulture != null && targetCulture != null ? diplomacyManager.IsAtWar(playerCulture.CultureId, targetCulture.CultureId) : false;
                    float tension = playerCulture != null && targetCulture != null ? diplomacyManager.GetTension(playerCulture.CultureId, targetCulture.CultureId) : 0f;
                    var targetGov = governmentManager.GetOrCreateGovernment(targetCulture);

                    if (ForeignCultureNameText != null)
                    {
                        ForeignCultureNameText.SetText($"{targetGov.OrganizationName} ({selectedForeignIndex + 1}/{foreignCultures.Count})");
                        ForeignCultureNameText.color = targetCulture.CultureColor;
                    }

                    if (ForeignRelationInfoText != null)
                    {
                        int targetTiles = tileManager != null ? tileManager.GetTileCountForOwner(targetCulture.CultureId) : 0;
                        string status = atWar ? "AT WAR" : "PEACE";

                        ForeignRelationInfoText.SetText(
                            $"Status: [{status}] | Border Tension: {tension:F0}%\n" +
                            $"Target Stance: {targetCulture.Stance} | Territory: {targetTiles} tiles | Identity: {targetCulture.CultureIdentity:F0}%"
                        );
                    }

                    if (ToggleWarPeaceBtnText != null)
                    {
                        ToggleWarPeaceBtnText.SetText(atWar ? "Make Peace" : "Declare War");
                    }
                }

                private void TryReformPlayer(OrganizationType targetType)
                {
                    var playerCulture = GetPlayerCulture();

                    if (playerCulture == null || governmentManager == null) return;

                    governmentManager.TryReformGovernment(playerCulture.CultureId, targetType);

                    UpdateUI();
                }

                private void CycleStanceButtonHandler()
                {
                    var playerCulture = GetPlayerCulture();

                    if (playerCulture == null) return;

                    playerCulture.Stance = playerCulture.Stance switch
                    {
                        CultureStance.Passive => CultureStance.Defensive,
                        CultureStance.Defensive => CultureStance.Aggressive,
                        _ => CultureStance.Passive
                    };

                    UpdateUI();
                }

                private void NextForeignCultureButtonHandler()
                {
                    selectedForeignIndex++;

                    UpdateUI();
                }

                private void ToggleWarPeaceButtonHandler()
                {
                    var playerCulture = GetPlayerCulture();

                    if (playerCulture == null || diplomacyManager == null) return;

                    var foreignCultures = GetForeignCultures(playerCulture.CultureId);

                    if (foreignCultures.Count == 0) return;

                    if (selectedForeignIndex >= foreignCultures.Count) selectedForeignIndex = 0;

                    var targetCulture = foreignCultures[selectedForeignIndex];

                    if (diplomacyManager.IsAtWar(playerCulture.CultureId, targetCulture.CultureId))
                    {
                        diplomacyManager.MakePeace(playerCulture.CultureId, targetCulture.CultureId);
                    }
                    else
                    {
                        diplomacyManager.DeclareWar(playerCulture.CultureId, targetCulture.CultureId);
                    }

                    UpdateUI();
                }

                private void ClosePanelButtonHandler()
                {
                    if (PoliticsPanel != null) PoliticsPanel.SetActive(false);
                }
            }
        }
    }
}