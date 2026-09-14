using UnityEngine;

namespace ER
{
    using UI.InGame;
    using UI.MainMenu;

    namespace Core
    {
        using Configs;
        using Simulation;
        using Culture;
        using Resources;
        using Players;
        using World;
        using World.Tiles;
        using Unit;
        using Controls;

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

            public static CameraManager CameraManager {get; private set;}
            public static SimulationManager SimulationManager {get; private set;}
            public static DateManager DateManager {get; private set;}
            public static CultureManager CultureManager {get; private set;}
            public static ResourceManager ResourceManager {get; private set;}
            public static PlayerManager PlayerManager {get; private set;}
            public static WorldGeneratorManager WorldGeneratorManager {get; private set;}
            public static TileManager TileManager {get; private set;}
            public static TileVisualManager TileVisualManager {get; private set;}
            public static UnitManager UnitManager {get; private set;}
            public static SelectionManager SelectionManager {get; private set;}
            public static OrdersManager OrdersManager {get; private set;}
            public static DatePanelManager DatePanelManager {get; private set;}
            public static MainPanelManager MainPanelManager {get; private set;}
            public static MainMenuPanelManager MainMenuPanelManager {get; private set;}

            public static void RegisterCameraManager(CameraManager manager) => CameraManager = manager;
            public static void RegisterSimulationManager(SimulationManager manager) => SimulationManager = manager;
            public static void RegisterDateManager(DateManager manager) => DateManager = manager;
            public static void RegisterCultureManager(CultureManager manager) => CultureManager = manager;
            public static void RegisterResourceManager(ResourceManager manager) => ResourceManager = manager;
            public static void RegisterPlayerManager(PlayerManager manager) => PlayerManager = manager;
            public static void RegisterWorldGeneratorManager(WorldGeneratorManager manager) => WorldGeneratorManager = manager;
            public static void RegisterTileManager(TileManager manager) => TileManager = manager;
            public static void RegisterTileVisualManager(TileVisualManager manager) => TileVisualManager = manager;
            public static void RegisterUnitManager(UnitManager manager) => UnitManager = manager;
            public static void RegisterSelectionManager(SelectionManager manager) => SelectionManager = manager;
            public static void RegisterOrdersManager(OrdersManager manager) => OrdersManager = manager;
            public static void RegisterDatePanelManager(DatePanelManager manager) => DatePanelManager = manager;
            public static void RegisterMainPanelManager(MainPanelManager manager) => MainPanelManager = manager;
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

            void Start()
            {
                CultureManager.Initialize();

                PlayerManager.Initialize();

                WorldGeneratorManager.Initialize();

                TileManager.Initialize();

                TileVisualManager.Initialize();

                UnitManager.Initialize();

                SelectionManager.Initialize();

                OrdersManager.Initialize();
            }
        }
    }
}
