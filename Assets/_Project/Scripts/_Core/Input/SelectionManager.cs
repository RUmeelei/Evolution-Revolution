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
        using Culture;
        using World;
        using World.Tiles;

        public class SelectionManager : MonoBehaviour
        {
            public static SelectionManager Instance {get; private set;}

            [Header("Main")]
            [SerializeField] private Camera Cam;
            [SerializeField] private RectTransform SelectionBoxUI;

            private TileManager tileManager;
            private UnitManager unitManager;
            private CultureManager cultureManager;

            private MainConfig mainConfig;

            public List<Vector2Int> SelectedTiles = new List<Vector2Int>();
            public List<Unit> SelectedUnits = new List<Unit>();

            private Vector2 DragStartPos;
            private Vector2 DragEndPos;
            private bool IsDragging;
            private bool IsDragThresholdMet;

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

                cultureManager = CoreManager.CultureManager;

                mainConfig = CoreManager.MainConfig;
            }

            void Update()
            {
                bool isOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject() && !IsDragging;

                if (isOverUI) return;

                if (Input.GetMouseButtonDown(0))
                {
                    DragStartPos = Input.mousePosition;

                    IsDragging = true;
                    IsDragThresholdMet = false;

                    HideSelectionBox();
                }

                if (Input.GetMouseButton(0) && IsDragging)
                {
                    DragEndPos = Input.mousePosition;
                    
                    if (!IsDragThresholdMet && Vector2.Distance(DragStartPos, DragEndPos) > mainConfig.SelectionDragThreshold)
                    {
                        IsDragThresholdMet = true;

                        ShowSelectionBox();
                    }

                    if (IsDragThresholdMet)
                    {
                        UpdateSelectionBox();
                    }
                }

                if (Input.GetMouseButtonUp(0))
                {
                    if (IsDragThresholdMet) HandleDrag();
                    else HandleSingleClick();

                    IsDragging = false;
                    IsDragThresholdMet = false;
                    HideSelectionBox();
                }

                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    SelectedUnits.Clear();

                    ClearTiles();
                }
            }

            private void HandleSingleClick()
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

            private void HandleDrag()
            {
                Vector2 worldStart = Cam.ScreenToWorldPoint(DragStartPos);

                Vector2 worldEnd = Cam.ScreenToWorldPoint(DragEndPos);

                float minX = Mathf.Min(worldStart.x, worldEnd.x);
                float maxX = Mathf.Max(worldStart.x, worldEnd.x);

                float minY = Mathf.Min(worldStart.y, worldEnd.y);
                float maxY = Mathf.Max(worldStart.y, worldEnd.y);

                Rect worldRect = new Rect(minX, minY, maxX - minX, maxY - minY);

                bool isShift = Input.GetKey(KeyCode.LeftShift);

                if (!isShift)
                {
                    SelectedUnits.Clear();

                    ClearTiles();
                }

                List<Unit> allUnits = unitManager.GetAllUnits();

                foreach (var unit in allUnits)
                {
                    if (unit == null) continue;

                    if (worldRect.Contains(unit.Position))
                    {
                        var player = cultureManager.GetPlayerForCulture(unit.CultureId);

                        if (player != null && !player.PlayerAI)
                        {
                            if (!SelectedUnits.Contains(unit)) SelectedUnits.Add(unit);
                        }
                    }
                }
            }

            private void DeselectUnits(List<Unit> units)
            {
                foreach (var unit in units)
                {
                    if (SelectedUnits.Contains(unit)) SelectedUnits.Remove(unit);
                }
            }

            private void SelectUnits(List<Unit> units, bool isShift)
            {
                ClearTiles();

                if (!isShift) SelectedUnits.Clear();

                foreach (var unit in units)
                {
                    if (SelectedUnits.Contains(unit) && SelectedUnits.Count > 1) SelectedUnits.Remove(unit);
                    else SelectedUnits.Add(unit);
                }
            }

            private void SelectTile(Vector2 mousePos, bool isShift)
            {
                if (SelectedUnits.Count > 0) SelectedUnits.Clear();

                if (!isShift) ClearTiles();

                Vector2Int tile = tileManager.WorldToTilePos(mousePos);

                if (SelectedTiles.Contains(tile) && SelectedTiles.Count > 1) RemoveTile(tile);
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

            private void ShowSelectionBox()
            {
                if (SelectionBoxUI != null) SelectionBoxUI.gameObject.SetActive(true);
            }

            private void HideSelectionBox()
            {
                if (SelectionBoxUI != null) SelectionBoxUI.gameObject.SetActive(false);
            }

            private void UpdateSelectionBox()
            {
                if (SelectionBoxUI == null) return;

                RectTransform parentRect = SelectionBoxUI.parent as RectTransform;

                if (parentRect == null) return;
                
                Canvas parentCanvas = SelectionBoxUI.GetComponentInParent<Canvas>();

                Camera canvasCam = parentCanvas != null ? parentCanvas.worldCamera : null;
                
                Vector3 worldStart, worldEnd;

                RectTransformUtility.ScreenPointToWorldPointInRectangle(parentRect, DragStartPos, canvasCam, out worldStart);
                RectTransformUtility.ScreenPointToWorldPointInRectangle(parentRect, DragEndPos, canvasCam, out worldEnd);
                
                Vector3 localStart = parentRect.InverseTransformPoint(worldStart);
                Vector3 localEnd = parentRect.InverseTransformPoint(worldEnd);
                
                Vector2 parentBottomLeft = new Vector2(-parentRect.rect.width * parentRect.pivot.x, -parentRect.rect.height * parentRect.pivot.y);
                
                Vector2 relStart = new Vector2(localStart.x - parentBottomLeft.x, localStart.y - parentBottomLeft.y);
                Vector2 relEnd = new Vector2(localEnd.x - parentBottomLeft.x, localEnd.y - parentBottomLeft.y);
                
                Vector2 min = Vector2.Min(relStart, relEnd);
                Vector2 max = Vector2.Max(relStart, relEnd);
                
                SelectionBoxUI.anchorMin = Vector2.zero;
                SelectionBoxUI.anchorMax = Vector2.zero;
                SelectionBoxUI.pivot = Vector2.zero;

                SelectionBoxUI.anchoredPosition = min;
                SelectionBoxUI.sizeDelta = max - min;
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