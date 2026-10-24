using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using ER.Simulation.Time;
using ER.World.Inventory;
using ER.World.Polity;
using ER.World.Grid;
using ER.World.Resources;
using ER.World.Housing;
using ER.World.Fauna;
using ER.World.Construction;
using ER.World.Culture;
using ER.World.Crafting;

namespace ER.World.Population.Tasks
{
    public class PopTaskManager : MonoBehaviour
    {
        public static PopTaskManager Instance { get; private set; }


        [Header("Work Balance Parameters")]
        [SerializeField] private float interactionDistance = 1.85f;

        [SerializeField] private float baseWorkSpeedPerTick = 6.0f;

        [SerializeField] private float maxCarryingWeightKg = 12.0f;

        [SerializeField] private byte hunterAttackPower = 28;


        [Header("Target Quota Limits")]
        [SerializeField] private int maxHuntersPerAnimal = 3;

        [SerializeField] private int maxWorkersPerResourceNode = 2;

        [SerializeField] private int maxBuildersPerBlueprint = 4;

        [SerializeField] private int maxHaulersPerBlueprint = 2;


        [Header("References")]
        [SerializeField] private SimulationClockManager clockManager;

        [SerializeField] private PopulationManager populationManager;

        [SerializeField] private WorldResourceManager resourceManager;

        [SerializeField] private InventoryManager inventoryManager;

        [SerializeField] private PolityRegionManager polityManager;

        [SerializeField] private WorldGridManager gridManager;

        [SerializeField] private HouseholdManager householdManager;

        [SerializeField] private AnimalHerdManager herdManager;

        [SerializeField] private ConstructionManager constructionManager;

        [SerializeField] private ShrineManager shrineManager;

        [SerializeField] private CraftingManager craftingManager;


        private NativeList<PopTaskData> popTasks;

        private bool isInitialized = false;


        public bool IsInitialized => isInitialized;


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

            if (populationManager == null) populationManager = PopulationManager.Instance;

            if (resourceManager == null) resourceManager = WorldResourceManager.Instance;

            if (inventoryManager == null) inventoryManager = InventoryManager.Instance;

            if (polityManager == null) polityManager = PolityRegionManager.Instance;

            if (gridManager == null) gridManager = WorldGridManager.Instance;

            if (householdManager == null) householdManager = HouseholdManager.Instance;

            if (herdManager == null) herdManager = AnimalHerdManager.Instance;

            if (constructionManager == null) constructionManager = ConstructionManager.Instance;

            if (shrineManager == null) shrineManager = ShrineManager.Instance;

            if (craftingManager == null) craftingManager = CraftingManager.Instance;

            if (resourceManager != null) resourceManager.EnsureInitialized();

            if (populationManager != null) populationManager.EnsureInitialized();

            if (householdManager != null) householdManager.EnsureInitialized();

            if (herdManager != null) herdManager.EnsureInitialized();

            if (constructionManager != null) constructionManager.EnsureInitialized();

            if (shrineManager != null) shrineManager.EnsureInitialized();

            if (craftingManager != null) craftingManager.EnsureInitialized();

            if (clockManager != null)
            {
                clockManager.OnTick += HandleWorkCycleTick;
            }

            isInitialized = true;
        }


        private void InitializeStorage()
        {
            if (!popTasks.IsCreated)
            {
                popTasks = new NativeList<PopTaskData>(4096, Allocator.Persistent);
            }
        }


        private void OnDestroy()
        {
            if (clockManager != null)
            {
                clockManager.OnTick -= HandleWorkCycleTick;
            }

            DisposeStorage();
        }


        private void OnApplicationQuit()
        {
            DisposeStorage();
        }


        private void DisposeStorage()
        {
            if (popTasks.IsCreated)
            {
                popTasks.Dispose();
            }

            isInitialized = false;
        }


        private void HandleWorkCycleTick(WorldTimeData timeData)
        {
            if (!isInitialized || populationManager == null)
            {
                return;
            }

            NativeArray<PopData> pops = populationManager.ActivePops;

            int popCount = populationManager.TotalPopulationCount;

            EnsureTaskArrayCapacity(popCount);

            float tileSize = gridManager.MapConfig.TileWorldSize;

            for (int i = 0; i < popCount; i++)
            {
                PopData pop = pops[i];

                if (!pop.IsAlive || pop.LifeStage < PopLifeStage.Child)
                {
                    continue;
                }

                PopTaskData task = popTasks[i];

                if (task.TaskType == PopTaskType.RestAtDwelling)
                {
                    ExecuteRestAtDwellingTask(ref pop, ref task);

                    popTasks[i] = task;

                    pops[i] = pop;

                    continue;
                }

                if (task.TaskType == PopTaskType.None)
                {
                    if (EvaluateAndAssignTask(ref pop, ref task))
                    {
                        popTasks[i] = task;

                        pops[i] = pop;
                    }

                    continue;
                }

                ExecuteActiveTask(ref pop, ref task, tileSize);

                popTasks[i] = task;

                pops[i] = pop;
            }
        }


        private void EnsureTaskArrayCapacity(int targetCount)
        {
            while (popTasks.Length < targetCount)
            {
                popTasks.Add(PopTaskData.CreateEmpty(popTasks.Length + 1));
            }
        }


