using System;
using UnityEngine;
using System.Collections.Generic;

namespace ER
{
    namespace World
    {
        namespace Tiles
        {
            using Core;
            using Configs;
            using Simulation;
            using Culture;
            using Players;
            using Politics.External.Diplomacy;

            public class TileManager : MonoBehaviour
            {
                public static TileManager Instance {get; private set;}

                [Header("Main")]
                public bool EnableLogging;

                private MainConfig mainConfig;

                private CultureManager cultureManager;
                private TileVisualManager tileVisualManager;

                public event Action<int, int> OnTileChanged;

                private Dictionary<string, int> tilesByOwner = new Dictionary<string, int>();
                private Dictionary<string, int> tilesByCulture = new Dictionary<string, int>();
                
                private HashSet<Vector2Int> activeCaptureTiles = new HashSet<Vector2Int>();
                private HashSet<Vector2Int> structureTiles = new HashSet<Vector2Int>();
                private HashSet<Vector2Int> roadTiles = new HashSet<Vector2Int>();
                private HashSet<Vector2Int> devastatedTiles = new HashSet<Vector2Int>();

                private Dictionary<Vector2Int, Dictionary<string, float>> influenceAccumulator = new Dictionary<Vector2Int, Dictionary<string, float>>();

                private Dictionary<string, float> culturalHostility = new Dictionary<string, float>();

                private List<WorldAnchorObject> worldObjects = new List<WorldAnchorObject>();
                private int nextWorldObjectId = 1;

                private TileData[] Tiles;

                private int Width;
                private int Height;

                public int WorldWidth => Width;
                public int WorldHeight => Height;

                public IReadOnlyList<WorldAnchorObject> GetWorldObjects() => worldObjects;
                public IReadOnlyCollection<Vector2Int> GetRoadTiles() => roadTiles;
                public IReadOnlyCollection<Vector2Int> GetActiveCaptureTiles() => activeCaptureTiles;
                public IReadOnlyCollection<Vector2Int> GetStructureTiles() => structureTiles;

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

                    CoreManager.RegisterTileManager(this);
                }

                public void Initialize()
                {
                    mainConfig = CoreManager.MainConfig;
                    cultureManager = CoreManager.CultureManager;
                    tileVisualManager = CoreManager.TileVisualManager;

                    Width = WorldCreationData.WorldSize;
                    Height = WorldCreationData.WorldSize;

                    Tiles = new TileData[Width * Height];

                    GenerateWorld();

                    if (cultureManager.GetActiveCultures().Count > 0)
                    {
                        GenerateCultureCapitals();
                    }
                }

                private void GenerateWorld()
                {
                    tilesByOwner.Clear();
                    tilesByCulture.Clear();
                    activeCaptureTiles.Clear();
                    structureTiles.Clear();
                    roadTiles.Clear();
                    devastatedTiles.Clear();
                    culturalHostility.Clear();
                    worldObjects.Clear();
                    nextWorldObjectId = 1;

                    Tiles = WorldMapGenerator.GetOrGenerateWorldData(Width, Height, mainConfig, WorldMapGenerator.CurrentSeed);

                    for (int i = 0; i < Tiles.Length; i++)
                    {
                        if (string.IsNullOrEmpty(Tiles[i].DeJureOwner))
                        {
                            Tiles[i].DeJureOwner = "CUL_NONE";
                        }
                    }
                }

                private void GenerateCultureCapitals()
                {
                    var activeCultures = cultureManager.GetActiveCultures();

                    List<Vector2Int> placedCapitals = new List<Vector2Int>();

                    float desiredMinDist = Mathf.Max(3f, Width * 0.12f);

                    foreach (var culture in activeCultures)
                    {
                        int bestX = -1;
                        int bestY = -1;
                        float bestScore = -9999f;

                        for (int y = 1; y < Height - 1; y++)
                        {
                            for (int x = 1; x < Width - 1; x++)
                            {
                                int i = y * Width + x;

                                if (Tiles[i].Owner != "CUL_NONE") continue;

                                Vector2Int candidatePos = new Vector2Int(x, y);

                                float nearestCapDist = 999f;

                                foreach (var cap in placedCapitals)
                                {
                                    float d = Vector2Int.Distance(candidatePos, cap);
                                    if (d < nearestCapDist) nearestCapDist = d;
                                }
                                
                                if (nearestCapDist < 2f) continue;

                                float capitalScore = UnityEngine.Random.Range(0f, 2f);

                                if (nearestCapDist >= desiredMinDist) capitalScore += 20f;
                                else capitalScore += nearestCapDist * 2f;

                                if (Tiles[i].Type == TileType.Grass) capitalScore += 15f;
                                else if (Tiles[i].Type == TileType.Sand) capitalScore += 8f;
                                else if (Tiles[i].Type == TileType.Rock) capitalScore += 5f;
                                else if (Tiles[i].Type == TileType.Water) capitalScore -= 30f;

                                if (capitalScore > bestScore)
                                {
                                    bestScore = capitalScore;
                                    bestX = x;
                                    bestY = y;
                                }
                            }
                        }

                        if (bestX == -1)
                        {
                            bestX = UnityEngine.Random.Range(1, Width - 1);
                            bestY = UnityEngine.Random.Range(1, Height - 1);
                        }
                        
                        if (GetTile(bestX, bestY).Type == TileType.Water)
                        {
                            SetTileType(bestX, bestY, TileType.Grass);
                            SetTileElevation(bestX, bestY, 4.0f);
                        }

                        Vector2Int capPos = new Vector2Int(bestX, bestY);

                        SetTileOwner(bestX, bestY, culture.CultureId);
                        culture.SetCultureCapital(capPos);
                        placedCapitals.Add(capPos);

                        CreateInitialCapitalAnchor(culture.CultureId, capPos);
                    }
                }

