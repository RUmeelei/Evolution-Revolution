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

            void Start()
            {
                if (Cam == null)
                {
                    Cam = Camera.main;
                }
            }

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
                if (Input.GetKeyDown(KeyCode.R))
                {
                    HandleMobilizationOrder(true);
                }
            
                if (Input.GetKeyDown(KeyCode.T))
                {
                    HandleMobilizationOrder(false);
                }
            
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
            
            private void HandleMobilizationOrder(bool mobilize)
            {
                var units = selectionManager.SelectedUnits;
            
                if (units.Count == 0) return;
            
                foreach (var unit in units)
                {
                    if (unit == null || !unit.IsAlive) continue;
            
                    if (unit is not Human human) continue;
            
                    var player = cultureManager.GetPlayerForCulture(unit.CultureId);
            
                    if (player == null || player.PlayerAI) continue;
            
                    if (mobilize)
                    {
                        unitManager.MobilizeHuman(human);
                    }
                    else
                    {
                        unitManager.DemobilizeHuman(human);
                    }
                }
            }

            private void HandleUnitsOrder(Vector2 mousePos, bool isShift)
            {
                var units = selectionManager.SelectedUnits;

                if (units.Count == 0) return;

                var unitsAtMousePos = unitManager.GetUnitsAtPosition(mousePos, 0.15f);

                foreach (var unit in units)
                {
                    var player = cultureManager.GetPlayerForCulture(unit.CultureId);

                    if (player == null || player.PlayerAI) continue;

                    bool clickedOnOwnUnit = false;

                    Unit enemyTarget = null;
                    
                    foreach (var u in unitsAtMousePos)
                    {
                        if (u.CultureId == unit.CultureId)
                        {
                            clickedOnOwnUnit = true;

                            break;
                        }
                        
                        if (UnitCombatSystem.IsEnemy(unit, u))
                        {
                            enemyTarget = u;

                            break;
                        }
                    }
                    
                    if (clickedOnOwnUnit)
                    {
                        unit.ClearPlayerOrder();

                        unit.ClearAttackTarget();

                        unit.Stop();

                        unit.AIState = UnitAIState.Idle;
                    }
                    else if (enemyTarget != null)
                    {
                        unit.SetAttackOrder(enemyTarget);
                    }
                    else
                    {
                        unit.SetMoveOrder(mousePos);
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