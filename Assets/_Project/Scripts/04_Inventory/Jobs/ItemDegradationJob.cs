using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace ER.World.Inventory
{
    [BurstCompile]
    public struct ItemDegradationJob : IJob
    {
        public NativeList<InventoryHeaderData> Inventories;

        public NativeList<InventorySlotData> Slots;

        [ReadOnly] public NativeParallelHashMap<ushort, ItemPhysicalProperties> ItemProperties;

        public ulong CurrentTick;

        public byte AmbientMoisture;


        public void Execute()
        {
            for (int i = 0; i < Slots.Length; i++)
            {
                InventorySlotData slot = Slots[i];

                if (!ItemProperties.TryGetValue(slot.ItemTypeId, out ItemPhysicalProperties properties))
                {
                    continue;
                }

                int headerIndex = FindHeaderIndex(slot.InventoryId);

                if (headerIndex < 0)
                {
                    continue;
                }

                InventoryHeaderData header = Inventories[headerIndex];

                ApplyMoistureDiffusion(ref slot, properties, header.ProtectionLevel);

                bool isSpoiled = ProcessQualityDegradation(ref slot, properties);

                if (isSpoiled)
                {
                    float lostMass = slot.Quantity * properties.MassPerUnitKg;

                    float lostVolume = slot.Quantity * properties.VolumePerUnitLiters;

                    header.CurrentWeightKg = Math.Max(0.0f, header.CurrentWeightKg - lostMass);

                    header.CurrentVolumeLiters = Math.Max(0.0f, header.CurrentVolumeLiters - lostVolume);

                    header.SlotCount--;

                    Inventories[headerIndex] = header;

                    Slots.RemoveAtSwapBack(i);

                    i--;
                }
                else
                {
                    Slots[i] = slot;
                }
            }
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


        private void ApplyMoistureDiffusion(ref InventorySlotData slot, ItemPhysicalProperties properties, byte protectionLevel)
        {
            if (properties.MoistureAbsorbRate <= 0.0f || AmbientMoisture <= slot.Moisture)
            {
                return;
            }

            float exposureFactor = (255 - protectionLevel) / 255.0f;

            if (exposureFactor <= 0.01f)
            {
                return;
            }

            int moistureDiff = AmbientMoisture - slot.Moisture;

            float delta = moistureDiff * properties.MoistureAbsorbRate * exposureFactor;

            slot.Moisture = (byte)Math.Min(255, slot.Moisture + (int)Math.Max(1.0f, delta));
        }


        private bool ProcessQualityDegradation(ref InventorySlotData slot, ItemPhysicalProperties properties)
        {
            if (properties.ShelfLifeTicks == 0)
            {
                return false;
            }

            ulong ageTicks = CurrentTick > slot.CreationTick ? CurrentTick - slot.CreationTick : 0;

            float ageRatio = (float)ageTicks / properties.ShelfLifeTicks;

            float moistureMultiplier = 1.0f + (slot.Moisture / 255.0f) * 2.0f;

            int qualityLoss = (int)(ageRatio * 255.0f * moistureMultiplier);

            int currentCalculatedQuality = 255 - qualityLoss;

            if (currentCalculatedQuality <= 0)
            {
                slot.Quality = 0;

                return true;
            }

            slot.Quality = (byte)currentCalculatedQuality;

            return false;
        }
    }
}