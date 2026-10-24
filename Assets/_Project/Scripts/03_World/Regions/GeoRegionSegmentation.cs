using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using ER.World.Grid;

namespace ER.World.Regions
{
    public static class GeoRegionSegmentation
    {
        private struct HeapNode
        {
            public int TileIndex;

            public int Cost;
        }


        private class FastMinHeap
        {
            private HeapNode[] elements;

            private int count;


            public int Count => count;


            public FastMinHeap(int capacity)
            {
                elements = new HeapNode[capacity];

                count = 0;
            }


            public void Push(int tileIndex, int cost)
            {
                if (count >= elements.Length)
                {
                    Array.Resize(ref elements, elements.Length * 2);
                }

                elements[count] = new HeapNode { TileIndex = tileIndex, Cost = cost };

                BubbleUp(count);

                count++;
            }


            public HeapNode Pop()
            {
                HeapNode result = elements[0];

                count--;

                if (count > 0)
                {
                    elements[0] = elements[count];

                    BubbleDown(0);
                }

                return result;
            }


            private void BubbleUp(int index)
            {
                while (index > 0)
                {
                    int parent = (index - 1) / 2;

                    if (elements[index].Cost >= elements[parent].Cost)
                    {
                        break;
                    }

                    Swap(index, parent);

                    index = parent;
                }
            }


            private void BubbleDown(int index)
            {
                while (true)
                {
                    int left = 2 * index + 1;

                    int right = 2 * index + 2;

                    int smallest = index;

                    if (left < count && elements[left].Cost < elements[smallest].Cost)
                    {
                        smallest = left;
                    }

                    if (right < count && elements[right].Cost < elements[smallest].Cost)
                    {
                        smallest = right;
                    }

                    if (smallest == index)
                    {
                        break;
                    }

                    Swap(index, smallest);

                    index = smallest;
                }
            }


            private void Swap(int a, int b)
            {
                HeapNode temp = elements[a];

                elements[a] = elements[b];

                elements[b] = temp;
            }
        }


        private struct RegionSeed
        {
            public ushort Id;

            public int TileIndex;

            public bool IsOcean;

            public int X;

            public int Y;
        }


        public static GeoRegionData[] GenerateRegions(
            NativeArray<TileData> tiles,
            WorldMapConfig mapConfig,
            int targetLandRegionsCount = 180,
            int targetOceanRegionsCount = 30)
        {
            int totalTiles = mapConfig.TotalTilesCount;

            ushort[] regionAssignments = new ushort[totalTiles];

            int[] costToReach = new int[totalTiles];

            for (int i = 0; i < totalTiles; i++)
            {
                costToReach[i] = int.MaxValue;
            }

            List<RegionSeed> allSeeds = DistributeTopographicalSeeds(tiles, mapConfig, targetLandRegionsCount, targetOceanRegionsCount);

            FastMinHeap heap = new FastMinHeap(allSeeds.Count * 256);

            for (int i = 0; i < allSeeds.Count; i++)
            {
                RegionSeed seed = allSeeds[i];

                regionAssignments[seed.TileIndex] = seed.Id;

                costToReach[seed.TileIndex] = 0;

                heap.Push(seed.TileIndex, 0);
            }

            int[] neighborOffsetsX = { 0, 0, -1, 1 };

            int[] neighborOffsetsY = { -1, 1, 0, 0 };

            while (heap.Count > 0)
            {
                HeapNode currentNode = heap.Pop();

                int currentIndex = currentNode.TileIndex;

                if (currentNode.Cost > costToReach[currentIndex])
                {
                    continue;
                }

                ushort currentRegionId = regionAssignments[currentIndex];

                bool isCurrentOcean = currentRegionId > targetLandRegionsCount;

                mapConfig.IndexToCoords(currentIndex, out int curX, out int curY);

                TileData currentTile = tiles[currentIndex];

                for (int dir = 0; dir < 4; dir++)
                {
                    int neighborX = curX + neighborOffsetsX[dir];

                    int neighborY = curY + neighborOffsetsY[dir];

                    if (!mapConfig.IsCoordsValid(neighborX, neighborY))
                    {
                        continue;
                    }

                    int neighborIndex = mapConfig.CoordsToIndex(neighborX, neighborY);

                    TileData neighborTile = tiles[neighborIndex];

                    bool isNeighborOcean = neighborTile.Terrain == TerrainType.WaterDeep;

                    if (isCurrentOcean != isNeighborOcean)
                    {
                        continue;
                    }

                    if (!isCurrentOcean && neighborTile.Terrain == TerrainType.Mountains)
                    {
                        continue;
                    }

                    int traversalWeight = CalculateTopographicalWeight(currentTile, neighborTile, isCurrentOcean);

                    int newTotalCost = currentNode.Cost + traversalWeight;

                    if (newTotalCost < costToReach[neighborIndex])
                    {
                        costToReach[neighborIndex] = newTotalCost;

                        regionAssignments[neighborIndex] = currentRegionId;

                        heap.Push(neighborIndex, newTotalCost);
                    }
                }
            }

            for (int i = 0; i < totalTiles; i++)
            {
                TileData tile = tiles[i];

                tile.GeoRegionId = regionAssignments[i];

                tiles[i] = tile;
            }

            return CompileRegionData(tiles, mapConfig, allSeeds, regionAssignments);
        }


