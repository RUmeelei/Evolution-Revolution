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

                void Awake()
                {
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
                }

                private void NewGameButtonHandler()
                {
                    SceneManager.LoadScene("Game");
                }

                private void LoadGameButtonHandler()
                {
                    
                }

                private void SettingsButtonHandler()
                {
                    
                }

                private void ExitGameButtonHandler()
                {
                    Application.Quit();

                    #if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false;
                    #endif
                }
            }
        }
    }
}