using System.Collections.Generic;
using UnityEngine;
using ER.World.Grid;
using ER.World.Visual;
using ER.World.Polity;

namespace ER.World.Chunks
{
    public class ChunkManager : MonoBehaviour
    {
        public static ChunkManager Instance { get; private set; }


        [Header("Configuration References")]
        [SerializeField] private WorldGridManager gridManager;

        [SerializeField] private TilePaletteConfig paletteConfig;

        [SerializeField] private Material chunkMaterial;


        private WorldChunk[,] chunks;

        private bool isInitialized = false;


        public bool IsInitialized => isInitialized;

        public WorldChunk[,] Chunks => chunks;


        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }


        private void Start()
        {
            EnsureInitialized();
        }


        public void EnsureInitialized()
        {
            if (isInitialized)
            {
                return;
            }

            if (gridManager == null)
            {
                gridManager = WorldGridManager.Instance;
            }

            if (gridManager != null && !gridManager.IsInitialized)
            {
                gridManager.InitializeGrid();
            }

            if (gridManager != null && gridManager.IsInitialized)
            {
                InitializeChunkGrid();
            }
        }


        public void InitializeChunkGrid()
        {
            if (isInitialized)
            {
                return;
            }

            WorldMapConfig config = gridManager.MapConfig;

            int countX = config.ChunksCountX;

            int countY = config.ChunksCountY;

            int chunkSize = config.ChunkSize;

            float tileSize = config.TileWorldSize;

            chunks = new WorldChunk[countX, countY];

            if (chunkMaterial == null)
            {
                Shader unlitColorShader = Shader.Find("Sprites/Default");

                if (unlitColorShader == null) unlitColorShader = Shader.Find("UI/Default");

                chunkMaterial = new Material(unlitColorShader);
            }

            for (int cy = 0; cy < countY; cy++)
            {
                for (int cx = 0; cx < countX; cx++)
                {
                    GameObject chunkObj = new GameObject($"Chunk_{cx}_{cy}");

                    chunkObj.transform.SetParent(transform);

                    float posX = cx * chunkSize * tileSize;

                    float posY = cy * chunkSize * tileSize;

                    chunkObj.transform.position = new Vector3(posX, posY, 0.0f);

                    WorldChunk chunk = chunkObj.AddComponent<WorldChunk>();

                    chunk.Initialize(cx * chunkSize, cy * chunkSize, chunkSize, tileSize, chunkMaterial);

                    chunk.RefreshChunkColors(gridManager.RawTiles, config, paletteConfig, WorldMapDisplayMode.Biomes);

                    chunks[cx, cy] = chunk;
                }
            }

            isInitialized = true;

            Debug.Log($"[ChunkManager] Successfully created {countX * countY} chunks spanning {countX * chunkSize * tileSize:F0}x{countY * chunkSize * tileSize:F0} world meters.");
        }


        public bool TryGetChunkAtWorldPos(float worldX, float worldY, out WorldChunk chunk)
        {
            if (!isInitialized || gridManager == null)
            {
                chunk = null;

                return false;
            }

            WorldMapConfig config = gridManager.MapConfig;

            float chunkWorldSpan = config.ChunkSize * config.TileWorldSize;

            int cx = Mathf.FloorToInt(worldX / chunkWorldSpan);

            int cy = Mathf.FloorToInt(worldY / chunkWorldSpan);

            if (cx >= 0 && cx < config.ChunksCountX && cy >= 0 && cy < config.ChunksCountY)
            {
                chunk = chunks[cx, cy];

                return true;
            }

            chunk = null;

            return false;
        }
    }
}