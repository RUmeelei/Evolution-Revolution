using UnityEngine;

namespace ER
{
    namespace Configs
    {
        [CreateAssetMenu(fileName = "MainConfig", menuName = "ER/Configs/Main Config")]
        public class MainConfig : ScriptableObject
        {
            [Header("Economy Loop")]
            public int GatherSeason = 0; // 0 - Per step | 1 - Per cycle | 2 - Per tier

            [Header("Starting Resources & Pop")]
            public int StartPopulation = 5;
            public float StartFood = 100f;
            public float StartWood = 40f;
            public float StartStone = 30f;
            public float StartIron = 20f;
            public float StartMoney = 0f;

            [Header("Resource Gathering & Upkeep")]
            public int UnitMaxCarryCapacity = 5;
            public float HarvestFoodAmount = 8f;
            public float HarvestWoodAmount = 6f;
            public float HarvestStoneAmount = 5f;
            public float HarvestIronAmount = 4f;
            public float FoodUpkeepPerUnit = 0.25f;

            [Header("Population Growth")]
            public float BaseFoodRequiredToGrow = 100f;
            public float FoodRequiredPerPop = 20f;
            public float BaseFoodCostToSpawn = 45f;
            public float FoodCostPerPop = 12f;

            [Header("Structure Costs & Storage")]
            public float OutpostWoodCost = 15f;
            public float OutpostStoneCost = 10f;

            public float HubWoodCost = 20f;
            public float HubStoneCost = 10f;
            public float HubMaxCapacity = 40f;

            public float SettlementWoodCost = 25f;
            public float SettlementStoneCost = 20f;
            public float SettlementIronCost = 10f;

            [Header("World Generation")]
            public float TileSize = 1f;
            public float WorldNoiseScale = 0.08f;
            [Range(0f, 5f)] public float WaterElevationThreshold = 2.5f;
            [Range(0f, 6f)] public float SandElevationThreshold = 3.2f;
            [Range(5f, 10f)] public float RockElevationThreshold = 7.2f;
            [Range(0f, 1f)] public float ContinentFalloffStrength = 0.35f;

            [Range(1, 8)] public int ContinentalPlatesCount = 3;
            [Range(1, 6)] public int OceanicPlatesCount = 3;
            [Range(0f, 4f)] public float MountainRidgeStrength = 2.2f;
            [Range(0, 8)] public int MajorRiversCount = 3;
            [Range(0.01f, 0.5f)] public float CulturalAssimilationChance = 0.08f;

            public string[] CultureNamePrefixes;
            public string[] CultureNameSuffixes;

            [Header("Camera")]
            public float CameraSmoothing = 1f;
            public float CameraKeyboardScrollSpeed = 5f;
            public float CameraZoomSpeed = 1000f;
            
            public float CameraMinHeight = 1f;
            public float CameraMaxHeight = 500f;

            [Header("Cultures")]
            public float WarningIdentityThreshold = 50f;
            public float ErrorIdentityThreshold = 10f;

            public int MinStartTraits = 1;
            public int MaxStartTraits = 2;

            [Header("Units")]
            public float UnitBaseMaxSpeed = 0.8f;
            public float UnitSmoothSpeed = 10f;
            public float UnitAccelerationRate = 0.25f;
            public float UnitDecelerationRate = 1f;

            public int UnitVisualInitialSize = 200;
            public float UnitMobilizationCost = 10f;

            [Tooltip("Visual scale of unit sprite (0.22 fits 10x10 = 100 units per tile)")]
            public float UnitVisualScale = 0.22f;
            
            [Tooltip("Physical separation radius between units (0.09 fits 10x10 units per 1x1 tile)")]
            public float UnitSeparationRadius = 0.09f;

            [Header("Selection")]
            public float SelectionDragThreshold = 10f;

            [Header("Tile Capture & Regional Influence")]
            [Tooltip("Tile capture speed (% per second)")]
            public float CaptureRatePerSecond = 10f;

            [Tooltip("Capture progress decay rate (% per second)")]
            public float CaptureDecayRatePerSecond = 5f;

            [Tooltip("Multiplier for passive regional assimilation from structures")]
            public float RegionalInfluenceFactor = 0.12f;

            [Header("Roads & Communication Lines")]
            public float RoadTrampleRatePerSecond = 12f;
            public float DirtPathDecayRatePerSecond = 0.8f;

            public float DirtPathSpeedMultiplier = 1.20f;
            public float PavedRoadSpeedMultiplier = 1.45f;
            public float RailwaySpeedMultiplier = 2.00f;

            public float PavedRoadWoodCost = 4f;
            public float PavedRoadStoneCost = 8f;

            public float RailwayWoodCost = 8f;
            public float RailwayIronCost = 12f;

            public float WatchtowerWoodCost = 6f;
            public float TelegraphWoodCost = 4f;
            public float TelegraphIronCost = 6f;
            public float TelephoneIronCost = 10f;

            [Header("Unrest, Devastation & Occupation")]
            [Tooltip("How fast devastation on a tile heals per second when peaceful")]
            public float DevastationRecoveryPerSecond = 0.6f;

            [Tooltip("Threshold of combined devastation + blood hostility above which occupied locals strike/protest")]
            public float RadicalismStrikeThreshold = 35f;

            [Header("Game")]
            [Tooltip("How often (in seconds) to check the conditions")]
            public float CheckInterval = 1f;
            public bool SpectatorMode = false;

            [Header("War & Diplomacy Balance")]
            [Tooltip("How many seconds without any tile changing hands before a war is considered a stalemate")]
            public float WarStalemateTimeout = 35f;
            
            [Tooltip("Base peace recovery time per captured tile or exhaustion")]
            public float BasePeaceRecoverySeconds = 20f;
            public float PeaceRecoveryPerTileLost = 8f;
            
            [Tooltip("Food threshold below which a state seeks peace")]
            public float WarExhaustionFoodThreshold = 25f;
            
            [Tooltip("Identity threshold below which a state seeks peace")]
            public float WarExhaustionIdentityThreshold = 55f;
        }
    }
}