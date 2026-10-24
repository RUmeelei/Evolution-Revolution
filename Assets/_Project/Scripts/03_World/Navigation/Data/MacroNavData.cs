using System;
using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace ER.World.Navigation
{
    public enum MacroNodeType : byte
    {
        RegionCenter = 0,
        MountainPass = 1,
        RiverFord = 2,
        CoastalGate = 3
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MacroNavNode
    {
        public int NodeId;

        public ushort GeoRegionIdA;

        public ushort GeoRegionIdB;

        public MacroNodeType NodeType;

        public float WorldPositionX;

        public float WorldPositionY;

        public float Elevation;

        public bool IsPassable;


        public static MacroNavNode Create(
            int id,
            ushort regionA,
            ushort regionB,
            MacroNodeType type,
            float x,
            float y,
            float elevation)
        {
            return new MacroNavNode
            {
                NodeId = id,
                GeoRegionIdA = regionA,
                GeoRegionIdB = regionB,
                NodeType = type,
                WorldPositionX = x,
                WorldPositionY = y,
                Elevation = elevation,
                IsPassable = true
            };
        }
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MacroNavEdge
    {
        public int EdgeId;

        public int FromNodeId;

        public int ToNodeId;

        public float PhysicalLengthMeters;

        public float TraversalWeight;

        public bool IsBlocked;


        public static MacroNavEdge Create(
            int edgeId,
            int from,
            int to,
            float length,
            float weight)
        {
            return new MacroNavEdge
            {
                EdgeId = edgeId,
                FromNodeId = from,
                ToNodeId = to,
                PhysicalLengthMeters = length,
                TraversalWeight = weight,
                IsBlocked = false
            };
        }
    }

    public struct MacroPathResult
    {
        public bool Success;

        public float TotalDistanceMeters;

        public float TotalTraversalCost;

        public int[] NodeIds;

        public float2[] Waypoints;
    }
}