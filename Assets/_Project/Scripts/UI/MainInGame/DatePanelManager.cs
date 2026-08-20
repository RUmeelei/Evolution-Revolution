using TMPro;
using UnityEngine;

namespace ER
{
    namespace UI
    {
        namespace InGame
        {
            using Core;
            using Simulation;

            public class DatePanelManager : MonoBehaviour
            {
                [Header("Main")]
                [SerializeField] private TextMeshProUGUI StepValue;
                [SerializeField] private TextMeshProUGUI CycleValue;
                [SerializeField] private TextMeshProUGUI TierValue;

                private SimulationManager simulationManager;
                private DateManager dateManager;

                void Start()
                {
                    simulationManager = CoreManager.SimulationManager;
                    dateManager = CoreManager.DateManager;
                }

                void Update()
                {
                    if (StepValue != null)
                    {
                        StepValue.SetText($"{dateManager.GetStep()}");
                    }

                    if (CycleValue != null)
                    {
                        CycleValue.SetText($"{dateManager.GetCycle()}");
                    }

                    if (TierValue != null)
                    {
                        TierValue.SetText($"{dateManager.GetTier()}");
                    }
                }
            }
        }
    }
}