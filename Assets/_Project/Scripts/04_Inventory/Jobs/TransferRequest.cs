using System.Runtime.InteropServices;

namespace ER.World.Inventory
{
    public enum TransferResultStatus : byte
    {
        Pending = 0,
        Success = 1,
        Partial = 2,
        FailedSourceEmpty = 3,
        FailedTargetFull = 4,
        FailedInvalidInventory = 5,
        FailedInvalidItem = 6
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TransferRequest
    {
        public int SourceInventoryId;

        public int TargetInventoryId;

        public ushort ItemTypeId;

        public uint RequestedQuantity;

        public uint TransferredQuantity;

        public TransferResultStatus Status;


        public static TransferRequest Create(int sourceId, int targetId, ushort itemTypeId, uint quantity)
        {
            return new TransferRequest
            {
                SourceInventoryId = sourceId,
                TargetInventoryId = targetId,
                ItemTypeId = itemTypeId,
                RequestedQuantity = quantity,
                TransferredQuantity = 0,
                Status = TransferResultStatus.Pending
            };
        }
    }
}