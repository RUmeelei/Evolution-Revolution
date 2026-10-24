using System;
using System.Runtime.InteropServices;

namespace ER.World.Crafting
{
    public enum CraftingStationType : byte
    {
        None = 0,
        CampHearth = 1,
        TanningRack = 2,
        FlintKnappingStation = 3,
        CarpentryBench = 4,
        DryingRack = 5
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CraftingStationData
    {
        public int StationId;

        public ushort PolityId;

        public ushort HomeAnchorId;

        public CraftingStationType StationType;

        public float WorldPositionX;

        public float WorldPositionY;

        public bool IsOccupied;

        public bool IsActive;


        public static CraftingStationData Create(
            int id,
            ushort polityId,
            ushort anchorId,
            CraftingStationType type,
            float x,
            float y)
        {
            return new CraftingStationData
            {
                StationId = id,
                PolityId = polityId,
                HomeAnchorId = anchorId,
                StationType = type,
                WorldPositionX = x,
                WorldPositionY = y,
                IsOccupied = false,
                IsActive = true
            };
        }
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CraftingRecipeData
    {
        public ushort RecipeId;

        public CraftingStationType RequiredStation;

        public ushort InputItemA;

        public uint InputCountA;

        public ushort InputItemB;

        public uint InputCountB;

        public ushort OutputItem;

        public uint OutputCount;

        public float WorkLaborRequired;


        public static CraftingRecipeData Create(
            ushort recipeId,
            CraftingStationType station,
            ushort inA,
            uint countA,
            ushort outItem,
            uint outCount,
            float labor,
            ushort inB = 0,
            uint countB = 0)
        {
            return new CraftingRecipeData
            {
                RecipeId = recipeId,
                RequiredStation = station,
                InputItemA = inA,
                InputCountA = countA,
                InputItemB = inB,
                InputCountB = countB,
                OutputItem = outItem,
                OutputCount = outCount,
                WorkLaborRequired = labor
            };
        }
    }
}