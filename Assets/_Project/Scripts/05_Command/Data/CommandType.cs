namespace ER.World.Command
{
    public enum CommandType : ushort
    {
        MoveToPosition = 0,
        GatherResource = 1,
        TransferCargo = 2,
        ConstructBlueprint = 3,
        DigTrench = 4,
        AttackTarget = 5,
        SetWorkPriority = 6,
        MobilizeMilitia = 7
    }

    public enum CommandStatus : byte
    {
        QueuedInTransit = 0,
        Delivered = 1,
        Executing = 2,
        Completed = 3,
        Cancelled = 4,
        InterruptedLost = 5
    }

    public enum TransmissionMedium : byte
    {
        AcousticVoice = 0,
        RunnerCourier = 1,
        MountedMessenger = 2,
        OpticalHeliograph = 3,
        ElectricTelegraph = 4
    }
}