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

            public class MainMenuPanelManager : MonoBehaviour
            {
                public static MainMenuPanelManager Instance {get; private set;}

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

                // private int CultureTraitsPoints;

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

                    if (StartNewGameButton != null) StartNewGameButton.onClick.AddListener(StartNewGameButtonHandler);
                    if (CloseCultureCreationPanelButton != null) CloseCultureCreationPanelButton.onClick.AddListener(CloseCultureCreationPanelButtonHandler);

                    if (LoadGameButton != null) LoadGameButton.onClick.AddListener(LoadGameButtonHandler);
                    if (CloseLoadGamePanelButton != null) CloseLoadGamePanelButton.onClick.AddListener(CloseLoadGamePanelButtonHandler);

                    if (CloseSettingsPanelButton != null) CloseSettingsPanelButton.onClick.AddListener(CloseSettingsPanelButtonHandler);
                
                    LoadTraitsFromResources();
                }

                private void OpenNewGamePanelButtonHandler()
                {
                    CloseLoadGamePanelButtonHandler();
                    CloseSettingsPanelButtonHandler();

                    if (NewGamePanel != null)
                    {
                        NewGamePanel.gameObject.SetActive(true);
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
                    // CultureTraitsPoints = 3;

                    if (CultureCreationPanel != null)
                    {
                        CultureCreationPanel.gameObject.SetActive(true);
                    }
                    else SceneManager.LoadScene("Game");

                    if (WorldSizeSlider != null)
                    {
                        WorldCreationData.WorldSize = (int)WorldSizeSlider.value;
                    }

                    if (NeutralCulturesToggle != null)
                    {
                        WorldCreationData.ToggleNeutralCultures = NeutralCulturesToggle;
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
                    string name = CultureNameInput.text.Trim();
                    string description = CultureDescriptionInput.text.Trim();

                    if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(description)) return;

                    CultureCreationData.Name = name;
                    CultureCreationData.Description = description;

                    CultureCreationData.Color = Random.ColorHSV(0f, 1f, 0f, 1f, 0f, 1f);

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

                private void LoadTraitsFromResources()
                {
                    string[] guids = UnityEditor.AssetDatabase.FindAssets("t:CultureTrait");

                    foreach (string guid in guids)
                    {
                        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);

                        CultureTrait trait = UnityEditor.AssetDatabase.LoadAssetAtPath<CultureTrait>(path);

                        if (trait != null) CultureTraits.Add(trait);
                    }
                }
            }
        }
    }
}