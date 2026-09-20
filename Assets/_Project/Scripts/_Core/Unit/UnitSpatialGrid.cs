using System.Collections.Generic;
using UnityEngine;

namespace ER
{
    namespace Unit
    {
        public class UnitSpatialGrid
        {
            private List<Unit>[] Cells;
            private int GridWidth;
            private int GridHeight;
            private float CellSize;

            public UnitSpatialGrid(float worldWidth, float worldHeight, float cellSize)
            {
                CellSize = cellSize;

                GridWidth = Mathf.CeilToInt(worldWidth / CellSize);
                GridHeight = Mathf.CeilToInt(worldHeight / CellSize);

                Cells = new List<Unit>[GridWidth * GridHeight];

                for (int i = 0; i < Cells.Length; i++)
                {
                    Cells[i] = new List<Unit>();
                }
            }
            
            private int GetCellIndex(Vector2 position)
            {
                int x = Mathf.Clamp(Mathf.FloorToInt(position.x / CellSize), 0, GridWidth - 1);
                int y = Mathf.Clamp(Mathf.FloorToInt(position.y / CellSize), 0, GridHeight - 1);

                return y * GridWidth + x;
            }
            
            public void Register(Unit unit)
            {
                int index = GetCellIndex(unit.Position);

                Cells[index].Add(unit);
            }
            
            public void Unregister(Unit unit)
            {
                int index = GetCellIndex(unit.Position);

                Cells[index].Remove(unit);
            }

            public void UpdatePosition(Unit unit, Vector2 oldPosition)
            {
                int oldIndex = GetCellIndex(oldPosition);
                int newIndex = GetCellIndex(unit.Position);

                if (oldIndex != newIndex)
                {
                    Cells[oldIndex].Remove(unit);

                    Cells[newIndex].Add(unit);
                }
            }
            
            public List<Unit> GetUnitsInRadius(Vector2 position, float radius, bool single = false)
            {
                List<Unit> result = new List<Unit>();
                
                int minX = Mathf.Clamp(Mathf.FloorToInt((position.x - radius) / CellSize), 0, GridWidth - 1);
                int maxX = Mathf.Clamp(Mathf.FloorToInt((position.x + radius) / CellSize), 0, GridWidth - 1);
                int minY = Mathf.Clamp(Mathf.FloorToInt((position.y - radius) / CellSize), 0, GridHeight - 1);
                int maxY = Mathf.Clamp(Mathf.FloorToInt((position.y + radius) / CellSize), 0, GridHeight - 1);
                
                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        int index = y * GridWidth + x;
                        
                        foreach (var unit in Cells[index])
                        {
                            if (Vector2.Distance(unit.Position, position) <= radius)
                            {
                                result.Add(unit);

                                if (single) break;
                            }
                        }
                    }
                }

                return result;
            }

            public int CountAllUnits()
            {
                int count = 0;

                for (int i = 0; i < Cells.Length; i++)
                {
                    count += Cells[i].Count;
                }

                return count;
            }
        }
    }
}