                public WorldAnchorObject CreateInitialCapitalAnchor(string cultureId, Vector2Int capTile)
                {
                    Vector2 pos = GetTileCenter(capTile.x, capTile.y);

                    var townHall = new WorldAnchorObject
                    {
                        ObjectId = $"OBJ_{nextWorldObjectId++:D4}",
                        ObjectType = WorldObjectType.TownHall,
                        Position = pos,
                        FootprintRadius = 0.22f,
                        DeFactoOwnerId = cultureId,
                        DeJureOwnerId = cultureId,
                        IsUnderConstruction = false,
                        BuildProgress = 100f,
                        Integrity = 100f,
                        ControlPower = 100f
                    };

                    worldObjects.Add(townHall);
                    ProjectAnchorControl(townHall, establishDeJureOnWildLand: true);

                    return townHall;
                }

                public WorldAnchorObject TryStartConstructionSite(Vector2 worldPos, WorldObjectType type, string ownerId)
                {
                    TileData tileAtPos = WorldToTile(worldPos);
                    if (tileAtPos.Type == TileType.Water) return null;

                    float newRadius = (type == WorldObjectType.TownHall || type == WorldObjectType.Fortress) ? 0.22f : 0.15f;

                    for (int i = 0; i < worldObjects.Count; i++)
                    {
                        if (Vector2.Distance(worldPos, worldObjects[i].Position) < (newRadius + worldObjects[i].FootprintRadius))
                        {
                            return null;
                        }
                    }

                    float reqWood = type switch
                    {
                        WorldObjectType.Camp => 8f,
                        WorldObjectType.TownHall => mainConfig != null ? mainConfig.SettlementWoodCost : 25f,
                        WorldObjectType.Fortress => mainConfig != null ? mainConfig.OutpostWoodCost : 15f,
                        WorldObjectType.Warehouse => mainConfig != null ? mainConfig.HubWoodCost : 20f,
                        _ => 10f
                    };

                    float reqStone = type switch
                    {
                        WorldObjectType.Camp => 0f,
                        WorldObjectType.TownHall => mainConfig != null ? mainConfig.SettlementStoneCost : 20f,
                        WorldObjectType.Fortress => mainConfig != null ? mainConfig.OutpostStoneCost : 10f,
                        WorldObjectType.Warehouse => mainConfig != null ? mainConfig.HubStoneCost : 10f,
                        _ => 6f
                    };

                    var site = new WorldAnchorObject
                    {
                        ObjectId = $"OBJ_{nextWorldObjectId++:D4}",
                        ObjectType = type,
                        Position = worldPos,
                        FootprintRadius = newRadius,
                        DeFactoOwnerId = ownerId,
                        DeJureOwnerId = string.IsNullOrEmpty(tileAtPos.DeJureOwner) || tileAtPos.DeJureOwner == "CUL_NONE" ? ownerId : tileAtPos.DeJureOwner,
                        IsUnderConstruction = true,
                        RequiredWood = reqWood,
                        RequiredStone = reqStone,
                        DeliveredWood = 0f,
                        DeliveredStone = 0f,
                        BuildProgress = 10f,
                        Integrity = 100f,
                        ControlPower = 25f
                    };

                    worldObjects.Add(site);

                    Vector2Int tilePos = WorldToTilePos(worldPos);
                    TileData td = GetTile(tilePos.x, tilePos.y);
                    td.Structure = type switch
                    {
                        WorldObjectType.TownHall => TileStructureType.Settlement,
                        WorldObjectType.Fortress => TileStructureType.Outpost,
                        _ => TileStructureType.LogisticsHub
                    };
                    UpdateTileData(tilePos.x, tilePos.y, td);

                    return site;
                }
                
                public void AddTileDevastation(int x, int y, float amount)
                {
                    if (x < 0 || x >= Width || y < 0 || y >= Height || amount <= 0f) return;

                    int i = y * Width + x;
                    TileData t = Tiles[i];
                    if (t.Type == TileType.Water) return;

                    t.LocalDevastation = Mathf.Clamp(t.LocalDevastation + amount, 0f, 100f);
                    Tiles[i] = t;
                    devastatedTiles.Add(new Vector2Int(x, y));
                }

                private static string GetCulturePairKey(string c1, string c2)
                {
                    return string.CompareOrdinal(c1, c2) < 0 ? $"{c1}_{c2}" : $"{c2}_{c1}";
                }
                
                public void AddCulturalHostility(string c1, string c2, float amount)
                {
                    if (string.IsNullOrEmpty(c1) || string.IsNullOrEmpty(c2) || c1 == c2 || c1 == "CUL_NONE" || c2 == "CUL_NONE") return;

                    string key = GetCulturePairKey(c1, c2);
                    culturalHostility.TryGetValue(key, out float cur);
                    culturalHostility[key] = Mathf.Clamp(cur + amount, 0f, 100f);
                }

                public float GetCulturalHostility(string c1, string c2)
                {
                    if (string.IsNullOrEmpty(c1) || string.IsNullOrEmpty(c2) || c1 == c2) return 0f;

                    string key = GetCulturePairKey(c1, c2);
                    return culturalHostility.TryGetValue(key, out float val) ? val : 0f;
                }