        private int CountWorkersTargetingNode(PopTaskType taskType, int targetNodeId)
        {
            int count = 0;

            for (int i = 0; i < popTasks.Length; i++)
            {
                if (popTasks[i].TaskType == taskType && popTasks[i].TargetNodeId == targetNodeId)
                {
                    count++;
                }
            }

            return count;
        }


        private bool EvaluateAndAssignTask(ref PopData pop, ref PopTaskData task)
        {
            if (clockManager != null)
            {
                int hour = clockManager.CurrentTime.CurrentHour;

                if (hour >= 21 || hour < 5)
                {
                    Vector2 sleepTarget;

                    if (pop.DwellingId > 0 && householdManager != null && householdManager.TryGetDwellingPosition(pop.DwellingId, out float2 dPos))
                    {
                        sleepTarget = new Vector2(dPos.x + UnityEngine.Random.Range(-1.2f, 1.2f), dPos.y + UnityEngine.Random.Range(-1.2f, 1.2f));
                    }
                    else
                    {
                        sleepTarget = ResolveHearthPosition(pop.HomeAnchorId, pop.PopId);
                    }

                    task.PopId = pop.PopId;

                    task.TaskType = PopTaskType.RestAtDwelling;

                    task.TargetNodeId = pop.DwellingId;

                    task.TargetWorldX = sleepTarget.x;

                    task.TargetWorldY = sleepTarget.y;

                    task.CurrentProgress = 0.0f;

                    task.RequiredWork = 10.0f;

                    pop.TargetPositionX = sleepTarget.x;

                    pop.TargetPositionY = sleepTarget.y;

                    pop.ActivityState = PopActivityState.Moving;

                    return true;
                }
            }

            if (pop.CurrentCalories < 900.0f)
            {
                AssignHaulTask(ref pop, ref task);

                return true;
            }

            if (pop.BodyTemperature < 34.0f && pop.ActivityState != PopActivityState.Resting)
            {
                AssignHaulTask(ref pop, ref task);

                return true;
            }

            if (pop.ActivityState == PopActivityState.Resting && pop.BodyTemperature >= 36.4f)
            {
                pop.ActivityState = PopActivityState.Idle;
            }

            if (inventoryManager != null && inventoryManager.TryGetHeader(pop.PersonalInventoryId, out InventoryHeaderData header))
            {
                if (header.CurrentWeightKg >= maxCarryingWeightKg)
                {
                    AssignHaulTask(ref pop, ref task);

                    return true;
                }
            }

            Vector2 popPos = new Vector2(pop.WorldPositionX, pop.WorldPositionY);

            Vector2 hearthPos = ResolveHearthPosition(pop.HomeAnchorId, pop.PopId);

            if (EvaluateBurialTasks(ref pop, ref task))
            {
                return true;
            }

            if (EvaluateConstructionTasks(ref pop, ref task, hearthPos))
            {
                return true;
            }

            if (EvaluateCraftingTasks(ref pop, ref task, hearthPos))
            {
                return true;
            }

            if (pop.Gender == Gender.Male && pop.LifeStage >= PopLifeStage.Youth && herdManager != null && herdManager.IsInitialized)
            {
                var animals = herdManager.RawAnimals;

                int bestAnimalIdx = -1;

                float minAnimalDist = float.MaxValue;

                for (int a = 0; a < animals.Length; a++)
                {
                    AnimalData animal = animals[a];

                    if (!animal.IsActive) continue;

                    if (CountWorkersTargetingNode(PopTaskType.HuntAnimal, animal.AnimalId) >= maxHuntersPerAnimal)
                    {
                        continue;
                    }

                    Vector2 aPos = new Vector2(animal.WorldPositionX, animal.WorldPositionY);

                    float distFromHearth = Vector2.Distance(aPos, hearthPos);

                    if (distFromHearth > 110.0f) continue;

                    float dist = Vector2.Distance(popPos, aPos);

                    if (dist < 60.0f && dist < minAnimalDist)
                    {
                        minAnimalDist = dist;

                        bestAnimalIdx = a;
                    }
                }

                if (bestAnimalIdx >= 0)
                {
                    AnimalData targetAnimal = animals[bestAnimalIdx];

                    task.PopId = pop.PopId;

                    task.TaskType = PopTaskType.HuntAnimal;

                    task.TargetNodeId = targetAnimal.AnimalId;

                    task.TargetWorldX = targetAnimal.WorldPositionX;

                    task.TargetWorldY = targetAnimal.WorldPositionY;

                    task.CurrentProgress = 0.0f;

                    task.RequiredWork = 10.0f;

                    task.YieldItemTypeId = 4;

                    pop.TargetPositionX = targetAnimal.WorldPositionX;

                    pop.TargetPositionY = targetAnimal.WorldPositionY;

                    pop.ActivityState = PopActivityState.Moving;

                    return true;
                }
            }

            if (resourceManager == null || !resourceManager.IsInitialized)
            {
                return false;
            }

            var nodes = resourceManager.RawNodes;

            int bestNodeIndex = -1;

            float minDistance = float.MaxValue;

            for (int n = 0; n < nodes.Length; n++)
            {
                WorldResourceNodeData node = nodes[n];

                if (node.IsDepleted) continue;

                if (pop.LifeStage == PopLifeStage.Child && node.NodeType != ResourceNodeType.BerryBush)
                {
                    continue;
                }

                PopTaskType candidateTaskType = PopTaskType.ForageBerries;

                if (node.NodeType == ResourceNodeType.FallenTree) candidateTaskType = PopTaskType.HarvestWood;
                else if (node.NodeType == ResourceNodeType.FlintDeposit) candidateTaskType = PopTaskType.MineFlint;

                if (CountWorkersTargetingNode(candidateTaskType, node.NodeId) >= maxWorkersPerResourceNode)
                {
                    continue;
                }

                float dist = Vector2.Distance(popPos, new Vector2(node.WorldPositionX, node.WorldPositionY));

                if (dist < 45.0f && dist < minDistance)
                {
                    minDistance = dist;

                    bestNodeIndex = n;
                }
            }

            if (bestNodeIndex >= 0)
            {
                WorldResourceNodeData targetNode = nodes[bestNodeIndex];

                task.PopId = pop.PopId;

                task.TargetNodeId = targetNode.NodeId;

                task.TargetWorldX = targetNode.WorldPositionX;

                task.TargetWorldY = targetNode.WorldPositionY;

                task.CurrentProgress = 0.0f;

                task.YieldItemTypeId = targetNode.ItemTypeId;

                switch (targetNode.NodeType)
                {
                    case ResourceNodeType.BerryBush:
                        task.TaskType = PopTaskType.ForageBerries;
                        task.RequiredWork = targetNode.Hardness * 4.0f;
                        break;

                    case ResourceNodeType.FallenTree:
                        task.TaskType = PopTaskType.HarvestWood;
                        task.RequiredWork = targetNode.Hardness * 18.0f;
                        break;

                    case ResourceNodeType.FlintDeposit:
                        task.TaskType = PopTaskType.MineFlint;
                        task.RequiredWork = targetNode.Hardness * 16.0f;
                        break;
                }

                pop.TargetPositionX = targetNode.WorldPositionX;

                pop.TargetPositionY = targetNode.WorldPositionY;

                pop.ActivityState = PopActivityState.Moving;

                return true;
            }

            return false;
        }


