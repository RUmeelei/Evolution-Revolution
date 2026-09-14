using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace ER
{
    namespace Controls
    {
        using Core;
        using Configs;
        using Unit;
        using World;
        using World.Tiles;
        
        public class SelectionManager : MonoBehaviour
        {
            public static SelectionManager Instance {get; private set;}

            [Header("Main")]
            [SerializeField] private Camera Cam;

            private TileManager tileManager;
            private UnitManager unitManager;

            private MainConfig mainConfig;

            public List<Vector2Int> SelectedTiles = new List<Vector2Int>();
            public List<Unit> SelectedUnits = new List<Unit>();

            void Awake()
            {
                if (transform.parent != null)
                {
                    transform.SetParent(null);
                }

                if (Instance != null && Instance != this)
                {
                    Destroy(gameObject); return;
                }

                Instance = this;

                DontDestroyOnLoad(gameObject);

                CoreManager.RegisterSelectionManager(this);
            }

            public void Initialize()
            {
                tileManager = CoreManager.TileManager;

                unitManager = CoreManager.UnitManager;

                mainConfig = CoreManager.MainConfig;
            }

            void Update()
            {
                bool isOverUI = EventSystem.current.IsPointerOverGameObject();

                if (isOverUI) return;

                if (Input.GetMouseButtonUp(0))
                {
                    Vector2 mousePos = Cam.ScreenToWorldPoint(Input.mousePosition);

                    bool isShift = Input.GetKey(KeyCode.LeftShift);

                    List<Unit> unitsAtMousePos = unitManager.GetUnitsAtPosition(mousePos, 0.1f);

                    if (unitsAtMousePos.Count > 0)
                    {
                        SelectUnits(unitsAtMousePos, isShift);
                    }
                    else if (IsInsideWorld(mousePos))
                    {
                        SelectTile(mousePos, isShift);
                    }
                    else
                    {
                        ClearTiles();

                        if (SelectedUnits.Count > 0) SelectedUnits.Clear();
                    }
                }
            }

            private void SelectUnits(List<Unit> units, bool isShift)
            {
                ClearTiles();

                if (!isShift) SelectedUnits.Clear();

                foreach (var unit in units)
                {
                    if (SelectedUnits.Contains(unit)) SelectedUnits.Remove(unit);
                    else SelectedUnits.Add(unit);
                }
            }

            private void SelectTile(Vector2 mousePos, bool isShift)
            {
                if (SelectedUnits.Count > 0) SelectedUnits.Clear();

                if (!isShift) ClearTiles();

                Vector2Int tile = tileManager.WorldToTilePos(mousePos);

                if (SelectedTiles.Contains(tile)) RemoveTile(tile);
                else AddTile(tile);
                
                RefreshSelectedTiles();
            }

            private void AddTile(Vector2Int tile)
            {
                SelectedTiles.Add(tile);

                RefreshTile(tile);
            }

            private void RemoveTile(Vector2Int tile)
            {
                SelectedTiles.Remove(tile);

                RefreshTile(tile);
            }

            private void ClearTiles()
            {
                if (SelectedTiles.Count <= 0) return;

                var oldTiles = new List<Vector2Int>(SelectedTiles);

                SelectedTiles.Clear();

                foreach (var tile in oldTiles)
                {
                    tileManager.NotifyTileChanged(tile.x, tile.y);
                }
            }

            private void RefreshSelectedTiles()
            {
                foreach (var tile in SelectedTiles)
                {
                    tileManager.NotifyTileChanged(tile.x, tile.y);
                }
            }

            private void RefreshTile(Vector2Int tile)
            {
                tileManager.NotifyTileChanged(tile.x, tile.y);
            }

            private bool IsInsideWorld(Vector2 pos)
            {
                float maxX = tileManager.WorldWidth * mainConfig.TileSize;
                float maxY = tileManager.WorldHeight * mainConfig.TileSize;

                return pos.x >= 0 && pos.y >= 0 && pos.x < maxX && pos.y < maxY;
            }
        }
    }
}