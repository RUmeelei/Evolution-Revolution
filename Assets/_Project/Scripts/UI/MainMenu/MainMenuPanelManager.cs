using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace ER
{
    namespace UI
    {
        namespace MainMenu
        {
            using Core;
            using Configs;
            using Culture;
            using World;
            using World.Tiles;
            using Unit;

            public class MainMenuPanelManager : MonoBehaviour
            {
                public static MainMenuPanelManager Instance { get; private set; }

                public static List<CultureTrait> CultureTraits = new List<CultureTrait>();

                [Header("Main")]
                [SerializeField] private Button OpenNewGamePanelButton;
                [SerializeField] private Button OpenLoadGamePanelButton;
                [SerializeField] private Button OpenSettingsPanelButton;
                [SerializeField] private Button ExitGameButton;

                [Header("New Game")]
                [SerializeField] private GameObject NewGamePanel;
                [SerializeField] private Button ContinueToCulturesButton;
                [SerializeField] private Button CloseNewGamePanelButton;
                [SerializeField] private Slider WorldSizeSlider;
                [SerializeField] private Toggle NeutralCulturesToggle;
                [SerializeField] private Slider AICulturesCountSlider;
                [SerializeField] private Slider NeutralCulturesCountSlider;

                [Header("World Preview")]
                [SerializeField] private Image WorldPreviewImage;
                [SerializeField] private RawImage WorldPreviewRawImage;
                [SerializeField] private Button RegenerateWorldPreviewButton;

                [Header("Culture Creation")]
                [SerializeField] private GameObject CultureCreationPanel;
                [SerializeField] private Button StartNewGameButton;
                [SerializeField] private Button CloseCultureCreationPanelButton;
                [SerializeField] private TMP_InputField CultureNameInput;
                [SerializeField] private TMP_InputField CultureDescriptionInput;
                [SerializeField] private TextMeshProUGUI CultureTraitsPointsText;

                [Header("Load Game")]
                [SerializeField] private GameObject LoadGamePanel;
                [SerializeField] private Button LoadGameButton;
                [SerializeField] private Button CloseLoadGamePanelButton;

                [Header("Settings")]
                [SerializeField] private GameObject SettingsPanel;
                [SerializeField] private Button CloseSettingsPanelButton;

                private MainConfig mainConfig;
                private Texture2D currentPreviewTexture;

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

                    CoreManager.RegisterMainMenuPanelManager(this);
                }

                void Start()
                {
                    mainConfig = CoreManager.MainConfig;

                    if (OpenNewGamePanelButton != null) OpenNewGamePanelButton.onClick.AddListener(OpenNewGamePanelButtonHandler);
                    if (OpenLoadGamePanelButton != null) OpenLoadGamePanelButton.onClick.AddListener(OpenLoadGamePanelButtonHandler);
                    if (OpenSettingsPanelButton != null) OpenSettingsPanelButton.onClick.AddListener(OpenSettingsPanelButtonHandler);
                    if (ExitGameButton != null) ExitGameButton.onClick.AddListener(ExitGameButtonHandler);

                    if (ContinueToCulturesButton != null) ContinueToCulturesButton.onClick.AddListener(ContinueToCulturesButtonHandler);
                    if (CloseNewGamePanelButton != null) CloseNewGamePanelButton.onClick.AddListener(CloseNewGamePanelButtonHandler);

                    if (WorldSizeSlider != null) WorldSizeSlider.onValueChanged.AddListener(_ => RefreshWorldPreview(false));
                    if (RegenerateWorldPreviewButton != null) RegenerateWorldPreviewButton.onClick.AddListener(() => RefreshWorldPreview(true));

                    if (StartNewGameButton != null) StartNewGameButton.onClick.AddListener(StartNewGameButtonHandler);
                    if (CloseCultureCreationPanelButton != null) CloseCultureCreationPanelButton.onClick.AddListener(CloseCultureCreationPanelButtonHandler);

                    if (LoadGameButton != null) LoadGameButton.onClick.AddListener(LoadGameButtonHandler);
                    if (CloseLoadGamePanelButton != null) CloseLoadGamePanelButton.onClick.AddListener(CloseLoadGamePanelButtonHandler);

                    if (CloseSettingsPanelButton != null) CloseSettingsPanelButton.onClick.AddListener(CloseSettingsPanelButtonHandler);
                }

                private void RefreshWorldPreview(bool rollNewSeed)
                {
                    if (mainConfig == null) mainConfig = CoreManager.MainConfig;

                    int seed = WorldMapGenerator.EnsureSeed(rollNewSeed);
                    int size = WorldSizeSlider != null ? Mathf.Max(10, (int)WorldSizeSlider.value) : 64;

                    if (currentPreviewTexture != null)
                    {
                        Destroy(currentPreviewTexture);
                    }

                    currentPreviewTexture = WorldMapGenerator.GeneratePreviewTexture(size, mainConfig, seed);

                    if (WorldPreviewRawImage != null)
                    {
                        WorldPreviewRawImage.texture = currentPreviewTexture;
                        WorldPreviewRawImage.color = Color.white;
                    }

                    if (WorldPreviewImage != null)
                    {
                        Sprite previewSprite = Sprite.Create(
                            currentPreviewTexture,
                            new Rect(0, 0, currentPreviewTexture.width, currentPreviewTexture.height),
                            new Vector2(0.5f, 0.5f),
                            100f
                        );

                        WorldPreviewImage.sprite = previewSprite;
                        WorldPreviewImage.color = Color.white;
                    }
                }

                private void OpenNewGamePanelButtonHandler()
                {
                    CloseLoadGamePanelButtonHandler();
                    CloseSettingsPanelButtonHandler();

                    if (NewGamePanel != null)
                    {
                        NewGamePanel.gameObject.SetActive(true);
                        RefreshWorldPreview(WorldMapGenerator.CurrentSeed == 0);
                    }
                }

                private void OpenLoadGamePanelButtonHandler()
                {
                    CloseNewGamePanelButtonHandler();
                    CloseSettingsPanelButtonHandler();

                    if (LoadGamePanel != null)
                    {
                        LoadGamePanel.gameObject.SetActive(true);
                    }
                }

                private void OpenSettingsPanelButtonHandler()
                {
                    CloseNewGamePanelButtonHandler();
                    CloseLoadGamePanelButtonHandler();

                    if (SettingsPanel != null)
                    {
                        SettingsPanel.gameObject.SetActive(true);
                    }
                }

                private void ExitGameButtonHandler()
                {
                    Application.Quit();

                    #if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false;
                    #endif
                }

                private void ContinueToCulturesButtonHandler()
                {
                    if (WorldSizeSlider != null)
                    {
                        WorldCreationData.WorldSize = Mathf.Max(10, (int)WorldSizeSlider.value);
                    }

                    if (NeutralCulturesToggle != null)
                    {
                        WorldCreationData.ToggleNeutralCultures = NeutralCulturesToggle.isOn;
                    }

                    if (AICulturesCountSlider != null)
                    {
                        WorldCreationData.AICulturesCount = (int)AICulturesCountSlider.value;
                    }

                    if (NeutralCulturesCountSlider != null)
                    {
                        WorldCreationData.NeutralCulturesCount = (int)NeutralCulturesCountSlider.value;
                    }

                    CloseNewGamePanelButtonHandler();

                    if (CultureCreationPanel != null)
                    {
                        CultureCreationPanel.gameObject.SetActive(true);
                    }
                    else
                    {
                        SceneManager.LoadScene("Game");
                    }
                }

                private void CloseNewGamePanelButtonHandler()
                {
                    if (NewGamePanel != null)
                    {
                        NewGamePanel.gameObject.SetActive(false);
                    }
                }

                private void StartNewGameButtonHandler()
                {
                    string name = CultureNameInput != null ? CultureNameInput.text.Trim() : "";
                    string description = CultureDescriptionInput != null ? CultureNameInput.text.Trim() : "";

                    if (string.IsNullOrEmpty(name)) name = "Player Empire";
                    if (string.IsNullOrEmpty(description)) description = $"The proud people of {name}, ready to shape history.";

                    CultureCreationData.Name = name;
                    CultureCreationData.Description = description;
                    
                    CultureCreationData.Color = Random.ColorHSV(0f, 1f, 0.65f, 1f, 0.75f, 1f);
                    CultureCreationData.CultureEthnicity = UnitEthnicity.European;

                    SceneManager.LoadScene("Game");
                }

                private void CloseCultureCreationPanelButtonHandler()
                {
                    if (CultureCreationPanel != null)
                    {
                        CultureCreationPanel.gameObject.SetActive(false);
                    }

                    OpenNewGamePanelButtonHandler();
                }

                private void LoadGameButtonHandler()
                {
                    SceneManager.LoadScene("Game");
                }

                private void CloseLoadGamePanelButtonHandler()
                {
                    if (LoadGamePanel != null)
                    {
                        LoadGamePanel.gameObject.SetActive(false);
                    }
                }

                private void CloseSettingsPanelButtonHandler()
                {
                    if (SettingsPanel != null)
                    {
                        SettingsPanel.gameObject.SetActive(false);
                    }
                }
            }
        }
    }
}