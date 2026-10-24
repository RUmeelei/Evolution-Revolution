using UnityEngine;
using ER.World.Grid;

namespace ER.Core.Cameras
{
    [RequireComponent(typeof(Camera))]
    public class StrategicCameraController : MonoBehaviour
    {
        public static StrategicCameraController Instance { get; private set; }


        [Header("Configuration")]
        [SerializeField] private WorldMapConfig mapConfig;


        [Header("Zoom Boundaries (Meters)")]
        [SerializeField] private float minZoom = 35.0f;

        [SerializeField] private float maxZoom = 65000.0f;

        [SerializeField] private float zoomSensitivity = 1.25f;

        [SerializeField] private float zoomDamping = 10.0f;


        [Header("Movement Speeds (Relative to Altitude)")]
        [SerializeField] private float basePanSpeed = 1.2f;

        [SerializeField] private float panDamping = 12.0f;

        [SerializeField] private float fastMoveMultiplier = 2.5f;


        private Camera targetCamera;

        private float targetZoomLevel;

        private Vector3 targetPosition;

        private bool isDragging = false;

        private Vector3 dragOriginMousePosition;

        private Vector3 dragOriginWorldPosition;


        public Camera TargetCamera => targetCamera;

        public float CurrentAltitude => targetCamera != null ? (targetCamera.orthographic ? targetCamera.orthographicSize : Mathf.Abs(transform.position.z)) : 100.0f;

        public float TargetZoomLevel => targetZoomLevel;


        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            targetCamera = GetComponent<Camera>();

            ConfigureCameraClippingPlanes();
        }


        private void Start()
        {
            if (mapConfig == null && WorldGridManager.Instance != null)
            {
                mapConfig = WorldGridManager.Instance.MapConfig;
            }

            InitializeCameraPosition();
        }


        private void ConfigureCameraClippingPlanes()
        {
            targetCamera.nearClipPlane = 0.1f;

            targetCamera.farClipPlane = 200000.0f;
        }


        private void InitializeCameraPosition()
        {
            targetPosition = transform.position;

            if (targetCamera.orthographic)
            {
                targetZoomLevel = Mathf.Clamp(targetCamera.orthographicSize, minZoom, maxZoom);

                targetCamera.orthographicSize = targetZoomLevel;
            }
            else
            {
                targetZoomLevel = Mathf.Clamp(Mathf.Abs(transform.position.z), minZoom, maxZoom);

                targetPosition.z = -targetZoomLevel;

                transform.position = targetPosition;
            }
        }


        private void Update()
        {
            HandleZoomInput();

            HandlePanInput();

            HandleMouseDragInput();

            ApplySmoothTransformation();
        }


        private void HandleZoomInput()
        {
            float scroll = Input.GetAxisRaw("Mouse ScrollWheel");

            if (Mathf.Abs(scroll) > 0.001f)
            {
                float zoomFactor = 1.0f - scroll * zoomSensitivity;

                targetZoomLevel = Mathf.Clamp(targetZoomLevel * zoomFactor, minZoom, maxZoom);
            }
        }


        private void HandlePanInput()
        {
            float moveX = Input.GetAxisRaw("Horizontal");

            float moveY = Input.GetAxisRaw("Vertical");

            if (Mathf.Abs(moveX) < 0.001f && Mathf.Abs(moveY) < 0.001f)
            {
                return;
            }

            float speedMultiplier = Input.GetKey(KeyCode.LeftShift) ? fastMoveMultiplier : 1.0f;

            float currentAlt = CurrentAltitude;

            float dynamicSpeed = currentAlt * basePanSpeed * speedMultiplier;

            Vector3 moveDirection = new Vector3(moveX, moveY, 0.0f).normalized;

            targetPosition += moveDirection * (dynamicSpeed * Time.unscaledDeltaTime);
        }


        private void HandleMouseDragInput()
        {
            if (Input.GetMouseButtonDown(2))
            {
                isDragging = true;

                dragOriginMousePosition = Input.mousePosition;

                dragOriginWorldPosition = targetPosition;
            }
            else if (Input.GetMouseButtonUp(2))
            {
                isDragging = false;
            }

            if (isDragging && Input.GetMouseButton(2))
            {
                float currentAlt = CurrentAltitude;

                Vector3 mouseDelta = Input.mousePosition - dragOriginMousePosition;

                float factor = (currentAlt * 2.0f) / Screen.height;

                targetPosition = dragOriginWorldPosition - new Vector3(mouseDelta.x * factor, mouseDelta.y * factor, 0.0f);
            }
        }


        private void ApplySmoothTransformation()
        {
            ClampTargetPositionWithinBounds();

            float deltaTime = Time.unscaledDeltaTime;

            if (targetCamera.orthographic)
            {
                targetCamera.orthographicSize = Mathf.Lerp(targetCamera.orthographicSize, targetZoomLevel, deltaTime * zoomDamping);

                Vector3 currentPos = transform.position;

                Vector3 newPos = Vector3.Lerp(currentPos, new Vector3(targetPosition.x, targetPosition.y, currentPos.z), deltaTime * panDamping);

                transform.position = newPos;
            }
            else
            {
                targetPosition.z = -targetZoomLevel;

                transform.position = Vector3.Lerp(transform.position, targetPosition, deltaTime * panDamping);
            }
        }


        private void ClampTargetPositionWithinBounds()
        {
            if (mapConfig == null)
            {
                return;
            }

            float mapLimitX = mapConfig.MapWidth * mapConfig.TileWorldSize;

            float mapLimitY = mapConfig.MapHeight * mapConfig.TileWorldSize;

            targetPosition.x = Mathf.Clamp(targetPosition.x, 0.0f, mapLimitX);

            targetPosition.y = Mathf.Clamp(targetPosition.y, 0.0f, mapLimitY);
        }


        public void FocusOnCoordinates(float worldX, float worldY, float desiredZoom = 120.0f)
        {
            targetPosition.x = worldX;

            targetPosition.y = worldY;

            targetZoomLevel = Mathf.Clamp(desiredZoom, minZoom, maxZoom);
        }
    }
}