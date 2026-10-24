using System;
using UnityEngine;

namespace ER.World.Inventory
{
    public static class ToolDurabilitySystem
    {
        public static float GetToolEfficiency(InventoryManager inventoryManager, int inventoryId, ushort toolItemTypeId)
        {
            if (inventoryManager == null || !inventoryManager.IsInitialized)
            {
                return 0.35f;
            }

            var slots = inventoryManager.RawSlots;

            byte highestQuality = 0;

            bool found = false;

            for (int i = 0; i < slots.Length; i++)
            {
                InventorySlotData slot = slots[i];

                if (slot.InventoryId == inventoryId && slot.ItemTypeId == toolItemTypeId && slot.Quantity > 0)
                {
                    if (slot.Quality > highestQuality)
                    {
                        highestQuality = slot.Quality;

                        found = true;
                    }
                }
            }

            if (!found)
            {
                return 0.35f;
            }

            float qualityRatio = highestQuality / 255.0f;

            return 0.40f + 0.60f * qualityRatio;
        }


        public static bool ApplyWear(InventoryManager inventoryManager, int inventoryId, ushort toolItemTypeId, byte wearAmount, out bool toolBroke)
        {
            toolBroke = false;

            if (inventoryManager == null || !inventoryManager.IsInitialized || wearAmount == 0)
            {
                return false;
            }

            var slots = inventoryManager.RawSlots;

            int targetSlotIndex = -1;

            byte lowestQuality = byte.MaxValue;

            for (int i = 0; i < slots.Length; i++)
            {
                InventorySlotData slot = slots[i];

                if (slot.InventoryId == inventoryId && slot.ItemTypeId == toolItemTypeId && slot.Quantity > 0)
                {
                    if (slot.Quality <= lowestQuality)
                    {
                        lowestQuality = slot.Quality;

                        targetSlotIndex = i;
                    }
                }
            }

            if (targetSlotIndex < 0)
            {
                return false;
            }

            InventorySlotData targetSlot = slots[targetSlotIndex];

            if (targetSlot.Quality <= wearAmount)
            {
                toolBroke = true;

                if (!inventoryManager.ItemDatabase.TryGetItemConfig(toolItemTypeId, out ItemTypeConfig config))
                {
                    return false;
                }

                if (targetSlot.Quantity > 1)
                {
                    targetSlot.Quantity--;

                    targetSlot.Quality = 255;

                    slots[targetSlotIndex] = targetSlot;

                    if (inventoryManager.TryGetHeader(inventoryId, out InventoryHeaderData header))
                    {
                        header.CurrentWeightKg = Math.Max(0.0f, header.CurrentWeightKg - config.MassPerUnitKg);

                        header.CurrentVolumeLiters = Math.Max(0.0f, header.CurrentVolumeLiters - config.VolumePerUnitLiters);

                        inventoryManager.TrySetHeader(inventoryId, header);
                    }
                }
                else
                {
                    if (inventoryManager.TryGetHeader(inventoryId, out InventoryHeaderData header))
                    {
                        header.CurrentWeightKg = Math.Max(0.0f, header.CurrentWeightKg - config.MassPerUnitKg);

                        header.CurrentVolumeLiters = Math.Max(0.0f, header.CurrentVolumeLiters - config.VolumePerUnitLiters);

                        header.SlotCount--;

                        inventoryManager.TrySetHeader(inventoryId, header);
                    }

                    slots.RemoveAtSwapBack(targetSlotIndex);
                }
            }
            else
            {
                targetSlot.Quality -= wearAmount;

                slots[targetSlotIndex] = targetSlot;
            }

            return true;
        }
    }
}