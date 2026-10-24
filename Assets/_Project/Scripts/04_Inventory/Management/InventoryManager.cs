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
            if (!isInitialized || itemDatabase == null || quantity == 0)
            {
                return false;
            }

            if (!itemDatabase.TryGetItemConfig(itemTypeId, out ItemTypeConfig config))
            {
                return false;
            }

            if (!TryGetHeader(inventoryId, out InventoryHeaderData header))
            {
                return false;
            }

            float addedWeight = config.MassPerUnitKg * quantity;

            float addedVolume = config.VolumePerUnitLiters * quantity;

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
                    uint spaceInSlot = config.MaxStackSize > slot.Quantity ? config.MaxStackSize - slot.Quantity : 0;

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
                uint stackAmount = Math.Min(remainingToAdd, config.MaxStackSize);

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


        public TransferResultStatus ExecuteTransfer(int sourceId, int targetId, ushort itemTypeId, uint quantity, out uint transferredAmount)
        {
            if (!isInitialized || quantity == 0)
            {
                transferredAmount = 0;

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