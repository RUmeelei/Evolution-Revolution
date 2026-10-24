using System;
using System.Runtime.InteropServices;

namespace ER.World.Fauna
{
    public enum AnimalType : byte
    {
        Reindeer = 0,
        WildBoar = 1
    }

    public enum AnimalState : byte
    {
        Grazing = 0,
        Fleeing = 1,
        Exhausted = 2,
        Carcass = 3
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct AnimalData
    {
        public int AnimalId;

        public ushort GeoRegionId;

        public AnimalType Type;

        public AnimalState State;

        public float WorldPositionX;

        public float WorldPositionY;

        public float TargetPositionX;

        public float TargetPositionY;

        public float PastureCenterX;

        public float PastureCenterY;

        public float VelocityX;

        public float VelocityY;

        public float Stamina;

        public byte Health;

        public byte MaxHealth;

        public uint RemainingMeat;

        public uint RemainingHides;

        public bool IsActive;


        public static AnimalData Create(
            int id,
            ushort regionId,
            AnimalType type,
            float x,
            float y)
        {
            byte hp = type == AnimalType.Reindeer ? (byte)75 : (byte)120;

            uint meat = type == AnimalType.Reindeer ? 35u : 55u;

            uint hides = type == AnimalType.Reindeer ? 6u : 4u;

            return new AnimalData
            {
                AnimalId = id,
                GeoRegionId = regionId,
                Type = type,
                State = AnimalState.Grazing,
                WorldPositionX = x,
                WorldPositionY = y,
                TargetPositionX = x,
                TargetPositionY = y,
                PastureCenterX = x,
                PastureCenterY = y,
                VelocityX = 0.0f,
                VelocityY = 0.0f,
                Stamina = 100.0f,
                Health = hp,
                MaxHealth = hp,
                RemainingMeat = meat,
                RemainingHides = hides,
                IsActive = true
            };
        }
    }
}