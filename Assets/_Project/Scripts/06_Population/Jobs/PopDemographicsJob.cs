using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace ER.World.Population
{
    [BurstCompile]
    public struct PopDemographicsJob : IJobParallelFor
    {
        public NativeArray<PopData> Pops;

        public ushort InfantMaxDays;

        public ushort ChildMaxDays;

        public ushort YouthMaxDays;

        public ushort AdultMaxDays;

        public ushort ElderBaseAgeDays;

        public float BaseSenescenceChance;

        public float SenescenceCurveMultiplier;


        public void Execute(int index)
        {
            PopData pop = Pops[index];

            if (!pop.IsAlive)
            {
                return;
            }

            pop.AgeInDays++;

            if (pop.AgeInDays < InfantMaxDays)
            {
                pop.LifeStage = PopLifeStage.Infant;
            }
            else if (pop.AgeInDays < ChildMaxDays)
            {
                pop.LifeStage = PopLifeStage.Child;
            }
            else if (pop.AgeInDays < YouthMaxDays)
            {
                pop.LifeStage = PopLifeStage.Youth;
            }
            else if (pop.AgeInDays < AdultMaxDays)
            {
                pop.LifeStage = PopLifeStage.Adult;
            }
            else
            {
                pop.LifeStage = PopLifeStage.Elder;
            }

            if (pop.MatingCooldownDays > 0)
            {
                pop.MatingCooldownDays--;
            }

            if (pop.IsPregnant)
            {
                pop.PregnancyDays++;
            }

            if (pop.LifeStage == PopLifeStage.Elder)
            {
                int daysOverFifty = pop.AgeInDays - ElderBaseAgeDays;

                float mortalityChance = BaseSenescenceChance + (daysOverFifty * SenescenceCurveMultiplier);

                uint hash = (uint)pop.PopId * 374761393u + (uint)pop.AgeInDays * 668265263u;

                hash = (hash ^ (hash >> 13)) * 1274126177u;

                float roll = (hash & 0x00FFFFFF) / 16777215.0f;

                if (roll < mortalityChance)
                {
                    pop.IsAlive = false;

                    pop.Health = 0;

                    pop.ActivityState = PopActivityState.Idle;

                    pop.VelocityX = 0.0f;

                    pop.VelocityY = 0.0f;
                }
            }

            Pops[index] = pop;
        }
    }
}