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

            public class ResourcesPanelManager : MonoBehaviour
            {
                public static ResourcesPanelManager Instance {get; private set;}

                [Header("Main")]
                [SerializeField] private GameObject ResourcesPanel;
                [SerializeField] private GameObject PoliticsPanel;

                [SerializeField] private TextMeshProUGUI FoodResourceText;
                [SerializeField] private TextMeshProUGUI MaterialsResourceText;
                [SerializeField] private TextMeshProUGUI MoneyResourceText;
                [SerializeField] private TextMeshProUGUI WoodResourceText;
                [SerializeField] private TextMeshProUGUI StoneResourceText;

                [SerializeField] private Button ClosePanelButton;

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

                    CoreManager.RegisterResourcesPanelManager(this);
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

                    if (ClosePanelButton != null)
                    {
                        ClosePanelButton.onClick.AddListener(ClosePanelButtonHandler);
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

                    if (Input.GetKeyUp(KeyCode.C))
                    {
                        if (ResourcesPanel != null)
                        {
                            ResourcesPanel.SetActive(!ResourcesPanel.gameObject.activeInHierarchy);

                            if (ResourcesPanel.activeInHierarchy && PoliticsPanel != null) PoliticsPanel.SetActive(false);

                            UpdateUI();
                        }
                    } 
                }

                private PlayerData GetTargetPlayer()
                {
                    if (selectionManager != null && selectionManager.SelectedUnits.Count > 0 && selectionManager.SelectedUnits[0] != null)
                    {
                        var selectedUnitPlayer = cultureManager.GetPlayerForCulture(selectionManager.SelectedUnits[0].CultureId);

                        if (selectedUnitPlayer != null) return selectedUnitPlayer;
                    }

                    if (selectionManager != null && selectionManager.SelectedTiles.Count > 0 && tileManager != null)
                    {
                        var tile = tileManager.GetTile(selectionManager.SelectedTiles[0].x, selectionManager.SelectedTiles[0].y);

                        var selectedTilePlayer = cultureManager.GetPlayerForCulture(tile.Owner);

                        if (selectedTilePlayer != null) return selectedTilePlayer;
                    }

                    return playerManager.NonAIPlayer;
                }

                private void UpdateUI()
                {
                    if (cultureManager == null || playerManager == null || selectionManager == null || tileManager == null) return;
                
                    PlayerData targetPlayer = GetTargetPlayer();
                
                    if (FoodResourceText != null)
                    {
                        FoodResourceText.SetText($"Food : {(targetPlayer != null ? targetPlayer.GetFood() : 0f):F0}");
                    }
                
                    if (MaterialsResourceText != null)
                    {
                        MaterialsResourceText.SetText($"Iron : {(targetPlayer != null ? targetPlayer.GetIron() : 0f):F0}");
                    }
                
                    if (MoneyResourceText != null)
                    {
                        MoneyResourceText.SetText($"Money : {(targetPlayer != null ? targetPlayer.GetMoney() : 0f):F0}");
                    }
                
                    if (WoodResourceText != null)
                    {
                        WoodResourceText.SetText($"Wood : {(targetPlayer != null ? targetPlayer.GetWood() : 0f):F0}");
                    }
                
                    if (StoneResourceText != null)
                    {
                        StoneResourceText.SetText($"Stone : {(targetPlayer != null ? targetPlayer.GetStone() : 0f):F0}");
                    }
                }

                private void ClosePanelButtonHandler()
                {
                    ResourcesPanel.SetActive(false);
                }
            }
        }
    }
}