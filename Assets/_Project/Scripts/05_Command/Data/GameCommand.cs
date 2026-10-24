using System.Runtime.InteropServices;

namespace ER.World.Command
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GameCommand
    {
        public ulong CommandId;

        public CommandType Type;

        public ushort SourcePolityId;

        public ushort SourceAnchorId;

        public float TargetWorldX;

        public float TargetWorldY;

        public int TargetEntityId;

        public ushort PayloadId;

        public uint PayloadQuantity;

        public ulong IssuedTick;

        public ulong DeliveryTick;

        public CommandStatus Status;

        public TransmissionMedium Medium;


        public static GameCommand Create(
            ulong id,
            CommandType type,
            ushort polityId,
            ushort anchorId,
            float targetX,
            float targetY,
            ulong issuedTick,
            ulong deliveryTick,
            TransmissionMedium medium,
            int targetEntity = -1,
            ushort payloadId = 0,
            uint payloadQuantity = 0)
        {
            return new GameCommand
            {
                CommandId = id,
                Type = type,
                SourcePolityId = polityId,
                SourceAnchorId = anchorId,
                TargetWorldX = targetX,
                TargetWorldY = targetY,
                TargetEntityId = targetEntity,
                PayloadId = payloadId,
                PayloadQuantity = payloadQuantity,
                IssuedTick = issuedTick,
                DeliveryTick = deliveryTick,
                Status = CommandStatus.QueuedInTransit,
                Medium = medium
            };
        }
    }
}