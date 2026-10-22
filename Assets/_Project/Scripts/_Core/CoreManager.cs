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
        using Politics;
        using Politics.External.Diplomacy;
        using Politics.Internal.Government;
        using World;
        using World.Tiles;
        using Unit;
        using Controls;
        using Game;

        public class CoreManager : MonoBehaviour
        {
            public static CoreManager Instance {get; private set;}

            public static MainConfig MainConfig => Instance?.MainCfg;
            public static SimulationConfig SimulationConfig => Instance?.SimulationCfg;
            public static AIConfig AIConfig => Instance?.AICfg;
            public static CombatConfig CombatConfig => Instance?.CombatCfg;
            public static UIConfig UIConfig => Instance?.UICfg;

            [SerializeField] private MainConfig MainCfg;
            [SerializeField] private SimulationConfig SimulationCfg;
            [SerializeField] private AIConfig AICfg;
            [SerializeField] private CombatConfig CombatCfg;
            [SerializeField] private UIConfig UICfg;

            public static CameraManager CameraManager {get; private set;}
            public static SimulationManager SimulationManager {get; private set;}
            public static DateManager DateManager {get; private set;}
            public static CultureManager CultureManager {get; private set;}
            public static ResourceManager ResourceManager {get; private set;}
            public static PlayerManager PlayerManager {get; private set;}
            public static DiplomacyManager DiplomacyManager {get; private set;}
            public static GovernmentManager GovernmentManager {get; private set;}
            public static WorldGeneratorManager WorldGeneratorManager {get; private set;}
            public static TileManager TileManager {get; private set;}
            public static TileVisualManager TileVisualManager {get; private set;}
            public static UnitManager UnitManager {get; private set;}
            public static UnitVisualPoolManager UnitVisualPoolManager {get; private set;}
            public static SelectionManager SelectionManager {get; private set;}
            public static OrdersManager OrdersManager {get; private set;}
            public static VictoryManager VictoryManager {get; private set;}
            public static DatePanelManager DatePanelManager {get; private set;}
            public static GameOverPanelManager GameOverPanelManager {get; private set;}
            public static MainPanelManager MainPanelManager {get; private set;}
            public static PoliticsPanelManager PoliticsPanelManager {get; private set;}
            public static ResourcesPanelManager ResourcesPanelManager {get; private set;}
            public static SelectionPanelManager SelectionPanelManager {get; private set;}
            public static MainMenuPanelManager MainMenuPanelManager {get; private set;}

            public static void RegisterCameraManager(CameraManager manager) => CameraManager = manager;
            public static void RegisterSimulationManager(SimulationManager manager) => SimulationManager = manager;
            public static void RegisterDateManager(DateManager manager) => DateManager = manager;
            public static void RegisterCultureManager(CultureManager manager) => CultureManager = manager;
            public static void RegisterResourceManager(ResourceManager manager) => ResourceManager = manager;
            public static void RegisterPlayerManager(PlayerManager manager) => PlayerManager = manager;
            public static void RegisterDiplomacyManager(DiplomacyManager manager) => DiplomacyManager = manager;
            public static void RegisterGovernmentManager(GovernmentManager manager) => GovernmentManager = manager;
            public static void RegisterWorldGeneratorManager(WorldGeneratorManager manager) => WorldGeneratorManager = manager;
            public static void RegisterTileManager(TileManager manager) => TileManager = manager;
            public static void RegisterTileVisualManager(TileVisualManager manager) => TileVisualManager = manager;
            public static void RegisterUnitManager(UnitManager manager) => UnitManager = manager;
            public static void RegisterUnitVisualPoolManager(UnitVisualPoolManager manager) => UnitVisualPoolManager = manager;
            public static void RegisterSelectionManager(SelectionManager manager) => SelectionManager = manager;
            public static void RegisterOrdersManager(OrdersManager manager) => OrdersManager = manager;
            public static void RegisterVictoryManager(VictoryManager manager) => VictoryManager = manager;
            public static void RegisterDatePanelManager(DatePanelManager manager) => DatePanelManager = manager;
            public static void RegisterGameOverPanelManager(GameOverPanelManager manager) => GameOverPanelManager = manager;
            public static void RegisterMainPanelManager(MainPanelManager manager) => MainPanelManager = manager;
            public static void RegisterPoliticsPanelManager(PoliticsPanelManager manager) => PoliticsPanelManager = manager;
            public static void RegisterResourcesPanelManager(ResourcesPanelManager manager) => ResourcesPanelManager = manager;
            public static void RegisterSelectionPanelManager(SelectionPanelManager manager) => SelectionPanelManager = manager;
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
                if (DateManager != null) DateManager.Initialize();
                if (CultureManager != null) CultureManager.Initialize();
                if (PlayerManager != null) PlayerManager.Initialize();
                if (DiplomacyManager != null) DiplomacyManager.Initialize();
                if (WorldGeneratorManager != null) WorldGeneratorManager.Initialize();
                if (TileManager != null) TileManager.Initialize();
                if (TileVisualManager != null) TileVisualManager.Initialize();
                if (UnitVisualPoolManager != null) UnitVisualPoolManager.Initialize();
                if (UnitManager != null) UnitManager.Initialize();
                if (SelectionManager != null) SelectionManager.Initialize();
                if (OrdersManager != null) OrdersManager.Initialize();
                if (VictoryManager != null) VictoryManager.Initialize();
            }
            
            public static void RestartGame()
            {
                CameraManager = null;
                SimulationManager = null;
                DateManager = null;
                CultureManager = null;
                ResourceManager = null;
                PlayerManager = null;
                DiplomacyManager = null;
                WorldGeneratorManager = null;
                TileManager = null;
                TileVisualManager = null;
                UnitManager = null;
                UnitVisualPoolManager = null;
                SelectionManager = null;
                OrdersManager = null;
                DatePanelManager = null;
                GameOverPanelManager = null;
                MainPanelManager = null;
                MainMenuPanelManager = null;
                VictoryManager = null;
                
                var dontDestroyObjects = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

                foreach (var obj in dontDestroyObjects)
                {
                    if (obj.gameObject.scene.name == "DontDestroyOnLoad")
                    {
                        Destroy(obj.gameObject);
                    }
                }
                
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            }
            
            //public static void RestartGame()
            //{
            //    if (PlayerManager != null) PlayerManager.ResetState();
            
            //    if (UnitManager != null) UnitManager.ResetState();
            
            //    if (CultureManager != null) CultureManager.ResetState();
            
            //    if (TileManager != null) TileManager.ResetState();
            
            //    if (TileVisualManager != null) TileVisualManager.ResetState();
            
            //    if (WorldGeneratorManager != null) WorldGeneratorManager.ResetState();
            
            //    if (DiplomacyManager != null) DiplomacyManager.ResetState();
            
            //    if (ResourceManager != null) ResourceManager.ResetState();
            
            //    if (SelectionManager != null) SelectionManager.ResetState();
            
            //    if (OrdersManager != null) OrdersManager.ResetState();
            
            //    if (VictoryManager != null) VictoryManager.ResetState();
            
            //    if (SimulationManager != null) SimulationManager.ResetState();
            
            //    if (DateManager != null) DateManager.ResetState();
            
            //    if (CameraManager != null) CameraManager.ResetState();
            
            //    UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
            //}
        }
    }
}
