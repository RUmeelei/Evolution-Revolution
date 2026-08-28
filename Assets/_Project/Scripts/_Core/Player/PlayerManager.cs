using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace ER
{
    namespace Players
    {
        using Core;
        using Configs;
        using Simulation;
        // using Government;
        using Culture;
        using Resources;

        public class PlayerManager : MonoBehaviour
        {
            public static PlayerManager Instance {get; private set;}

            [Header("Main")]
            public bool EnableLogging;

            public List<PlayerData> Players = new List<PlayerData>();
            private Dictionary<string, PlayerData> PlayersDictionary = new Dictionary<string, PlayerData>();

            private SimulationManager simulationManager;
            private DateManager dateManager;
            private ResourceManager resourceManager;
            private CultureManager cultureManager;

            private MainConfig mainConfig;
            private SimulationConfig simulationConfig;

            public event Action<float> OnPlayerTick;

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

                CoreManager.RegisterPlayerManager(this);
            }

            void Start()
            {
                mainConfig = CoreManager.MainConfig;

                simulationConfig = CoreManager.SimulationConfig;

                simulationManager = CoreManager.SimulationManager;

                dateManager = CoreManager.DateManager;

                cultureManager = CoreManager.CultureManager;

                switch (mainConfig.GatherSeason)
                {
                    case 0:
                        dateManager.OnStep += GatherSeason;

                        break;

                    case 1:
                        dateManager.OnCycle += GatherSeason;

                        break;

                    default:
                        dateManager.OnTier += GatherSeason;

                        break;
                }

                resourceManager = CoreManager.ResourceManager;

                resourceManager.OnResourceTick += ProcessPlayerTick;
            }

            void OnDestroy()
            {
                if (dateManager != null)
                {
                    switch (mainConfig.GatherSeason)
                    {
                        case 0:
                            dateManager.OnStep -= GatherSeason;

                            break;

                        case 1:
                            dateManager.OnCycle -= GatherSeason;

                            break;

                        default:
                            dateManager.OnTier -= GatherSeason;
                            
                            break;
                    }
                }

                if (resourceManager != null) resourceManager.OnResourceTick -= ProcessPlayerTick;
            }

            public void Initialize()
            {
                var newPlayer = CreatePlayer(playerId : "PLAYER_0001", playerName : "RUmeelei");

                newPlayer.GatherResource("Fruits", 100f);

                AttachPlayerCulture(newPlayer.PlayerId, "CUL_0001");
            }

            private PlayerData CreatePlayer(string playerId, string playerName)
            {
                PlayerData player = new PlayerData()
                {
                    PlayerId = playerId,
                    PlayerName = playerName,
                };

                Players.Add(player);

                PlayersDictionary[player.PlayerId] = player;

                Debug.Log($"Created {player.PlayerName} player with id {player.PlayerId}");

                return player;
            }

            public void AttachPlayerCulture(string playerId, string cultureId)
            {
                PlayersDictionary.TryGetValue(playerId, out var player);

                if (!cultureManager.GetAllCultures().TryGetValue(cultureId, out var culture))
                {
                    Debug.Log($"Failed to attach culture for {player.PlayerName}");
                    
                    return;
                }

                player.PlayerCulture = culture;

                Debug.Log($"Attached {culture.CultureName} culture to {player.PlayerName}");
            }

            public PlayerData GetPlayer(string playerId)
            {
                PlayersDictionary.TryGetValue(playerId, out PlayerData player);

                return player;
            }

            public List<PlayerData> GetAllPlayers() => Players;

            private void GatherSeason(float delta)
            {
                float multiplier = mainConfig.GatherSeason switch
                {
                    0 => 1f,
                    1 => simulationConfig.StepsPerCycle,
                    _ => simulationConfig.CyclesPerTier,
                };

                foreach (var player in Players)
                {
                    if (player.PlayerCulture.CultureDefeated) continue;

                    float gatheredFood = UnityEngine.Random.Range(0f, 30f) * multiplier;

                    float identityPenalty = player.PlayerCulture.CultureIdentity <= mainConfig.WarningIdentityThreshold ? 50f / player.PlayerCulture.CultureIdentity : 0;

                    gatheredFood -= identityPenalty;

                    gatheredFood = Mathf.Max(0, gatheredFood);

                    player.GatherResource("Fruits", gatheredFood * delta);

                    Debug.Log($"Gathered {gatheredFood * delta} food for {player.PlayerName}");
                }
            }

            private void ProcessPlayerTick(float delta)
            {
                OnPlayerTick?.Invoke(delta);

                foreach (var player in Players)
                {
                    if (player.PlayerCulture.CultureDefeated) continue;

                    float spentFood = UnityEngine.Random.Range(5f, 15f);

                    player.SpendResource("Fruits", 15f * delta);
                }
            }
        }
    }
}
