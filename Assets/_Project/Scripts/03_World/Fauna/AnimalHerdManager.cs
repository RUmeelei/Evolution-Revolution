using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using ER.Simulation.Time;
using ER.World.Grid;
using ER.World.Polity;
using ER.World.Population;

namespace ER.World.Fauna
{
    public class AnimalHerdManager : MonoBehaviour
    {
        public static AnimalHerdManager Instance { get; private set; }


        [Header("Herd Balance Parameters")]
        [SerializeField] private int herdCountPerSettlement = 3;

        [SerializeField] private int animalsPerHerd = 6;

        [SerializeField] private float minSpawnDistance = 35.0f;

        [SerializeField] private float maxSpawnDistance = 85.0f;

        [SerializeField] private float alertRadius = 14.0f;

        [SerializeField] private float walkSpeed = 0.65f;

        [SerializeField] private float fleeSpeed = 2.10f;

        [SerializeField] private float pastureTerritoryRadius = 55.0f;


        [Header("System References")]
        [SerializeField] private SimulationClockManager clockManager;

        [SerializeField] private PolityRegionManager polityManager;

        [SerializeField] private WorldGridManager gridManager;

        [SerializeField] private PopulationManager populationManager;


        [Header("Debug Visualizer")]
        [SerializeField] private bool drawFaunaGizmos = true;


        private NativeList<AnimalData> animals;

        private int nextAnimalId = 1;

        private bool isInitialized = false;


        public bool IsInitialized => isInitialized;

        public NativeList<AnimalData> RawAnimals => animals;


        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            InitializeStorage();
        }


        private void Start()
        {
            EnsureInitialized();
        }


        public void EnsureInitialized()
        {
            if (isInitialized)
            {
                return;
            }

            if (clockManager == null) clockManager = SimulationClockManager.Instance;

            if (polityManager == null) polityManager = PolityRegionManager.Instance;

            if (gridManager == null) gridManager = WorldGridManager.Instance;

            if (populationManager == null) populationManager = PopulationManager.Instance;

            if (polityManager != null)
            {
                polityManager.EnsureInitialized();
            }

            if (polityManager == null || !polityManager.IsInitialized || polityManager.Anchors.Count == 0)
            {
                return;
            }

            SpawnInitialHerds();

            if (clockManager != null)
            {
                clockManager.OnTick += HandleFaunaTick;
            }

            isInitialized = true;
        }


        private void InitializeStorage()
        {
            if (!animals.IsCreated)
            {
                animals = new NativeList<AnimalData>(512, Allocator.Persistent);
            }
        }


        private void OnDestroy()
        {
            if (clockManager != null)
            {
                clockManager.OnTick -= HandleFaunaTick;
            }

            DisposeStorage();
        }


        private void OnApplicationQuit()
        {
            DisposeStorage();
        }


        private void DisposeStorage()
        {
            if (animals.IsCreated)
            {
                animals.Dispose();
            }

            isInitialized = false;
        }


        private void SpawnInitialHerds()
        {
            var anchors = polityManager.Anchors;

            float tileSize = gridManager.MapConfig.TileWorldSize;

            for (int a = 0; a < anchors.Count; a++)
            {
                PolityAnchorData anchor = anchors[a];

                float centerX = (anchor.TileX + 0.5f) * tileSize;

                float centerY = (anchor.TileY + 0.5f) * tileSize;

                for (int h = 0; h < herdCountPerSettlement; h++)
                {
                    Vector2 herdOffset = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(minSpawnDistance, maxSpawnDistance);

                    float herdCenterX = centerX + herdOffset.x;

                    float herdCenterY = centerY + herdOffset.y;

                    AnimalType herdType = (h % 2 == 0) ? AnimalType.Reindeer : AnimalType.WildBoar;

                    for (int i = 0; i < animalsPerHerd; i++)
                    {
                        Vector2 scatter = UnityEngine.Random.insideUnitCircle * 7.0f;

                        int id = nextAnimalId++;

                        AnimalData animal = AnimalData.Create(id, anchor.GeoRegionId, herdType, herdCenterX + scatter.x, herdCenterY + scatter.y);

                        animals.Add(animal);
                    }
                }
            }

            Debug.Log($"[AnimalHerdManager] Successfully spawned {animals.Length} wild game animals tethered to valley pastures.");
        }


