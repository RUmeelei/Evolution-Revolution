using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using ER.World.Grid;

namespace ER.World.Regions
{
    public static class GeoRegionAdjacencyBuilder
    {
        private class EdgeAccumulator
        {
            public ushort RegionA;

            public ushort RegionB;

            public int ContactCount;

            public int BestPassXA;

            public int BestPassYA;

            public int BestPassXB;

            public int BestPassYB;

            public int LowestCost;

            public byte LowestElevation;

            public bool IsOcean;
        }


        public static MacroAdjacencyGraph BuildAdjacencyGraph(NativeArray<TileData> tiles, WorldMapConfig mapConfig)
        {
            MacroAdjacencyGraph graph = new MacroAdjacencyGraph();

            int width = mapConfig.MapWidth;

            int height = mapConfig.MapHeight;

            Dictionary<long, EdgeAccumulator> edgeMap = new Dictionary<long, EdgeAccumulator>(512);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int currentIndex = mapConfig.CoordsToIndex(x, y);

                    TileData currentTile = tiles[currentIndex];

                    ushort regionCurrent = currentTile.GeoRegionId;

                    if (regionCurrent == 0)
                    {
                        continue;
                    }

                    if (x + 1 < width)
                    {
                        ProcessNeighborPair(x, y, x + 1, y, currentTile, tiles, mapConfig, edgeMap);
                    }

                    if (y + 1 < height)
                    {
                        ProcessNeighborPair(x, y, x, y + 1, currentTile, tiles, mapConfig, edgeMap);
                    }
                }
            }

            foreach (var pair in edgeMap.Values)
            {
                GeoRegionEdge edge = new GeoRegionEdge
                {
                    RegionA = pair.RegionA,
                    RegionB = pair.RegionB,
                    ContactTilesCount = pair.ContactCount,
                    PassTileXA = pair.BestPassXA,
                    PassTileYA = pair.BestPassYA,
                    PassTileXB = pair.BestPassXB,
                    PassTileYB = pair.BestPassYB,
                    BestPassElevation = pair.LowestElevation,
                    MinTransitCost = pair.LowestCost,
                    IsOceanConnection = pair.IsOcean
                };

                graph.AddOrUpdateEdge(edge);
            }

            return graph;
        }


        private static void ProcessNeighborPair(
            int xA, int yA,
            int xB, int yB,
            TileData tileA,
            NativeArray<TileData> tiles,
            WorldMapConfig mapConfig,
            Dictionary<long, EdgeAccumulator> edgeMap)
        {
            int indexB = mapConfig.CoordsToIndex(xB, yB);

            TileData tileB = tiles[indexB];

            ushort regA = tileA.GeoRegionId;

            ushort regB = tileB.GeoRegionId;

            if (regB == 0 || regA == regB)
            {
                return;
            }

            long edgeKey = MacroAdjacencyGraph.MakeEdgeKey(regA, regB);

            int pairCost = (tileA.MovementCost + tileB.MovementCost) / 2 + Math.Abs(tileA.Elevation - tileB.Elevation) * 3;

            byte maxElevation = (byte)Math.Max(tileA.Elevation, tileB.Elevation);

            bool isOceanEdge = tileA.Terrain == TerrainType.WaterDeep && tileB.Terrain == TerrainType.WaterDeep;

            if (!edgeMap.TryGetValue(edgeKey, out EdgeAccumulator accum))
            {
                accum = new EdgeAccumulator
                {
                    RegionA = regA < regB ? regA : regB,
                    RegionB = regA < regB ? regB : regA,
                    ContactCount = 1,
                    BestPassXA = xA,
                    BestPassYA = yA,
                    BestPassXB = xB,
                    BestPassYB = yB,
                    LowestCost = pairCost,
                    LowestElevation = maxElevation,
                    IsOcean = isOceanEdge
                };

                edgeMap[edgeKey] = accum;
            }
            else
            {
                accum.ContactCount++;

                if (pairCost < accum.LowestCost)
                {
                    accum.LowestCost = pairCost;

                    accum.LowestElevation = maxElevation;

                    accum.BestPassXA = xA;

                    accum.BestPassYA = yA;

                    accum.BestPassXB = xB;

                    accum.BestPassYB = yB;
                }
            }
        }
    }
}