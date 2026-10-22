using UnityEngine;
using System.Collections.Generic;

namespace ER
{
    namespace Unit
    {
        using Core;
        using Configs;
        using Simulation;
        using Culture;
        using Players;
        using World.Tiles;

        public class UnitManager : MonoBehaviour
        {
            public static UnitManager Instance { get; private set; }

            [Header("Main")]
            public bool EnableLogging;

            [Header("Visual")]
            [SerializeField] private CultureSprites[] CultureSprites;

            private Dictionary<UnitEthnicity, CultureSprites> SpritesByCulture = new();

            private List<Unit> Units = new List<Unit>();
            private Dictionary<string, Unit> UnitsDictionary = new Dictionary<string, Unit>();
            
            private Dictionary<string, List<Unit>> UnitsByCulture = new Dictionary<string, List<Unit>>();
            private static readonly List<Unit> EmptyUnitList = new List<Unit>(0);

            private List<Unit> DeadUnits = new List<Unit>();
            private List<Vector2Int> captureTilesBuffer = new List<Vector2Int>();

            private float CaptureDecayTimer = 0f;
            private const float CaptureDecayInterval = 0.5f;

            private int NextUnitId = 1;

            private AIConfig aiConfig;
            private MainConfig mainConfig;
            private CombatConfig combatConfig;

            private SimulationManager simulationManager;
            private DateManager dateManager;
            private CultureManager cultureManager;
            private TileManager tileManager;
            private UnitVisualPoolManager unitVisualPoolManager;
            private PlayerManager playerManager;

            private UnitSpatialGrid UnitSpatialGrid;

            private float worldWidth;
            private float worldHeight;

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

                CoreManager.RegisterUnitManager(this);
            }

            void OnDestroy()
            {
                if (simulationManager != null) simulationManager.OnTick -= ProcessUnitTick;
                if (dateManager != null) dateManager.OnTier -= ProcessUnitAging;
            }

            public void Initialize()
            {
                SpritesByCulture.Clear();
                if (CultureSprites != null)
                {
                    foreach (var es in CultureSprites)
                    {
                        if (es != null) SpritesByCulture[es.Ethnicity] = es;
                    }
                }

                aiConfig = CoreManager.AIConfig;
                mainConfig = CoreManager.MainConfig;
                combatConfig = CoreManager.CombatConfig;

                simulationManager = CoreManager.SimulationManager;
                simulationManager.OnTick -= ProcessUnitTick;
                simulationManager.OnTick += ProcessUnitTick;

                dateManager = CoreManager.DateManager;
                dateManager.OnTier -= ProcessUnitAging;
                dateManager.OnTier += ProcessUnitAging;

                cultureManager = CoreManager.CultureManager;
                tileManager = CoreManager.TileManager;
                unitVisualPoolManager = CoreManager.UnitVisualPoolManager;
                playerManager = CoreManager.PlayerManager;

                worldWidth = tileManager.WorldWidth * mainConfig.TileSize;
                worldHeight = tileManager.WorldHeight * mainConfig.TileSize;

                UnitSpatialGrid = new UnitSpatialGrid(worldWidth, worldHeight, mainConfig.TileSize * 4f);

                var cultures = cultureManager.GetActiveCultures();

                foreach (var culture in cultures)
                {
                    Vector2 position = tileManager.GetTileCenter(culture.CultureCapital.x, culture.CultureCapital.y);

                    int startPop = mainConfig != null && mainConfig.StartPopulation > 0 ? mainConfig.StartPopulation : 5;

                    for (int i = 0; i < startPop; i++)
                    {
                        Vector2 spawnPos = position + new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(-0.3f, 0.3f));
                    
                        PopulationGroup startGroup = (i < Mathf.CeilToInt(startPop * 0.6f)) ? PopulationGroup.Producers : PopulationGroup.Military;
                    
                        CreateHuman($"Citizen {i + 1}", culture.CultureName, 18, spawnPos, culture.CultureId, startGroup);
                    }
                }
            }

            private UnitSpriteSet GetSpritesForUnit(Unit unit)
            {
                var ethnicity = cultureManager.GetCulture(unit.CultureId)?.CultureEthnicity ?? UnitEthnicity.European;

                if (!SpritesByCulture.TryGetValue(ethnicity, out var sprites) || sprites == null)
                {
                    sprites = (CultureSprites != null && CultureSprites.Length > 0) ? CultureSprites[0] : null;
                }

                if (sprites == null) return null;

                return unit switch
                {
                    Human _ => sprites.Human,
                    _ => sprites.Human
                };
            }

