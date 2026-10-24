using System;
using System.Runtime.InteropServices;
using ER.World.Command;

namespace ER.World.Command.Communication
{
    public enum CommunicationNodeType : byte
    {
        None = 0,
        CampHearth = 1,
        Watchtower = 2,
        OpticalRelayTower = 3,
        TelegraphSwitchboard = 4,
        CentralHQStation = 5
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CommunicationNodeData
    {
        public ushort NodeId;

        public ushort PolityId;

        public ushort AnchorId;

        public CommunicationNodeType NodeType;

        public float WorldPositionX;

        public float WorldPositionY;

        public float TransmissionRange;

        public bool IsActive;


        public CommunicationNodeType Type => NodeType;

        public float WorldX => WorldPositionX;

        public float WorldY => WorldPositionY;


        public static CommunicationNodeData Create(
            ushort id,
            ushort polityId,
            ushort anchorId,
            CommunicationNodeType type,
            float worldX,
            float worldY,
            float range)
        {
            return new CommunicationNodeData
            {
                NodeId = id,
                PolityId = polityId,
                AnchorId = anchorId,
                NodeType = type,
                WorldPositionX = worldX,
                WorldPositionY = worldY,
                TransmissionRange = range,
                IsActive = true
            };
        }
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CommunicationLineData
    {
        public int LineId;

        public ushort PolityId;

        public ushort NodeAId;

        public ushort NodeBId;

        public TransmissionMedium Medium;

        public float Length;

        public bool IsActive;


        public bool IsFunctional => IsActive;


        public static CommunicationLineData Create(
            int id,
            ushort polityId,
            ushort nodeAId,
            ushort nodeBId,
            TransmissionMedium medium,
            float length)
        {
            return new CommunicationLineData
            {
                LineId = id,
                PolityId = polityId,
                NodeAId = nodeAId,
                NodeBId = nodeBId,
                Medium = medium,
                Length = length,
                IsActive = true
            };
        }
    }
}