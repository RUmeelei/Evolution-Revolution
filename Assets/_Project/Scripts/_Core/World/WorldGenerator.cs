using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace ER
{
    namespace World
    {
        using Core;
        using Configs;
        using Players;
        using Culture;

        public class WorldGeneratorManager : MonoBehaviour
        {
            public static WorldGeneratorManager Instance {get; private set;}

            [Header("Main")]
            public bool Govno;

            private PlayerManager playerManager;
            private CultureManager cultureManager;

            private MainConfig mainConfig;
            private AIConfig aiConfig;

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

                CoreManager.RegisterWorldGeneratorManager(this);
            }

            public void Initialize()
            {
                playerManager = CoreManager.PlayerManager;

                cultureManager = CoreManager.CultureManager;

                mainConfig = CoreManager.MainConfig;

                aiConfig = CoreManager.AIConfig;

                GenerateWorld();
            }

            public void GenerateWorld()
            {
                List<string> cultureIds = GenerateCultures(1);

                List<string> aiCultureIds = GenerateAICultures(WorldCreationData.AICulturesCount);

                List<string> neutralCultureIds = GenerateNeutralCultures(WorldCreationData.NeutralCulturesCount);

                CreatePlayers(cultureIds);

                CreateAIPlayers(aiCultureIds);

                if (WorldCreationData.ToggleNeutralCultures) CreateNeutralCultures(neutralCultureIds);
            }

            private List<string> GenerateCultures(int count)
            {
                List<string> cultureIds = new List<string>();

                for (int i = 0; i < count; i++)
                {
                    string name = GenerateCultureName();

                    Color color = Random.ColorHSV(0f, 1f, 0f, 1f, 0f, 1f);

                    var culture = cultureManager.CreateCulture(
                        cultureName: CultureCreationData.Name,
                        cultureDescription: CultureCreationData.Description,
                        cultureColor: CultureCreationData.Color
                    );

                    cultureManager.AddRandomTraits(culture.CultureId);

                    cultureIds.Add(culture.CultureId);
                }

                return cultureIds;
            }

            private List<string> GenerateAICultures(int count)
            {
                List<string> cultureIds = new List<string>();

                for (int i = 0; i < count; i++)
                {
                    string name = GenerateCultureName();

                    Color color = Random.ColorHSV(0f, 1f, 0f, 1f, 0f, 1f);

                    var culture = cultureManager.CreateCulture(
                        cultureColor: color,
                        cultureName: name,
                        cultureDescription: $"The {name} culture, forged by the AI."
                    );

                    cultureManager.AddRandomTraits(culture.CultureId);

                    cultureIds.Add(culture.CultureId);
                }

                return cultureIds;
            }

            private List<string> GenerateNeutralCultures(int count)
            {
                List<string> cultureIds = new List<string>();

                for (int i = 0; i < count; i++)
                {
                    string name = GenerateCultureName();

                    Color color = Random.ColorHSV(0.1f, 0.4f, 0.1f, 0.4f, 0.1f, 0.4f);

                    var culture = cultureManager.CreateCulture(
                        cultureColor: color,
                        cultureName: name,
                        cultureDescription: $"The {name} culture, neutral and ancient."
                    );

                    cultureIds.Add(culture.CultureId);
                }

                return cultureIds;
            }

            private string GetRandomTraitId()
            {
                var traits = cultureManager.CultureTraitTemplates;

                if (traits == null || traits.Count == 0) return null;

                return traits[Random.Range(0, traits.Count)].TraitId;
            }

            private string GenerateCultureName()
            {
                if (mainConfig.CultureNamePrefixes == null || mainConfig.CultureNamePrefixes.Length == 0) return $"Culture_{Random.Range(100, 999)}";

                string prefix = mainConfig.CultureNamePrefixes[Random.Range(0, mainConfig.CultureNamePrefixes.Length)];
                string suffix = mainConfig.CultureNameSuffixes != null && mainConfig.CultureNameSuffixes.Length > 0 ? mainConfig.CultureNameSuffixes[Random.Range(0, mainConfig.CultureNameSuffixes.Length)] : "";

                return $"{prefix}{suffix}";
            }

            public void CreatePlayers(List<string> cultureIds)
            {
                for (int i = 0; i < cultureIds.Count; i++)
                {
                    string playerId = $"PLAYER_{i + 1:D4}";
                    string playerName = $"RUmeelei";

                    var player = playerManager.CreatePlayer(playerId, playerName, isAI : false);

                    playerManager.AttachPlayerCulture(player.PlayerId, cultureIds[i]);

                    player.GatherResource("Fruits", 100f);
                }
            }

            public void CreateAIPlayers(List<string> cultureIds)
            {
                for (int i = 0; i < cultureIds.Count; i++)
                {
                    string playerId = $"PLAYER_AI_{i + 1:D4}";
                    string playerName = $"AI_{i + 1}";

                    var player = playerManager.CreatePlayer(playerId, playerName, isAI : true);

                    playerManager.AttachPlayerCulture(player.PlayerId, cultureIds[i]);

                    player.GatherResource("Fruits", 100f);
                }
            }

            public void CreateNeutralCultures(List<string> cultureIds)
            {
                foreach (var cultureId in cultureIds)
                {
                    var culture = cultureManager.GetCulture(cultureId);

                    if (culture != null)
                    {
                        culture.CultureIdentity = Random.Range(60f, 90f);
                    }
                }
            }
        }
    }
}