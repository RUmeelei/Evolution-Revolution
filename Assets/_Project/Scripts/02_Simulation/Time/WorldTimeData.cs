using System;
using System.Runtime.InteropServices;

namespace ER.Simulation.Time
{
    public enum Season : byte
    {
        Spring = 0,
        Summer = 1,
        Autumn = 2,
        Winter = 3
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct WorldTimeData
    {
        public const int TicksPerHour = 1;
        public const int HoursPerDay = 24;
        public const int DaysPerSeason = 30;
        public const int SeasonsPerYear = 4;

        public const int TicksPerDay = HoursPerDay * TicksPerHour;
        public const int TicksPerSeason = DaysPerSeason * TicksPerDay;
        public const int TicksPerYear = SeasonsPerYear * TicksPerSeason;

        public ulong TotalTicks;

        public int CurrentHour;
        public int CurrentDay;
        public int CurrentYear;

        public Season CurrentSeason;

        public static WorldTimeData CreateInitial()
        {
            return new WorldTimeData
            {
                TotalTicks = 0,
                CurrentHour = 0,
                CurrentDay = 1,
                CurrentSeason = Season.Spring,
                CurrentYear = 1
            };
        }

        public void AdvanceTick(out bool dayChanged, out bool seasonChanged, out bool yearChanged)
        {
            TotalTicks++;

            dayChanged = false;
            seasonChanged = false;
            yearChanged = false;

            CurrentHour++;

            if (CurrentHour < HoursPerDay)
            {
                return;
            }

            CurrentHour = 0;
            CurrentDay++;
            dayChanged = true;

            if (CurrentDay <= DaysPerSeason)
            {
                return;
            }

            CurrentDay = 1;

            byte nextSeason = (byte)((byte)CurrentSeason + 1);

            if (nextSeason >= SeasonsPerYear)
            {
                CurrentSeason = Season.Spring;
                CurrentYear++;
                seasonChanged = true;
                yearChanged = true;
            }
            else
            {
                CurrentSeason = (Season)nextSeason;
                seasonChanged = true;
            }
        }
    }
}