        private void HandleFaunaTick(WorldTimeData timeData)
        {
            if (!isInitialized || animals.Length == 0 || populationManager == null)
            {
                return;
            }

            NativeArray<PopData> pops = populationManager.ActivePops;

            int popCount = populationManager.TotalPopulationCount;

            for (int i = 0; i < animals.Length; i++)
            {
                AnimalData animal = animals[i];

                if (!animal.IsActive || animal.State == AnimalState.Carcass)
                {
                    continue;
                }

                float2 animalPos = new float2(animal.WorldPositionX, animal.WorldPositionY);

                float2 pastureCenter = new float2(animal.PastureCenterX, animal.PastureCenterY);

                float minPopDist = float.MaxValue;

                float2 nearestPopPos = float2.zero;

                for (int p = 0; p < popCount; p++)
                {
                    PopData pop = pops[p];

                    if (!pop.IsAlive) continue;

                    float dist = math.distance(animalPos, new float2(pop.WorldPositionX, pop.WorldPositionY));

                    if (dist < minPopDist)
                    {
                        minPopDist = dist;

                        nearestPopPos = new float2(pop.WorldPositionX, pop.WorldPositionY);
                    }
                }

                if (animal.State == AnimalState.Exhausted)
                {
                    animal.Stamina = math.min(100.0f, animal.Stamina + 4.0f);

                    if (animal.Stamina >= 60.0f)
                    {
                        animal.State = AnimalState.Grazing;
                    }
                }
                else if (minPopDist < alertRadius)
                {
                    animal.State = AnimalState.Fleeing;

                    animal.Stamina = math.max(0.0f, animal.Stamina - 5.5f);

                    if (animal.Stamina <= 0.0f)
                    {
                        animal.State = AnimalState.Exhausted;
                    }
                    else
                    {
                        float2 fleeDir = math.normalize(animalPos - nearestPopPos);

                        if (math.distance(animalPos, pastureCenter) > pastureTerritoryRadius)
                        {
                            float2 backToPastureDir = math.normalize(pastureCenter - animalPos);

                            fleeDir = math.normalize(fleeDir + backToPastureDir * 1.5f);
                        }

                        animal.TargetPositionX = animal.WorldPositionX + fleeDir.x * 16.0f;

                        animal.TargetPositionY = animal.WorldPositionY + fleeDir.y * 16.0f;
                    }
                }
                else
                {
                    animal.Stamina = math.min(100.0f, animal.Stamina + 2.0f);

                    if (animal.State == AnimalState.Fleeing && minPopDist > alertRadius * 1.5f)
                    {
                        animal.State = AnimalState.Grazing;
                    }
                }

                float currentSpeed = walkSpeed;

                if (animal.State == AnimalState.Fleeing)
                {
                    currentSpeed = fleeSpeed;
                }
                else if (animal.State == AnimalState.Exhausted)
                {
                    currentSpeed = walkSpeed * 0.45f;
                }

                float2 targetPos = new float2(animal.TargetPositionX, animal.TargetPositionY);

                float distToTarget = math.distance(animalPos, targetPos);

                if (distToTarget > 0.5f)
                {
                    float2 moveDir = math.normalize(targetPos - animalPos);

                    animal.WorldPositionX += moveDir.x * currentSpeed;

                    animal.WorldPositionY += moveDir.y * currentSpeed;

                    animal.VelocityX = moveDir.x * currentSpeed;

                    animal.VelocityY = moveDir.y * currentSpeed;
                }
                else if (animal.State == AnimalState.Grazing && UnityEngine.Random.value < 0.06f)
                {
                    Vector2 roam = UnityEngine.Random.insideUnitCircle * 8.0f;

                    animal.TargetPositionX = animal.PastureCenterX + roam.x;

                    animal.TargetPositionY = animal.PastureCenterY + roam.y;
                }

                animals[i] = animal;
            }
        }


        public bool TryGetAnimal(int animalId, out AnimalData animal, out int arrayIndex)
        {
            for (int i = 0; i < animals.Length; i++)
            {
                if (animals[i].AnimalId == animalId && animals[i].IsActive)
                {
                    animal = animals[i];

                    arrayIndex = i;

                    return true;
                }
            }

            animal = default;

            arrayIndex = -1;

            return false;
        }


        public void SetAnimal(int index, AnimalData animal)
        {
            if (index >= 0 && index < animals.Length)
            {
                animals[index] = animal;
            }
        }


        private void OnDrawGizmos()
        {
            if (!drawFaunaGizmos || !animals.IsCreated || animals.Length == 0)
            {
                return;
            }

            for (int i = 0; i < animals.Length; i++)
            {
                AnimalData a = animals[i];

                if (!a.IsActive) continue;

                Vector3 pos = new Vector3(a.WorldPositionX, a.WorldPositionY, -2.4f);

                if (a.State == AnimalState.Carcass)
                {
                    Gizmos.color = new Color(0.6f, 0.1f, 0.1f, 0.85f);

                    Gizmos.DrawCube(pos, new Vector3(1.3f, 0.7f, 0.4f));

                    continue;
                }

                if (a.State == AnimalState.Exhausted)
                {
                    Gizmos.color = Color.yellow;
                }
                else
                {
                    Gizmos.color = a.Type == AnimalType.Reindeer ? new Color(0.75f, 0.55f, 0.35f) : new Color(0.35f, 0.25f, 0.18f);
                }

                Gizmos.DrawSphere(pos, 0.95f);

                if (a.State == AnimalState.Fleeing)
                {
                    Gizmos.color = Color.red;

                    Gizmos.DrawLine(pos, pos + new Vector3(a.VelocityX, a.VelocityY, 0.0f) * 2.0f);
                }
            }
        }
    }
}