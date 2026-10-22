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
        using Culture;
        using Resources;
        using World.Tiles;
        using Unit;

        public class PlayerManager : MonoBehaviour
        {
            public static PlayerManager Instance { get; private set; }

            [Header("Main")]
            public bool EnableLogging;

            public List<PlayerData> Players = new List<PlayerData>();

            private Dictionary<string, PlayerData> PlayersDictionary = new Dictionary<string, PlayerData>();
            private Dictionary<string, PlayerData> PlayersByCulture = new Dictionary<string, PlayerData>();

            private SimulationManager simulationManager;
            private DateManager dateManager;
            private ResourceManager resourceManager;
            private CultureManager cultureManager;
            private TileManager tileManager;
            private UnitManager unitManager;

            private MainConfig mainConfig;
            private SimulationConfig simulationConfig;

            public event Action<float> OnPlayerTick;

            public PlayerData NonAIPlayer;

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

                tileManager = CoreManager.TileManager;

                unitManager = CoreManager.UnitManager;

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
                if (dateManager != null && mainConfig != null)
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
            }

            public PlayerData CreatePlayer(string playerId, string playerName, bool isAI = false)
            {
                PlayerData player = new PlayerData()
                {
                    PlayerId = playerId,
                    PlayerName = playerName,
                    PlayerDefeated = false,
                    PlayerAI = isAI,
                };

                Players.Add(player);

                PlayersDictionary[player.PlayerId] = player;

                Debug.Log($"Created {player.PlayerName} player with id {player.PlayerId}");

                if (!player.PlayerAI) NonAIPlayer = player;

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

                PlayersByCulture[cultureId] = player;

                Debug.Log($"Attached {culture.CultureName} culture to {player.PlayerName}");
            }

            public PlayerData GetPlayer(string playerId)
            {
                PlayersDictionary.TryGetValue(playerId, out PlayerData player);

                return player;
            }

            public PlayerData GetPlayerForCulture(string cultureId)
            {
                PlayersByCulture.TryGetValue(cultureId, out var player);

                return player;
            }

            public List<PlayerData> GetActivePlayers() => Players;

            public Dictionary<string, PlayerData> GetAllPlayers() => PlayersDictionary;

            private void GatherSeason(float delta)
            {
                if (tileManager == null) tileManager = CoreManager.TileManager;
                if (unitManager == null) unitManager = CoreManager.UnitManager;

                foreach (var player in Players)
                {
                    if (player.PlayerDefeated || player.PlayerCulture == null || player.PlayerCulture.CultureDefeated) continue;

                    string cultureId = player.PlayerCulture.CultureId;
                    int ownedTiles = tileManager != null ? Mathf.Max(1, tileManager.GetTileCountForOwner(cultureId)) : 1;
                    int population = unitManager != null ? unitManager.GetUnitsByCulture(cultureId).Count : 1;
                    
                    int workedTiles = Mathf.Min(ownedTiles, population * 3);
                    int unworkedTiles = Mathf.Max(0, ownedTiles - workedTiles);

                    float baseFoodYield = (workedTiles * 2.8f) + (unworkedTiles * 0.25f) + (population * 1.0f);
                    float identityFactor = Mathf.Clamp(player.PlayerCulture.CultureIdentity / 100f, 0.25f, 1.25f);

                    float gatheredFood = (5f + ownedTiles * 0.4f) * identityFactor;
                    float gatheredMaterials = (1f + ownedTiles * 0.15f) * identityFactor;

                    player.GatherResource("Fruits", gatheredFood);
                    player.GatherResource("Materials", gatheredMaterials);
                    
                    int maxSupportedPop = Mathf.Clamp(5 + Mathf.RoundToInt(Mathf.Sqrt(ownedTiles) * 2.2f), 6, 10000);
                    
                    float requiredFoodToGrow = mainConfig.BaseFoodRequiredToGrow + (population * mainConfig.FoodRequiredPerPop);
                    float foodCostToSpawn = mainConfig.BaseFoodCostToSpawn + (population * mainConfig.FoodCostPerPop);

                    if (unitManager != null && player.GetFood() >= requiredFoodToGrow && population < maxSupportedPop)
                    {
                        player.SpendResource("Fruits", foodCostToSpawn);

                        Vector2Int spawnTilePos = player.PlayerCulture.CultureCapital;

                        foreach (var pos in tileManager.GetStructureTiles())
                        {
                            TileData st = tileManager.GetTile(pos.x, pos.y);

                            if (st.Owner == cultureId && st.Structure == TileStructureType.Settlement && UnityEngine.Random.value < 0.5f)
                            {
                                spawnTilePos = pos;

                                break;
                            }
                        }

                        Vector2 centerPos = tileManager.GetTileCenter(spawnTilePos.x, spawnTilePos.y);
                        
                        Vector2 spawnPos = centerPos + new Vector2(UnityEngine.Random.Range(-1.5f, 1.5f), UnityEngine.Random.Range(-1.5f, 1.5f));

                        PopulationGroup newGroup = (population % 3 == 0) ? PopulationGroup.Military : PopulationGroup.Producers;

                        TileData spawnTile = tileManager.GetTile((int)spawnPos.x, (int)spawnPos.y);

                        bool canSpawn = spawnTile.Owner == player.PlayerCulture.CultureId && spawnTile.CaptureProgress <= 0;

                        if (canSpawn)
                        {
                            unitManager.CreateHuman(
                                $"Citizen {population + 1}",
                                $"{player.PlayerCulture.CultureName}",
                                18,
                                spawnPos,
                                cultureId,
                                newGroup
                            );
                        }

                        if (EnableLogging)
                        {
                            Debug.Log($"[{player.PlayerCulture.CultureName}] New citizen born! Pop: {population + 1}/{maxSupportedPop}");
                        }
                    }
                }
            }

            private void ProcessPlayerTick(float delta)
            {
                OnPlayerTick?.Invoke(delta);

                if (unitManager == null) unitManager = CoreManager.UnitManager;

                foreach (var player in Players)
                {
                    if (player.PlayerCulture == null || player.PlayerCulture.CultureDefeated)
                    {
                        DefeatPlayer(player.PlayerId);

                        continue;
                    }
                    
                    int population = unitManager != null ? unitManager.GetUnitsByCulture(player.PlayerCulture.CultureId).Count : 0;

                    float foodUpkeepPerSecond = population * mainConfig.FoodUpkeepPerUnit;

                    player.SpendResource("Fruits", foodUpkeepPerSecond * delta);
                }
            }

            public void DefeatPlayer(string id)
            {
                var player = GetPlayer(id);

                if (player == null || player.PlayerDefeated) return;

                player.PlayerDefeated = true;

                if (player.PlayerCulture != null) PlayersByCulture.Remove(player.PlayerCulture.CultureId);

                Debug.Log($"{player.PlayerName} has been defeated");
            }
        }
    }
}