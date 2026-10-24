using UnityEngine;
using ER.World.Grid;
using ER.Simulation.Time;

namespace ER.World.Chunks
{
    public class ChunkCullingSystem : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private Camera targetCamera;

        [SerializeField] private ChunkManager chunkManager;

        [SerializeField] private SimulationHotkeysDebugger hotkeysDebugger;


        [Header("Diagnostics")]
        [SerializeField] private int visibleChunksCount = 0;

        [SerializeField] private int totalCulledChunksCount = 0;


        private Bounds cameraFrustumBounds;


        public int VisibleChunksCount => visibleChunksCount;

        public int TotalCulledChunksCount => totalCulledChunksCount;


        private void Start()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (chunkManager == null)
            {
                chunkManager = ChunkManager.Instance;
            }

            if (hotkeysDebugger == null)
            {
                hotkeysDebugger = FindFirstObjectByType<SimulationHotkeysDebugger>();
            }
        }


        private void LateUpdate()
        {
            if (targetCamera == null || chunkManager == null || !chunkManager.IsInitialized)
            {
                return;
            }

            UpdateCameraBounds();

            ExecuteFrustumCull();
        }


        private void UpdateCameraBounds()
        {
            float verticalSize = targetCamera.orthographicSize * 2.0f;

            float horizontalSize = verticalSize * targetCamera.aspect;

            Vector3 cameraPosition = targetCamera.transform.position;

            Vector3 center = new Vector3(cameraPosition.x, cameraPosition.y, 0.0f);

            Vector3 size = new Vector3(horizontalSize + 32.0f, verticalSize + 32.0f, 50.0f);

            cameraFrustumBounds = new Bounds(center, size);
        }


        private void ExecuteFrustumCull()
        {
            WorldChunk[,] chunks = chunkManager.Chunks;

            int countX = chunks.GetLength(0);

            int countY = chunks.GetLength(1);

            int visibleCounter = 0;

            int culledCounter = 0;

            for (int y = 0; y < countY; y++)
            {
                for (int x = 0; x < countX; x++)
                {
                    WorldChunk chunk = chunks[x, y];

                    if (chunk == null)
                    {
                        continue;
                    }

                    bool shouldBeVisible = cameraFrustumBounds.Intersects(chunk.ChunkBounds);

                    chunk.SetVisible(shouldBeVisible);

                    if (shouldBeVisible)
                    {
                        visibleCounter++;
                    }
                    else
                    {
                        culledCounter++;
                    }
                }
            }

            visibleChunksCount = visibleCounter;

            totalCulledChunksCount = culledCounter;
        }


        private void OnGUI()
        {
            if (hotkeysDebugger == null || !hotkeysDebugger.ShowDebugOverlay)
            {
                return;
            }

            GUI.Box(new Rect(12, 185, 260, 65), "Viewport Frustum Culling");

            GUI.Label(new Rect(24, 205, 240, 20), $"Visible Chunks: {visibleChunksCount} / 256");

            GUI.Label(new Rect(24, 225, 240, 20), $"Culled (Sleeping): {totalCulledChunksCount} chunks");
        }
    }
}