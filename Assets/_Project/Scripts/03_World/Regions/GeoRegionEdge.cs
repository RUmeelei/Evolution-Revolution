using System;

namespace ER.World.Regions
{
    [Serializable]
    public struct GeoRegionEdge
    {
        public ushort RegionA;

        public ushort RegionB;

        public int ContactTilesCount;

        public int PassTileXA;

        public int PassTileYA;

        public int PassTileXB;

        public int PassTileYB;

        public byte BestPassElevation;

        public int MinTransitCost;

        public bool IsOceanConnection;


        public ushort GetOppositeRegion(ushort regionId)
        {
            if (regionId == RegionA)
            {
                return RegionB;
            }

            if (regionId == RegionB)
            {
                return RegionA;
            }

            return 0;
        }
    }
}