                public bool CanAffordStructure(TileStructureType structureType, PlayerData player)
                {
                    if (player == null || mainConfig == null) return false;

                    return structureType switch
                    {
                        TileStructureType.Outpost => player.GetWood() >= mainConfig.OutpostWoodCost && player.GetStone() >= mainConfig.OutpostStoneCost,
                        TileStructureType.LogisticsHub => player.GetWood() >= mainConfig.HubWoodCost && player.GetStone() >= mainConfig.HubStoneCost,
                        TileStructureType.Settlement => player.GetWood() >= mainConfig.SettlementWoodCost && player.GetStone() >= mainConfig.SettlementStoneCost && player.GetIron() >= mainConfig.SettlementIronCost,
                        _ => false
                    };
                }

                public static float GetStructureBuildCost(TileStructureType structureType)
                {
                    return structureType switch
                    {
                        TileStructureType.Outpost => 20f,
                        TileStructureType.LogisticsHub => 25f,
                        TileStructureType.Settlement => 40f,
                        _ => 0f
                    };
                }

                public bool TryBuildStructure(int x, int y, TileStructureType structureType, PlayerData player)
                {
                    if (x < 0 || x >= Width || y < 0 || y >= Height) return false;
                    if (player == null || player.PlayerCulture == null) return false;
                
                    TileData tile = GetTile(x, y);
                
                    if (tile.Type == TileType.Water) return false;
                    if (tile.Owner != player.PlayerCulture.CultureId) return false;
                    if (tile.Structure == structureType) return false;
                
                    if (!CanAffordStructure(structureType, player)) return false;
                
                    switch (structureType)
                    {
                        case TileStructureType.Outpost:
                            player.SpendResource("Wood", mainConfig.OutpostWoodCost);
                            player.SpendResource("Stone", mainConfig.OutpostStoneCost);
                            break;
                
                        case TileStructureType.LogisticsHub:
                            player.SpendResource("Wood", mainConfig.HubWoodCost);
                            player.SpendResource("Stone", mainConfig.HubStoneCost);
                            break;
                
                        case TileStructureType.Settlement:
                            player.SpendResource("Wood", mainConfig.SettlementWoodCost);
                            player.SpendResource("Stone", mainConfig.SettlementStoneCost);
                            player.SpendResource("Iron", mainConfig.SettlementIronCost);
                            break;
                    }

                    WorldObjectType anchorType = structureType switch
                    {
                        TileStructureType.Settlement => WorldObjectType.TownHall,
                        TileStructureType.Outpost => WorldObjectType.Fortress,
                        _ => WorldObjectType.Warehouse
                    };

                    var site = TryStartConstructionSite(GetTileCenter(x, y), anchorType, player.PlayerCulture.CultureId);
                    if (site != null)
                    {
                        site.DeliveredWood = site.RequiredWood;
                        site.DeliveredStone = site.RequiredStone;
                    }

                    tile.Structure = structureType;
                    UpdateTileData(x, y, tile);
                
                    if (EnableLogging)
                    {
                        Debug.Log($"[{player.PlayerCulture.CultureName}] Started building {structureType} at ({x}, {y})");
                    }
                
                    return true;
                }

                public bool CanAffordRoad(TileRoadType roadType, PlayerData player)
                {
                    if (player == null || mainConfig == null) return false;

                    return roadType switch
                    {
                        TileRoadType.PavedRoad => player.GetWood() >= mainConfig.PavedRoadWoodCost && player.GetStone() >= mainConfig.PavedRoadStoneCost,
                        TileRoadType.Railway => player.GetWood() >= mainConfig.RailwayWoodCost && player.GetIron() >= mainConfig.RailwayIronCost,
                        _ => false
                    };
                }

                public bool TryBuildRoad(int x, int y, TileRoadType targetRoad, PlayerData player)
                {
                    if (x < 0 || x >= Width || y < 0 || y >= Height) return false;
                    if (player == null || player.PlayerCulture == null) return false;

                    TileData tile = GetTile(x, y);

                    if (tile.Type == TileType.Water) return false;
                    if (tile.Owner != player.PlayerCulture.CultureId) return false;
                    if (tile.Road >= targetRoad) return false;

                    if (!CanAffordRoad(targetRoad, player)) return false;

                    switch (targetRoad)
                    {
                        case TileRoadType.PavedRoad:
                            player.SpendResource("Wood", mainConfig.PavedRoadWoodCost);
                            player.SpendResource("Stone", mainConfig.PavedRoadStoneCost);
                            break;

                        case TileRoadType.Railway:
                            player.SpendResource("Wood", mainConfig.RailwayWoodCost);
                            player.SpendResource("Iron", mainConfig.RailwayIronCost);
                            break;
                    }

                    tile.Road = targetRoad;
                    tile.RoadTraffic = 100f;

                    UpdateTileData(x, y, tile);
                    return true;
                }

                public bool TryBuildCommLine(int x, int y, TileCommLineType targetComm, PlayerData player)
                {
                    if (x < 0 || x >= Width || y < 0 || y >= Height) return false;
                    if (player == null || player.PlayerCulture == null || mainConfig == null) return false;

                    TileData tile = GetTile(x, y);

                    if (tile.Type == TileType.Water) return false;
                    if (tile.Owner != player.PlayerCulture.CultureId) return false;
                    if (tile.CommLine >= targetComm) return false;

                    switch (targetComm)
                    {
                        case TileCommLineType.Watchtowers:
                            if (player.GetWood() < mainConfig.WatchtowerWoodCost) return false;
                            player.SpendResource("Wood", mainConfig.WatchtowerWoodCost);
                            break;

                        case TileCommLineType.TelegraphLine:
                            if (player.GetWood() < mainConfig.TelegraphWoodCost || player.GetIron() < mainConfig.TelegraphIronCost) return false;
                            player.SpendResource("Wood", mainConfig.TelegraphWoodCost);
                            player.SpendResource("Iron", mainConfig.TelegraphIronCost);
                            break;

                        case TileCommLineType.TelephoneLine:
                            if (player.GetIron() < mainConfig.TelephoneIronCost) return false;
                            player.SpendResource("Iron", mainConfig.TelephoneIronCost);
                            break;
                    }

                    tile.CommLine = targetComm;
                    UpdateTileData(x, y, tile);
                    return true;
                }

