using System;
using System.Runtime.InteropServices;

namespace ER.World.Population
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PopData
    {
        public int PopId;

        public ushort PolityId;

        public ushort HomeAnchorId;

        public int PersonalInventoryId;

        public int HouseholdId;

        public int DwellingId;

        public float WorldPositionX;

        public float WorldPositionY;

        public float TargetPositionX;

        public float TargetPositionY;

        public float VelocityX;

        public float VelocityY;

        public PopActivityState ActivityState;

        public PopLifeStage LifeStage;

        public Gender Gender;

        public bool IsAlive;

        public bool IsBuried;

        public ulong BirthTick;

        public ushort AgeInDays;

        public byte Health;

        public float CurrentCalories;

        public float MaxCalories;

        public float CurrentHydration;

        public float BodyTemperature;

        public byte WarmthInsulation;

        public bool IsPregnant;

        public ushort PregnancyDays;

        public ushort MatingCooldownDays;

        public int SpousePopId;


        public static PopData Create(
            int popId,
            ushort polityId,
            ushort homeAnchorId,
            int inventoryId,
            float worldX,
            float worldY,
            ulong currentTick,
            ushort ageDays,
            Gender gender,
            PopDemographicsConfig config = null)
        {
            PopLifeStage stage = PopLifeStage.Adult;

            if (config != null)
            {
                stage = config.ResolveLifeStage(ageDays);
            }

            return new PopData
            {
                PopId = popId,
                PolityId = polityId,
                HomeAnchorId = homeAnchorId,
                PersonalInventoryId = inventoryId,
                HouseholdId = 0,
                DwellingId = 0,
                WorldPositionX = worldX,
                WorldPositionY = worldY,
                TargetPositionX = worldX,
                TargetPositionY = worldY,
                VelocityX = 0.0f,
                VelocityY = 0.0f,
                ActivityState = PopActivityState.Idle,
                LifeStage = stage,
                Gender = gender,
                IsAlive = true,
                IsBuried = false,
                BirthTick = currentTick,
                AgeInDays = ageDays,
                Health = 255,
                CurrentCalories = 2000.0f,
                MaxCalories = 2500.0f,
                CurrentHydration = 100.0f,
                BodyTemperature = 36.6f,
                WarmthInsulation = 20,
                IsPregnant = false,
                PregnancyDays = 0,
                MatingCooldownDays = 0,
                SpousePopId = 0
            };
        }
    }
}