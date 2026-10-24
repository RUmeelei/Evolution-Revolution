using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace ER.World.Inventory
{
    [BurstCompile]
    public struct TransferResourceJob : IJob
    {
        public NativeList<InventoryHeaderData> Inventories;

        public NativeList<InventorySlotData> Slots;

        [ReadOnly] public NativeParallelHashMap<ushort, ItemPhysicalProperties> ItemProperties;

        public NativeArray<TransferRequest> Requests;

        public ulong CurrentTick;


        public void Execute()
        {
            for (int r = 0; r < Requests.Length; r++)
            {
                TransferRequest request = Requests[r];

                ProcessTransfer(ref request);

                Requests[r] = request;
            }
        }


        private void ProcessTransfer(ref TransferRequest request)
        {
            if (request.SourceInventoryId == request.TargetInventoryId || request.RequestedQuantity == 0)
            {
                request.Status = TransferResultStatus.FailedInvalidInventory;
                return;
            }

            if (!ItemProperties.TryGetValue(request.ItemTypeId, out ItemPhysicalProperties properties))
            {
                request.Status = TransferResultStatus.FailedInvalidItem;
                return;
            }

            int sourceHeaderIndex = FindHeaderIndex(request.SourceInventoryId);

            int targetHeaderIndex = FindHeaderIndex(request.TargetInventoryId);

            if (sourceHeaderIndex < 0 || targetHeaderIndex < 0)
            {
                request.Status = TransferResultStatus.FailedInvalidInventory;
                return;
            }

            InventoryHeaderData sourceHeader = Inventories[sourceHeaderIndex];

            InventoryHeaderData targetHeader = Inventories[targetHeaderIndex];

            uint sourceAvailable = CalculateAvailableQuantity(request.SourceInventoryId, request.ItemTypeId);

            if (sourceAvailable == 0)
            {
                request.Status = TransferResultStatus.FailedSourceEmpty;
                return;
            }

            uint maxAffordableByWeight = uint.MaxValue;

            if (properties.MassPerUnitKg > 0.0f)
            {
                float remainingWeight = targetHeader.MaxWeightKg - targetHeader.CurrentWeightKg;

                if (remainingWeight <= 0.0f)
                {
                    request.Status = TransferResultStatus.FailedTargetFull;
                    return;
                }

                maxAffordableByWeight = (uint)(remainingWeight / properties.MassPerUnitKg);
            }

            uint maxAffordableByVolume = uint.MaxValue;

            if (properties.VolumePerUnitLiters > 0.0f)
            {
                float remainingVolume = targetHeader.MaxVolumeLiters - targetHeader.CurrentVolumeLiters;

                if (remainingVolume <= 0.0f)
                {
                    request.Status = TransferResultStatus.FailedTargetFull;
                    return;
                }

                maxAffordableByVolume = (uint)(remainingVolume / properties.VolumePerUnitLiters);
            }

            uint allowedByTarget = Math.Min(maxAffordableByWeight, maxAffordableByVolume);

            if (allowedByTarget == 0)
            {
                request.Status = TransferResultStatus.FailedTargetFull;
                return;
            }

            uint toTransfer = Math.Min(request.RequestedQuantity, Math.Min(sourceAvailable, allowedByTarget));

            if (toTransfer == 0)
            {
                request.Status = TransferResultStatus.FailedTargetFull;
                return;
            }

            DeductFromSource(request.SourceInventoryId, request.ItemTypeId, toTransfer, ref sourceHeader);

            AddToTarget(request.TargetInventoryId, request.ItemTypeId, toTransfer, properties, ref targetHeader);

            float totalMass = toTransfer * properties.MassPerUnitKg;

            float totalVolume = toTransfer * properties.VolumePerUnitLiters;

            sourceHeader.CurrentWeightKg -= totalMass;

            sourceHeader.CurrentVolumeLiters -= totalVolume;

            targetHeader.CurrentWeightKg += totalMass;

            targetHeader.CurrentVolumeLiters += totalVolume;

            Inventories[sourceHeaderIndex] = sourceHeader;

            Inventories[targetHeaderIndex] = targetHeader;

            request.TransferredQuantity = toTransfer;

            request.Status = toTransfer == request.RequestedQuantity ? TransferResultStatus.Success : TransferResultStatus.Partial;
        }


        private int FindHeaderIndex(int inventoryId)
        {
            for (int i = 0; i < Inventories.Length; i++)
            {
                if (Inventories[i].InventoryId == inventoryId)
                {
                    return i;
                }
            }

            return -1;
        }


        private uint CalculateAvailableQuantity(int inventoryId, ushort itemTypeId)
        {
            uint total = 0;

            for (int i = 0; i < Slots.Length; i++)
            {
                if (Slots[i].InventoryId == inventoryId && Slots[i].ItemTypeId == itemTypeId)
                {
                    total += Slots[i].Quantity;
                }
            }

            return total;
        }


        private void DeductFromSource(int inventoryId, ushort itemTypeId, uint quantityToDeduct, ref InventoryHeaderData header)
        {
            uint remaining = quantityToDeduct;

            for (int i = 0; i < Slots.Length && remaining > 0; i++)
            {
                InventorySlotData slot = Slots[i];

                if (slot.InventoryId == inventoryId && slot.ItemTypeId == itemTypeId)
                {
                    if (slot.Quantity <= remaining)
                    {
                        remaining -= slot.Quantity;

                        Slots.RemoveAtSwapBack(i);

                        header.SlotCount--;

                        i--;
                    }
                    else
                    {
                        slot.Quantity -= remaining;

                        remaining = 0;

                        Slots[i] = slot;
                    }
                }
            }
        }


        private void AddToTarget(int inventoryId, ushort itemTypeId, uint quantityToAdd, ItemPhysicalProperties properties, ref InventoryHeaderData header)
        {
            uint remaining = quantityToAdd;

            for (int i = 0; i < Slots.Length && remaining > 0; i++)
            {
                InventorySlotData slot = Slots[i];

                if (slot.InventoryId == inventoryId && slot.ItemTypeId == itemTypeId)
                {
                    uint availableInStack = properties.MaxStackSize > slot.Quantity ? properties.MaxStackSize - slot.Quantity : 0;

                    if (availableInStack > 0)
                    {
                        uint amount = Math.Min(availableInStack, remaining);

                        slot.Quantity += amount;

                        remaining -= amount;

                        Slots[i] = slot;
                    }
                }
            }

            while (remaining > 0)
            {
                uint stackAmount = Math.Min(remaining, properties.MaxStackSize);

                InventorySlotData newSlot = InventorySlotData.Create(
                    inventoryId,
                    itemTypeId,
                    stackAmount,
                    CurrentTick);

                Slots.Add(newSlot);

                header.SlotCount++;

                remaining -= stackAmount;
            }
        }
    }
}