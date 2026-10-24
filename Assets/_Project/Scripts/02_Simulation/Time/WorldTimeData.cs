using System;
using System.Runtime.InteropServices;
using UnityEngine;

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
        public const int TicksPerHour = 50;

        public const int HoursPerDay = 24;

        public const int DaysPerSeason = 16;

        public const int SeasonsPerYear = 4;

        public const int TicksPerDay = HoursPerDay * TicksPerHour;

        public const int TicksPerSeason = DaysPerSeason * TicksPerDay;

        public const int TicksPerYear = SeasonsPerYear * TicksPerSeason;

        public const int DaysPerYear = DaysPerSeason * SeasonsPerYear;


        public ulong TotalTicks;

        public int CurrentHour;

        public int CurrentDay;

        public int CurrentYear;

        public Season CurrentSeason;

        public float AmbientTemperature;


        public bool IsFreezing => AmbientTemperature <= 0.0f;

        public float DailyProgressRatio => (TotalTicks % TicksPerDay) / (float)TicksPerDay;


        public static WorldTimeData CreateInitial()
        {
            WorldTimeData time = new WorldTimeData
            {
                TotalTicks = 0,
                CurrentHour = 0,
                CurrentDay = 1,
                CurrentSeason = Season.Spring,
                CurrentYear = 1,
                AmbientTemperature = CalculateAmbientTemperature(Season.Spring, 1, 0, 0)
            };

            return time;
        }


        public void AdvanceTick(out bool dayChanged, out bool seasonChanged, out bool yearChanged)
        {
            TotalTicks++;

            dayChanged = false;

            seasonChanged = false;

            yearChanged = false;

            int tickInDay = (int)(TotalTicks % (ulong)TicksPerDay);

            CurrentHour = tickInDay / TicksPerHour;

            if (tickInDay == 0)
            {
                dayChanged = true;

                CurrentDay++;

                if (CurrentDay > DaysPerSeason)
                {
                    CurrentDay = 1;

                    seasonChanged = true;

                    byte nextSeason = (byte)((byte)CurrentSeason + 1);

                    if (nextSeason >= SeasonsPerYear)
                    {
                        CurrentSeason = Season.Spring;

                        CurrentYear++;

                        yearChanged = true;
                    }
                    else
                    {
                        CurrentSeason = (Season)nextSeason;
                    }
                }
            }

            AmbientTemperature = CalculateAmbientTemperature(CurrentSeason, CurrentDay, CurrentHour, tickInDay);
        }


        public static float CalculateAmbientTemperature(Season season, int dayInSeason, int hour, int tickInDay)
        {
            float dayProgress = (tickInDay % TicksPerDay) / (float)TicksPerDay;

            float totalYearDay = ((int)season * DaysPerSeason) + (dayInSeason - 1) + dayProgress;

            float summerPeakDay = DaysPerSeason * 1.5f;

            float seasonAngle = (totalYearDay - summerPeakDay) * (2.0f * Mathf.PI / DaysPerYear);

            float seasonalTemp = 6.0f + Mathf.Cos(seasonAngle) * 16.0f;

            float hourProgress = (hour + (tickInDay % TicksPerHour) / (float)TicksPerHour) / (float)HoursPerDay;

            float dayAngle = (hourProgress - 0.58f) * (2.0f * Mathf.PI);

            float diurnalTemp = Mathf.Cos(dayAngle) * 3.5f;

            return seasonalTemp + diurnalTemp;
        }
    }
}