                public void AddRoadTraffic(int x, int y, float amount)
                {
                    if (x < 0 || x >= Width || y < 0 || y >= Height || amount <= 0f) return;

                    int i = y * Width + x;
                    TileData tile = Tiles[i];

                    if (tile.Type == TileType.Water) return;

                    if (tile.Road >= TileRoadType.PavedRoad)
                    {
                        tile.RoadTraffic = 100f;
                        Tiles[i] = tile;
                        return;
                    }

                    tile.RoadTraffic = Mathf.Clamp(tile.RoadTraffic + amount, 0f, 100f);
                    roadTiles.Add(new Vector2Int(x, y));

                    if (tile.Road == TileRoadType.None && tile.RoadTraffic >= 100f)
                    {
                        tile.Road = TileRoadType.DirtPath;
                        UpdateTileData(x, y, tile);
                    }
                    else
                    {
                        Tiles[i] = tile;
                    }
                }

                public void ProcessRoadAndDevastationDecay(float delta)
                {
                    if (mainConfig == null) return;

                    if (roadTiles.Count > 0)
                    {
                        float decay = mainConfig.DirtPathDecayRatePerSecond * delta;
                        var roadSnapshot = new List<Vector2Int>(roadTiles);

                        foreach (var pos in roadSnapshot)
                        {
                            TileData t = GetTile(pos.x, pos.y);

                            if (t.Road == TileRoadType.DirtPath || (t.Road == TileRoadType.None && t.RoadTraffic > 0f))
                            {
                                t.RoadTraffic = Mathf.Max(0f, t.RoadTraffic - decay);

                                if (t.Road == TileRoadType.DirtPath && t.RoadTraffic <= 5f)
                                {
                                    t.Road = TileRoadType.None;
                                    UpdateTileData(pos.x, pos.y, t);
                                }
                                else
                                {
                                    Tiles[pos.y * Width + pos.x] = t;
                                }
                            }
                        }
                    }

                    if (devastatedTiles.Count > 0)
                    {
                        float heal = mainConfig.DevastationRecoveryPerSecond * delta;
                        var devSnapshot = new List<Vector2Int>(devastatedTiles);

                        foreach (var pos in devSnapshot)
                        {
                            int idx = pos.y * Width + pos.x;
                            TileData t = Tiles[idx];
                            t.LocalDevastation = Mathf.Max(0f, t.LocalDevastation - heal);
                            Tiles[idx] = t;

                            if (t.LocalDevastation <= 0.05f)
                            {
                                devastatedTiles.Remove(pos);
                            }
                        }
                    }
                }

                public void DepositToHub(int x, int y, float food, float materials)
                {
                    if (x < 0 || x >= Width || y < 0 || y >= Height) return;

                    TileData tile = GetTile(x, y);

                    if (tile.Structure != TileStructureType.LogisticsHub) return;

                    tile.StoredFood = Mathf.Clamp(tile.StoredFood + food, 0f, mainConfig.HubMaxCapacity);
                    tile.StoredMaterials = Mathf.Clamp(tile.StoredMaterials + materials, 0f, mainConfig.HubMaxCapacity);

                    UpdateTileData(x, y, tile);
                }

                public void ExtractFromHub(int x, int y, out float extractedFood, out float extractedMaterials)
                {
                    extractedFood = 0f;
                    extractedMaterials = 0f;

                    if (x < 0 || x >= Width || y < 0 || y >= Height) return;

                    TileData tile = GetTile(x, y);

                    if (tile.Structure != TileStructureType.LogisticsHub) return;

                    extractedFood = Mathf.Min(15f, tile.StoredFood);
                    extractedMaterials = Mathf.Min(10f, tile.StoredMaterials);

                    tile.StoredFood -= extractedFood;
                    tile.StoredMaterials -= extractedMaterials;
                    
                    if (tile.DeFactoOwner != tile.ResidentCultureId && (extractedFood > 0f || extractedMaterials > 0f))
                    {
                        tile.LocalDevastation = Mathf.Clamp(tile.LocalDevastation + 15f, 0f, 100f);
                        devastatedTiles.Add(new Vector2Int(x, y));
                        AddCulturalHostility(tile.DeFactoOwner, tile.ResidentCultureId, 5f);
                    }

                    UpdateTileData(x, y, tile);
                }

