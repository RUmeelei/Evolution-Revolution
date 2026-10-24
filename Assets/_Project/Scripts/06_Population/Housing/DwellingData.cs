using System;
using System.Runtime.InteropServices;

namespace ER.World.Housing
{
    public enum DwellingType : byte
    {
        None = 0,
        PrimitiveLeanTo = 1,
        DugoutShelter = 2,
        LogCabin = 3
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DwellingData
    {
        public int DwellingId;

        public ushort PolityId;

        public ushort HomeAnchorId;

        public DwellingType Type;

        public float WorldPositionX;

        public float WorldPositionY;

        public byte Capacity;

        public byte OccupantCount;

        public float ThermalBonus;

        public bool IsActive;


        public static DwellingData Create(
            int id,
            ushort polityId,
            ushort anchorId,
            DwellingType type,
            float x,
            float y,
            byte capacity = 5,
            float thermalBonus = 18.0f)
        {
            return new DwellingData
            {
                DwellingId = id,
                PolityId = polityId,
                HomeAnchorId = anchorId,
                Type = type,
                WorldPositionX = x,
                WorldPositionY = y,
                Capacity = capacity,
                OccupantCount = 0,
                ThermalBonus = thermalBonus,
                IsActive = true
            };
        }
    }
}