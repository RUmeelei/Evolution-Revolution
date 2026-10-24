using System;
using System.Runtime.InteropServices;

namespace ER.World.Inventory
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct InventoryHeaderData
    {
        public int InventoryId;

        public float MaxWeightKg;

        public float MaxVolumeLiters;

        public float CurrentWeightKg;

        public float CurrentVolumeLiters;

        public byte ProtectionLevel;

        public byte Flags;

        public ushort SlotCount;


        public const byte FlagIsLocked = 1 << 0;

        public const byte FlagAllowOverflow = 1 << 1;


        public static InventoryHeaderData Create(
            int inventoryId,
            float maxWeight,
            float maxVolume,
            byte protection = 128)
        {
            return new InventoryHeaderData
            {
                InventoryId = inventoryId,
                MaxWeightKg = maxWeight,
                MaxVolumeLiters = maxVolume,
                CurrentWeightKg = 0.0f,
                CurrentVolumeLiters = 0.0f,
                ProtectionLevel = protection,
                Flags = 0,
                SlotCount = 0
            };
        }


        public bool CanAccommodate(float addedWeight, float addedVolume)
        {
            if ((Flags & FlagAllowOverflow) != 0)
            {
                return true;
            }

            return (CurrentWeightKg + addedWeight <= MaxWeightKg) &&
                   (CurrentVolumeLiters + addedVolume <= MaxVolumeLiters);
        }
    }
}