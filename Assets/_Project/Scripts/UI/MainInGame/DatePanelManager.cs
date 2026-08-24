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
            using Configs;
            using Simulation;

            public class DatePanelManager : MonoBehaviour
            {
                public static DatePanelManager Instance {get; private set;}

                [Header("Main")]
                [SerializeField] private TextMeshProUGUI StepValue;
                [SerializeField] private TextMeshProUGUI CycleValue;
                [SerializeField] private TextMeshProUGUI TierValue;
                
                [SerializeField] private Button PauseButton;
                [SerializeField] private Button FirstSpeedButton;
                [SerializeField] private Button SecondSpeedButton;
                [SerializeField] private Button ThirdSpeedButton;
                [SerializeField] private Button FourthSpeedButton;

                private SimulationManager simulationManager;
                private DateManager dateManager;

                private UIConfig uiConfig;

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

                    CoreManager.RegisterDatePanelManager(this);
                }

                void Start()
                {
                    simulationManager = CoreManager.SimulationManager;
                    dateManager = CoreManager.DateManager;

                    uiConfig = CoreManager.UIConfig;

                    if (PauseButton != null) PauseButton.onClick.AddListener(PauseButtonHandler);

                    if (FirstSpeedButton != null) FirstSpeedButton.onClick.AddListener(FirstSpeedButtonHandler);
                    if (SecondSpeedButton != null) SecondSpeedButton.onClick.AddListener(SecondSpeedButtonHandler);
                    if (ThirdSpeedButton != null) ThirdSpeedButton.onClick.AddListener(ThirdSpeedButtonHandler);
                    if (FourthSpeedButton != null) FourthSpeedButton.onClick.AddListener(FourthSpeedButtonHandler);
                }

                void Update()
                {
                    if (StepValue != null)
                    {
                        StepValue.SetText($"{dateManager.Step}");
                    }

                    if (CycleValue != null)
                    {
                        CycleValue.SetText($"{dateManager.Cycle}");
                    }

                    if (TierValue != null)
                    {
                        TierValue.SetText($"{dateManager.Tier}");
                    }

                    if (PauseButton != null)
                    {
                        PauseButton.GetComponent<Image>().color = !simulationManager.IsSimulationRunning ? uiConfig.HoverButtonColor : uiConfig.BaseButtonColor;
                    }

                    if (FirstSpeedButton != null)
                    {
                        FirstSpeedButton.GetComponent<Image>().color = (simulationManager.SimulationSpeed == 1 && simulationManager.IsSimulationRunning) ? uiConfig.HoverButtonColor : uiConfig.BaseButtonColor;
                    }

                    if (SecondSpeedButton != null)
                    {
                        SecondSpeedButton.GetComponent<Image>().color = (simulationManager.SimulationSpeed == 2 && simulationManager.IsSimulationRunning) ? uiConfig.HoverButtonColor : uiConfig.BaseButtonColor;
                    }

                    if (ThirdSpeedButton != null)
                    {
                        ThirdSpeedButton.GetComponent<Image>().color = (simulationManager.SimulationSpeed == 3 && simulationManager.IsSimulationRunning) ? uiConfig.HoverButtonColor : uiConfig.BaseButtonColor;
                    }

                    if (FourthSpeedButton != null)
                    {
                        FourthSpeedButton.GetComponent<Image>().color = (simulationManager.SimulationSpeed == 4 && simulationManager.IsSimulationRunning) ? uiConfig.HoverButtonColor : uiConfig.BaseButtonColor;
                    }
                }

                private void PauseButtonHandler()
                {
                    simulationManager.Pause();
                }

                private void FirstSpeedButtonHandler()
                {
                    simulationManager.Unpause();
                    simulationManager.SetSimulationSpeed(1);
                }

                private void SecondSpeedButtonHandler()
                {
                    simulationManager.Unpause();
                    simulationManager.SetSimulationSpeed(2);
                }

                private void ThirdSpeedButtonHandler()
                {
                    simulationManager.Unpause();
                    simulationManager.SetSimulationSpeed(3);
                }

                private void FourthSpeedButtonHandler()
                {
                    simulationManager.Unpause();
                    simulationManager.SetSimulationSpeed(4);
                }
            }
        }
    }
}