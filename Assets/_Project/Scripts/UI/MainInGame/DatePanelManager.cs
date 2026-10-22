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
                public static DatePanelManager Instance { get; private set; }

                [Header("Main")]
                [SerializeField] private TextMeshProUGUI StepValue;
                [SerializeField] private TextMeshProUGUI CycleValue;
                [SerializeField] private TextMeshProUGUI TierValue;

                [SerializeField] private Button PauseButton;
                [SerializeField] private Button FirstSpeedButton;
                [SerializeField] private Button SecondSpeedButton;
                [SerializeField] private Button ThirdSpeedButton;
                [SerializeField] private Button FourthSpeedButton;

                private Image pauseBtnImage;
                private Image firstSpeedBtnImage;
                private Image secondSpeedBtnImage;
                private Image thirdSpeedBtnImage;
                private Image fourthSpeedBtnImage;

                private SimulationManager simulationManager;
                private DateManager dateManager;
                private UIConfig uiConfig;

                private int lastStep = -1;
                private int lastCycle = -1;
                private int lastTier = -1;

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

                    if (PauseButton != null)
                    {
                        PauseButton.onClick.AddListener(PauseButtonHandler);
                        pauseBtnImage = PauseButton.GetComponent<Image>();
                    }

                    if (FirstSpeedButton != null)
                    {
                        FirstSpeedButton.onClick.AddListener(FirstSpeedButtonHandler);
                        firstSpeedBtnImage = FirstSpeedButton.GetComponent<Image>();
                    }

                    if (SecondSpeedButton != null)
                    {
                        SecondSpeedButton.onClick.AddListener(SecondSpeedButtonHandler);
                        secondSpeedBtnImage = SecondSpeedButton.GetComponent<Image>();
                    }

                    if (ThirdSpeedButton != null)
                    {
                        ThirdSpeedButton.onClick.AddListener(ThirdSpeedButtonHandler);
                        thirdSpeedBtnImage = ThirdSpeedButton.GetComponent<Image>();
                    }

                    if (FourthSpeedButton != null)
                    {
                        FourthSpeedButton.onClick.AddListener(FourthSpeedButtonHandler);
                        fourthSpeedBtnImage = FourthSpeedButton.GetComponent<Image>();
                    }
                }

                void Update()
                {
                    if (dateManager == null || simulationManager == null || uiConfig == null) return;
                    
                    if (Input.GetKeyDown(KeyCode.Alpha1)) FirstSpeedButtonHandler();
                    else if (Input.GetKeyDown(KeyCode.Alpha2)) SecondSpeedButtonHandler();
                    else if (Input.GetKeyDown(KeyCode.Alpha3)) ThirdSpeedButtonHandler();
                    else if (Input.GetKeyDown(KeyCode.Alpha4)) FourthSpeedButtonHandler();
                    
                    if (StepValue != null && dateManager.Step != lastStep)
                    {
                        lastStep = dateManager.Step;
                        StepValue.SetText("{0:00}", lastStep);
                    }

                    if (CycleValue != null && dateManager.Cycle != lastCycle)
                    {
                        lastCycle = dateManager.Cycle;
                        CycleValue.SetText("{0:00}", lastCycle);
                    }

                    if (TierValue != null && dateManager.Tier != lastTier)
                    {
                        lastTier = dateManager.Tier;
                        TierValue.SetText("{0}", lastTier);
                    }

                    bool isRunning = simulationManager.IsSimulationRunning;

                    int speed = simulationManager.SimulationSpeed;

                    if (pauseBtnImage != null)
                        pauseBtnImage.color = !isRunning ? uiConfig.HoverButtonColor : uiConfig.BaseButtonColor;

                    if (firstSpeedBtnImage != null)
                        firstSpeedBtnImage.color = (speed == 1 && isRunning) ? uiConfig.HoverButtonColor : uiConfig.BaseButtonColor;

                    if (secondSpeedBtnImage != null)
                        secondSpeedBtnImage.color = (speed == 2 && isRunning) ? uiConfig.HoverButtonColor : uiConfig.BaseButtonColor;

                    if (thirdSpeedBtnImage != null)
                        thirdSpeedBtnImage.color = (speed == 3 && isRunning) ? uiConfig.HoverButtonColor : uiConfig.BaseButtonColor;

                    if (fourthSpeedBtnImage != null)
                        fourthSpeedBtnImage.color = (speed == 4 && isRunning) ? uiConfig.HoverButtonColor : uiConfig.BaseButtonColor;
                }

                private void PauseButtonHandler() => simulationManager.Pause();

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