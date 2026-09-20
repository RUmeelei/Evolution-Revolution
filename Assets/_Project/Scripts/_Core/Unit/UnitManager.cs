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
            public static UnitManager Instance {get; private set;}

            [Header("Main")]
            public bool EnableLogging;

            [Header("Visual")]
            [SerializeField] private CultureSprites[] CultureSprites;

            private Dictionary<UnitEthnicity, CultureSprites> SpritesByCulture = new();

            private List<Unit> Units = new List<Unit>();
            private Dictionary<string, Unit> UnitsDictionary = new Dictionary<string, Unit>();

            private List<Unit> DeadUnits = new List<Unit>();

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

            private UnitSpatialGrid unitSpatialGrid;

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

            void Start()
            {
                foreach (var es in CultureSprites)
                {
                    SpritesByCulture[es.Ethnicity] = es;
                }
            }

            void OnDestroy()
            {
                simulationManager.OnTick -= ProcessUnitTick;
            }

            public void Initialize()
            {
                aiConfig = CoreManager.AIConfig;

                mainConfig = CoreManager.MainConfig;

                combatConfig = CoreManager.CombatConfig;

                simulationManager = CoreManager.SimulationManager;

                simulationManager.OnTick += ProcessUnitTick;

                dateManager = CoreManager.DateManager;

                dateManager.OnTier += ProcessUnitAging;

                cultureManager = CoreManager.CultureManager;

                tileManager = CoreManager.TileManager;

                unitVisualPoolManager = CoreManager.UnitVisualPoolManager;
                
                playerManager = CoreManager.PlayerManager;

                worldWidth = tileManager.WorldWidth * mainConfig.TileSize;
                worldHeight = tileManager.WorldHeight * mainConfig.TileSize;

                unitSpatialGrid = new UnitSpatialGrid(worldWidth, worldHeight, mainConfig.TileSize * 4f);

                var cultures = cultureManager.GetActiveCultures();

                foreach (var culture in cultures)
                {
                    Vector2 position = tileManager.GetTileCenter(culture.CultureCapital.x, culture.CultureCapital.y);

                    for (int i = 0; i < 5; i++)
                    {
                        CreateHuman($"Name {culture.CultureName} {i}", $"Last Name {culture.CultureName} {i}", 0, position, culture.CultureId);
                    }
                }
            }

            private CultureSprites GetCultureSprites(UnitEthnicity ethnicity)
            {
                SpritesByCulture.TryGetValue(ethnicity, out var sprites);

                return sprites ?? (CultureSprites.Length > 0 ? CultureSprites[0] : null);
            }

            private UnitSpriteSet GetSpritesForUnit(Unit unit)
            {
                var ethnicity = cultureManager.GetCulture(unit.CultureId)?.CultureEthnicity ?? UnitEthnicity.European;
                                
                SpritesByCulture.TryGetValue(ethnicity, out var sprites);

                return unit switch
                {
                    Human _ => sprites.Human,
                    _ => sprites.Human
                };
            }

            public Human CreateHuman(string firstName, string lastName, int age, Vector2 position, string cultureId = "CUL_NONE")
            {
                string id = $"HUM_{NextUnitId++:D4}";

                var human = new Human(id, firstName, lastName, age, position, cultureId);

                human.InitializeCombat(combatConfig.BaseAttackDamage, combatConfig.BaseAttackRange, combatConfig.BaseAttackCooldown);

                AddUnit(human);

                CreateUnitVisual(human);

                return human;
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
            }

            private void AddUnit(Unit unit)
            {
                if (unit == null) return;

                Units.Add(unit);

                UnitsDictionary[unit.UnitId] = unit;

                unitSpatialGrid.Register(unit);
            }

            public Unit GetUnit(string id)
            {
                UnitsDictionary.TryGetValue(id, out var unit);

                return unit;
            }

            public List<Unit> GetAllUnits() => Units;

            public List<Unit> GetUnitsAtPosition(Vector2 position, float radius = 1f, bool single = false)
            {
                return unitSpatialGrid.GetUnitsInRadius(position, radius, single);
            }

            public List<Unit> GetUnitsByCulture(string cultureId)
            {
                return Units.FindAll(u => u.CultureId == cultureId);
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

                unitSpatialGrid.Unregister(unit);
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
                        UnitAISystem.ProcessUnit(unit, aiConfig, unitSpatialGrid, cultureManager, playerManager, worldWidth, worldHeight);
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

                    // Movement

                    if (unit.HasTarget)
                    {
                        Vector2 oldPosition = unit.Position;

                        float healthPercent = unit.GetCurrentHealthPercent();

                        float healthFactor = combatConfig.MinHealthSpeedFactor + (1f - combatConfig.MinHealthSpeedFactor) * healthPercent;
                        
                        float adrenalineMultiplier = (healthPercent < combatConfig.AdrenalineHealthThreshold) ? combatConfig.AdrenalineSpeedMultiplier : 1f;

                        float effectiveMaxSpeed = unit.MaxSpeed * healthFactor * adrenalineMultiplier;

                        float effectiveAcceleration = mainConfig.UnitAccelerationRate * delta * healthFactor * adrenalineMultiplier;

                        unit.Speed = Mathf.Min(effectiveMaxSpeed, unit.Speed + effectiveAcceleration);

                        unit.MoveTowardsTarget(delta);

                        unitSpatialGrid.UpdatePosition(unit, oldPosition);
                    }
                    else if (unit.Speed > 0)
                    {
                        unit.Speed = Mathf.Max(0, unit.Speed - mainConfig.UnitDecelerationRate * delta);
                    }
                }

                foreach (var dead in DeadUnits)
                {
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
        }
    }
}