        private bool EvaluateBurialTasks(ref PopData pop, ref PopTaskData task)
        {
            if (shrineManager == null || populationManager == null || pop.LifeStage < PopLifeStage.Youth)
            {
                return false;
            }

            NativeArray<PopData> pops = populationManager.ActivePops;

            int count = populationManager.TotalPopulationCount;

            Vector2 popPos = new Vector2(pop.WorldPositionX, pop.WorldPositionY);

            int deadPopIndex = -1;

            float minDeadDist = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                PopData candidate = pops[i];

                if (candidate.IsAlive || candidate.Health > 0 || candidate.IsBuried) continue;

                if (candidate.WorldPositionX == 0.0f && candidate.WorldPositionY == 0.0f) continue;

                if (CountWorkersTargetingNode(PopTaskType.BuryCorpse, i) >= 1)
                {
                    continue;
                }

                float dist = Vector2.Distance(popPos, new Vector2(candidate.WorldPositionX, candidate.WorldPositionY));

                if (dist < 50.0f && dist < minDeadDist)
                {
                    minDeadDist = dist;

                    deadPopIndex = i;
                }
            }

            if (deadPopIndex >= 0)
            {
                PopData deadBody = pops[deadPopIndex];

                task.PopId = pop.PopId;

                task.TaskType = PopTaskType.BuryCorpse;

                task.TargetNodeId = deadPopIndex;

                task.TargetWorldX = deadBody.WorldPositionX;

                task.TargetWorldY = deadBody.WorldPositionY;

                task.CurrentProgress = 0.0f;

                task.RequiredWork = 8.0f;

                pop.TargetPositionX = deadBody.WorldPositionX;

                pop.TargetPositionY = deadBody.WorldPositionY;

                pop.ActivityState = PopActivityState.Moving;

                return true;
            }

