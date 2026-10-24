using System;
using System.Runtime.InteropServices;

namespace ER.World.Resources
{
    public enum ResourceNodeType : byte
    {
        FallenTree = 0,
        FlintDeposit = 1,
        BerryBush = 2
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct WorldResourceNodeData
    {
        public int NodeId;

        public ResourceNodeType NodeType;

        public ushort ItemTypeId;

        public ushort RequiredToolTypeId;

        public float WorldPositionX;

        public float WorldPositionY;

        public uint RemainingYield;

        public float Hardness;

        public bool IsDepleted;


        public static WorldResourceNodeData Create(
            int id,
            ResourceNodeType type,
            ushort itemTypeId,
            ushort toolTypeId,
            float x,
            float y,
            uint yieldAmount,
            float hardness = 1.0f)
        {
            return new WorldResourceNodeData
            {
                NodeId = id,
                NodeType = type,
                ItemTypeId = itemTypeId,
                RequiredToolTypeId = toolTypeId,
                WorldPositionX = x,
                WorldPositionY = y,
                RemainingYield = yieldAmount,
                Hardness = hardness,
                IsDepleted = false
            };
        }
    }
}