using System;
using ER.World.Grid;

namespace ER.World.Regions
{
    [Serializable]
    public struct GeoRegionData
    {
        public ushort RegionId;

        public bool IsOceanRegion;

        public int CenterTileX;

        public int CenterTileY;

        public int TotalTilesCount;

        public byte AverageElevation;

        public byte AverageMoisture;

        public BiomeType DominantBiome;

        public int MinTileX;

        public int MaxTileX;

        public int MinTileY;

        public int MaxTileY;


        public static GeoRegionData CreateDefault(ushort id, bool isOcean)
        {
            return new GeoRegionData
            {
                RegionId = id,
                IsOceanRegion = isOcean,
                CenterTileX = 0,
                CenterTileY = 0,
                TotalTilesCount = 0,
                AverageElevation = 0,
                AverageMoisture = 0,
                DominantBiome = BiomeType.TemperatePlains,
                MinTileX = int.MaxValue,
                MaxTileX = int.MinValue,
                MinTileY = int.MaxValue,
                MaxTileY = int.MinValue
            };
        }
    }
}