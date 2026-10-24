using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using ER.World.Grid;

namespace ER.World.Population
{
    [BurstCompile]
    public struct BuildPopSpatialGridJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<PopData> Pops;

        public NativeParallelMultiHashMap<int, int>.ParallelWriter SpatialGrid;

        public float CellSize;


        public void Execute(int index)
        {
            PopData pop = Pops[index];

            if (!pop.IsAlive)
            {
                return;
            }

            int cellKey = ComputeCellKey(pop.WorldPositionX, pop.WorldPositionY, CellSize);

            SpatialGrid.Add(cellKey, index);
        }


        public static int ComputeCellKey(float x, float y, float cellSize)
        {
            int cx = (int)math.floor(x / cellSize);

            int cy = (int)math.floor(y / cellSize);

            return (cx * 73856093) ^ (cy * 19349663);
        }
    }

    [BurstCompile]
    public struct PopLocomotionJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<PopData> ReadPops;

        public NativeArray<PopData> WritePops;

        [ReadOnly] public NativeParallelMultiHashMap<int, int> SpatialGrid;

        [ReadOnly] public NativeArray<TileData> Tiles;

        public int MapWidth;

        public int MapHeight;

        public float TileWorldSize;

        public float DeltaTime;

        public float BaseWalkSpeed;

        public float Acceleration;

        public float Drag;

        public float SeparationRadius;

        public float SeparationWeight;

        public float ArrivalDistance;

        public float SpatialCellSize;

        public ulong CurrentTick;


        public void Execute(int index)
        {
            PopData pop = ReadPops[index];

            if (!pop.IsAlive || pop.ActivityState == PopActivityState.Resting)
            {
                pop.VelocityX = 0.0f;
                pop.VelocityY = 0.0f;
                WritePops[index] = pop;
                return;
            }

            float2 currentPos = new float2(pop.WorldPositionX, pop.WorldPositionY);

            float2 currentVel = new float2(pop.VelocityX, pop.VelocityY);

            float2 targetPos = new float2(pop.TargetPositionX, pop.TargetPositionY);

            float distToTarget = math.distance(currentPos, targetPos);

            float2 desiredVelocity = float2.zero;

            if (distToTarget > ArrivalDistance)
            {
                float2 targetDir = (targetPos - currentPos) / distToTarget;

                float targetSpeed = BaseWalkSpeed;

                float slowingRadius = ArrivalDistance * 3.0f;

                if (distToTarget < slowingRadius)
                {
                    targetSpeed = BaseWalkSpeed * (distToTarget / slowingRadius);
                }

                desiredVelocity = targetDir * targetSpeed;

                pop.ActivityState = PopActivityState.Moving;
            }
            else
            {
                desiredVelocity = float2.zero;

                pop.ActivityState = PopActivityState.Idle;
            }

            float2 separationForce = CalculateSeparation(index, currentPos);

            desiredVelocity += separationForce * SeparationWeight;

            float2 obstacleAvoidance = CalculateObstacleAvoidance(currentPos);

            desiredVelocity += obstacleAvoidance * 3.0f;

            float maxSpeed = BaseWalkSpeed * 1.5f;

            float desiredSpeed = math.length(desiredVelocity);

            if (desiredSpeed > maxSpeed)
            {
                desiredVelocity = (desiredVelocity / desiredSpeed) * maxSpeed;
            }

            float smoothing = math.clamp(Acceleration * 0.05f * DeltaTime, 0.15f, 0.45f);

            if (pop.ActivityState == PopActivityState.Idle && math.lengthsq(desiredVelocity) < 0.01f)
            {
                currentVel = math.lerp(currentVel, float2.zero, 0.35f);
            }
            else
            {
                currentVel = math.lerp(currentVel, desiredVelocity, smoothing);
            }

            currentPos += currentVel * DeltaTime;

            ClampWithinMapBounds(ref currentPos);

            pop.WorldPositionX = currentPos.x;

            pop.WorldPositionY = currentPos.y;

            pop.VelocityX = currentVel.x;

            pop.VelocityY = currentVel.y;

            WritePops[index] = pop;
        }


        private float2 CalculateSeparation(int selfIndex, float2 pos)
        {
            float2 totalRepulsion = float2.zero;

            int cellX = (int)math.floor(pos.x / SpatialCellSize);

            int cellY = (int)math.floor(pos.y / SpatialCellSize);

            float sepRadiusSqr = SeparationRadius * SeparationRadius;

            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int neighborKey = ((cellX + dx) * 73856093) ^ ((cellY + dy) * 19349663);

                    if (!SpatialGrid.TryGetFirstValue(neighborKey, out int otherIndex, out var iterator))
                    {
                        continue;
                    }

                    do
                    {
                        if (otherIndex == selfIndex)
                        {
                            continue;
                        }

                        PopData other = ReadPops[otherIndex];

                        float2 otherPos = new float2(other.WorldPositionX, other.WorldPositionY);

                        float2 diff = pos - otherPos;

                        float distSqr = math.lengthsq(diff);

                        if (distSqr > 0.0001f && distSqr < sepRadiusSqr)
                        {
                            float dist = math.sqrt(distSqr);

                            float strength = 1.0f - (dist / SeparationRadius);

                            totalRepulsion += (diff / dist) * strength;
                        }
                    }
                    while (SpatialGrid.TryGetNextValue(out otherIndex, ref iterator));
                }
            }

            float repMagnitude = math.length(totalRepulsion);

            if (repMagnitude > 1.5f)
            {
                totalRepulsion = (totalRepulsion / repMagnitude) * 1.5f;
            }

            return totalRepulsion;
        }


        private float2 CalculateObstacleAvoidance(float2 pos)
        {
            int tileX = (int)math.floor(pos.x / TileWorldSize);

            int tileY = (int)math.floor(pos.y / TileWorldSize);

            float2 push = float2.zero;

            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = tileX + dx;

                    int ny = tileY + dy;

                    if (!IsCoordsValid(nx, ny))
                    {
                        continue;
                    }

                    int tileIdx = CoordsToIndex(nx, ny);

                    TileData tile = Tiles[tileIdx];

                    if (tile.Terrain == TerrainType.WaterDeep || tile.Terrain == TerrainType.Mountains)
                    {
                        float2 tileCenter = new float2((nx + 0.5f) * TileWorldSize, (ny + 0.5f) * TileWorldSize);

                        float2 toPop = pos - tileCenter;

                        float dist = math.length(toPop);

                        if (dist < TileWorldSize * 1.1f && dist > 0.001f)
                        {
                            push += (toPop / dist) * (1.0f - (dist / (TileWorldSize * 1.1f)));
                        }
                    }
                }
            }

            float pushMagnitude = math.length(push);

            if (pushMagnitude > 1.5f)
            {
                push = (push / pushMagnitude) * 1.5f;
            }

            return push;
        }


        private bool IsCoordsValid(int x, int y)
        {
            return x >= 0 && x < MapWidth && y >= 0 && y < MapHeight;
        }


        private int CoordsToIndex(int x, int y)
        {
            return y * MapWidth + x;
        }


        private void ClampWithinMapBounds(ref float2 pos)
        {
            float maxX = MapWidth * TileWorldSize - 1.0f;

            float maxY = MapHeight * TileWorldSize - 1.0f;

            pos.x = math.clamp(pos.x, 1.0f, maxX);

            pos.y = math.clamp(pos.y, 1.0f, maxY);
        }
    }
}