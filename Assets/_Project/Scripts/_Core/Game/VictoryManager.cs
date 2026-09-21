using UnityEngine;

namespace ER
{
    namespace Game
    {
        using Core;
        using Simulation;
        using Culture;
        using Unit;
        using Players;
        using World.Tiles;
        using Configs;
        
        public class VictoryManager : MonoBehaviour
        {
            public static VictoryManager Instance {get; private set;}

            [Header("Main")]
            public bool EnableLogging;

            private MainConfig mainConfig;

            private SimulationManager simulationManager;
            private CultureManager cultureManager;
            private UnitManager unitManager;
            private TileManager tileManager;
            private PlayerManager playerManager;

            private float checkTimer = 0f;

            public bool IsGameOver {get; private set;}
            public string WinnerPlayerId {get; private set;}

            public event System.Action OnGameOver;
            public event System.Action<string> OnPlayerDefeated; // playerId
            public event System.Action<string> OnPlayerVictory;   // playerId

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

                CoreManager.RegisterVictoryManager(this);
            }

            void OnDestroy()
            {
                if (simulationManager != null) simulationManager.OnTick -= ProcessVictoryTick;

                if (cultureManager != null) cultureManager.OnCultureDefeated -= HandleCultureDefeated;
            }

            public void Initialize()
            {
                mainConfig = CoreManager.MainConfig;

                simulationManager = CoreManager.SimulationManager;

                cultureManager = CoreManager.CultureManager;

                unitManager = CoreManager.UnitManager;

                tileManager = CoreManager.TileManager;

                playerManager = CoreManager.PlayerManager;

                simulationManager.OnTick += ProcessVictoryTick;

                IsGameOver = false;

                WinnerPlayerId = null;

                cultureManager.OnCultureDefeated += HandleCultureDefeated;

                if (EnableLogging)
                {
                    Debug.Log("VictoryManager initialized");
                }
            }
            
            private void HandleCultureDefeated(string cultureId)
            {
                if (IsGameOver) return;
                
                var player = cultureManager.GetPlayerForCulture(cultureId);

                if (player != null && !player.PlayerAI)
                {
                    OnPlayerDefeated?.Invoke(player.PlayerId);

                    if (EnableLogging)
                    {
                        Debug.Log($"Player {player.PlayerName} defeated (culture {cultureId} destroyed)");
                    }
                }
                
                CheckVictoryConditions();
            }

            private void ProcessVictoryTick(float delta)
            {
                if (IsGameOver) return;

                checkTimer += delta;

                if (checkTimer < mainConfig.CheckInterval) return;

                checkTimer = 0f;

                CheckDefeatConditions();
                CheckVictoryConditions();
            }
            
            private void CheckDefeatConditions()
            {
                var activeCultures = cultureManager.GetActiveCultures();

                foreach (var culture in activeCultures)
                {
                    if (IsCultureDefeated(culture))
                    {
                        cultureManager.DefeatCulture(culture.CultureId);

                        if (EnableLogging)
                        {
                            Debug.Log($"Culture {culture.CultureName} defeated: no units and no territory");
                        }
                        
                        var player = cultureManager.GetPlayerForCulture(culture.CultureId);

                        if (player != null)
                        {
                            OnPlayerDefeated?.Invoke(player.PlayerId);

                            if (EnableLogging)
                            {
                                Debug.Log($"Player {player.PlayerName} defeated");
                            }
                        }
                    }
                }
            }
            
            private bool IsCultureDefeated(CultureData culture)
            {
                var units = unitManager.GetUnitsByCulture(culture.CultureId);

                bool hasUnits = false;

                foreach (var unit in units)
                {
                    if (unit.IsAlive)
                    {
                        hasUnits = true;

                        break;
                    }
                }

                if (hasUnits) return false;
                
                bool hasTerritory = HasCultureTerritory(culture.CultureId);

                return !hasTerritory;
            }
            
            private bool HasCultureTerritory(string cultureId)
            {
                return tileManager.GetTileCountForOwner(cultureId) > 0;
            }

            void Update()
            {
                if (Input.GetKeyDown(KeyCode.F10))
                {
                    var activeCultures = cultureManager.GetActiveCultures();

                    if (activeCultures.Count > 0)
                    {
                        cultureManager.DefeatCulture(activeCultures[0].CultureId);
                    }
                }
            }

            private void CheckVictoryConditions()
            {
                if (IsGameOver) return;

                var activeCultures = cultureManager.GetActiveCultures();
                
                if (activeCultures.Count > 1) return;
                
                if (activeCultures.Count == 0)
                {
                    EndGame(null);

                    return;
                }
                
                var winnerCulture = activeCultures[0];

                var winnerPlayer = cultureManager.GetPlayerForCulture(winnerCulture.CultureId);

                if (winnerPlayer != null && !winnerPlayer.PlayerAI)
                {
                    EndGame(winnerPlayer.PlayerId);
                }
                else
                {
                    EndGame(null);
                }
            }
            
            private void EndGame(string winnerPlayerId)
            {
                if (IsGameOver) return;

                IsGameOver = true;

                WinnerPlayerId = winnerPlayerId;

                if (EnableLogging)
                {
                    Debug.Log($"Game over! Winner: {winnerPlayerId}");
                }

                OnPlayerVictory?.Invoke(winnerPlayerId);

                OnGameOver?.Invoke();
            }
        }
    }
}