            return false;
        }


        private bool EvaluateConstructionTasks(ref PopData pop, ref PopTaskData task, Vector2 hearthPos)
        {
            if (constructionManager == null || !constructionManager.IsInitialized || pop.LifeStage < PopLifeStage.Youth)
            {
                return false;
            }

            var blueprints = constructionManager.RawBlueprints;

            for (int i = 0; i < blueprints.Length; i++)
            {
                BuildingBlueprintData bp = blueprints[i];

                if (!bp.IsActive || bp.HomeAnchorId != pop.HomeAnchorId) continue;

                if (bp.State == BlueprintState.UnderConstruction)
                {
                    if (CountWorkersTargetingNode(PopTaskType.BuildStructure, bp.BlueprintId) >= maxBuildersPerBlueprint)
                    {
                        continue;
                    }

                    float angle = (pop.PopId * 0.785398f);

                    float standRadius = 1.35f;

                    float targetX = bp.WorldPositionX + Mathf.Cos(angle) * standRadius;

                    float targetY = bp.WorldPositionY + Mathf.Sin(angle) * standRadius;

                    task.PopId = pop.PopId;

                    task.TaskType = PopTaskType.BuildStructure;

                    task.TargetNodeId = bp.BlueprintId;

                    task.TargetWorldX = targetX;

                    task.TargetWorldY = targetY;

                    task.CurrentProgress = 0.0f;

                    task.RequiredWork = 10.0f;

                    pop.TargetPositionX = targetX;

                    pop.TargetPositionY = targetY;

                    pop.ActivityState = PopActivityState.Moving;

                    return true;
                }

                if (bp.State == BlueprintState.AwaitingMaterials && inventoryManager != null && polityManager != null)
                {
                    if (CountWorkersTargetingNode(PopTaskType.HaulToConstruction, bp.BlueprintId) >= maxHaulersPerBlueprint)
                    {
                        continue;
                    }

                    if (polityManager.TryGetAnchor(pop.HomeAnchorId, out PolityAnchorData anchor) && anchor.StorageInventoryId > 0)
                    {
                        ushort neededItem = 0;

                        if (bp.DeliveredWood < bp.RequiredWood && inventoryManager.GetItemCount(anchor.StorageInventoryId, 1) > 0)
                        {
                            neededItem = 1;
                        }
                        else if (bp.DeliveredStone < bp.RequiredStone && inventoryManager.GetItemCount(anchor.StorageInventoryId, 2) > 0)
                        {
                            neededItem = 2;
                        }

                        if (neededItem > 0)
                        {
                            float angle = (pop.PopId * 0.785398f);

                            float targetX = bp.WorldPositionX + Mathf.Cos(angle) * 1.5f;

                            float targetY = bp.WorldPositionY + Mathf.Sin(angle) * 1.5f;

                            uint hasInHands = inventoryManager.GetItemCount(pop.PersonalInventoryId, neededItem);

                            if (hasInHands > 0)
                            {
                                task.PopId = pop.PopId;

                                task.TaskType = PopTaskType.HaulToConstruction;

                                task.TargetNodeId = bp.BlueprintId;

                                task.TargetWorldX = targetX;

                                task.TargetWorldY = targetY;

                                task.YieldItemTypeId = neededItem;

                                pop.TargetPositionX = targetX;

                                pop.TargetPositionY = targetY;

                                pop.ActivityState = PopActivityState.Moving;

                                return true;
                            }
                            else
                            {
                                inventoryManager.ExecuteTransfer(anchor.StorageInventoryId, pop.PersonalInventoryId, neededItem, 2, out uint got);

                                if (got > 0)
                                {
                                    task.PopId = pop.PopId;

                                    task.TaskType = PopTaskType.HaulToConstruction;

                                    task.TargetNodeId = bp.BlueprintId;

                                    task.TargetWorldX = targetX;

                                    task.TargetWorldY = targetY;

                                    task.YieldItemTypeId = neededItem;

                                    pop.TargetPositionX = targetX;

                                    pop.TargetPositionY = targetY;

                                    pop.ActivityState = PopActivityState.Moving;

                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }


        private bool EvaluateCraftingTasks(ref PopData pop, ref PopTaskData task, Vector2 hearthPos)
        {
            if (craftingManager == null || inventoryManager == null || polityManager == null || pop.LifeStage < PopLifeStage.Youth)
            {
                return false;
            }

            if (!polityManager.TryGetAnchor(pop.HomeAnchorId, out PolityAnchorData anchor) || anchor.StorageInventoryId <= 0)
            {
                return false;
            }

            int storageId = anchor.StorageInventoryId;

            uint rawMeat = inventoryManager.GetItemCount(storageId, 4);

            uint wood = inventoryManager.GetItemCount(storageId, 1);

            Vector2 popPos = new Vector2(pop.WorldPositionX, pop.WorldPositionY);

            if (rawMeat >= 2 && wood >= 1)
            {
                if (craftingManager.TryGetNearestAvailableStation(
                    new float2(popPos.x, popPos.y),
                    pop.HomeAnchorId,
                    CraftingStationType.DryingRack,
                    stationId => CountWorkersTargetingNode(PopTaskType.CraftAtStation, stationId) >= 1,
                    out int stationId,
                    out float2 rackPos))
                {
                    task.PopId = pop.PopId;

                    task.TaskType = PopTaskType.CraftAtStation;

                    task.TargetNodeId = stationId;

                    task.TargetWorldX = rackPos.x;

                    task.TargetWorldY = rackPos.y;

                    task.CurrentProgress = 0.0f;

                    task.RequiredWork = 12.0f;

                    task.YieldItemTypeId = 8;

                    pop.TargetPositionX = rackPos.x;

                    pop.TargetPositionY = rackPos.y;

                    pop.ActivityState = PopActivityState.Moving;

                    return true;
                }
            }

            uint rawHides = inventoryManager.GetItemCount(storageId, 5);

            if (rawHides >= 2)
            {
                if (craftingManager.TryGetNearestAvailableStation(
                    new float2(popPos.x, popPos.y),
                    pop.HomeAnchorId,
                    CraftingStationType.TanningRack,
                    stationId => CountWorkersTargetingNode(PopTaskType.CraftAtStation, stationId) >= 1,
                    out int stationId,
                    out float2 tanPos))
                {
                    task.PopId = pop.PopId;

                    task.TaskType = PopTaskType.CraftAtStation;

                    task.TargetNodeId = stationId;

                    task.TargetWorldX = tanPos.x;

                    task.TargetWorldY = tanPos.y;

                    task.CurrentProgress = 0.0f;

                    task.RequiredWork = 10.0f;

                    task.YieldItemTypeId = 6;

                    pop.TargetPositionX = tanPos.x;

                    pop.TargetPositionY = tanPos.y;

                    pop.ActivityState = PopActivityState.Moving;

                    return true;
                }
            }

            return false;
        }


        private void AssignHaulTask(ref PopData pop, ref PopTaskData task)
        {
            Vector2 targetPos = ResolveHearthPosition(pop.HomeAnchorId, pop.PopId);

            bool hasCargo = false;

            if (inventoryManager != null && inventoryManager.TryGetHeader(pop.PersonalInventoryId, out InventoryHeaderData header))
            {
                hasCargo = header.CurrentWeightKg > 0.1f;
            }

            if (!hasCargo && pop.BodyTemperature < 35.8f && householdManager != null)
            {
                if (householdManager.TryGetDwellingPosition(pop.DwellingId, out float2 dwellingPos))
                {
                    targetPos = new Vector2(dwellingPos.x, dwellingPos.y);
                }
            }

            task.PopId = pop.PopId;

            task.TaskType = PopTaskType.HaulToHearth;

            task.TargetNodeId = -1;

            task.TargetWorldX = targetPos.x;

            task.TargetWorldY = targetPos.y;

            task.CurrentProgress = 0.0f;

            task.RequiredWork = 1.0f;

            pop.TargetPositionX = targetPos.x;

            pop.TargetPositionY = targetPos.y;

            pop.ActivityState = PopActivityState.Moving;
        }


        private void ExecuteActiveTask(ref PopData pop, ref PopTaskData task, float tileSize)
        {
            if (task.TaskType == PopTaskType.RestAtDwelling)
            {
                ExecuteRestAtDwellingTask(ref pop, ref task);

                return;
            }

            if (task.TaskType == PopTaskType.BuryCorpse)
            {
                ExecuteBurialTask(ref pop, ref task);

                return;
            }

            if (task.TaskType == PopTaskType.HuntAnimal)
            {
                ExecuteHuntingTask(ref pop, ref task);

                return;
            }

            if (task.TaskType == PopTaskType.CraftAtStation)
            {
                ExecuteCraftAtStationTask(ref pop, ref task);

                return;
            }

            if (task.TaskType == PopTaskType.HaulToConstruction)
            {
                ExecuteHaulToConstructionTask(ref pop, ref task);

                return;
            }

            if (task.TaskType == PopTaskType.BuildStructure)
            {
                ExecuteBuildStructureTask(ref pop, ref task);

                return;
            }

            Vector2 currentPos = new Vector2(pop.WorldPositionX, pop.WorldPositionY);

            Vector2 targetPos = new Vector2(task.TargetWorldX, task.TargetWorldY);

            float distance = Vector2.Distance(currentPos, targetPos);

            if (distance > interactionDistance)
            {
                pop.TargetPositionX = task.TargetWorldX;

                pop.TargetPositionY = task.TargetWorldY;

                pop.ActivityState = PopActivityState.Moving;

                return;
            }

            pop.ActivityState = PopActivityState.Working;

            if (task.TaskType == PopTaskType.HaulToHearth)
            {
                UnloadCargoAtSettlement(ref pop, ref task);

                return;
            }

            if (!resourceManager.TryGetNode(task.TargetNodeId, out WorldResourceNodeData node, out int nodeIndex))
            {
                task.TaskType = PopTaskType.None;

                pop.ActivityState = PopActivityState.Idle;

                return;
            }

            float efficiency = 0.50f;

            if (inventoryManager != null)
            {
                efficiency = ToolDurabilitySystem.GetToolEfficiency(inventoryManager, pop.PersonalInventoryId, node.RequiredToolTypeId);
            }

            task.CurrentProgress += baseWorkSpeedPerTick * efficiency;

            if (task.CurrentProgress >= task.RequiredWork)
            {
                task.CurrentProgress = 0.0f;

                uint yieldToExtract = node.NodeType == ResourceNodeType.BerryBush ? Math.Min(node.RemainingYield, 4u) : Math.Min(node.RemainingYield, 2u);

                if (inventoryManager != null && inventoryManager.TryAddItem(pop.PersonalInventoryId, node.ItemTypeId, yieldToExtract))
                {
                    node.RemainingYield -= yieldToExtract;

                    if (node.RemainingYield == 0)
                    {
                        node.IsDepleted = true;
                    }

                    resourceManager.SetNode(nodeIndex, node);

                    if (node.RequiredToolTypeId > 0 && inventoryManager != null)
                    {
                        ToolDurabilitySystem.ApplyWear(inventoryManager, pop.PersonalInventoryId, node.RequiredToolTypeId, 1, out _);
                    }

                    if (node.IsDepleted || IsInventoryOverloaded(pop.PersonalInventoryId))
                    {
                        AssignHaulTask(ref pop, ref task);
                    }
                }
                else
                {
                    AssignHaulTask(ref pop, ref task);
                }
            }
        }


        private void ExecuteRestAtDwellingTask(ref PopData pop, ref PopTaskData task)
        {
            if (clockManager != null)
            {
                int hour = clockManager.CurrentTime.CurrentHour;

                if (hour >= 5 && hour < 21)
                {
                    task.TaskType = PopTaskType.None;

                    pop.ActivityState = PopActivityState.Idle;

                    return;
                }
            }

            Vector2 popPos = new Vector2(pop.WorldPositionX, pop.WorldPositionY);

            Vector2 targetPos = new Vector2(task.TargetWorldX, task.TargetWorldY);

            if (Vector2.Distance(popPos, targetPos) > interactionDistance)
            {
                pop.TargetPositionX = task.TargetWorldX;

                pop.TargetPositionY = task.TargetWorldY;

                pop.ActivityState = PopActivityState.Moving;

                return;
            }

            pop.ActivityState = PopActivityState.Resting;

            pop.VelocityX = 0.0f;

            pop.VelocityY = 0.0f;
        }


        private void ExecuteCraftAtStationTask(ref PopData pop, ref PopTaskData task)
        {
            Vector2 popPos = new Vector2(pop.WorldPositionX, pop.WorldPositionY);

            Vector2 targetPos = new Vector2(task.TargetWorldX, task.TargetWorldY);

            if (Vector2.Distance(popPos, targetPos) > interactionDistance)
            {
                pop.TargetPositionX = task.TargetWorldX;

                pop.TargetPositionY = task.TargetWorldY;

                pop.ActivityState = PopActivityState.Moving;

                return;
            }

            pop.ActivityState = PopActivityState.Working;

            task.CurrentProgress += baseWorkSpeedPerTick * 0.45f;

            if (task.CurrentProgress >= task.RequiredWork)
            {
                task.CurrentProgress = 0.0f;

                if (polityManager != null && polityManager.TryGetAnchor(pop.HomeAnchorId, out PolityAnchorData anchor) && inventoryManager != null)
                {
                    int storageId = anchor.StorageInventoryId;

                    if (task.YieldItemTypeId == 8)
                    {
                        uint meat = inventoryManager.GetItemCount(storageId, 4);

                        uint wood = inventoryManager.GetItemCount(storageId, 1);

                        if (meat >= 2 && wood >= 1)
                        {
                            inventoryManager.ExecuteTransfer(storageId, 0, 4, 2, out _);

                            inventoryManager.ExecuteTransfer(storageId, 0, 1, 1, out _);

                            inventoryManager.TryAddItem(storageId, 8, 2);

                            Debug.Log($"[PopTaskManager] Pop #{pop.PopId} smoked 2x Meat at Station #{task.TargetNodeId}!");
                        }
                    }
                    else if (task.YieldItemTypeId == 6)
                    {
                        uint hides = inventoryManager.GetItemCount(storageId, 5);

                        if (hides >= 2)
                        {
                            inventoryManager.ExecuteTransfer(storageId, 0, 5, 2, out _);

                            inventoryManager.TryAddItem(storageId, 6, 2);

                            Debug.Log($"[PopTaskManager] Pop #{pop.PopId} cured 2x Fur at Station #{task.TargetNodeId}!");
                        }
                    }
                }

                task.TaskType = PopTaskType.None;

                pop.ActivityState = PopActivityState.Idle;
            }
        }


        private void ExecuteHaulToConstructionTask(ref PopData pop, ref PopTaskData task)
        {
            if (constructionManager == null || !constructionManager.TryGetBlueprint(task.TargetNodeId, out BuildingBlueprintData bp, out _) || bp.State != BlueprintState.AwaitingMaterials)
            {
                if (inventoryManager != null && inventoryManager.GetItemCount(pop.PersonalInventoryId, task.YieldItemTypeId) > 0)
                {
                    AssignHaulTask(ref pop, ref task);
                }
                else
                {
                    task.TaskType = PopTaskType.None;

                    pop.ActivityState = PopActivityState.Idle;
                }

                return;
            }

            Vector2 popPos = new Vector2(pop.WorldPositionX, pop.WorldPositionY);

            Vector2 targetPos = new Vector2(task.TargetWorldX, task.TargetWorldY);

            if (Vector2.Distance(popPos, targetPos) > interactionDistance)
            {
                pop.TargetPositionX = task.TargetWorldX;

                pop.TargetPositionY = task.TargetWorldY;

                pop.ActivityState = PopActivityState.Moving;

                return;
            }

            if (inventoryManager != null)
            {
                uint count = inventoryManager.GetItemCount(pop.PersonalInventoryId, task.YieldItemTypeId);

                if (count > 0)
                {
                    uint needed = 0;

                    if (task.YieldItemTypeId == 1 && bp.DeliveredWood < bp.RequiredWood)
                    {
                        needed = bp.RequiredWood - bp.DeliveredWood;
                    }
                    else if (task.YieldItemTypeId == 2 && bp.DeliveredStone < bp.RequiredStone)
                    {
                        needed = bp.RequiredStone - bp.DeliveredStone;
                    }

                    if (needed > 0)
                    {
                        uint toDeliver = Math.Min(count, needed);

                        inventoryManager.ExecuteTransfer(pop.PersonalInventoryId, 0, task.YieldItemTypeId, toDeliver, out uint transferred);

                        if (transferred > 0)
                        {
                            constructionManager.DeliverMaterialToBlueprint(task.TargetNodeId, task.YieldItemTypeId, transferred);

                            Debug.Log($"[PopTaskManager] Pop {pop.PopId} delivered {transferred}x item {task.YieldItemTypeId} to blueprint {task.TargetNodeId}.");
                        }
                    }
                }

                if (inventoryManager.GetItemCount(pop.PersonalInventoryId, task.YieldItemTypeId) > 0)
                {
                    AssignHaulTask(ref pop, ref task);

                    return;
                }
            }

            task.TaskType = PopTaskType.None;

            pop.ActivityState = PopActivityState.Idle;
        }


        private void ExecuteBuildStructureTask(ref PopData pop, ref PopTaskData task)
        {
            if (constructionManager == null || !constructionManager.TryGetBlueprint(task.TargetNodeId, out BuildingBlueprintData bp, out _) || bp.State != BlueprintState.UnderConstruction)
            {
                task.TaskType = PopTaskType.None;

                pop.ActivityState = PopActivityState.Idle;

                return;
            }

            Vector2 popPos = new Vector2(pop.WorldPositionX, pop.WorldPositionY);

            Vector2 targetPos = new Vector2(task.TargetWorldX, task.TargetWorldY);

            if (Vector2.Distance(popPos, targetPos) > interactionDistance)
            {
                pop.TargetPositionX = task.TargetWorldX;

                pop.TargetPositionY = task.TargetWorldY;

                pop.ActivityState = PopActivityState.Moving;

                return;
            }

            pop.ActivityState = PopActivityState.Working;

            bool isFinished = constructionManager.ApplyConstructionLabor(task.TargetNodeId, baseWorkSpeedPerTick * 0.4f);

            if (isFinished)
            {
                task.TaskType = PopTaskType.None;

                pop.ActivityState = PopActivityState.Idle;
            }
        }


        private void ExecuteBurialTask(ref PopData pop, ref PopTaskData task)
        {
            NativeArray<PopData> pops = populationManager.ActivePops;

            int deadIndex = task.TargetNodeId;

            if (deadIndex < 0 || deadIndex >= pops.Length)
            {
                task.TaskType = PopTaskType.None;

                pop.ActivityState = PopActivityState.Idle;

                return;
            }

            PopData deadPop = pops[deadIndex];

            if (deadPop.IsAlive || deadPop.IsBuried)
            {
                task.TaskType = PopTaskType.None;

                pop.ActivityState = PopActivityState.Idle;

                return;
            }

            Vector2 popPos = new Vector2(pop.WorldPositionX, pop.WorldPositionY);

            Vector2 targetPos = new Vector2(task.TargetWorldX, task.TargetWorldY);

            if (Vector2.Distance(popPos, targetPos) > interactionDistance)
            {
                pop.TargetPositionX = task.TargetWorldX;

                pop.TargetPositionY = task.TargetWorldY;

                pop.ActivityState = PopActivityState.Moving;

                return;
            }

            if (shrineManager != null && shrineManager.TryGetNearestShrine(pop.HomeAnchorId, out AncestralShrineData shrine))
            {
                Vector2 shrinePos = new Vector2(shrine.WorldPositionX, shrine.WorldPositionY);

                float distToShrine = Vector2.Distance(popPos, shrinePos);

                if (distToShrine > interactionDistance * 1.5f)
                {
                    deadPop.WorldPositionX = pop.WorldPositionX;

                    deadPop.WorldPositionY = pop.WorldPositionY;

                    pops[deadIndex] = deadPop;

                    task.TargetWorldX = shrinePos.x;

                    task.TargetWorldY = shrinePos.y;

                    pop.TargetPositionX = shrinePos.x;

                    pop.TargetPositionY = shrinePos.y;

                    pop.ActivityState = PopActivityState.Moving;

                    return;
                }

                shrineManager.RecordBurial(pop.HomeAnchorId);

                deadPop.IsBuried = true;

                deadPop.WorldPositionX = 0.0f;

                deadPop.WorldPositionY = 0.0f;

                deadPop.Health = 0;

                pops[deadIndex] = deadPop;

                task.TaskType = PopTaskType.None;

                pop.ActivityState = PopActivityState.Idle;

                Debug.Log($"[PopTaskManager] Pop #{pop.PopId} buried deceased ancestor in Sacred Mound #{shrine.ShrineId}!");
            }
            else
            {
                deadPop.IsBuried = true;

                deadPop.WorldPositionX = 0.0f;

                deadPop.WorldPositionY = 0.0f;

                pops[deadIndex] = deadPop;

                task.TaskType = PopTaskType.None;

                pop.ActivityState = PopActivityState.Idle;
            }
        }


        private void ExecuteHuntingTask(ref PopData pop, ref PopTaskData task)
        {
            Vector2 popPos = new Vector2(pop.WorldPositionX, pop.WorldPositionY);

            Vector2 hearthPos = ResolveHearthPosition(pop.HomeAnchorId, pop.PopId);

            float distFromHearth = Vector2.Distance(popPos, hearthPos);

            if (distFromHearth > 120.0f || pop.CurrentCalories < 850.0f || pop.BodyTemperature < 33.8f)
            {
                task.TaskType = PopTaskType.None;

                pop.ActivityState = PopActivityState.Idle;

                AssignHaulTask(ref pop, ref task);

                return;
            }

            if (herdManager == null || !herdManager.TryGetAnimal(task.TargetNodeId, out AnimalData animal, out int animalIdx))
            {
                task.TaskType = PopTaskType.None;

                pop.ActivityState = PopActivityState.Idle;

                return;
            }

            Vector2 animalPos = new Vector2(animal.WorldPositionX, animal.WorldPositionY);

            float distance = Vector2.Distance(popPos, animalPos);

            if (animal.State != AnimalState.Carcass)
            {
                task.TargetWorldX = animal.WorldPositionX;

                task.TargetWorldY = animal.WorldPositionY;

                pop.TargetPositionX = animal.WorldPositionX;

                pop.TargetPositionY = animal.WorldPositionY;

                pop.ActivityState = PopActivityState.Moving;

                if (distance <= interactionDistance)
                {
                    pop.ActivityState = PopActivityState.Working;

                    if (animal.Health <= hunterAttackPower)
                    {
                        animal.Health = 0;

                        animal.State = AnimalState.Carcass;

                        animal.VelocityX = 0.0f;

                        animal.VelocityY = 0.0f;
                    }
                    else
                    {
                        animal.Health -= hunterAttackPower;

                        animal.State = AnimalState.Fleeing;
                    }

                    herdManager.SetAnimal(animalIdx, animal);
                }

                return;
            }

            if (distance > interactionDistance)
            {
                pop.TargetPositionX = animal.WorldPositionX;

                pop.TargetPositionY = animal.WorldPositionY;

                pop.ActivityState = PopActivityState.Moving;

                return;
            }

            pop.ActivityState = PopActivityState.Working;

            task.CurrentProgress += baseWorkSpeedPerTick * 0.85f;

            if (task.CurrentProgress >= task.RequiredWork)
            {
                task.CurrentProgress = 0.0f;

                uint meatToTake = Math.Min(animal.RemainingMeat, 8u);

                uint hidesToTake = Math.Min(animal.RemainingHides, 2u);

                if (meatToTake > 0 && inventoryManager != null)
                {
                    inventoryManager.TryAddItem(pop.PersonalInventoryId, 4, meatToTake);

                    animal.RemainingMeat -= meatToTake;
                }

                if (hidesToTake > 0 && inventoryManager != null)
                {
                    inventoryManager.TryAddItem(pop.PersonalInventoryId, 5, hidesToTake);

                    animal.RemainingHides -= hidesToTake;
                }

                if (animal.RemainingMeat == 0 && animal.RemainingHides == 0)
                {
                    animal.IsActive = false;
                }

                herdManager.SetAnimal(animalIdx, animal);

                AssignHaulTask(ref pop, ref task);
            }
        }


        private bool IsInventoryOverloaded(int inventoryId)
        {
            if (inventoryManager != null && inventoryManager.TryGetHeader(inventoryId, out InventoryHeaderData header))
            {
                return header.CurrentWeightKg >= maxCarryingWeightKg;
            }

            return false;
        }


        private void UnloadCargoAtSettlement(ref PopData pop, ref PopTaskData task)
        {
            if (inventoryManager == null || polityManager == null)
            {
                task.TaskType = PopTaskType.None;

                pop.ActivityState = PopActivityState.Idle;

                return;
            }

            if (!polityManager.TryGetAnchor(pop.HomeAnchorId, out PolityAnchorData anchor) || anchor.StorageInventoryId <= 0)
            {
                task.TaskType = PopTaskType.None;

                pop.ActivityState = PopActivityState.Idle;

                return;
            }

            int targetStorageId = anchor.StorageInventoryId;

            ushort[] resourceTypes = { 1, 2, 3, 4, 5, 6, 7, 8 };

            bool anyTransferSuccessful = false;

            for (int r = 0; r < resourceTypes.Length; r++)
            {
                ushort rId = resourceTypes[r];

                uint count = inventoryManager.GetItemCount(pop.PersonalInventoryId, rId);

                if (count > 0)
                {
                    inventoryManager.ExecuteTransfer(pop.PersonalInventoryId, targetStorageId, rId, count, out uint transferred);

                    if (transferred > 0)
                    {
                        anyTransferSuccessful = true;
                    }
                }
            }

            if (anyTransferSuccessful || !IsInventoryOverloaded(pop.PersonalInventoryId))
            {
                task.TaskType = PopTaskType.None;

                if (pop.BodyTemperature < 34.0f)
                {
                    pop.ActivityState = PopActivityState.Resting;
                }
                else
                {
                    if (householdManager != null && householdManager.TryGetDwellingPosition(pop.DwellingId, out float2 dPos))
                    {
                        float offsetX = UnityEngine.Random.Range(-2.2f, 2.2f);

                        float offsetY = UnityEngine.Random.Range(-2.2f, 2.2f);

                        pop.TargetPositionX = dPos.x + offsetX;

                        pop.TargetPositionY = dPos.y + offsetY;

                        pop.ActivityState = PopActivityState.Moving;
                    }
                    else
                    {
                        pop.ActivityState = PopActivityState.Idle;
                    }
                }
            }
            else
            {
                task.TaskType = PopTaskType.None;

                pop.ActivityState = PopActivityState.Idle;
            }
        }


        private Vector2 ResolveHearthPosition(ushort anchorId, int popId = 0)
        {
            if (polityManager == null || gridManager == null) return Vector2.zero;

            var anchors = polityManager.Anchors;

            float tileSize = gridManager.MapConfig.TileWorldSize;

            for (int i = 0; i < anchors.Count; i++)
            {
                if (anchors[i].AnchorId == anchorId)
                {
                    float centerX = (anchors[i].TileX + 0.5f) * tileSize;

                    float centerY = (anchors[i].TileY + 0.5f) * tileSize;

                    if (popId > 0)
                    {
                        float angle = (popId * 0.785398f);

                        return new Vector2(centerX + Mathf.Cos(angle) * 2.2f, centerY + Mathf.Sin(angle) * 2.2f);
                    }

                    return new Vector2(centerX, centerY);
                }
            }

            return Vector2.zero;
        }
    }
}