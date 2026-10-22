using UnityEngine;
using System.Collections.Generic;

namespace ER
{
    namespace World
    {
        namespace Tiles
        {
            public static class PathfindingSystem
            {
                private struct Node
                {
                    public int X;
                    public int Y;
                    public float G;
                    public float H;
                    public float F => G + H;
                    public int ParentIndex;
                }

                public static bool HasWaterOnLine(Vector2 startWorld, Vector2 endWorld, TileManager tileManager)
                {
                    if (tileManager == null) return false;

                    float dist = Vector2.Distance(startWorld, endWorld);
                    int steps = Mathf.CeilToInt(dist * 2f);

                    if (steps <= 1)
                    {
                        return tileManager.WorldToTile(endWorld).Type == TileType.Water;
                    }

                    for (int i = 1; i <= steps; i++)
                    {
                        Vector2 sample = Vector2.Lerp(startWorld, endWorld, i / (float)steps);

                        if (tileManager.WorldToTile(sample).Type == TileType.Water)
                        {
                            return true;
                        }
                    }

                    return false;
                }

                public static bool TryFindPath(Vector2 startWorld, Vector2 targetWorld, TileManager tileManager, List<Vector2> outWaypoints)
                {
                    outWaypoints.Clear();

                    if (tileManager == null) return false;

                    if (!HasWaterOnLine(startWorld, targetWorld, tileManager))
                    {
                        outWaypoints.Add(targetWorld);
                        return true;
                    }

                    int width = tileManager.WorldWidth;
                    int height = tileManager.WorldHeight;

                    Vector2Int startTile = tileManager.WorldToTilePos(startWorld);
                    Vector2Int targetTile = tileManager.WorldToTilePos(targetWorld);
                    
                    if (tileManager.GetTile(targetTile.x, targetTile.y).Type == TileType.Water)
                    {
                        return false;
                    }

                    int startIdx = startTile.y * width + startTile.x;
                    int targetIdx = targetTile.y * width + targetTile.x;

                    if (startIdx == targetIdx)
                    {
                        outWaypoints.Add(targetWorld);
                        return true;
                    }

                    var openList = new List<int>(64);
                    var closedSet = new HashSet<int>();
                    var nodes = new Dictionary<int, Node>(128);

                    Node startNode = new Node
                    {
                        X = startTile.x,
                        Y = startTile.y,
                        G = 0f,
                        H = Vector2Int.Distance(startTile, targetTile),
                        ParentIndex = -1
                    };

                    nodes[startIdx] = startNode;
                    openList.Add(startIdx);

                    int[] dx = { 0, 0, -1, 1, -1, 1, -1, 1 };
                    int[] dy = { -1, 1, 0, 0, -1, -1, 1, 1 };

                    int iterations = 0;
                    int maxIterations = 600;
                    int foundTargetIdx = -1;

                    while (openList.Count > 0 && iterations < maxIterations)
                    {
                        iterations++;

                        int bestListPos = 0;
                        int currentIdx = openList[0];
                        Node currentNode = nodes[currentIdx];

                        for (int i = 1; i < openList.Count; i++)
                        {
                            int candidateIdx = openList[i];
                            if (nodes[candidateIdx].F < currentNode.F)
                            {
                                currentIdx = candidateIdx;
                                currentNode = nodes[candidateIdx];
                                bestListPos = i;
                            }
                        }

                        openList.RemoveAt(bestListPos);
                        closedSet.Add(currentIdx);

                        if (currentIdx == targetIdx)
                        {
                            foundTargetIdx = currentIdx;
                            break;
                        }

                        for (int dir = 0; dir < 8; dir++)
                        {
                            int nx = currentNode.X + dx[dir];
                            int ny = currentNode.Y + dy[dir];

                            if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;

                            int neighborIdx = ny * width + nx;
                            if (closedSet.Contains(neighborIdx)) continue;

                            TileData neighborTile = tileManager.GetTile(nx, ny);

                            float stepDist = dir < 4 ? 1.0f : 1.414f;
                            
                            float terrainCost = neighborTile.GetPathfindingCost();

                            float tentativeG = currentNode.G + (stepDist * terrainCost);

                            if (!nodes.TryGetValue(neighborIdx, out Node neighborNode) || tentativeG < neighborNode.G)
                            {
                                neighborNode.X = nx;
                                neighborNode.Y = ny;
                                neighborNode.G = tentativeG;
                                neighborNode.H = Vector2Int.Distance(new Vector2Int(nx, ny), targetTile);
                                neighborNode.ParentIndex = currentIdx;

                                nodes[neighborIdx] = neighborNode;

                                if (!openList.Contains(neighborIdx))
                                {
                                    openList.Add(neighborIdx);
                                }
                            }
                        }
                    }

                    if (foundTargetIdx == -1)
                    {
                        outWaypoints.Add(targetWorld);
                        return true;
                    }

                    int traceIdx = foundTargetIdx;
                    while (traceIdx != -1 && traceIdx != startIdx)
                    {
                        Node n = nodes[traceIdx];
                        outWaypoints.Add(tileManager.GetTileCenter(n.X, n.Y));
                        traceIdx = n.ParentIndex;
                    }

                    outWaypoints.Reverse();
                    outWaypoints[outWaypoints.Count - 1] = targetWorld;

                    return true;
                }
            }
        }
    }
}