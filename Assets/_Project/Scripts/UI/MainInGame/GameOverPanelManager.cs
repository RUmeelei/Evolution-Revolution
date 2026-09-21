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
            using Game;
            using Culture;
            using Configs;

            public class GameOverPanelManager : MonoBehaviour
            {
                public static GameOverPanelManager Instance {get; private set;}

                [Header("UI References")]
                [SerializeField] private GameObject GameOverPanel;
                [SerializeField] private TextMeshProUGUI GameOverTitleText;
                [SerializeField] private TextMeshProUGUI GameOverDescriptionText;
                [SerializeField] private Button GameOverReturnButton;

                private UIConfig uiConfig;

                private VictoryManager victoryManager;
                private CultureManager cultureManager;

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

                    CoreManager.RegisterGameOverPanelManager(this);
                }

                void Start()
                {
                    uiConfig = CoreManager.UIConfig;

                    victoryManager = CoreManager.VictoryManager;

                    cultureManager = CoreManager.CultureManager;

                    if (victoryManager != null)
                    {
                        victoryManager.OnGameOver += ShowGameOverScreen;
                    }

                    if (GameOverReturnButton != null)
                    {
                        GameOverReturnButton.onClick.AddListener(OnReturnToMenuClicked);
                    }

                    if (GameOverPanel != null)
                    {
                        GameOverPanel.SetActive(false);
                    }
                }

                void OnDestroy()
                {
                    if (victoryManager != null)
                    {
                        victoryManager.OnGameOver -= ShowGameOverScreen;
                    }
                }

                private void ShowGameOverScreen()
                {
                    if (GameOverPanel == null || GameOverTitleText == null) return;

                    GameOverPanel.SetActive(true);

                    string winnerPlayerId = victoryManager.WinnerPlayerId;

                    if (winnerPlayerId != null)
                    {
                        var winnerCulture = cultureManager.GetActiveCultures();

                        if (winnerCulture.Count > 0)
                        {
                            var winnerPlayer = cultureManager.GetPlayerForCulture(winnerCulture[0].CultureId);

                            if (winnerPlayer != null && !winnerPlayer.PlayerAI)
                            {
                                GameOverTitleText.text = "VICTORY";
                                GameOverTitleText.color = uiConfig.SuccessColor;

                                GameOverDescriptionText.text = $"THE ERA IS OVER\n\nEternal glory to your people\n\nThe chronicles of the world close their final page\n\nThe last foreign flag has been lowered. The last foreign language has fallen silent. The last foreign ruler has knelt or turned to dust. Not a single state remains that would dare call itself your equal. Not a single culture remains that has not dissolved into yours.\n\nThe world is no longer divided by borders.\n\nIt is entirely - yours.";
                            }
                            else
                            {
                                GameOverTitleText.text = "DEFEAT";
                                GameOverTitleText.color = uiConfig.ErrorColor;

                                GameOverDescriptionText.text = $"THE ERA IS OVER\n\nA ruins of defeated nation\n\nThe death of civilization\n\nInevitable, sad or joyful, it doesn't matter anymore\n\nYour last flag has been lowered. Your last language has fallen silent. Your last ruler - deposed, killed, or exiled. Not a single city remains that would dare call itself yours. Not a single soul remains that would remember your name.\n\nThe world is no longer divided by borders.\n\nIt is entirely someone else's.";
                            }
                        }
                        else
                        {
                            GameOverTitleText.text = "DRAW";
                            GameOverTitleText.color = uiConfig.WarningColor;

                            GameOverDescriptionText.text = $"THE ERA IS OVER\n\nA world divided, a history unwritten\n\nThe chronicles of the world close their final page\n\nNo flag has been lowered. No language has fallen silent. No ruler has knelt or turned to dust. The last states still stand, watching each other across ruined borders. No culture has dissolved into another. No people has been erased from memory. No one has won - and no one has lost.\n\nThe world is still divided by borders.\n\nAnd it belongs to no one.";
                        }
                    }
                    else
                    {
                        GameOverTitleText.text = "DRAW";
                        GameOverTitleText.color = uiConfig.WarningColor;

                        GameOverDescriptionText.text = $"THE ERA IS OVER\n\nA world divided, a history unwritten\n\nThe chronicles of the world close their final page\n\nNo flag has been lowered. No language has fallen silent. No ruler has knelt or turned to dust. The last states still stand, watching each other across ruined borders. No culture has dissolved into another. No people has been erased from memory. No one has won - and no one has lost.\n\nThe world is still divided by borders.\n\nAnd it belongs to no one.";
                    }
                }

                private void OnReturnToMenuClicked()
                {
                    CoreManager.RestartGame();
                }
            }
        }
    }
}