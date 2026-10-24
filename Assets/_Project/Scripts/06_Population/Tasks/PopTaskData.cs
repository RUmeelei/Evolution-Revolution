using System;
using System.Runtime.InteropServices;

namespace ER.World.Population.Tasks
{
    public enum PopTaskType : byte
    {
        None = 0,
        ForageBerries = 1,
        HarvestWood = 2,
        MineFlint = 3,
        HaulToHearth = 4,
        HuntAnimal = 5,
        CraftAtStation = 6,
        HaulToConstruction = 7,
        BuildStructure = 8
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PopTaskData
    {
        public int TaskId;

        public int PopId;

        public PopTaskType TaskType;

        public int TargetNodeId;

        public float TargetWorldX;

        public float TargetWorldY;

        public float CurrentProgress;

        public float RequiredWork;

        public ushort YieldItemTypeId;


        public static PopTaskData CreateEmpty(int taskId)
        {
            return new PopTaskData
            {
                TaskId = taskId,
                PopId = 0,
                TaskType = PopTaskType.None,
                TargetNodeId = -1,
                TargetWorldX = 0.0f,
                TargetWorldY = 0.0f,
                CurrentProgress = 0.0f,
                RequiredWork = 0.0f,
                YieldItemTypeId = 0
            };
        }
    }
}