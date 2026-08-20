using UnityEngine;

namespace ER
{
    namespace Core
    {
        using Configs;
        using Simulation;

        public class CoreManager : MonoBehaviour
        {
            public static CoreManager Instance {get; private set;}

            public static MainConfig MainConfig {get; private set;}
            public static SimulationConfig SimulationConfig {get; private set;}
            public static AIConfig AIConfig {get; private set;}

            [SerializeField] private MainConfig MainCfg;
            [SerializeField] private SimulationConfig SimulationCfg;
            [SerializeField] private AIConfig AICfg;

            public static SimulationManager SimulationManager {get; private set;}
            public static DateManager DateManager {get; private set;}

            public static void RegisterMainConfig(MainConfig config) => MainConfig = config;
            public static void RegisterSimulationConfig(SimulationConfig config) => SimulationConfig = config;
            public static void RegisterAIConfig(AIConfig config) => AIConfig = config;

            public static void RegisterSimulationManager(SimulationManager manager) => SimulationManager = manager;
            public static void RegisterDateManager(DateManager manager) => DateManager = manager;

            void Awake()
            {
                if (Instance != null && Instance != this)
                {
                    Destroy(gameObject); return;
                }

                Instance = this;

                DontDestroyOnLoad(gameObject);

                RegisterMainConfig(MainCfg);
                RegisterSimulationConfig(SimulationCfg);
                RegisterAIConfig(AICfg);
            }
        }
    }
}
