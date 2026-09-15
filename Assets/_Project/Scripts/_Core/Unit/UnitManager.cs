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
            [SerializeField] private GameObject UnitVisualPrefab;
            [SerializeField] private CultureSprites[] CultureSprites;

            private Dictionary<UnitEthnicity, CultureSprites> SpritesByCulture = new();

            private List<Unit> Units = new List<Unit>();
            private Dictionary<string, Unit> UnitsDictionary = new Dictionary<string, Unit>();

            private int NextUnitId = 1;

            private AIConfig aiConfig;
            private MainConfig mainConfig;

            private SimulationManager simulationManager;
            private DateManager dateManager;
            private CultureManager cultureManager;
            private TileManager tileManager;

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

                simulationManager = CoreManager.SimulationManager;

                simulationManager.OnTick += ProcessUnitTick;

                dateManager = CoreManager.DateManager;

                dateManager.OnTier += ProcessUnitAging;

                cultureManager = CoreManager.CultureManager;

                tileManager = CoreManager.TileManager;

                var cultures = cultureManager.GetActiveCultures();

                foreach (var culture in cultures)
                {
                    Vector2 position = tileManager.GetTileCenter(culture.CultureCapital.x, culture.CultureCapital.y);

                    CreateHuman($"Name {culture.CultureName}", $"Last Name {culture.CultureName}", 0, position, culture.CultureId);
                }
            }

            private CultureSprites GetCultureSprites(UnitEthnicity ethnicity)
            {
                SpritesByCulture.TryGetValue(ethnicity, out var sprites);

                return sprites ?? (CultureSprites.Length > 0 ? CultureSprites[0] : null);
            }

            private UnitSpriteSet GetSpritesForUnit(Unit unit)
            {
                UnitEthnicity cultureEthnicity = CultureCreationData.CultureEthnicity;

                SpritesByCulture.TryGetValue(cultureEthnicity, out var sprites);

                if (sprites == null) return null;

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

                AddUnit(human);

                CreateUnitVisual(human);

                return human;
            }
            
            private void CreateUnitVisual(Unit unit)
            {
                if (UnitVisualPrefab == null) return;

                var sprites = GetSpritesForUnit(unit);

                if (sprites == null) return;

                GameObject viewGO = Instantiate(UnitVisualPrefab);
                
                viewGO.name = $"{unit.UnitName}_View";

                var view = viewGO.GetComponent<UnitVisual>();

                view.Initialize(unit, sprites);

                unit.UnitVisual = view;
            }

            private void AddUnit(Unit unit)
            {
                if (unit == null) return;

                Units.Add(unit);

                UnitsDictionary[unit.UnitId] = unit;
            }

            public Unit GetUnit(string id)
            {
                UnitsDictionary.TryGetValue(id, out var unit);

                return unit;
            }

            public List<Unit> GetAllUnits() => Units;

            public List<Unit> GetUnitsAtPosition(Vector2 position, float radius = 1f)
            {
                return Units.FindAll(u => Vector2.Distance(u.Position, position) <= radius);
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

                if (unit.UnitVisual != null) Destroy(unit.UnitVisual.gameObject);
            }

            private void ProcessUnitTick(float delta)
            {
                List<Unit> deadUnits = new List<Unit>();

                foreach (var unit in Units)
                {
                    if (!unit.IsAlive)
                    {
                        deadUnits.Add(unit);

                        continue;
                    }

                    var player = cultureManager.GetPlayerForCulture(unit.CultureId);

                    unit.Update(delta);

                    // Movement

                    if (unit.HasTarget)
                    {
                        unit.Speed = Mathf.Min(unit.MaxSpeed * unit.GetCurrentHealthPercent(), unit.Speed + (mainConfig.UnitAccelerationRate * delta * unit.GetCurrentHealthPercent()));

                        unit.MoveTowardsTarget(delta);
                    }
                    else if (unit.Speed > 0)
                    {
                        unit.Speed = Mathf.Max(0, unit.Speed - mainConfig.UnitDecelerationRate * delta);
                    }

                    // Targeting
                    if (aiConfig.IsAIEnabled)
                    {
                        if (!unit.HasTarget && (player == null || player.PlayerAI) && Random.value > 0.9f)
                        {
                            Vector2 target = unit.Position + new Vector2(Random.Range(-2f, 2f), Random.Range(-2f, 2f));

                            float maxWidth = tileManager.WorldWidth * mainConfig.TileSize;
                            float maxHeight = tileManager.WorldHeight * mainConfig.TileSize;

                            target.x = Mathf.Clamp(target.x, 0, maxWidth);
                            target.y = Mathf.Clamp(target.y, 0, maxHeight);

                            unit.SetTarget(target);
                        }
                    }

                    // Debug

                    // if (Random.value > 0.95f)
                    // {
                    //     unit.TakeDamage(5f);
                    // }
                }

                foreach (var dead in deadUnits)
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