        private static int CalculateTopographicalWeight(TileData fromTile, TileData toTile, bool isOcean)
        {
            if (isOcean)
            {
                return 15;
            }

            int baseCost = 8;

            int elevationFactor = toTile.Elevation / 16;

            int elevationBarrier = elevationFactor * elevationFactor;

            int climbPenalty = 0;

            if (toTile.Elevation > fromTile.Elevation)
            {
                climbPenalty = (toTile.Elevation - fromTile.Elevation) * 4;
            }

            int terrainPenalty = 0;

            if (toTile.Terrain == TerrainType.Hills)
            {
                terrainPenalty = 55;
            }
            else if (toTile.Terrain == TerrainType.Swamp)
            {
                terrainPenalty = 60;
            }
            else if (toTile.Terrain == TerrainType.WaterShallow)
            {
                terrainPenalty = 90;
            }

            return baseCost + elevationBarrier + climbPenalty + terrainPenalty;
        }


        private static List<RegionSeed> DistributeTopographicalSeeds(
            NativeArray<TileData> tiles,
            WorldMapConfig mapConfig,
            int landTarget,
            int oceanTarget)
        {
            List<RegionSeed> seeds = new List<RegionSeed>(landTarget + oceanTarget);

            List<int> landCandidates = new List<int>(10000);

            List<int> oceanCandidates = new List<int>(5000);

            int width = mapConfig.MapWidth;

            int height = mapConfig.MapHeight;

            int step = 6;

            for (int y = step; y < height - step; y += step)
            {
                for (int x = step; x < width - step; x += step)
                {
                    int index = mapConfig.CoordsToIndex(x, y);

                    TileData tile = tiles[index];

                    if (tile.Terrain == TerrainType.Plains && tile.Elevation < 135)
                    {
                        landCandidates.Add(index);
                    }
                    else if (tile.Terrain == TerrainType.WaterDeep)
                    {
                        oceanCandidates.Add(index);
                    }
                }
            }

            ushort regionIdCounter = 1;

            SelectFarthestSeeds(landCandidates, seeds, landTarget, ref regionIdCounter, false, mapConfig);

            SelectFarthestSeeds(oceanCandidates, seeds, oceanTarget, ref regionIdCounter, true, mapConfig);

            return seeds;
        }


