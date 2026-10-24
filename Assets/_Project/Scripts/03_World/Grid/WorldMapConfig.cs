using UnityEngine;

namespace ER.World.Grid
{
    [CreateAssetMenu(fileName = "WorldMapConfig", menuName = "ER/03_World/Config/World Map Config")]
    public class WorldMapConfig : ScriptableObject
    {
        [Header("Global Grid Dimensions")]
        [SerializeField] private int mapWidth = 1000;

        [SerializeField] private int mapHeight = 1000;

        [SerializeField] private float tileWorldSize = 100.0f;


        [Header("Chunk Grid Dimensions")]
        [SerializeField] private int chunkSize = 50;


        public int MapWidth => mapWidth;

        public int MapHeight => mapHeight;

        public float TileWorldSize => tileWorldSize;

        public int TotalTilesCount => mapWidth * mapHeight;

        public int ChunkSize => chunkSize;

        public int ChunksCountX => mapWidth / chunkSize;

        public int ChunksCountY => mapHeight / chunkSize;


        public int CoordsToIndex(int x, int y)
        {
            return y * mapWidth + x;
        }


        public void IndexToCoords(int index, out int x, out int y)
        {
            x = index % mapWidth;

            y = index / mapWidth;
        }


        public bool IsCoordsValid(int x, int y)
        {
            return x >= 0 && x < mapWidth && y >= 0 && y < mapHeight;
        }


        public Vector2 TileToWorldPosition(int x, int y)
        {
            return new Vector2((x + 0.5f) * tileWorldSize, (y + 0.5f) * tileWorldSize);
        }


        public void WorldToTileCoords(float worldX, float worldY, out int tileX, out int tileY)
        {
            tileX = Mathf.FloorToInt(worldX / tileWorldSize);

            tileY = Mathf.FloorToInt(worldY / tileWorldSize);
        }
    }
}