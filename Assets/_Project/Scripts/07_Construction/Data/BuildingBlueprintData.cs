using System;
using System.Runtime.InteropServices;

namespace ER.World.Construction
{
    public enum BuildingType : byte
    {
        None = 0,
        DugoutShelter = 1,
        Storehouse = 2,
        PalisadeWall = 3,
        DryingRack = 4
    }

    public enum BlueprintState : byte
    {
        AwaitingMaterials = 0,
        UnderConstruction = 1,
        Completed = 2,
        Cancelled = 3
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BuildingBlueprintData
    {
        public int BlueprintId;

        public ushort PolityId;

        public ushort HomeAnchorId;

        public BuildingType TargetBuildingType;

        public BlueprintState State;

        public float WorldPositionX;

        public float WorldPositionY;

        public uint RequiredWood;

        public uint DeliveredWood;

        public uint RequiredStone;

        public uint DeliveredStone;

        public float RequiredLabor;

        public float CurrentLaborProgress;

        public bool IsActive;


        public bool HasAllMaterials => DeliveredWood >= RequiredWood && DeliveredStone >= RequiredStone;

        public float MaterialFillRatio => (RequiredWood + RequiredStone) == 0 ? 1.0f : (float)(DeliveredWood + DeliveredStone) / (RequiredWood + RequiredStone);

        public float LaborFillRatio => RequiredLabor <= 0.0f ? 1.0f : CurrentLaborProgress / RequiredLabor;


        public static BuildingBlueprintData Create(
            int id,
            ushort polityId,
            ushort anchorId,
            BuildingType type,
            float x,
            float y)
        {
            uint wood = 0;

            uint stone = 0;

            float labor = 10.0f;

            switch (type)
            {
                case BuildingType.DugoutShelter:
                    wood = 6;
                    stone = 0;
                    labor = 30.0f;
                    break;

                case BuildingType.Storehouse:
                    wood = 12;
                    stone = 4;
                    labor = 60.0f;
                    break;

                case BuildingType.PalisadeWall:
                    wood = 3;
                    stone = 0;
                    labor = 15.0f;
                    break;

                case BuildingType.DryingRack:
                    wood = 4;
                    stone = 0;
                    labor = 20.0f;
                    break;
            }

            return new BuildingBlueprintData
            {
                BlueprintId = id,
                PolityId = polityId,
                HomeAnchorId = anchorId,
                TargetBuildingType = type,
                State = BlueprintState.AwaitingMaterials,
                WorldPositionX = x,
                WorldPositionY = y,
                RequiredWood = wood,
                DeliveredWood = 0,
                RequiredStone = stone,
                DeliveredStone = 0,
                RequiredLabor = labor,
                CurrentLaborProgress = 0.0f,
                IsActive = true
            };
        }
    }
}