using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace ER.World.Navigation
{
    public class MacroNavGraph
    {
        private readonly Dictionary<int, MacroNavNode> nodes = new Dictionary<int, MacroNavNode>();

        private readonly Dictionary<int, List<MacroNavEdge>> adjacencyList = new Dictionary<int, List<MacroNavEdge>>();

        private readonly Dictionary<ushort, int> regionCenterLookup = new Dictionary<ushort, int>();


        public IReadOnlyDictionary<int, MacroNavNode> Nodes => nodes;

        public int NodeCount => nodes.Count;


        public void Clear()
        {
            nodes.Clear();

            adjacencyList.Clear();

            regionCenterLookup.Clear();
        }


        public void AddNode(MacroNavNode node)
        {
            nodes[node.NodeId] = node;

            if (!adjacencyList.ContainsKey(node.NodeId))
            {
                adjacencyList[node.NodeId] = new List<MacroNavEdge>();
            }

            if (node.NodeType == MacroNodeType.RegionCenter && node.GeoRegionIdA > 0)
            {
                regionCenterLookup[node.GeoRegionIdA] = node.NodeId;
            }
        }


        public void AddEdge(MacroNavEdge edge)
        {
            if (adjacencyList.ContainsKey(edge.FromNodeId))
            {
                adjacencyList[edge.FromNodeId].Add(edge);
            }
        }


        public bool TryGetRegionCenterNodeId(ushort regionId, out int nodeId)
        {
            return regionCenterLookup.TryGetValue(regionId, out nodeId);
        }


        public MacroPathResult FindPath(int startNodeId, int targetNodeId)
        {
            MacroPathResult result = new MacroPathResult
            {
                Success = false,
                TotalDistanceMeters = 0.0f,
                TotalTraversalCost = 0.0f,
                NodeIds = Array.Empty<int>(),
                Waypoints = Array.Empty<float2>()
            };

            if (!nodes.ContainsKey(startNodeId) || !nodes.ContainsKey(targetNodeId))
            {
                return result;
            }

            if (startNodeId == targetNodeId)
            {
                MacroNavNode singleNode = nodes[startNodeId];

                result.Success = true;

                result.NodeIds = new[] { startNodeId };

                result.Waypoints = new[] { new float2(singleNode.WorldPositionX, singleNode.WorldPositionY) };

                return result;
            }

            Dictionary<int, float> gScore = new Dictionary<int, float>();

            Dictionary<int, float> fScore = new Dictionary<int, float>();

            Dictionary<int, int> cameFrom = new Dictionary<int, int>();

            List<int> openSet = new List<int>();

            HashSet<int> closedSet = new HashSet<int>();

            MacroNavNode targetNode = nodes[targetNodeId];

            float2 targetPos = new float2(targetNode.WorldPositionX, targetNode.WorldPositionY);

            gScore[startNodeId] = 0.0f;

            fScore[startNodeId] = Heuristic(nodes[startNodeId], targetPos);

            openSet.Add(startNodeId);

            while (openSet.Count > 0)
            {
                int current = GetLowestFScoreNode(openSet, fScore);

                if (current == targetNodeId)
                {
                    return ReconstructPath(current, cameFrom, gScore[targetNodeId]);
                }

                openSet.Remove(current);

                closedSet.Add(current);

                if (!adjacencyList.TryGetValue(current, out List<MacroNavEdge> edges))
                {
                    continue;
                }

                for (int i = 0; i < edges.Count; i++)
                {
                    MacroNavEdge edge = edges[i];

                    if (edge.IsBlocked) continue;

                    int neighbor = edge.ToNodeId;

                    if (closedSet.Contains(neighbor)) continue;

                    if (!nodes.TryGetValue(neighbor, out MacroNavNode neighborNode) || !neighborNode.IsPassable)
                    {
                        continue;
                    }

                    float tentativeG = gScore[current] + edge.TraversalWeight;

                    if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;

                        gScore[neighbor] = tentativeG;

                        fScore[neighbor] = tentativeG + Heuristic(neighborNode, targetPos);

                        if (!openSet.Contains(neighbor))
                        {
                            openSet.Add(neighbor);
                        }
                    }
                }
            }

            return result;
        }


        private float Heuristic(MacroNavNode node, float2 targetPos)
        {
            float2 nodePos = new float2(node.WorldPositionX, node.WorldPositionY);

            return math.distance(nodePos, targetPos);
        }


        private int GetLowestFScoreNode(List<int> openSet, Dictionary<int, float> fScore)
        {
            int bestNode = openSet[0];

            float lowestF = fScore.ContainsKey(bestNode) ? fScore[bestNode] : float.MaxValue;

            for (int i = 1; i < openSet.Count; i++)
            {
                int candidate = openSet[i];

                float score = fScore.ContainsKey(candidate) ? fScore[candidate] : float.MaxValue;

                if (score < lowestF)
                {
                    lowestF = score;

                    bestNode = candidate;
                }
            }

            return bestNode;
        }


        private MacroPathResult ReconstructPath(int current, Dictionary<int, int> cameFrom, float totalCost)
        {
            List<int> pathNodes = new List<int> { current };

            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];

                pathNodes.Add(current);
            }

            pathNodes.Reverse();

            float2[] waypoints = new float2[pathNodes.Count];

            float totalDist = 0.0f;

            for (int i = 0; i < pathNodes.Count; i++)
            {
                MacroNavNode n = nodes[pathNodes[i]];

                waypoints[i] = new float2(n.WorldPositionX, n.WorldPositionY);

                if (i > 0)
                {
                    totalDist += math.distance(waypoints[i - 1], waypoints[i]);
                }
            }

            return new MacroPathResult
            {
                Success = true,
                TotalDistanceMeters = totalDist,
                TotalTraversalCost = totalCost,
                NodeIds = pathNodes.ToArray(),
                Waypoints = waypoints
            };
        }
    }
}