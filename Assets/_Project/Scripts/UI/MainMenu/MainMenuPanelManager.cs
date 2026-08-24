using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace ER
{
    namespace UI
    {
        namespace MainMenu
        {
            using Core;

            public class MainMenuPanelManager : MonoBehaviour
            {
                public static MainMenuPanelManager Instance {get; private set;}

                [Header("Main")]
                [SerializeField] private Button OpenNewGamePanelButton;
                [SerializeField] private Button OpenLoadGamePanelButton;
                [SerializeField] private Button OpenSettingsPanelButton;
                [SerializeField] private Button ExitGameButton;

                [Header("New Game")]
                [SerializeField] private GameObject NewGamePanel;
                [SerializeField] private Button StartNewGameButton;
                [SerializeField] private Button CloseNewGamePanelButton;

                [Header("Load Game")]
                [SerializeField] private GameObject LoadGamePanel;
                [SerializeField] private Button LoadGameButton;
                [SerializeField] private Button CloseLoadGamePanelButton;

                [Header("Settings")]
                [SerializeField] private GameObject SettingsPanel;
                [SerializeField] private Button CloseSettingsPanelButton;

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
                    if (OpenNewGamePanelButton != null) OpenNewGamePanelButton.onClick.AddListener(OpenNewGamePanelButtonHandler);
                    if (OpenLoadGamePanelButton != null) OpenLoadGamePanelButton.onClick.AddListener(OpenLoadGamePanelButtonHandler);
                    if (OpenSettingsPanelButton != null) OpenSettingsPanelButton.onClick.AddListener(OpenSettingsPanelButtonHandler);
                    if (ExitGameButton != null) ExitGameButton.onClick.AddListener(ExitGameButtonHandler);

                    if (StartNewGameButton != null) StartNewGameButton.onClick.AddListener(StartNewGamePanelButtonHandler);
                    if (CloseNewGamePanelButton != null) CloseNewGamePanelButton.onClick.AddListener(CloseNewGamePanelButtonHandler);

                    if (LoadGameButton != null) LoadGameButton.onClick.AddListener(LoadGameButtonHandler);
                    if (CloseLoadGamePanelButton != null) CloseLoadGamePanelButton.onClick.AddListener(CloseLoadGamePanelButtonHandler);

                    if (CloseSettingsPanelButton != null) CloseSettingsPanelButton.onClick.AddListener(CloseSettingsPanelButtonHandler);
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

                private void StartNewGamePanelButtonHandler()
                {
                    SceneManager.LoadScene("Game");
                }

                private void CloseNewGamePanelButtonHandler()
                {
                    if (NewGamePanel != null)
                    {
                        NewGamePanel.gameObject.SetActive(false);
                    }
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