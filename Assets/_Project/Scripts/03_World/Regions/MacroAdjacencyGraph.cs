using System;
using System.Collections.Generic;

namespace ER.World.Regions
{
    [Serializable]
    public class MacroAdjacencyGraph
    {
        private readonly List<GeoRegionEdge> edges = new List<GeoRegionEdge>();

        private readonly Dictionary<ushort, List<int>> adjacencyLookup = new Dictionary<ushort, List<int>>();

        private readonly Dictionary<long, int> edgeKeyToEdgeIndex = new Dictionary<long, int>();


        public IReadOnlyList<GeoRegionEdge> Edges => edges;

        public int TotalEdgesCount => edges.Count;


        public void Clear()
        {
            edges.Clear();

            adjacencyLookup.Clear();

            edgeKeyToEdgeIndex.Clear();
        }


        public void AddOrUpdateEdge(GeoRegionEdge edge)
        {
            long key = MakeEdgeKey(edge.RegionA, edge.RegionB);

            if (edgeKeyToEdgeIndex.TryGetValue(key, out int existingIndex))
            {
                edges[existingIndex] = edge;

                return;
            }

            int newIndex = edges.Count;

            edges.Add(edge);

            edgeKeyToEdgeIndex[key] = newIndex;

            RegisterInLookup(edge.RegionA, newIndex);

            RegisterInLookup(edge.RegionB, newIndex);
        }


        private void RegisterInLookup(ushort regionId, int edgeIndex)
        {
            if (!adjacencyLookup.TryGetValue(regionId, out List<int> list))
            {
                list = new List<int>(8);

                adjacencyLookup[regionId] = list;
            }

            list.Add(edgeIndex);
        }


        public bool TryGetEdge(ushort regionA, ushort regionB, out GeoRegionEdge edge)
        {
            long key = MakeEdgeKey(regionA, regionB);

            if (edgeKeyToEdgeIndex.TryGetValue(key, out int edgeIndex))
            {
                edge = edges[edgeIndex];

                return true;
            }

            edge = default;

            return false;
        }


        public bool AreAdjacent(ushort regionA, ushort regionB)
        {
            long key = MakeEdgeKey(regionA, regionB);

            return edgeKeyToEdgeIndex.ContainsKey(key);
        }


        public int GetConnectedEdges(ushort regionId, List<GeoRegionEdge> results)
        {
            if (results == null)
            {
                return 0;
            }

            results.Clear();

            if (!adjacencyLookup.TryGetValue(regionId, out List<int> edgeIndices))
            {
                return 0;
            }

            for (int i = 0; i < edgeIndices.Count; i++)
            {
                results.Add(edges[edgeIndices[i]]);
            }

            return results.Count;
        }


        public static long MakeEdgeKey(ushort a, ushort b)
        {
            ushort min = a < b ? a : b;

            ushort max = a < b ? b : a;

            return ((long)min << 32) | (long)max;
        }
    }
}