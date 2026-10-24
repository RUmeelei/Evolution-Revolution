using System.Runtime.InteropServices;

namespace ER.World.Inventory
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ItemPhysicalProperties
    {
        public float MassPerUnitKg;

        public float VolumePerUnitLiters;

        public uint MaxStackSize;

        public ulong ShelfLifeTicks;

        public float MoistureAbsorbRate;

        public byte BaseFlammability;
    }
}