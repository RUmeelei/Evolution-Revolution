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
                [SerializeField] private Button NewGameButton;
                [SerializeField] private Button LoadGameButton;
                [SerializeField] private Button SettingsButton;
                [SerializeField] private Button ExitGameButton;

                [Header("New Game")]
                [SerializeField] private GameObject NewGamePanel;
                [SerializeField] private Button StartNewGameButton;
                [SerializeField] private Button CloseButton;

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
                    if (NewGameButton != null) NewGameButton.onClick.AddListener(NewGameButtonHandler);
                    if (LoadGameButton != null) LoadGameButton.onClick.AddListener(LoadGameButtonHandler);
                    if (SettingsButton != null) SettingsButton.onClick.AddListener(SettingsButtonHandler);
                    if (ExitGameButton != null) ExitGameButton.onClick.AddListener(ExitGameButtonHandler);
                    if (StartNewGameButton != null) StartNewGameButton.onClick.AddListener(StartNewGamePanelButtonHandler);
                    if (CloseButton != null) CloseButton.onClick.AddListener(CloseNewGamePanelButtonHandler);
                }

                private void NewGameButtonHandler()
                {
                    if (NewGamePanel != null)
                    {
                        NewGamePanel.gameObject.SetActive(true);
                    }
                }

                private void LoadGameButtonHandler()
                {
                    CloseNewGamePanelButtonHandler();
                }

                private void SettingsButtonHandler()
                {
                    CloseNewGamePanelButtonHandler();
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
            }
        }
    }
}