            public Human CreateHuman(string firstName, string lastName, int age, Vector2 position, string cultureId = "CUL_NONE", PopulationGroup group = PopulationGroup.Producers)
            {
                string id = $"HUM_{NextUnitId++:D4}";

                var human = new Human(id, firstName, lastName, age, position, cultureId, group);

                float damage = combatConfig.BaseAttackDamage;

                if (group != PopulationGroup.Military)
                {
                    damage *= 0.5f;
                }

                var culture = cultureManager != null ? cultureManager.GetCulture(cultureId) : null;
                
                if (culture != null)
                {
                    if (culture.HasTrait("BarbarianNation"))
                    {
                        damage *= 1.25f;
                    }

                    if (culture.HasTrait("NomadicNation"))
                    {
                        human.SetSpeed(human.MaxSpeed);
                    }
                }

                RecalculateHumanCombat(human);

                AddUnit(human);

                CreateUnitVisual(human);

                return human;
            }

            public bool MobilizeHuman(Human human)
            {
                if (human == null || !human.IsAlive) return false;
            
                if (human.Group == PopulationGroup.Military) return false;
            
                var player = playerManager != null ? playerManager.GetPlayerForCulture(human.CultureId) : null;
            
                if (player != null)
                {
                    if (player.GetIron() < mainConfig.UnitMobilizationCost) return false;
            
                    player.SpendResource("Iron", mainConfig.UnitMobilizationCost);
                }
            
                human.Group = PopulationGroup.Military;
            
                var inventory = human.GetInventory();
            
                if (inventory != null) inventory.Clear();
            
                RecalculateHumanCombat(human);
            
                human.ClearPlayerOrder();
            
                human.Stop();
            
                human.DecisionTimer = 0f;
            
                return true;
            }
            
            public bool DemobilizeHuman(Human human)
            {
                if (human == null || !human.IsAlive) return false;
            
                if (human.Group != PopulationGroup.Military) return false;
            
                var player = playerManager != null ? playerManager.GetPlayerForCulture(human.CultureId) : null;
            
                if (player != null)
                {
                    player.GatherResource("Iron", mainConfig.UnitMobilizationCost * 0.1f);
                }
            
                human.Group = PopulationGroup.Producers;
            
                RecalculateHumanCombat(human);
            
                human.ClearAttackTarget();
            
                human.ClearPlayerOrder();
            
                human.Stop();
            
                human.DecisionTimer = 0f;
            
                return true;
            }
            
            private void RecalculateHumanCombat(Human human)
            {
                var culture = cultureManager != null ? cultureManager.GetCulture(human.CultureId) : null;

                float traitMult = (culture != null && culture.HasTrait("BarbarianNation")) ? 1.25f : 1.0f;

                if (human.Group != PopulationGroup.Military)
                {
                    human.ApplyEquipmentLoadout(
                        EquipmentLoadout.CreateUnarmed(),
                        combatConfig.BaseAttackDamage,
                        combatConfig.BaseAttackRange,
                        combatConfig.BaseAttackCooldown,
                        traitMult
                    );

                    return;
                }

                var player = playerManager != null ? playerManager.GetPlayerForCulture(human.CultureId) : null;

                ArmorTier chosenArmor = ArmorTier.Leather;

                if (player != null)
                {
                    if (player.GetOrCreateResource("Steel").Amount >= 5f) chosenArmor = ArmorTier.Steel;
                    else if (player.GetIron() >= 15f) chosenArmor = ArmorTier.Copper;
                }
                
                int roleRoll = Mathf.Abs(human.UnitId.GetHashCode()) % 5;

                WeaponType chosenWeapon = roleRoll switch
                {
                    0 => WeaponType.Spear,
                    1 => WeaponType.Sword,
                    2 => WeaponType.Bow,
                    3 => WeaponType.Halberd,
                    _ => WeaponType.Crossbow
                };

                var loadout = EquipmentLoadout.CreatePreset($"{chosenArmor} {chosenWeapon}", chosenWeapon, chosenArmor);

                human.ApplyEquipmentLoadout(
                    loadout,
                    combatConfig.BaseAttackDamage,
                    combatConfig.BaseAttackRange,
                    combatConfig.BaseAttackCooldown,
                    traitMult
                );
            }

