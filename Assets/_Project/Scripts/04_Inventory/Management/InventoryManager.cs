using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using ER.Simulation.Time;

namespace ER.World.Inventory
{
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }


        [Header("Configuration")]
        [SerializeField] private ItemDatabaseConfig itemDatabase;

        [SerializeField] private SimulationClockManager clockManager;


        private NativeList<InventoryHeaderData> inventories;

        private NativeList<InventorySlotData> slots;

        private NativeParallelHashMap<ushort, ItemPhysicalProperties> itemPropertiesMap;

        private int nextInventoryId = 1;

        private bool isInitialized = false;


        public bool IsInitialized => isInitialized;

        public ItemDatabaseConfig ItemDatabase => itemDatabase;

        public NativeList<InventoryHeaderData> RawInventories => inventories;

        public NativeList<InventorySlotData> RawSlots => slots;


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
            if (clockManager == null)
            {
                clockManager = SimulationClockManager.Instance;
            }

            if (clockManager != null)
            {
                clockManager.OnDayPassed += HandleDayPassed;
            }

            if (itemDatabase != null)
            {
                itemDatabase.BuildLookup();

                RebuildItemPropertiesMap();
            }
        }


        private void OnDestroy()
        {
            if (clockManager != null)
            {
                clockManager.OnDayPassed -= HandleDayPassed;
            }

            DisposeStorage();
        }


        private void OnApplicationQuit()
        {
            DisposeStorage();
        }


        public void InitializeStorage()
        {
            if (isInitialized)
            {
                return;
            }

            inventories = new NativeList<InventoryHeaderData>(2048, Allocator.Persistent);

            slots = new NativeList<InventorySlotData>(8192, Allocator.Persistent);

            itemPropertiesMap = new NativeParallelHashMap<ushort, ItemPhysicalProperties>(256, Allocator.Persistent);

            isInitialized = true;
        }


        public void DisposeStorage()
        {
            if (itemPropertiesMap.IsCreated)
            {
                itemPropertiesMap.Dispose();
            }

            if (slots.IsCreated)
            {
                slots.Dispose();
            }

            if (inventories.IsCreated)
            {
                inventories.Dispose();
            }

            isInitialized = false;
        }


        public void RebuildItemPropertiesMap()
        {
            if (!itemPropertiesMap.IsCreated || itemDatabase == null)
            {
                return;
            }

            itemPropertiesMap.Clear();

            IReadOnlyList<ItemTypeConfig> types = itemDatabase.ItemTypes;

            for (int i = 0; i < types.Count; i++)
            {
                ItemTypeConfig config = types[i];

                ItemPhysicalProperties properties = new ItemPhysicalProperties
                {
                    MassPerUnitKg = config.MassPerUnitKg,
                    VolumePerUnitLiters = config.VolumePerUnitLiters,
                    MaxStackSize = config.MaxStackSize,
                    ShelfLifeTicks = config.ShelfLifeTicks,
                    BaseFlammability = config.BaseFlammability,
                    MoistureAbsorbRate = config.MoistureAbsorbRate
                };

                itemPropertiesMap.TryAdd(config.ItemTypeId, properties);
            }
        }


        private void HandleDayPassed(WorldTimeData timeData)
        {
            RunItemDegradation(120);
        }


        public void RunItemDegradation(byte ambientMoisture)
        {
            if (!isInitialized || slots.Length == 0)
            {
                return;
            }

            ulong currentTick = clockManager != null ? clockManager.CurrentTime.TotalTicks : 0;

            ItemDegradationJob job = new ItemDegradationJob
            {
                Inventories = inventories,
                Slots = slots,
                ItemProperties = itemPropertiesMap,
                CurrentTick = currentTick,
                AmbientMoisture = ambientMoisture
            };

            job.Run();
        }


        public int CreateInventory(float maxWeightKg, float maxVolumeLiters, byte protectionLevel = 128)
        {
            if (!isInitialized)
            {
                return 0;
            }

            int id = nextInventoryId++;

            InventoryHeaderData header = InventoryHeaderData.Create(id, maxWeightKg, maxVolumeLiters, protectionLevel);

            inventories.Add(header);

            return id;
        }


        public bool TryGetHeader(int inventoryId, out InventoryHeaderData header)
        {
            for (int i = 0; i < inventories.Length; i++)
            {
                if (inventories[i].InventoryId == inventoryId)
                {
                    header = inventories[i];

                    return true;
                }
            }

            header = default;

            return false;
        }


        public bool TrySetHeader(int inventoryId, InventoryHeaderData updatedHeader)
        {
            for (int i = 0; i < inventories.Length; i++)
            {
                if (inventories[i].InventoryId == inventoryId)
                {
                    inventories[i] = updatedHeader;

                    return true;
                }
            }

            return false;
        }


        public bool TryAddItem(int inventoryId, ushort itemTypeId, uint quantity, byte quality = 255, byte moisture = 0)
        {
            if (!isInitialized || quantity == 0)
            {
                return false;
            }

            if (!TryGetHeader(inventoryId, out InventoryHeaderData header))
            {
                return false;
            }

            float unitMass = 1.0f;

            float unitVolume = 1.0f;

            uint maxStack = 100;

            if (itemDatabase != null && itemDatabase.TryGetItemConfig(itemTypeId, out ItemTypeConfig config))
            {
                unitMass = config.MassPerUnitKg;

                unitVolume = config.VolumePerUnitLiters;

                maxStack = config.MaxStackSize;
            }
            else
            {
                switch (itemTypeId)
                {
                    case 1: unitMass = 2.2f; unitVolume = 3.0f; maxStack = 50; break;
                    case 2: unitMass = 1.8f; unitVolume = 1.2f; maxStack = 50; break;
                    case 3: unitMass = 0.08f; unitVolume = 0.1f; maxStack = 200; break;
                    case 4: unitMass = 0.65f; unitVolume = 0.8f; maxStack = 50; break;
                    case 5: unitMass = 1.2f; unitVolume = 1.5f; maxStack = 30; break;
                    case 6: unitMass = 0.8f; unitVolume = 1.2f; maxStack = 30; break;
                    case 7: unitMass = 1.5f; unitVolume = 3.0f; maxStack = 10; break;
                    case 8: unitMass = 0.5f; unitVolume = 0.6f; maxStack = 50; break;
                }
            }

            float addedWeight = unitMass * quantity;

            float addedVolume = unitVolume * quantity;

            if (!header.CanAccommodate(addedWeight, addedVolume))
            {
                return false;
            }

            ulong currentTick = clockManager != null ? clockManager.CurrentTime.TotalTicks : 0;

            uint remainingToAdd = quantity;

            for (int i = 0; i < slots.Length; i++)
            {
                InventorySlotData slot = slots[i];

                if (slot.InventoryId == inventoryId && slot.ItemTypeId == itemTypeId && slot.Quality == quality)
                {
                    uint spaceInSlot = maxStack > slot.Quantity ? maxStack - slot.Quantity : 0;

                    if (spaceInSlot > 0)
                    {
                        uint amountToMerge = Math.Min(spaceInSlot, remainingToAdd);

                        slot.Quantity += amountToMerge;

                        remainingToAdd -= amountToMerge;

                        slots[i] = slot;

                        if (remainingToAdd == 0)
                        {
                            break;
                        }
                    }
                }
            }

            while (remainingToAdd > 0)
            {
                uint stackAmount = Math.Min(remainingToAdd, maxStack);

                InventorySlotData newSlot = InventorySlotData.Create(
                    inventoryId,
                    itemTypeId,
                    stackAmount,
                    currentTick,
                    quality,
                    moisture);

                slots.Add(newSlot);

                header.SlotCount++;

                remainingToAdd -= stackAmount;
            }

            header.CurrentWeightKg += addedWeight;

            header.CurrentVolumeLiters += addedVolume;

            TrySetHeader(inventoryId, header);

            return true;
        }


        public bool TryRemoveItem(int inventoryId, ushort itemTypeId, uint quantity, out uint removedAmount)
        {
            removedAmount = 0;

            if (!isInitialized || quantity == 0)
            {
                return false;
            }

            if (!TryGetHeader(inventoryId, out InventoryHeaderData header))
            {
                return false;
            }

            uint remainingToRemove = quantity;

            for (int i = 0; i < slots.Length; i++)
            {
                InventorySlotData slot = slots[i];

                if (slot.InventoryId == inventoryId && slot.ItemTypeId == itemTypeId)
                {
                    uint toTake = Math.Min(slot.Quantity, remainingToRemove);

                    slot.Quantity -= toTake;

                    remainingToRemove -= toTake;

                    removedAmount += toTake;

                    if (slot.Quantity == 0)
                    {
                        slots.RemoveAtSwapBack(i);

                        header.SlotCount--;

                        i--;
                    }
                    else
                    {
                        slots[i] = slot;
                    }

                    if (remainingToRemove == 0)
                    {
                        break;
                    }
                }
            }

            if (removedAmount > 0)
            {
                float unitMass = 1.0f;

                float unitVol = 1.0f;

                if (itemPropertiesMap.IsCreated && itemPropertiesMap.TryGetValue(itemTypeId, out ItemPhysicalProperties props))
                {
                    unitMass = props.MassPerUnitKg;

                    unitVol = props.VolumePerUnitLiters;
                }
                else if (itemDatabase != null && itemDatabase.TryGetItemConfig(itemTypeId, out ItemTypeConfig cfg))
                {
                    unitMass = cfg.MassPerUnitKg;

                    unitVol = cfg.VolumePerUnitLiters;
                }

                header.CurrentWeightKg = Mathf.Max(0.0f, header.CurrentWeightKg - (removedAmount * unitMass));

                header.CurrentVolumeLiters = Mathf.Max(0.0f, header.CurrentVolumeLiters - (removedAmount * unitVol));

                TrySetHeader(inventoryId, header);

                return true;
            }

            return false;
        }


        public TransferResultStatus ExecuteTransfer(int sourceId, int targetId, ushort itemTypeId, uint quantity, out uint transferredAmount)
        {
            if (!isInitialized || quantity == 0)
            {
                transferredAmount = 0;

                return TransferResultStatus.FailedInvalidInventory;
            }

            if (targetId == 0)
            {
                if (TryRemoveItem(sourceId, itemTypeId, quantity, out transferredAmount))
                {
                    return TransferResultStatus.Success;
                }

                return TransferResultStatus.FailedInvalidInventory;
            }

            NativeArray<TransferRequest> singleRequestArray = new NativeArray<TransferRequest>(1, Allocator.TempJob);

            singleRequestArray[0] = TransferRequest.Create(sourceId, targetId, itemTypeId, quantity);

            ulong currentTick = clockManager != null ? clockManager.CurrentTime.TotalTicks : 0;

            TransferResourceJob job = new TransferResourceJob
            {
                Inventories = inventories,
                Slots = slots,
                ItemProperties = itemPropertiesMap,
                Requests = singleRequestArray,
                CurrentTick = currentTick
            };

            job.Run();

            TransferRequest result = singleRequestArray[0];

            transferredAmount = result.TransferredQuantity;

            TransferResultStatus status = result.Status;

            singleRequestArray.Dispose();

            return status;
        }


        public uint GetItemCount(int inventoryId, ushort itemTypeId)
        {
            if (!isInitialized)
            {
                return 0;
            }

            uint total = 0;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].InventoryId == inventoryId && slots[i].ItemTypeId == itemTypeId)
                {
                    total += slots[i].Quantity;
                }
            }

            return total;
        }
    }
}