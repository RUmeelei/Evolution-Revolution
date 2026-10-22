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
                CellSize = Mathf.Max(0.5f, cellSize);

                GridWidth = Mathf.Max(1, Mathf.CeilToInt(worldWidth / CellSize));
                GridHeight = Mathf.Max(1, Mathf.CeilToInt(worldHeight / CellSize));

                Cells = new List<Unit>[GridWidth * GridHeight];

                for (int i = 0; i < Cells.Length; i++)
                {
                    Cells[i] = new List<Unit>(8);
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

                unit.GridCellIndex = index;

                Cells[index].Add(unit);
            }

            public void Unregister(Unit unit)
            {
                int index = unit.GridCellIndex >= 0 && unit.GridCellIndex < Cells.Length ? unit.GridCellIndex : GetCellIndex(unit.Position);

                Cells[index].Remove(unit);

                unit.GridCellIndex = -1;
            }

            public void UpdatePosition(Unit unit, Vector2 oldPosition)
            {
                int oldIndex = unit.GridCellIndex >= 0 && unit.GridCellIndex < Cells.Length ? unit.GridCellIndex : GetCellIndex(oldPosition);

                int newIndex = GetCellIndex(unit.Position);

                if (oldIndex != newIndex)
                {
                    Cells[oldIndex].Remove(unit);
                    Cells[newIndex].Add(unit);

                    unit.GridCellIndex = newIndex;
                }
            }

            public List<Unit> GetUnitsInRadius(Vector2 position, float radius, bool single = false)
            {
                List<Unit> result = new List<Unit>();

                float radiusSqr = radius * radius;

                int minX = Mathf.Clamp(Mathf.FloorToInt((position.x - radius) / CellSize), 0, GridWidth - 1);
                int maxX = Mathf.Clamp(Mathf.FloorToInt((position.x + radius) / CellSize), 0, GridWidth - 1);

                int minY = Mathf.Clamp(Mathf.FloorToInt((position.y - radius) / CellSize), 0, GridHeight - 1);
                int maxY = Mathf.Clamp(Mathf.FloorToInt((position.y + radius) / CellSize), 0, GridHeight - 1);

                for (int y = minY; y <= maxY; y++)
                {
                    int rowOffset = y * GridWidth;

                    for (int x = minX; x <= maxX; x++)
                    {
                        var cellUnits = Cells[rowOffset + x];

                        int count = cellUnits.Count;

                        for (int i = 0; i < count; i++)
                        {
                            var unit = cellUnits[i];
                            if ((unit.Position - position).sqrMagnitude <= radiusSqr)
                            {
                                result.Add(unit);
                                if (single) return result;
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