            private void CreateUnitVisual(Unit unit)
            {
                var sprites = GetSpritesForUnit(unit);
                if (sprites == null) return;

                UnitVisual view = unitVisualPoolManager.Get();

                if (view != null)
                {
                    view.gameObject.name = $"{unit.UnitName}_View";
                    view.Initialize(unit, sprites);
                }

                unit.UnitVisual = view;
            }

            private void AddUnit(Unit unit)
            {
                if (unit == null) return;

                Units.Add(unit);

                UnitsDictionary[unit.UnitId] = unit;

                if (!UnitsByCulture.TryGetValue(unit.CultureId, out var cultureList))
                {
                    cultureList = new List<Unit>(16);

                    UnitsByCulture[unit.CultureId] = cultureList;
                }
                cultureList.Add(unit);

                UnitSpatialGrid.Register(unit);
            }

            public Unit GetUnit(string id)
            {
                UnitsDictionary.TryGetValue(id, out var unit);

                return unit;
            }

            public List<Unit> GetAllUnits() => Units;

            public List<Unit> GetUnitsAtPosition(Vector2 position, float radius = 1f, bool single = false)
            {
                return UnitSpatialGrid.GetUnitsInRadius(position, radius, single);
            }

            public List<Unit> GetUnitsByCulture(string cultureId)
            {
                if (string.IsNullOrEmpty(cultureId)) return EmptyUnitList;

                return UnitsByCulture.TryGetValue(cultureId, out var list) ? list : EmptyUnitList;
            }

            public int GetUnitCountByCulture(string cultureId)
            {
                if (string.IsNullOrEmpty(cultureId)) return 0;

                return UnitsByCulture.TryGetValue(cultureId, out var list) ? list.Count : 0;
            }

            public List<Human> GetAllHumans()
            {
                return Units.FindAll(u => u is Human).ConvertAll(u => (Human)u);
            }

            public void RemoveUnit(string id)
            {
                var unit = GetUnit(id);

                if (unit == null) return;

                Units.Remove(unit);
                UnitsDictionary.Remove(id);

                if (UnitsByCulture.TryGetValue(unit.CultureId, out var cultureList))
                {
                    cultureList.Remove(unit);
                }

                UnitSpatialGrid.Unregister(unit);
            }

