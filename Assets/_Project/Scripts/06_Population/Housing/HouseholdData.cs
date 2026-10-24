using System;
using System.Runtime.InteropServices;

namespace ER.World.Housing
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct HouseholdData
    {
        public int HouseholdId;

        public ushort PolityId;

        public ushort HomeAnchorId;

        public int DwellingId;

        public int HeadPopId;

        public int SpousePopId;

        public byte ChildrenCount;

        public bool IsActive;


        public int TotalMembers => (HeadPopId > 0 ? 1 : 0) + (SpousePopId > 0 ? 1 : 0) + ChildrenCount;


        public static HouseholdData Create(
            int householdId,
            ushort polityId,
            ushort anchorId,
            int dwellingId,
            int headPopId,
            int spousePopId)
        {
            return new HouseholdData
            {
                HouseholdId = householdId,
                PolityId = polityId,
                HomeAnchorId = anchorId,
                DwellingId = dwellingId,
                HeadPopId = headPopId,
                SpousePopId = spousePopId,
                ChildrenCount = 0,
                IsActive = true
            };
        }
    }
}