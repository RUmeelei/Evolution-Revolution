using System;

namespace ER.World.Polity
{
    [Serializable]
    public struct PolityAnchorData
    {
        public ushort AnchorId;

        public PolityAnchorType AnchorType;

        public ushort OwnerPolityId;

        public ushort GeoRegionId;

        public int StorageInventoryId;

        public int TileX;

        public int TileY;

        public int InfluenceRadius;

        public bool IsActive;


        public static PolityAnchorData Create(
            ushort id,
            PolityAnchorType type,
            ushort polityId,
            ushort regionId,
            int storageInventoryId,
            int x,
            int y,
            int radius = 4)
        {
            return new PolityAnchorData
            {
                AnchorId = id,
                AnchorType = type,
                OwnerPolityId = polityId,
                GeoRegionId = regionId,
                StorageInventoryId = storageInventoryId,
                TileX = x,
                TileY = y,
                InfluenceRadius = radius,
                IsActive = true
            };
        }
    }
}