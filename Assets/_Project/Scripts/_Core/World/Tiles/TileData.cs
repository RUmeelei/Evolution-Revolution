using UnityEngine;

namespace ER
{
    namespace World
    {
        namespace Tiles
        {
            [System.Serializable]
            public struct TileData
            {
                public TileType Type; 

                public float Elevation;

                public int Variation;
            }

            public enum TileType
            {
                None = 0,

                Water = 10,
                Grass = 11,
                Sand = 12,
                Rock = 13,
            }
        }
    }
}