using System;
using UnityEngine;

namespace ER.World.Polity
{
    [Serializable]
    public struct PolityData
    {
        public ushort PolityId;

        public string PolityName;

        public Color32 MapColor;

        public ushort CapitalRegionId;

        public bool IsPlayerControlled;


        public static PolityData Create(ushort id, string name, Color32 color, ushort capitalRegion, bool isPlayer = false)
        {
            return new PolityData
            {
                PolityId = id,
                PolityName = name,
                MapColor = color,
                CapitalRegionId = capitalRegion,
                IsPlayerControlled = isPlayer
            };
        }
    }
}