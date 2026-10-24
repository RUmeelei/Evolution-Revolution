using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ER.Simulation.Time;
using ER.Core.Localization;

namespace ER.Core.UI
{
    public class TopTimeBarUI : MonoBehaviour
    {
        [Header("UI Text Displays")]
        [SerializeField] private TextMeshProUGUI dateDisplayLabel;

        [SerializeField] private TextMeshProUGUI speedStatusLabel;

        [SerializeField] private TextMeshProUGUI tickIndexLabel;


        [Header("Controls")]
        [SerializeField] private Button pauseButton;

        [SerializeField] private Button speed1Button;

        [SerializeField] private Button speed2Button;

        [SerializeField] private Button speed3Button;

        [SerializeField] private Button speed4Button;

        [SerializeField] private Button speed5Button;


        [Header("References")]
        [SerializeField] private SimulationClockManager clockManager;

        [SerializeField] private SimulationTimeConfig timeConfig;


        private readonly StringBuilder stringBuilder = new StringBuilder(128);


        private void Start()
        {
            if (clockManager == null)
            {
                clockManager = SimulationClockManager.Instance;
            }

            BindButtonEvents();

            SubscribeToSimulationEvents();

            UpdateFullDisplay();
        }


        private void OnDestroy()
        {
            UnsubscribeFromSimulationEvents();
        }


        private void BindButtonEvents()
        {
            if (pauseButton != null) pauseButton.onClick.AddListener(OnPauseClicked);

            if (speed1Button != null) speed1Button.onClick.AddListener(() => OnSpeedClicked(timeConfig.Speed1));

            if (speed2Button != null) speed2Button.onClick.AddListener(() => OnSpeedClicked(timeConfig.Speed2));

            if (speed3Button != null) speed3Button.onClick.AddListener(() => OnSpeedClicked(timeConfig.Speed3));

            if (speed4Button != null) speed4Button.onClick.AddListener(() => OnSpeedClicked(timeConfig.Speed4));

            if (speed5Button != null) speed5Button.onClick.AddListener(() => OnSpeedClicked(timeConfig.Speed5));
        }


        private void SubscribeToSimulationEvents()
        {
            if (clockManager == null)
            {
                return;
            }

            clockManager.OnTick += HandleTick;

            clockManager.OnDayPassed += HandleDateChange;

            clockManager.OnSeasonChanged += HandleDateChange;

            clockManager.OnYearPassed += HandleDateChange;

            clockManager.OnPauseStateChanged += HandlePauseStateChanged;

            clockManager.OnSpeedMultiplierChanged += HandleSpeedChanged;

            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged += UpdateFullDisplay;
            }
        }


        private void UnsubscribeFromSimulationEvents()
        {
            if (clockManager == null)
            {
                return;
            }

            clockManager.OnTick -= HandleTick;

            clockManager.OnDayPassed -= HandleDateChange;

            clockManager.OnSeasonChanged -= HandleDateChange;

            clockManager.OnYearPassed -= HandleDateChange;

            clockManager.OnPauseStateChanged -= HandlePauseStateChanged;

            clockManager.OnSpeedMultiplierChanged -= HandleSpeedChanged;

            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= UpdateFullDisplay;
            }
        }


        private void HandleTick(WorldTimeData timeData)
        {
            UpdateDateText(timeData);

            UpdateTickIndexText(timeData.TotalTicks);
        }


        private void HandleDateChange(WorldTimeData timeData)
        {
            UpdateDateText(timeData);
        }


        private void HandlePauseStateChanged(bool isPaused)
        {
            UpdateSpeedStatusDisplay();
        }


        private void HandleSpeedChanged(float speed)
        {
            UpdateSpeedStatusDisplay();
        }


        private void UpdateFullDisplay()
        {
            if (clockManager == null)
            {
                return;
            }

            WorldTimeData currentTime = clockManager.CurrentTime;

            UpdateDateText(currentTime);

            UpdateTickIndexText(currentTime.TotalTicks);

            UpdateSpeedStatusDisplay();
        }


        private void UpdateDateText(WorldTimeData time)
        {
            if (dateDisplayLabel == null)
            {
                return;
            }

            string seasonKey = GetSeasonLocalizationKey(time.CurrentSeason);

            string seasonName = LocalizationManager.Instance != null ? LocalizationManager.Instance.GetText(seasonKey) : time.CurrentSeason.ToString();

            stringBuilder.Clear();

            if (LocalizationManager.Instance != null)
            {
                string formatPattern = LocalizationManager.Instance.GetText("ui.date_format");

                stringBuilder.AppendFormat(formatPattern, time.CurrentYear, seasonName, time.CurrentDay, time.CurrentHour);
            }
            else
            {
                stringBuilder.Append($"Year {time.CurrentYear}, {seasonName}, Day {time.CurrentDay:00}, {time.CurrentHour:00}:00");
            }

            dateDisplayLabel.text = stringBuilder.ToString();
        }


        private void UpdateTickIndexText(ulong ticks)
        {
            if (tickIndexLabel == null)
            {
                return;
            }

            stringBuilder.Clear();

            stringBuilder.Append("Tick #").Append(ticks);

            tickIndexLabel.text = stringBuilder.ToString();
        }


        private void UpdateSpeedStatusDisplay()
        {
            if (speedStatusLabel == null || clockManager == null)
            {
                return;
            }

            stringBuilder.Clear();

            if (clockManager.IsPaused)
            {
                string pauseWord = LocalizationManager.Instance != null ? LocalizationManager.Instance.GetText("ui.pause") : "PAUSED";

                stringBuilder.Append(pauseWord);
            }
            else
            {
                stringBuilder.Append(clockManager.CurrentSpeedMultiplier.ToString("0.#")).Append("x");
            }

            speedStatusLabel.text = stringBuilder.ToString();
        }


        private string GetSeasonLocalizationKey(Season season)
        {
            switch (season)
            {
                case Season.Spring:
                    return "season.spring";

                case Season.Summer:
                    return "season.summer";

                case Season.Autumn:
                    return "season.autumn";

                case Season.Winter:
                    return "season.winter";

                default:
                    return "season.spring";
            }
        }


        private void OnPauseClicked()
        {
            if (clockManager != null)
            {
                clockManager.TogglePause();
            }
        }


        private void OnSpeedClicked(float speedMultiplier)
        {
            if (clockManager != null)
            {
                clockManager.SetPaused(false);

                clockManager.SetSpeedMultiplier(speedMultiplier);
            }
        }
    }
}