            private void ProcessUnitTick(float delta)
            {
                DeadUnits.Clear();

                foreach (var unit in Units)
                {
                    if (!unit.IsAlive)
                    {
                        DeadUnits.Add(unit);

                        continue;
                    }

                    unit.Update(delta);
                    unit.UpdateCombatCooldown(delta);

                    // AI

                    if (aiConfig.IsAIEnabled)
                    {
                        UnitAISystem.ProcessUnit(unit, aiConfig, combatConfig, UnitSpatialGrid, cultureManager, playerManager, tileManager, worldWidth, worldHeight);

                        if (playerManager.GetPlayerForCulture(unit.CultureId) != null && playerManager.GetPlayerForCulture(unit.CultureId).PlayerAI && !unit.IsAutonomous) unit.IsAutonomous = true;
                    }

                    // Combat

                    if (unit.AttackTarget != null)
                    {
                        if (!unit.AttackTarget.IsAlive)
                        {
                            unit.ClearAttackTarget();
                        }
                        else
                        {
                            float distToTarget = Vector2.Distance(unit.Position, unit.AttackTarget.Position);

                            if (distToTarget <= unit.AttackRange)
                            {
                                unit.Stop();
                                UnitCombatSystem.PerformAttack(unit, unit.AttackTarget);
                            }
                            else
                            {
                                if (!unit.HasTarget || Vector2.Distance(unit.TargetPosition, unit.AttackTarget.Position) > 0.3f)
                                {
                                    unit.SetTarget(unit.AttackTarget.Position);
                                }
                            }
                        }
                    }

                    // Movement & Separation

                    float separationRadius = mainConfig != null && mainConfig.UnitSeparationRadius > 0.02f ? mainConfig.UnitSeparationRadius : 0.09f;
                    var nearbyUnits = UnitSpatialGrid.GetUnitsInRadius(unit.Position, separationRadius);
                    
                    Vector2 separation = Vector2.zero;

                    foreach (var other in nearbyUnits)
                    {
                        if (other == unit) continue;

                        Vector2 diff = unit.Position - other.Position;

                        float dist = diff.magnitude;

                        if (dist > 0.0001f && dist < separationRadius)
                        {
                            float normalizedDist = dist / separationRadius;
                            float strength = (1f - normalizedDist);

                            separation += diff.normalized * strength;
                        }
                    }

                    if (separation != Vector2.zero)
                    {
                        Vector2 push = separation.normalized * 1.2f * delta;
                        Vector2 nextPos = unit.Position + push;

                        if (tileManager.WorldToTile(nextPos).Type != TileType.Water)
                        {
                            Vector2 oldP = unit.Position;
                            unit.ApplySeparation(push, worldWidth, worldHeight);
                            UnitSpatialGrid.UpdatePosition(unit, oldP);
                        }
                    }

                    if (unit.HasTarget)
                    {
                        Vector2 oldPosition = unit.Position;

                        float healthPercent = unit.GetCurrentHealthPercent();
                        float healthFactor = combatConfig.MinHealthSpeedFactor + (1f - combatConfig.MinHealthSpeedFactor) * healthPercent;
                        float adrenalineMultiplier = (healthPercent < combatConfig.AdrenalineHealthThreshold) ? combatConfig.AdrenalineSpeedMultiplier : 1f;

                        TileData tileUnderUnit = tileManager.WorldToTile(unit.Position);
                        float terrainSpeedFactor = tileUnderUnit.GetMovementSpeedMultiplier(
                            0.33f,
                            0.67f,
                            0.85f,
                            mainConfig.DirtPathSpeedMultiplier,
                            mainConfig.PavedRoadSpeedMultiplier,
                            mainConfig.RailwaySpeedMultiplier
                        );

                        float effectiveMaxSpeed = unit.MaxSpeed * healthFactor * adrenalineMultiplier * unit.EquipmentSpeedMultiplier * terrainSpeedFactor;
                        float effectiveAcceleration = mainConfig.UnitAccelerationRate * delta * healthFactor * adrenalineMultiplier;

                        unit.Speed = Mathf.Min(effectiveMaxSpeed, unit.Speed + effectiveAcceleration);
                        unit.MoveTowardsTarget(delta);

                        UnitSpatialGrid.UpdatePosition(unit, oldPosition);
                        
                        Vector2Int movedTilePos = tileManager.GetClosestTilePos(unit.Position);
                        tileManager.AddRoadTraffic(movedTilePos.x, movedTilePos.y, mainConfig.RoadTrampleRatePerSecond * delta);
                    }
                    else if (unit.Speed > 0)
                    {
                        unit.Speed = Mathf.Max(0, unit.Speed - mainConfig.UnitDecelerationRate * delta);
                    }

                    Vector2Int tilePos = tileManager.GetClosestTilePos(unit.Position);

                    TileData currentTile = tileManager.GetTile(tilePos.x, tilePos.y);

                    if (currentTile.Type != TileType.Water)
                    {
                        TileCaptureSystem.ProcessCapture(currentTile, tilePos.x, tilePos.y, unit, tileManager, mainConfig, delta);
                    }
                }

                CaptureDecayTimer += delta;

                if (CaptureDecayTimer >= CaptureDecayInterval)
                {
                    CaptureDecayTimer = 0f;

                    ProcessCaptureDecay();
                }

                foreach (var dead in DeadUnits)
                {
                    if (cultureManager != null) cultureManager.ChangeCultureIdentity(dead.CultureId, -1.8f);

                    RemoveUnit(dead.UnitId);
                }
            }

            private void ProcessUnitAging(float delta)
            {
                foreach (var unit in Units)
                {
                    if (!unit.IsAlive) continue;

                    unit.ChangeAge(1);
                }
            }

            private void ProcessCaptureDecay()
            {
                var activeCaptures = tileManager.GetActiveCaptureTiles();

                if (activeCaptures.Count == 0) return;

                captureTilesBuffer.Clear();
                captureTilesBuffer.AddRange(activeCaptures);

                foreach (var pos in captureTilesBuffer)
                {
                    TileData tile = tileManager.GetTile(pos.x, pos.y);

                    if (string.IsNullOrEmpty(tile.CapturingCultureId)) continue;

                    Vector2 tileCenter = tileManager.GetTileCenter(pos.x, pos.y);

                    var unitsOnTile = UnitSpatialGrid.GetUnitsInRadius(tileCenter, mainConfig.TileSize * 0.5f);

                    bool hasCapturer = false;

                    foreach (var unit in unitsOnTile)
                    {
                        if (unit.IsAlive && unit.CultureId == tile.CapturingCultureId)
                        {
                            hasCapturer = true;
                            break;
                        }
                    }

                    if (!hasCapturer)
                    {
                        TileCaptureSystem.DecayCapture(tile, pos.x, pos.y, tileManager, mainConfig, CaptureDecayInterval);
                    }
                }
            }
        }
    }
}