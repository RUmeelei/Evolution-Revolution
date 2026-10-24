using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using ER.World.Grid;

namespace ER.World.Population
{
    [BurstCompile]
    public struct PopMetabolismJob : IJobParallelFor
    {
        public NativeArray<PopData> Pops;

        [ReadOnly] public NativeArray<TileData> Tiles;

        [ReadOnly] public NativeArray<float2> HearthPositions;

        public int MapWidth;

        public int MapHeight;

        public float TileWorldSize;

        public float AmbientBaseTemperature;

        public float IdleCalorieBurn;

        public float MovingCalorieBurn;

        public float WorkingCalorieBurn;

        public float ShiveringCalorieCost;

        public float BaseHydrationDecay;

        public float WorkHydrationMultiplier;

        public float NominalBodyTemp;

        public float BaseComfortTemp;

        public float ActiveWarmingRate;

        public float PassiveCoolingRate;

        public float WorkHeatBoost;

        public float HearthRadius;

        public float HearthHeatBonus;

        public float HypothermiaCritTemp;

        public byte StarvationDamage;

        public byte DehydrationDamage;

        public byte HypothermiaDamage;

        public byte NaturalHealingRate;


        public void Execute(int index)
        {
            PopData pop = Pops[index];

            if (!pop.IsAlive)
            {
                return;
            }

            float calorieBurn = IdleCalorieBurn;

            float hydrationDecay = BaseHydrationDecay;

            float physicalHeatBoost = 0.0f;

            if (pop.ActivityState == PopActivityState.Working)
            {
                calorieBurn = WorkingCalorieBurn;

                hydrationDecay *= WorkHydrationMultiplier;

                physicalHeatBoost = WorkHeatBoost;
            }
            else if (pop.ActivityState == PopActivityState.Moving)
            {
                calorieBurn = MovingCalorieBurn;

                hydrationDecay *= 1.3f;

                physicalHeatBoost = WorkHeatBoost * 0.45f;
            }

            float2 popPos = new float2(pop.WorldPositionX, pop.WorldPositionY);

            float localAmbientTemp = CalculateLocalTemperature(popPos);

            float effectiveAmbient = localAmbientTemp + physicalHeatBoost;

            float insulationNorm = math.clamp(pop.WarmthInsulation / 255.0f, 0.05f, 0.90f);

            float comfortThreshold = BaseComfortTemp - (insulationNorm * 24.0f);

            if (effectiveAmbient >= comfortThreshold)
            {
                if (pop.BodyTemperature < NominalBodyTemp)
                {
                    float thermalSurplus = math.max(1.0f, (effectiveAmbient - comfortThreshold) * 0.12f);

                    pop.BodyTemperature = math.min(NominalBodyTemp, pop.BodyTemperature + ActiveWarmingRate * thermalSurplus);
                }
            }
            else
            {
                float coldDeficit = comfortThreshold - effectiveAmbient;

                float metabolicDefense = 0.0f;

                if (pop.CurrentCalories > 200.0f)
                {
                    calorieBurn += ShiveringCalorieCost * (coldDeficit / 15.0f);

                    metabolicDefense = 0.82f;
                }

                float unmitigatedDeficit = coldDeficit * (1.0f - metabolicDefense);

                float coolingStep = unmitigatedDeficit * PassiveCoolingRate * (1.0f - insulationNorm * 0.6f);

                pop.BodyTemperature = math.max(15.0f, pop.BodyTemperature - coolingStep);
            }

            pop.CurrentCalories = math.max(0.0f, pop.CurrentCalories - calorieBurn);

            pop.CurrentHydration = math.max(0.0f, pop.CurrentHydration - hydrationDecay);

            bool isSuffering = false;

            if (pop.CurrentCalories <= 0.0f)
            {
                ApplyDamage(ref pop, StarvationDamage);

                isSuffering = true;
            }

            if (pop.CurrentHydration <= 0.0f)
            {
                ApplyDamage(ref pop, DehydrationDamage);

                isSuffering = true;
            }

            if (pop.BodyTemperature < HypothermiaCritTemp)
            {
                ApplyDamage(ref pop, HypothermiaDamage);

                isSuffering = true;
            }

            if (!isSuffering && pop.Health < 255 && pop.CurrentCalories > 1400.0f && pop.CurrentHydration > 40.0f && pop.BodyTemperature > 35.5f)
            {
                pop.Health = (byte)math.min(255, pop.Health + NaturalHealingRate);
            }

            Pops[index] = pop;
        }


        private float CalculateLocalTemperature(float2 pos)
        {
            float temp = AmbientBaseTemperature;

            int tileX = (int)math.floor(pos.x / TileWorldSize);

            int tileY = (int)math.floor(pos.y / TileWorldSize);

            if (tileX >= 0 && tileX < MapWidth && tileY >= 0 && tileY < MapHeight)
            {
                int tileIdx = tileY * MapWidth + tileX;

                TileData tile = Tiles[tileIdx];

                float elevationLapse = (tile.Elevation / 255.0f) * 10.0f;

                temp -= elevationLapse;
            }

            float hearthBonus = 0.0f;

            float radiusSqr = HearthRadius * HearthRadius;

            for (int h = 0; h < HearthPositions.Length; h++)
            {
                float distSqr = math.distancesq(pos, HearthPositions[h]);

                if (distSqr < radiusSqr)
                {
                    float dist = math.sqrt(distSqr);

                    float normalizedDist = dist / HearthRadius;

                    float factor = (1.0f - normalizedDist) * (1.0f - normalizedDist);

                    float bonus = HearthHeatBonus * factor;

                    if (bonus > hearthBonus)
                    {
                        hearthBonus = bonus;
                    }
                }
            }

            return temp + hearthBonus;
        }


        private void ApplyDamage(ref PopData pop, byte damage)
        {
            if (pop.Health <= damage)
            {
                pop.Health = 0;

                pop.IsAlive = false;

                pop.ActivityState = PopActivityState.Idle;

                pop.VelocityX = 0.0f;

                pop.VelocityY = 0.0f;
            }
            else
            {
                pop.Health -= damage;
            }
        }
    }
}