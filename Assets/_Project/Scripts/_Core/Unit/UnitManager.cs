using UnityEngine;
using System.Collections.Generic;

namespace ER
{
    namespace Unit
    {
        using Core;
        using Simulation;
        using Culture;

        public class UnitManager : MonoBehaviour
        {
            public static UnitManager Instance { get; private set; }

            [Header("Main")]
            public bool EnableLogging;

            [Header("Visual")]
            [SerializeField] private GameObject UnitVisualPrefab;
            [SerializeField] private CultureSprites[] CultureSprites;

            private Dictionary<UnitEthnicity, CultureSprites> SpritesByCulture = new();

            private List<Unit> Units = new List<Unit>();
            private Dictionary<string, Unit> UnitsDictionary = new Dictionary<string, Unit>();

            private int NextUnitId = 1;

            private SimulationManager simulationManager;

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

                simulationManager = CoreManager.SimulationManager;

                simulationManager.OnTick += ProcessUnitTick;
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
            }
            
            public void UpdateUnits(float delta)
            {
                foreach (var unit in Units)
                {
                    unit.Update(delta);
                }
            }

            private void ProcessUnitTick(float delta)
            {
                foreach (var unit in Units)
                {
                    unit.Update(delta);

                    if (!unit.HasTarget) unit.SetTarget(new Vector2(Random.Range(-2f, 2f), Random.Range(-2f, 2f)));
                }
            }
        }
    }
}