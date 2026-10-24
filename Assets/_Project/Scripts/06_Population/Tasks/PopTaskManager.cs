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

            if (resourceManager != null) resourceManager.EnsureInitialized();

            if (populationManager != null) populationManager.EnsureInitialized();

            if (householdManager != null) householdManager.EnsureInitialized();

            if (herdManager != null) herdManager.EnsureInitialized();

            if (constructionManager != null) constructionManager.EnsureInitialized();

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


        private bool EvaluateAndAssignTask(ref PopData pop, ref PopTaskData task)
        {
            if (pop.CurrentCalories < 900.0f)
            {
                AssignHaulTask(ref pop, ref task);

                return true;
            }

            if (pop.BodyTemperature < 35.8f)
            {
                if (pop.BodyTemperature < 33.5f || pop.ActivityState == PopActivityState.Resting)
                {
                    AssignHaulTask(ref pop, ref task);

                    return true;
                }
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

            Vector2 hearthPos = ResolveHearthPosition(pop.HomeAnchorId);

            if (EvaluateConstructionTasks(ref pop, ref task, hearthPos))
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

                    Vector2 aPos = new Vector2(animal.WorldPositionX, animal.WorldPositionY);

                    float distFromHearth = Vector2.Distance(aPos, hearthPos);

                    if (distFromHearth > 105.0f) continue;

                    float dist = Vector2.Distance(popPos, aPos);

                    if (dist < 55.0f && dist < minAnimalDist)
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

                float dist = Vector2.Distance(popPos, new Vector2(node.WorldPositionX, node.WorldPositionY));

                if (dist < 40.0f && dist < minDistance)
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
                    task.PopId = pop.PopId;

                    task.TaskType = PopTaskType.BuildStructure;

                    task.TargetNodeId = bp.BlueprintId;

                    task.TargetWorldX = bp.WorldPositionX;

                    task.TargetWorldY = bp.WorldPositionY;

                    task.CurrentProgress = 0.0f;

                    task.RequiredWork = 10.0f;

                    pop.TargetPositionX = bp.WorldPositionX;

                    pop.TargetPositionY = bp.WorldPositionY;

                    pop.ActivityState = PopActivityState.Moving;

                    return true;
                }

                if (bp.State == BlueprintState.AwaitingMaterials && inventoryManager != null && polityManager != null)
                {
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
                            uint hasInHands = inventoryManager.GetItemCount(pop.PersonalInventoryId, neededItem);

                            if (hasInHands > 0)
                            {
                                task.PopId = pop.PopId;

                                task.TaskType = PopTaskType.HaulToConstruction;

                                task.TargetNodeId = bp.BlueprintId;

                                task.TargetWorldX = bp.WorldPositionX;

                                task.TargetWorldY = bp.WorldPositionY;

                                task.YieldItemTypeId = neededItem;

                                pop.TargetPositionX = bp.WorldPositionX;

                                pop.TargetPositionY = bp.WorldPositionY;

                                pop.ActivityState = PopActivityState.Moving;

                                return true;
                            }
                            else
                            {
                                inventoryManager.ExecuteTransfer(anchor.StorageInventoryId, pop.PersonalInventoryId, neededItem, 1, out uint got);

                                if (got > 0)
                                {
                                    task.PopId = pop.PopId;

                                    task.TaskType = PopTaskType.HaulToConstruction;

                                    task.TargetNodeId = bp.BlueprintId;

                                    task.TargetWorldX = bp.WorldPositionX;

                                    task.TargetWorldY = bp.WorldPositionY;

                                    task.YieldItemTypeId = neededItem;

                                    pop.TargetPositionX = bp.WorldPositionX;

                                    pop.TargetPositionY = bp.WorldPositionY;

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


        private void AssignHaulTask(ref PopData pop, ref PopTaskData task)
        {
            Vector2 targetPos = ResolveHearthPosition(pop.HomeAnchorId);

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
            if (task.TaskType == PopTaskType.HuntAnimal)
            {
                ExecuteHuntingTask(ref pop, ref task);

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


        private void ExecuteHaulToConstructionTask(ref PopData pop, ref PopTaskData task)
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

            if (constructionManager != null && inventoryManager != null)
            {
                uint count = inventoryManager.GetItemCount(pop.PersonalInventoryId, task.YieldItemTypeId);

                if (count > 0)
                {
                    inventoryManager.ExecuteTransfer(pop.PersonalInventoryId, 0, task.YieldItemTypeId, 1, out uint transferred);

                    if (transferred > 0)
                    {
                        constructionManager.DeliverMaterialToBlueprint(task.TargetNodeId, task.YieldItemTypeId, 1);
                    }
                }
            }

            task.TaskType = PopTaskType.None;

            pop.ActivityState = PopActivityState.Idle;
        }


        private void ExecuteBuildStructureTask(ref PopData pop, ref PopTaskData task)
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

            if (constructionManager != null)
            {
                bool isFinished = constructionManager.ApplyConstructionLabor(task.TargetNodeId, baseWorkSpeedPerTick * 0.4f);

                if (isFinished)
                {
                    task.TaskType = PopTaskType.None;

                    pop.ActivityState = PopActivityState.Idle;
                }
            }
        }


        private void ExecuteHuntingTask(ref PopData pop, ref PopTaskData task)
        {
            Vector2 popPos = new Vector2(pop.WorldPositionX, pop.WorldPositionY);

            Vector2 hearthPos = ResolveHearthPosition(pop.HomeAnchorId);

            float distFromHearth = Vector2.Distance(popPos, hearthPos);

            if (distFromHearth > 115.0f || pop.CurrentCalories < 850.0f || pop.BodyTemperature < 33.8f)
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

            ushort[] resourceTypes = { 1, 2, 3, 4, 5, 6, 7 };

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
                if (pop.BodyTemperature < 35.8f)
                {
                    pop.ActivityState = PopActivityState.Resting;
                }
                else
                {
                    task.TaskType = PopTaskType.None;

                    pop.ActivityState = PopActivityState.Idle;
                }
            }
            else
            {
                pop.ActivityState = PopActivityState.Resting;
            }
        }


        private Vector2 ResolveHearthPosition(ushort anchorId)
        {
            if (polityManager == null || gridManager == null) return Vector2.zero;

            var anchors = polityManager.Anchors;

            float tileSize = gridManager.MapConfig.TileWorldSize;

            for (int i = 0; i < anchors.Count; i++)
            {
                if (anchors[i].AnchorId == anchorId)
                {
                    return new Vector2((anchors[i].TileX + 0.5f) * tileSize, (anchors[i].TileY + 0.5f) * tileSize);
                }
            }

            return Vector2.zero;
        }
    }
}