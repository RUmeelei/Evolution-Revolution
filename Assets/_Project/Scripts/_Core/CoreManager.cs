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

            public static MainConfig ConfigMain {get; private set;}
            public static AIConfig ConfigAI {get; private set;}

            [SerializeField] private MainConfig MainCfg;
            [SerializeField] private AIConfig AICfg;

            public static SimulationManager SimulationManager {get; private set;}

            public static void RegisterMainConfig(MainConfig config) => ConfigMain = config;
            public static void RegisterAIConfig(AIConfig config) => ConfigAI = config;

            public static void RegisterSimulationManager(SimulationManager manager) => SimulationManager = manager;

            void Awake()
            {
                if (Instance != null && Instance != this)
                {
                    Destroy(gameObject); return;
                }

                Instance = this;

                DontDestroyOnLoad(gameObject);

                RegisterMainConfig(MainCfg);
                RegisterAIConfig(AICfg);
            }
        }
    }
}
