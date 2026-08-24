using UnityEngine;

namespace ER
{
    using UI.InGame;
    using UI.MainMenu;

    namespace Core
    {
        using Configs;
        using Simulation;

        public class CoreManager : MonoBehaviour
        {
            public static CoreManager Instance {get; private set;}

            public static MainConfig MainConfig => Instance?.MainCfg;
            public static SimulationConfig SimulationConfig => Instance?.SimulationCfg;
            public static AIConfig AIConfig => Instance?.AICfg;
            public static UIConfig UIConfig => Instance?.UICfg;

            [SerializeField] private MainConfig MainCfg;
            [SerializeField] private SimulationConfig SimulationCfg;
            [SerializeField] private AIConfig AICfg;
            [SerializeField] private UIConfig UICfg;

            public static SimulationManager SimulationManager {get; private set;}
            public static DateManager DateManager {get; private set;}
            public static DatePanelManager DatePanelManager {get; private set;}
            public static MainMenuPanelManager MainMenuPanelManager {get; private set;}

            public static void RegisterSimulationManager(SimulationManager manager) => SimulationManager = manager;
            public static void RegisterDateManager(DateManager manager) => DateManager = manager;
            public static void RegisterDatePanelManager(DatePanelManager manager) => DatePanelManager = manager;
            public static void RegisterMainMenuPanelManager(MainMenuPanelManager manager) => MainMenuPanelManager = manager;

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
            }
        }
    }
}
