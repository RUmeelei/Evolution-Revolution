using System;
using System.Runtime.InteropServices;

namespace ER.World.Inventory
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct InventorySlotData
    {
        public int InventoryId;

        public ushort ItemTypeId;

        public byte Moisture;

        public byte Quality;

        public uint Quantity;

        public ulong CreationTick;


        public static InventorySlotData Create(
            int inventoryId,
            ushort itemTypeId,
            uint quantity,
            ulong creationTick,
            byte quality = 255,
            byte moisture = 0)
        {
            return new InventorySlotData
            {
                InventoryId = inventoryId,
                ItemTypeId = itemTypeId,
                Moisture = moisture,
                Quality = quality,
                Quantity = quantity,
                CreationTick = creationTick
            };
        }
    }
}