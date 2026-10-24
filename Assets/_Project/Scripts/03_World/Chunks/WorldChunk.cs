using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using ER.World.Grid;
using ER.World.Visual;
using ER.World.Polity;

namespace ER.World.Chunks
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class WorldChunk : MonoBehaviour
    {
        private MeshFilter meshFilter;

        private MeshRenderer meshRenderer;

        private Mesh chunkMesh;

        private Color32[] vertexColors;

        private Bounds chunkBounds;

        private int originX;

        private int originY;

        private int size;

        private float tileSize;

        private bool isInitialized = false;


        public int OriginX => originX;

        public int OriginY => originY;

        public int Size => size;

        public Bounds ChunkBounds => chunkBounds;


        public void Initialize(int startTileX, int startTileY, int chunkSize, float worldTileSize, Material material)
        {
            originX = startTileX;

            originY = startTileY;

            size = chunkSize;

            tileSize = worldTileSize;

            meshFilter = GetComponent<MeshFilter>();

            meshRenderer = GetComponent<MeshRenderer>();

            meshRenderer.sharedMaterial = material;

            float span = size * tileSize;

            Vector3 center = transform.position + new Vector3(span * 0.5f, span * 0.5f, 0.0f);

            Vector3 boundsSize = new Vector3(span, span, 500.0f);

            chunkBounds = new Bounds(center, boundsSize);

            BuildChunkMesh();

            isInitialized = true;
        }


        public void SetVisible(bool isVisible)
        {
            if (meshRenderer != null && meshRenderer.enabled != isVisible)
            {
                meshRenderer.enabled = isVisible;
            }
        }


        private void BuildChunkMesh()
        {
            chunkMesh = new Mesh
            {
                name = $"ChunkMesh_{originX}_{originY}"
            };

            int totalTiles = size * size;

            int vertexCount = totalTiles * 4;

            int indexCount = totalTiles * 6;

            Vector3[] vertices = new Vector3[vertexCount];

            Vector2[] uvs = new Vector2[vertexCount];

            int[] triangles = new int[indexCount];

            vertexColors = new Color32[vertexCount];

            int vIndex = 0;

            int tIndex = 0;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float x0 = x * tileSize;

                    float y0 = y * tileSize;

                    float x1 = (x + 1) * tileSize;

                    float y1 = (y + 1) * tileSize;

                    vertices[vIndex + 0] = new Vector3(x0, y0, 0.0f);

                    vertices[vIndex + 1] = new Vector3(x1, y0, 0.0f);

                    vertices[vIndex + 2] = new Vector3(x0, y1, 0.0f);

                    vertices[vIndex + 3] = new Vector3(x1, y1, 0.0f);

                    uvs[vIndex + 0] = new Vector2(0.0f, 0.0f);

                    uvs[vIndex + 1] = new Vector2(1.0f, 0.0f);

                    uvs[vIndex + 2] = new Vector2(0.0f, 1.0f);

                    uvs[vIndex + 3] = new Vector2(1.0f, 1.0f);

                    triangles[tIndex + 0] = vIndex + 0;

                    triangles[tIndex + 1] = vIndex + 2;

                    triangles[tIndex + 2] = vIndex + 1;

                    triangles[tIndex + 3] = vIndex + 1;

                    triangles[tIndex + 4] = vIndex + 2;

                    triangles[tIndex + 5] = vIndex + 3;

                    vIndex += 4;

                    tIndex += 6;
                }
            }

            chunkMesh.vertices = vertices;

            chunkMesh.uv = uvs;

            chunkMesh.triangles = triangles;

            chunkMesh.colors32 = vertexColors;

            chunkMesh.RecalculateBounds();

            meshFilter.sharedMesh = chunkMesh;
        }


        public void RefreshChunkColors(
            NativeArray<TileData> tiles,
            WorldMapConfig config,
            TilePaletteConfig palette,
            WorldMapDisplayMode displayMode,
            IReadOnlyDictionary<ushort, PolityData> polities = null)
        {
            if (!isInitialized || !tiles.IsCreated || chunkMesh == null)
            {
                return;
            }

            int vIndex = 0;

            for (int y = 0; y < size; y++)
            {
                int globalY = originY + y;

                for (int x = 0; x < size; x++)
                {
                    int globalX = originX + x;

                    Color32 tileColor = new Color32(40, 40, 40, 255);

                    if (config.IsCoordsValid(globalX, globalY))
                    {
                        int globalIndex = config.CoordsToIndex(globalX, globalY);

                        TileData tile = tiles[globalIndex];

                        tileColor = ResolveTileColor(tile, palette, displayMode, polities);
                    }

                    vertexColors[vIndex + 0] = tileColor;

                    vertexColors[vIndex + 1] = tileColor;

                    vertexColors[vIndex + 2] = tileColor;

                    vertexColors[vIndex + 3] = tileColor;

                    vIndex += 4;
                }
            }

            chunkMesh.colors32 = vertexColors;
        }


        private Color32 ResolveTileColor(
            TileData tile,
            TilePaletteConfig palette,
            WorldMapDisplayMode mode,
            IReadOnlyDictionary<ushort, PolityData> polities)
        {
            if (palette == null)
            {
                return new Color32(80, 80, 80, 255);
            }

            return palette.EvaluateTileColor(tile, mode, polities);
        }
    }
}