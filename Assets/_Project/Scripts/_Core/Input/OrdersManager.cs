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

        public class OrdersManager : MonoBehaviour
        {
            public static OrdersManager Instance {get; private set;}

            [Header("Main")]
            [SerializeField] private Camera Cam;

            private CultureManager cultureManager;
            private TileManager tileManager;
            private UnitManager unitManager;
            private SelectionManager selectionManager;

            private MainConfig mainConfig;

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

                CoreManager.RegisterOrdersManager(this);
            }

            public void Initialize()
            {
                cultureManager = CoreManager.CultureManager;

                tileManager = CoreManager.TileManager;

                unitManager = CoreManager.UnitManager;

                selectionManager = CoreManager.SelectionManager;

                mainConfig = CoreManager.MainConfig;
            }

            public void Update()
            {
                bool isOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

                if (isOverUI) return;

                if (Input.GetMouseButtonUp(1))
                {
                    Vector2 mousePos = Cam.ScreenToWorldPoint(Input.mousePosition);

                    bool isShift = Input.GetKey(KeyCode.LeftShift);

                    if (!IsInsideWorld(mousePos)) return;

                    HandleUnitsOrder(mousePos, isShift);
                }
            }

            private void HandleUnitsOrder(Vector2 mousePos, bool isShift)
            {
                var units = selectionManager.SelectedUnits;

                if (units.Count > 0)
                {
                    foreach (var unit in units)
                    {
                        var player = cultureManager.GetPlayerForCulture(unit.CultureId);

                        if (player == null || player.PlayerAI) continue;

                        unit.SetTarget(mousePos);
                    }
                }
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