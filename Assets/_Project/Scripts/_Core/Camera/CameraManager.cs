using UnityEngine;
using UnityEngine.EventSystems;

namespace ER
{
    namespace Core
    {
        using Configs;
        using World.Tiles;
        using Players;

        public class CameraManager : MonoBehaviour
        {
            public static CameraManager Instance { get; private set; }

            [Header("Main")]
            [SerializeField] private Camera Cam;

            private Vector3 TargetPosition;
            private Vector3 LastMousePosition;

            private float TargetZoom;
            private bool IsDragging = false;
            private float SmoothSpeed;

            private MainConfig mainConfig;
            private TileManager tileManager;
            private PlayerManager playerManager;

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

                CoreManager.RegisterCameraManager(this);
            }

            void Start()
            {
                if (Cam == null)
                {
                    Cam = Camera.main;
                }

                mainConfig = CoreManager.MainConfig;
                tileManager = CoreManager.TileManager;
                playerManager = CoreManager.PlayerManager;

                TargetZoom = Cam.orthographicSize;
                TargetPosition = Cam.transform.position;
                SmoothSpeed = 10f / Mathf.Max(0.1f, mainConfig.CameraSmoothing);

                FocusOnPlayerCapital(true);
            }

            void Update()
            {
                if (Cam == null) return;
                
                if (Input.GetKeyDown(KeyCode.H) || Input.GetKeyDown(KeyCode.Home))
                {
                    FocusOnPlayerCapital(false);
                }

                HandleMovement();
                HandleZoom();
                ClampTargetToWorld();
                HandleSmoothing();
            }

            public void FocusOnPlayerCapital(bool instant = false)
            {
                if (playerManager == null) playerManager = CoreManager.PlayerManager;

                if (tileManager == null) tileManager = CoreManager.TileManager;

                if (playerManager == null || tileManager == null) return;

                var player = playerManager.GetPlayer("PLAYER_0001");
                if (player != null && player.PlayerCulture != null)
                {
                    Vector2 capCenter = tileManager.GetTileCenter(
                        player.PlayerCulture.CultureCapital.x,
                        player.PlayerCulture.CultureCapital.y
                    );
                    TargetPosition = new Vector3(capCenter.x, capCenter.y, -1f);

                    if (instant && Cam != null)
                    {
                        Cam.transform.position = TargetPosition;
                    }
                }
            }

            private void HandleMovement()
            {
                bool pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

                float speedMultiplier = mainConfig.CameraKeyboardScrollSpeed * (Cam.orthographicSize / Mathf.Max(1f, mainConfig.CameraMinHeight));

                if (Input.GetKey(KeyCode.LeftShift))
                {
                    speedMultiplier *= 2f;
                }

                if (!IsDragging && !pointerOverUI && !Input.GetKey(KeyCode.LeftControl))
                {
                    if (Input.GetKey(KeyCode.W)) TargetPosition += Vector3.up * speedMultiplier * Time.unscaledDeltaTime;
                    if (Input.GetKey(KeyCode.S)) TargetPosition += Vector3.down * speedMultiplier * Time.unscaledDeltaTime;
                    if (Input.GetKey(KeyCode.A)) TargetPosition += Vector3.left * speedMultiplier * Time.unscaledDeltaTime;
                    if (Input.GetKey(KeyCode.D)) TargetPosition += Vector3.right * speedMultiplier * Time.unscaledDeltaTime;
                }

                if (Input.GetMouseButtonDown(2) && !pointerOverUI)
                {
                    LastMousePosition = Input.mousePosition;
                    IsDragging = true;
                }

                if (Input.GetMouseButtonUp(2) || pointerOverUI)
                {
                    IsDragging = false;
                }

                if (IsDragging)
                {
                    Vector3 mouseDelta = Input.mousePosition - LastMousePosition;
                    LastMousePosition = Input.mousePosition;

                    float worldUnitsPerPixel = Cam.orthographicSize * 2f / Mathf.Max(1, Cam.pixelHeight);
                    Vector3 worldDelta = new Vector3(mouseDelta.x, mouseDelta.y, 0f) * worldUnitsPerPixel;

                    TargetPosition -= worldDelta;
                }
            }

            private void HandleZoom()
            {
                bool pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                if (!pointerOverUI && Input.mouseScrollDelta.y != 0)
                {
                    float newSize = TargetZoom - Input.mouseScrollDelta.y * mainConfig.CameraZoomSpeed * Time.unscaledDeltaTime;
                    TargetZoom = Mathf.Clamp(newSize, mainConfig.CameraMinHeight, mainConfig.CameraMaxHeight);
                }
            }

            private void ClampTargetToWorld()
            {
                if (tileManager == null) tileManager = CoreManager.TileManager;
                if (tileManager == null || mainConfig == null) return;

                float maxWorldX = tileManager.WorldWidth * mainConfig.TileSize;
                float maxWorldY = tileManager.WorldHeight * mainConfig.TileSize;

                TargetPosition.x = Mathf.Clamp(TargetPosition.x, 0f, maxWorldX);
                TargetPosition.y = Mathf.Clamp(TargetPosition.y, 0f, maxWorldY);
                TargetPosition.z = -1f;
            }

            private void HandleSmoothing()
            {
                if (IsDragging)
                {
                    Cam.transform.position = TargetPosition;
                }
                else
                {
                    Cam.transform.position = Vector3.Lerp(Cam.transform.position, TargetPosition, SmoothSpeed * Time.unscaledDeltaTime);
                }

                Cam.orthographicSize = Mathf.Lerp(Cam.orthographicSize, TargetZoom, SmoothSpeed * Time.unscaledDeltaTime);
            }
        }
    }
}