        private static void SelectFarthestSeeds(
            List<int> candidates,
            List<RegionSeed> outSeeds,
            int count,
            ref ushort idCounter,
            bool isOcean,
            WorldMapConfig mapConfig)
        {
            if (candidates.Count == 0)
            {
                return;
            }

            List<int> selectedIndices = new List<int>(count);

            int firstIndex = candidates[UnityEngine.Random.Range(0, candidates.Count)];

            selectedIndices.Add(firstIndex);

            mapConfig.IndexToCoords(firstIndex, out int fx, out int fy);

            outSeeds.Add(new RegionSeed
            {
                Id = idCounter++,
                TileIndex = firstIndex,
                IsOcean = isOcean,
                X = fx,
                Y = fy
            });

            for (int i = 1; i < count; i++)
            {
                int bestCandidate = -1;

                long maxMinDistance = -1;

                for (int c = 0; c < candidates.Count; c++)
                {
                    int candidateTile = candidates[c];

                    mapConfig.IndexToCoords(candidateTile, out int cx, out int cy);

                    long minDistanceToSelected = long.MaxValue;

                    for (int s = 0; s < selectedIndices.Count; s++)
                    {
                        mapConfig.IndexToCoords(selectedIndices[s], out int sx, out int sy);

                        long dx = cx - sx;

                        long dy = cy - sy;

                        long distSqr = dx * dx + dy * dy;

                        if (distSqr < minDistanceToSelected)
                        {
                            minDistanceToSelected = distSqr;
                        }
                    }

                    if (minDistanceToSelected > maxMinDistance)
                    {
                        maxMinDistance = minDistanceToSelected;

                        bestCandidate = candidateTile;
                    }
                }

                if (bestCandidate >= 0)
                {
                    selectedIndices.Add(bestCandidate);

                    mapConfig.IndexToCoords(bestCandidate, out int bx, out int by);

                    outSeeds.Add(new RegionSeed
                    {
                        Id = idCounter++,
                        TileIndex = bestCandidate,
                        IsOcean = isOcean,
                        X = bx,
                        Y = by
                    });
                }
            }
        }


        private static GeoRegionData[] CompileRegionData(
            NativeArray<TileData> tiles,
            WorldMapConfig mapConfig,
            List<RegionSeed> seeds,
            ushort[] regionAssignments)
        {
            int count = seeds.Count;

            Dictionary<ushort, GeoRegionData> map = new Dictionary<ushort, GeoRegionData>(count);

            Dictionary<ushort, long> elevationSums = new Dictionary<ushort, long>(count);

            Dictionary<ushort, long> moistureSums = new Dictionary<ushort, long>(count);

            for (int i = 0; i < count; i++)
            {
                ushort id = seeds[i].Id;

                map[id] = GeoRegionData.CreateDefault(id, seeds[i].IsOcean);

                elevationSums[id] = 0;

                moistureSums[id] = 0;
            }

            int totalTiles = mapConfig.TotalTilesCount;

            for (int i = 0; i < totalTiles; i++)
            {
                ushort regId = regionAssignments[i];

                if (regId == 0 || !map.ContainsKey(regId))
                {
                    continue;
                }

                mapConfig.IndexToCoords(i, out int x, out int y);

                TileData t = tiles[i];

                GeoRegionData data = map[regId];

                data.TotalTilesCount++;

                if (x < data.MinTileX) data.MinTileX = x;

                if (x > data.MaxTileX) data.MaxTileX = x;

                if (y < data.MinTileY) data.MinTileY = y;

                if (y > data.MaxTileY) data.MaxTileY = y;

                map[regId] = data;

                elevationSums[regId] += t.Elevation;

                moistureSums[regId] += t.Moisture;
            }

            GeoRegionData[] finalRegions = new GeoRegionData[count];

            for (int i = 0; i < count; i++)
            {
                RegionSeed s = seeds[i];

                GeoRegionData data = map[s.Id];

                data.CenterTileX = s.X;

                data.CenterTileY = s.Y;

                if (data.TotalTilesCount > 0)
                {
                    data.AverageElevation = (byte)(elevationSums[s.Id] / data.TotalTilesCount);

                    data.AverageMoisture = (byte)(moistureSums[s.Id] / data.TotalTilesCount);
                }

                finalRegions[i] = data;
            }

            return finalRegions;
        }
    }
}