                public void ProcessRegionalInfluence(float delta)
                {
                    if (cultureManager == null) return;

                    ProcessRoadAndDevastationDecay(delta);
                    ProcessAnchorsAndControlDecay(delta);

                    influenceAccumulator.Clear();

                    var activeCultures = cultureManager.GetActiveCultures();

                    foreach (var culture in activeCultures)
                    {
                        AccumulateInfluenceFromTile(culture.CultureCapital.x, culture.CultureCapital.y, culture, true, delta);
                    }

                    foreach (var pos in structureTiles)
                    {
                        TileData tile = GetTile(pos.x, pos.y);

                        if (tile.Structure == TileStructureType.None || tile.Owner == "CUL_NONE") continue;

                        var culture = cultureManager.GetCulture(tile.Owner);

                        if (culture == null || culture.CultureDefeated) continue;

                        bool isCap = culture.CultureCapital.x == pos.x && culture.CultureCapital.y == pos.y;

                        if (!isCap)
                        {
                            AccumulateInfluenceFromTile(pos.x, pos.y, culture, false, delta);
                        }
                    }
                    
                    foreach (var kvp in influenceAccumulator)
                    {
                        Vector2Int pos = kvp.Key;
                        var culturePowers = kvp.Value;

                        string bestCulture = null;
                        float bestPower = 0f;
                        float secondBestPower = 0f;

                        foreach (var entry in culturePowers)
                        {
                            if (entry.Value > bestPower)
                            {
                                secondBestPower = bestPower;
                                bestPower = entry.Value;
                                bestCulture = entry.Key;
                            }
                            else if (entry.Value > secondBestPower)
                            {
                                secondBestPower = entry.Value;
                            }
                        }

                        float netPower = bestPower - secondBestPower;

                        if (!string.IsNullOrEmpty(bestCulture) && netPower > 0.05f)
                        {
                            TileData targetTile = GetTile(pos.x, pos.y);

                            TileCaptureSystem.ApplyInfluence(targetTile, pos.x, pos.y, bestCulture, netPower, this);
                        }
                    }

                    float assimilationProb = (mainConfig != null ? mainConfig.CulturalAssimilationChance : 0.08f) * delta;

                    foreach (var culture in activeCultures)
                    {
                        TryAssimilateAroundTile(culture.CultureCapital.x, culture.CultureCapital.y, culture.CultureId, 2, assimilationProb);
                    }

                    foreach (var pos in structureTiles)
                    {
                        TileData st = GetTile(pos.x, pos.y);
                        if (st.Structure == TileStructureType.Settlement && st.Owner != "CUL_NONE")
                        {
                            TryAssimilateAroundTile(pos.x, pos.y, st.Owner, 2, assimilationProb);
                        }
                    }
                }

                public void ProcessAnchorsAndControlDecay(float delta)
                {
                    var unitManager = CoreManager.UnitManager;
                    if (unitManager == null) return;

                    for (int i = worldObjects.Count - 1; i >= 0; i--)
                    {
                        var anchor = worldObjects[i];
                        if (anchor.IsUnderConstruction) continue;

                        var nearbyUnits = unitManager.GetUnitsAtPosition(anchor.Position, 2.5f);
                        int ownerUnitsNear = 0;
                        int enemyMilitaryNear = 0;
                        string strongestEnemyId = "";

                        for (int u = 0; u < nearbyUnits.Count; u++)
                        {
                            var unit = nearbyUnits[u];
                            if (!unit.IsAlive) continue;

                            if (unit.CultureId == anchor.DeFactoOwnerId)
                            {
                                ownerUnitsNear++;
                            }
                            else if (DiplomacyManager.Instance != null && DiplomacyManager.Instance.IsAtWar(unit.CultureId, anchor.DeFactoOwnerId))
                            {
                                if (unit is Unit.Human h && h.Group == Unit.PopulationGroup.Military)
                                {
                                    enemyMilitaryNear++;
                                    strongestEnemyId = unit.CultureId;
                                }
                            }
                        }
                        
                        if (enemyMilitaryNear > ownerUnitsNear && !string.IsNullOrEmpty(strongestEnemyId))
                        {
                            anchor.CapturingOwnerId = strongestEnemyId;
                            anchor.CaptureProgress += (enemyMilitaryNear - ownerUnitsNear) * 14f * delta;
                            
                            Vector2Int anchorTile = WorldToTilePos(anchor.Position);
                            AddTileDevastation(anchorTile.x, anchorTile.y, 8f * delta);

                            if (anchor.CaptureProgress >= 100f)
                            {
                                string oldOwner = anchor.DeFactoOwnerId;
                                anchor.DeFactoOwnerId = strongestEnemyId;
                                anchor.CaptureProgress = 0f;
                                anchor.CapturingOwnerId = "";
                                anchor.ControlPower = 65f;

                                AddCulturalHostility(strongestEnemyId, oldOwner, 12f);
                                CultureAISystem.RegisterWarTileCaptured(strongestEnemyId, oldOwner, this);
                                ProjectAnchorControl(anchor, establishDeJureOnWildLand: false);
                            }
                        }
                        else
                        {
                            anchor.CaptureProgress = Mathf.Max(0f, anchor.CaptureProgress - 8f * delta);
                        }
                        
                        var ownerPlayer = CoreManager.PlayerManager != null ? CoreManager.PlayerManager.GetPlayerForCulture(anchor.DeFactoOwnerId) : null;
                        bool isStarving = ownerPlayer != null && ownerPlayer.GetFood() <= 1f;

                        if (ownerUnitsNear > 0 && !isStarving)
                        {
                            anchor.ControlPower = Mathf.Min(100f, anchor.ControlPower + 6f * delta);
                            anchor.Integrity = Mathf.Min(100f, anchor.Integrity + 3f * delta);
                            ProjectAnchorControl(anchor, establishDeJureOnWildLand: anchor.ObjectType == WorldObjectType.TownHall);
                        }
                        else
                        {
                            float decayRate = isStarving ? 4.5f : 2.5f;
                            anchor.ControlPower = Mathf.Max(0f, anchor.ControlPower - decayRate * delta);
                            anchor.Integrity = Mathf.Max(10f, anchor.Integrity - (decayRate * 0.4f) * delta);

                            if (anchor.ControlPower < 20f)
                            {
                                ShrinkAnchorControl(anchor);
                            }
                        }
                    }
                }

