using System;
using System.Runtime.InteropServices;

namespace ER.World.Grid
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TileData
    {
        public TerrainType Terrain;
        public BiomeType Biome;
        public byte MovementCost;
        public byte Elevation;

        public byte Moisture;
        public byte Fertility;
        public byte PopCount;
        public byte Flags;

        public ushort GeoRegionId;
        public ushort DeFactoPolityId;
        public ushort DeJurePolityId;
        public ushort ResourceReserve;

        public const byte FlagNone = 0;
        public const byte FlagHasRoad = 1 << 0;
        public const byte FlagHasRiver = 1 << 1;
        public const byte FlagIsFrozen = 1 << 2;
        public const byte FlagHasBuilding = 1 << 3;
        public const byte FlagIsOnFire = 1 << 4;

        public static TileData CreateDefault()
        {
            return new TileData
            {
                Terrain = TerrainType.Plains,
                Biome = BiomeType.TemperatePlains,
                MovementCost = 10,
                Elevation = 0,

                Moisture = 128,
                Fertility = 128,
                PopCount = 0,
                Flags = FlagNone,

                GeoRegionId = 0,
                DeFactoPolityId = 0,
                DeJurePolityId = 0,
                ResourceReserve = 1000
            };
        }

        public bool HasFlag(byte flag)
        {
            return (Flags & flag) != 0;
        }

        public void SetFlag(byte flag, bool value)
        {
            if (value)
            {
                Flags |= flag;
            }
            else
            {
                Flags &= (byte)~flag;
            }
        }
    }
}