using UnityEngine;
using UnityEngine.EventSystems;

namespace ER
{
    namespace Core
    {
        using Configs;

        public class CameraManager : MonoBehaviour
        {
            public static CameraManager Instance {get; private set;}

            [Header("Main")]
            [SerializeField] private Camera Cam;

            private Vector3 TargetPosition;
            private Vector3 LastMousePosition;

            private float TargetZoom;

            private bool IsDragging = false;

            private float SmoothSpeed;

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

                CoreManager.RegisterCameraManager(this);
            }

            void Start()
            {
                mainConfig = CoreManager.MainConfig;

                TargetZoom = Cam.orthographicSize;
                TargetPosition = Cam.transform.position;
                SmoothSpeed = 10f / mainConfig.CameraSmoothing;
            }
    
            void Update()
            {
                HandleMovement();
                HandleZoom();
                HandleSmoothing();
            }

            private void HandleMovement()
            {
                float speedMultiplier = mainConfig.CameraKeyboardScrollSpeed * (Cam.orthographicSize / mainConfig.CameraMinHeight);

                if (Input.GetKey(KeyCode.LeftShift))
                {
                    speedMultiplier = speedMultiplier * 2;
                }

                if (Input.GetKey(KeyCode.W) && !IsDragging && !EventSystem.current.IsPointerOverGameObject()) TargetPosition += Vector3.up * speedMultiplier * Time.deltaTime;
                if (Input.GetKey(KeyCode.S) && !IsDragging && !EventSystem.current.IsPointerOverGameObject()) TargetPosition += Vector3.down * speedMultiplier * Time.deltaTime;
                if (Input.GetKey(KeyCode.A) && !IsDragging && !EventSystem.current.IsPointerOverGameObject()) TargetPosition += Vector3.left * speedMultiplier * Time.deltaTime;
                if (Input.GetKey(KeyCode.D) && !IsDragging && !EventSystem.current.IsPointerOverGameObject()) TargetPosition += Vector3.right * speedMultiplier * Time.deltaTime;

                if (Input.GetMouseButtonDown(2) && !EventSystem.current.IsPointerOverGameObject())
                {
                    LastMousePosition = Input.mousePosition;
                    IsDragging = true;
                }
                if (Input.GetMouseButtonUp(2) || EventSystem.current.IsPointerOverGameObject())
                {
                    IsDragging = false;
                }

                if (IsDragging)
                {
                    Vector3 mouseDelta = Input.mousePosition - LastMousePosition;
                    LastMousePosition = Input.mousePosition;

                    float worldUnitsPerPixel = Cam.orthographicSize * 2f / Cam.pixelHeight;
                    Vector3 worldDelta = new Vector3(mouseDelta.x, mouseDelta.y, 0f) * worldUnitsPerPixel;

                    TargetPosition -= worldDelta;
                }
            }

            private void HandleZoom()
            {
                if (Input.mouseScrollDelta.y != 0)
                {
                    float newSize = TargetZoom - Input.mouseScrollDelta.y * mainConfig.CameraZoomSpeed * Time.deltaTime;

                    TargetZoom = Mathf.Clamp(newSize, mainConfig.CameraMinHeight, mainConfig.CameraMaxHeight);
                }
            }

            private void HandleSmoothing()
            {
                TargetPosition.z = -1f;

                if (IsDragging) 
                {
                    Cam.transform.position = TargetPosition;
                } 
                else
                {
                    Cam.transform.position = Vector3.Lerp(Cam.transform.position, TargetPosition, SmoothSpeed * Time.deltaTime);
                }

                Cam.orthographicSize = Mathf.Lerp(Cam.orthographicSize, TargetZoom, SmoothSpeed * Time.deltaTime);
            }
        }
    }
}