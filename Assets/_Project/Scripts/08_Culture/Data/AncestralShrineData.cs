using System;
using System.Runtime.InteropServices;

namespace ER.World.Culture
{
    public enum ShrineType : byte
    {
        None = 0,
        AncestralTotem = 1,
        BurialMound = 2,
        SacredMenhir = 3
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct AncestralShrineData
    {
        public int ShrineId;

        public ushort PolityId;

        public ushort HomeAnchorId;

        public ShrineType Type;

        public float WorldPositionX;

        public float WorldPositionY;

        public float SacredAuraRadius;

        public float MoraleRegenBonus;

        public uint BuriedAncestorsCount;

        public bool IsActive;


        public static AncestralShrineData Create(
            int id,
            ushort polityId,
            ushort anchorId,
            ShrineType type,
            float x,
            float y,
            float auraRadius = 32.0f,
            float moraleBonus = 15.0f)
        {
            return new AncestralShrineData
            {
                ShrineId = id,
                PolityId = polityId,
                HomeAnchorId = anchorId,
                Type = type,
                WorldPositionX = x,
                WorldPositionY = y,
                SacredAuraRadius = auraRadius,
                MoraleRegenBonus = moraleBonus,
                BuriedAncestorsCount = 0,
                IsActive = true
            };
        }
    }
}