                private void ProjectAnchorControl(WorldAnchorObject anchor, bool establishDeJureOnWildLand)
                {
                    Vector2Int centerTile = WorldToTilePos(anchor.Position);
                    int radius = anchor.GetMaxControlRadiusInTiles();

                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        for (int dx = -radius; dx <= radius; dx++)
                        {
                            int nx = centerTile.x + dx;
                            int ny = centerTile.y + dy;

                            if (nx < 0 || nx >= Width || ny < 0 || ny >= Height) continue;
                            if (Mathf.Sqrt(dx * dx + dy * dy) > radius + 0.3f) continue;

                            TileData t = GetTile(nx, ny);
                            if (t.Type == TileType.Water) continue;

                            if (!string.IsNullOrEmpty(t.DeFactoOwner) && t.DeFactoOwner != "CUL_NONE" && t.DeFactoOwner != anchor.DeFactoOwnerId)
                            {
                                bool atWar = DiplomacyManager.Instance != null && DiplomacyManager.Instance.IsAtWar(anchor.DeFactoOwnerId, t.DeFactoOwner);
                                if (!atWar) continue;
                            }

                            t.DeFactoOwner = anchor.DeFactoOwnerId;

                            if (establishDeJureOnWildLand && (string.IsNullOrEmpty(t.DeJureOwner) || t.DeJureOwner == "CUL_NONE"))
                            {
                                t.DeJureOwner = anchor.DeFactoOwnerId;
                            }

                            if (string.IsNullOrEmpty(t.ResidentCultureId) || t.ResidentCultureId == "CUL_NONE")
                            {
                                t.ResidentCultureId = anchor.DeFactoOwnerId;
                            }

                            UpdateTileData(nx, ny, t);
                        }
                    }
                }

                private void ShrinkAnchorControl(WorldAnchorObject anchor)
                {
                    Vector2Int centerTile = WorldToTilePos(anchor.Position);
                    int maxRadius = 4;

                    for (int dy = -maxRadius; dy <= maxRadius; dy++)
                    {
                        for (int dx = -maxRadius; dx <= maxRadius; dx++)
                        {
                            if (Mathf.Abs(dx) <= 1 && Mathf.Abs(dy) <= 1) continue;

                            int nx = centerTile.x + dx;
                            int ny = centerTile.y + dy;
                            if (nx < 0 || nx >= Width || ny < 0 || ny >= Height) continue;

                            TileData t = GetTile(nx, ny);
                            if (t.DeFactoOwner == anchor.DeFactoOwnerId && UnityEngine.Random.value < 0.15f)
                            {
                                t.DeFactoOwner = (!string.IsNullOrEmpty(t.DeJureOwner) && t.DeJureOwner != anchor.DeFactoOwnerId)
                                    ? t.DeJureOwner
                                    : "CUL_NONE";

                                UpdateTileData(nx, ny, t);
                            }
                        }
                    }
                }

                public void AnnexOccupiedTiles(string winnerId, string loserId)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        for (int x = 0; x < Width; x++)
                        {
                            TileData t = GetTile(x, y);
                            if (t.DeFactoOwner == winnerId && t.DeJureOwner == loserId)
                            {
                                t.DeJureOwner = winnerId;
                                UpdateTileData(x, y, t);
                            }
                        }
                    }
                }

                private void TryAssimilateAroundTile(int centerX, int centerY, string stateOwnerId, int radius, float probability)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        for (int dx = -radius; dx <= radius; dx++)
                        {
                            int nx = centerX + dx;
                            int ny = centerY + dy;

                            if (nx < 0 || nx >= Width || ny < 0 || ny >= Height) continue;

                            TileData t = GetTile(nx, ny);
                            
                            if (t.Owner == stateOwnerId && t.ResidentCultureId != stateOwnerId && t.LocalDevastation < 10f && UnityEngine.Random.value < probability)
                            {
                                t.ResidentCultureId = stateOwnerId;
                                UpdateTileData(nx, ny, t);
                            }
                        }
                    }
                }

                private void AccumulateInfluenceFromTile(int centerX, int centerY, CultureData culture, bool isCapital, float delta)
                {
                    TileData sourceTile = GetTile(centerX, centerY);

                    if (sourceTile.Owner != culture.CultureId) return;

                    float multiplier = sourceTile.GetInfluenceMultiplier(isCapital);
                    float identityFactor = Mathf.Clamp(culture.CultureIdentity / 100f, 0.3f, 1.3f);

                    var gov = CoreManager.GovernmentManager != null ? CoreManager.GovernmentManager.GetGovernment(culture.CultureId) : null;
                    float govMultiplier = gov != null ? gov.GetInfluenceMultiplier() : 1f;

                    int radius = multiplier >= 2.5f ? 2 : 1;
                    float baseAssimilatePower = mainConfig.CaptureRatePerSecond * mainConfig.RegionalInfluenceFactor * multiplier * identityFactor * govMultiplier * delta;

                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        for (int dx = -radius; dx <= radius; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;

                            int nx = centerX + dx;
                            int ny = centerY + dy;

                            if (nx < 0 || nx >= Width || ny < 0 || ny >= Height) continue;

                            float dist = Mathf.Sqrt(dx * dx + dy * dy);

                            if (dist > radius + 0.2f) continue;

                            TileData targetTile = GetTile(nx, ny);

                            if (targetTile.Type == TileType.Water || targetTile.Owner == culture.CultureId) continue;

                            if (targetTile.Owner != "CUL_NONE")
                            {
                                bool atWar = DiplomacyManager.Instance != null && DiplomacyManager.Instance.IsAtWar(culture.CultureId, targetTile.Owner);

                                if (!atWar) continue;
                            }

                            if (!BordersCultureTerritory(nx, ny, culture.CultureId)) continue;

                            float distanceFalloff = 1f / Mathf.Max(1f, dist);
                            float targetResistance = targetTile.GetCaptureResistance(false);

                            float influenceDelta = (baseAssimilatePower * distanceFalloff) / targetResistance;

                            Vector2Int targetPos = new Vector2Int(nx, ny);

                            if (!influenceAccumulator.TryGetValue(targetPos, out var dict))
                            {
                                dict = new Dictionary<string, float>();

                                influenceAccumulator[targetPos] = dict;
                            }

                            dict.TryGetValue(culture.CultureId, out float currentVal);

                            dict[culture.CultureId] = currentVal + influenceDelta;
                        }
                    }
                }

                public bool BordersCultureTerritory(int x, int y, string cultureId)
                {
                    int[] dx = { -1, 1, 0, 0 };
                    int[] dy = { 0, 0, -1, 1 };

                    for (int i = 0; i < 4; i++)
                    {
                        int nx = x + dx[i];
                        int ny = y + dy[i];

                        if (nx < 0 || nx >= Width || ny < 0 || ny >= Height) continue;

                        if (GetTile(nx, ny).Owner == cultureId) return true;
                    }

                    return false;
                }

                public Vector2Int? FindNewCultureCapital(string cultureId)
                {
                    if (string.IsNullOrEmpty(cultureId) || cultureId == "CUL_NONE") return null;

                    Vector2Int? bestCandidate = null;
                    float bestScore = -999f;

                    for (int y = 0; y < Height; y++)
                    {
                        for (int x = 0; x < Width; x++)
                        {
                            int i = y * Width + x;
                            TileData tile = Tiles[i];

                            if (tile.Owner != cultureId || tile.Type == TileType.Water) continue;

                            Vector2Int currentPos = new Vector2Int(x, y);
                            float candidateScore = 0f;

                            if (tile.Structure == TileStructureType.Settlement) candidateScore += 50f;
                            else if (tile.Structure == TileStructureType.Outpost) candidateScore += 15f;
                            else if (tile.Structure == TileStructureType.LogisticsHub) candidateScore += 10f;

                            if (tile.Type == TileType.Grass) candidateScore += 10f;
                            else if (tile.Type == TileType.Rock) candidateScore += 5f;

                            candidateScore += Mathf.Min(tile.Elevation, 5f);

                            var neighbors = GetNeighboringTiles(x, y);

                            foreach (var neighbor in neighbors)
                            {
                                if (neighbor.Owner == cultureId)
                                {
                                    candidateScore += 1.5f;
                                }
                            }

                            if (candidateScore > bestScore)
                            {
                                bestScore = candidateScore;
                                bestCandidate = currentPos;
                            }
                        }
                    }

                    return bestCandidate;
                }

                public TileData WorldToTile(Vector2 worldPos)
                {
                    int x = Mathf.FloorToInt(worldPos.x / mainConfig.TileSize);
                    int y = Mathf.FloorToInt(worldPos.y / mainConfig.TileSize);

                    x = Mathf.Clamp(x, 0, Width - 1);
                    y = Mathf.Clamp(y, 0, Height - 1);

                    return GetTile(x, y);
                }

                public Vector2Int WorldToTilePos(Vector2 worldPos)
                {
                    int x = Mathf.FloorToInt(worldPos.x / mainConfig.TileSize);
                    int y = Mathf.FloorToInt(worldPos.y / mainConfig.TileSize);

                    x = Mathf.Clamp(x, 0, Width - 1);
                    y = Mathf.Clamp(y, 0, Height - 1);

                    return new Vector2Int(x, y);
                }

                public TileData GetTile(int x, int y) => Tiles[y * Width + x];

                public Vector2 GetTileCenter(int x, int y)
                {
                    return new Vector2(x * mainConfig.TileSize + mainConfig.TileSize / 2f, y * mainConfig.TileSize + mainConfig.TileSize / 2f);
                }

                public Vector2Int GetClosestTilePos(Vector2 worldPos)
                {
                    int x = Mathf.RoundToInt((worldPos.x / mainConfig.TileSize) - 0.5f);
                    int y = Mathf.RoundToInt((worldPos.y / mainConfig.TileSize) - 0.5f);

                    x = Mathf.Clamp(x, 0, Width - 1);
                    y = Mathf.Clamp(y, 0, Height - 1);

                    return new Vector2Int(x, y);
                }

                public List<TileData> GetNeighboringTiles(int x, int y)
                {
                    var tiles = new List<TileData>(4);

                    foreach (var (dx, dy) in new[] { (-1, 0), (1, 0), (0, -1), (0, 1) })
                    {
                        int nx = x + dx;
                        int ny = y + dy;

                        if (nx >= 0 && nx < Width && ny >= 0 && ny < Height) tiles.Add(GetTile(nx, ny));
                    }

                    return tiles;
                }

                public void NotifyTileChanged(int x, int y)
                {
                    tileVisualManager.MarkTileDirty(x, y);
                    OnTileChanged?.Invoke(x, y);
                }

                public void SetTileType(int x, int y, TileType newType)
                {
                    if (x < 0 || x >= Width || y < 0 || y >= Height) return;

                    int i = y * Width + x;
                    TileData tile = Tiles[i];
                    tile.Type = newType;
                    Tiles[i] = tile;

                    NotifyTileChanged(x, y);
                }

                public void SetTileElevation(int x, int y, float newElevation)
                {
                    if (x < 0 || x >= Width || y < 0 || y >= Height) return;

                    int i = y * Width + x;
                    TileData tile = Tiles[i];
                    tile.Elevation = newElevation;
                    Tiles[i] = tile;

                    NotifyTileChanged(x, y);
                }

                public void SetTileVariation(int x, int y, int newVariation)
                {
                    if (x < 0 || x >= Width || y < 0 || y >= Height) return;

                    int i = y * Width + x;
                    TileData tile = Tiles[i];
                    tile.Variation = newVariation;
                    Tiles[i] = tile;

                    NotifyTileChanged(x, y);
                }

                public void SetTileOwner(int x, int y, string newOwner)
                {
                    if (x < 0 || x >= Width || y < 0 || y >= Height) return;
                    if (newOwner != "CUL_NONE" && cultureManager.GetCulture(newOwner) == null) return;

                    int i = y * Width + x;
                    TileData tile = Tiles[i];
                    string oldOwner = tile.Owner;

                    if (oldOwner == newOwner) return;

                    UpdateOwnerCount(oldOwner, -1);
                    UpdateOwnerCount(newOwner, +1);

                    if (string.IsNullOrEmpty(tile.ResidentCultureId) || tile.ResidentCultureId == "CUL_NONE")
                    {
                        UpdateCultureCount(tile.ResidentCultureId, -1);
                        UpdateCultureCount(newOwner, +1);
                        tile.ResidentCultureId = newOwner;
                    }

                    if (string.IsNullOrEmpty(tile.DeJureOwner) || tile.DeJureOwner == "CUL_NONE")
                    {
                        tile.DeJureOwner = newOwner;
                    }

                    tile.Owner = newOwner;
                    Tiles[i] = tile;

                    NotifyTileChanged(x, y);
                }

                private void UpdateOwnerCount(string ownerId, int delta)
                {
                    if (string.IsNullOrEmpty(ownerId) || ownerId == "CUL_NONE") return;

                    if (!tilesByOwner.TryGetValue(ownerId, out int count))
                    {
                        count = 0;
                    }

                    count += delta;

                    if (count <= 0)
                    {
                        tilesByOwner.Remove(ownerId);
                    }
                    else
                    {
                        tilesByOwner[ownerId] = count;
                    }
                }

                public void UpdateTileData(int x, int y, TileData newData)
                {
                    if (x < 0 || x >= Width || y < 0 || y >= Height) return;

                    int i = y * Width + x;
                    TileData oldTile = Tiles[i];

                    if (oldTile.Owner != newData.Owner)
                    {
                        UpdateOwnerCount(oldTile.Owner, -1);
                        UpdateOwnerCount(newData.Owner, +1);
                    }

                    if (oldTile.ResidentCultureId != newData.ResidentCultureId)
                    {
                        UpdateCultureCount(oldTile.ResidentCultureId, -1);
                        UpdateCultureCount(newData.ResidentCultureId, +1);
                    }

                    Vector2Int pos = new Vector2Int(x, y);

                    if (!string.IsNullOrEmpty(newData.CapturingCultureId) && newData.CaptureProgress > 0f)
                    {
                        activeCaptureTiles.Add(pos);
                    }
                    else
                    {
                        activeCaptureTiles.Remove(pos);
                    }

                    if (newData.Structure != TileStructureType.None)
                    {
                        structureTiles.Add(pos);
                    }
                    else
                    {
                        structureTiles.Remove(pos);
                    }

                    if (newData.Road != TileRoadType.None || newData.CommLine != TileCommLineType.None || newData.RoadTraffic > 0.1f)
                    {
                        roadTiles.Add(pos);
                    }
                    else
                    {
                        roadTiles.Remove(pos);
                    }

                    Tiles[i] = newData;

                    NotifyTileChanged(x, y);
                }

                public Color GetTileOwnerColor(int x, int y)
                {
                    if (x < 0 || x >= Width || y < 0 || y >= Height) return Color.clear;

                    var tile = GetTile(x, y);
                    if (tile.Owner == "CUL_NONE") return Color.clear;

                    var culture = cultureManager.GetCulture(tile.Owner);
                    if (culture == null) return Color.clear;

                    return culture.CultureColor;
                }

                public float GetTileCaptureProgress(int x, int y)
                {
                    if (x < 0 || x >= Width || y < 0 || y >= Height) return 0f;
                    return GetTile(x, y).CaptureProgress;
                }

                public Color GetTileCaptureColor(int x, int y)
                {
                    if (x < 0 || x >= Width || y < 0 || y >= Height) return Color.clear;

                    var tile = GetTile(x, y);
                    if (string.IsNullOrEmpty(tile.CapturingCultureId)) return Color.clear;

                    var culture = cultureManager.GetCulture(tile.CapturingCultureId);
                    if (culture == null) return Color.clear;

                    return culture.CultureColor;
                }

                public int GetTileCountForOwner(string cultureId)
                {
                    if (string.IsNullOrEmpty(cultureId)) return 0;
                    tilesByOwner.TryGetValue(cultureId, out int count);
                    return count;
                }

                public int GetTileCountForCulture(string cultureId)
                {
                    if (string.IsNullOrEmpty(cultureId)) return 0;
                    tilesByCulture.TryGetValue(cultureId, out int count);
                    return count;
                }

                private void UpdateCultureCount(string cultureId, int delta)
                {
                    if (string.IsNullOrEmpty(cultureId) || cultureId == "CUL_NONE") return;

                    tilesByCulture.TryGetValue(cultureId, out int count);
                    count += delta;

                    if (count <= 0) tilesByCulture.Remove(cultureId);
                    else tilesByCulture[cultureId] = count;
                }
            }
        }
    }
}