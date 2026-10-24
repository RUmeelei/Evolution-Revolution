using System;
using System.Collections.Generic;
using UnityEngine;
using ER.World.Grid;
using ER.World.Visual;
using ER.World.Polity;

namespace ER.World.Chunks
{
    [CreateAssetMenu(fileName = "TilePaletteConfig", menuName = "ER/03_World/Visual/Palette Config")]
    public class TilePaletteConfig : ScriptableObject
    {
        [Header("Terrain Base Colors")]
        [SerializeField] private Color32 plainsColor = new Color32(95, 145, 65, 255);

        [SerializeField] private Color32 hillsColor = new Color32(140, 130, 80, 255);

        [SerializeField] private Color32 mountainsColor = new Color32(130, 135, 140, 255);

        [SerializeField] private Color32 waterShallowColor = new Color32(65, 135, 175, 255);

        [SerializeField] private Color32 waterDeepColor = new Color32(35, 80, 135, 255);

        [SerializeField] private Color32 swampColor = new Color32(75, 95, 65, 255);


        [Header("Biome Modifiers")]
        [SerializeField] private Color32 forestTint = new Color32(40, 85, 40, 255);

        [SerializeField] private Color32 taigaTint = new Color32(50, 75, 65, 255);

        [SerializeField] private Color32 steppeTint = new Color32(165, 150, 85, 255);

        [SerializeField] private Color32 desertTint = new Color32(210, 180, 110, 255);

        [SerializeField] private Color32 tundraTint = new Color32(130, 140, 125, 255);


        [Header("Political Overlay Defaults")]
        [SerializeField] private Color32 neutralWildernessColor = new Color32(55, 60, 65, 255);

        [SerializeField] private Color32 contestedTerritoryColor = new Color32(145, 60, 60, 255);


        [Header("Feature Overlays")]
        [SerializeField] private Color32 iceOverlayColor = new Color32(190, 220, 240, 255);

        [SerializeField] private Color32 roadOverlayColor = new Color32(110, 95, 75, 255);


        public Color32 EvaluateTileColor(
            TileData tile,
            WorldMapDisplayMode displayMode = WorldMapDisplayMode.Biomes,
            IReadOnlyDictionary<ushort, PolityData> polities = null)
        {
            if (displayMode == WorldMapDisplayMode.PoliticalDeFacto || displayMode == WorldMapDisplayMode.PoliticalDeJure)
            {
                if (tile.Terrain == TerrainType.Mountains || tile.GeoRegionId == 0)
                {
                    return GetMountainRidgeShade(tile.Elevation);
                }

                if (tile.Terrain == TerrainType.WaterDeep)
                {
                    return waterDeepColor;
                }

                ushort polityId = displayMode == WorldMapDisplayMode.PoliticalDeFacto ? tile.DeFactoPolityId : tile.DeJurePolityId;

                if (polityId == 0 || polities == null || !polities.TryGetValue(polityId, out PolityData polity))
                {
                    byte shade = (byte)(tile.Elevation / 10);

                    return new Color32((byte)(neutralWildernessColor.r + shade), (byte)(neutralWildernessColor.g + shade), (byte)(neutralWildernessColor.b + shade), 255);
                }

                Color32 polColor = polity.MapColor;

                byte elevShade = (byte)(tile.Elevation / 8);

                polColor.r = (byte)Mathf.Clamp(polColor.r + elevShade, 0, 255);

                polColor.g = (byte)Mathf.Clamp(polColor.g + elevShade, 0, 255);

                polColor.b = (byte)Mathf.Clamp(polColor.b + elevShade, 0, 255);

                return polColor;
            }

            if (displayMode == WorldMapDisplayMode.GeoRegions)
            {
                if (tile.Terrain == TerrainType.Mountains || tile.GeoRegionId == 0)
                {
                    return GetMountainRidgeShade(tile.Elevation);
                }

                return GenerateRegionColor(tile.GeoRegionId, tile.Elevation);
            }

            if (tile.HasFlag(TileData.FlagIsFrozen))
            {
                return iceOverlayColor;
            }

            if (tile.HasFlag(TileData.FlagHasRoad))
            {
                return roadOverlayColor;
            }

            Color32 baseColor = GetTerrainBaseColor(tile.Terrain);

            if (tile.Terrain == TerrainType.Plains || tile.Terrain == TerrainType.Hills)
            {
                baseColor = BlendBiomeColor(baseColor, tile.Biome);
            }

            byte elevationShade = (byte)Mathf.Clamp(tile.Elevation / 8, 0, 30);

            baseColor.r = (byte)Mathf.Clamp(baseColor.r + elevationShade, 0, 255);

            baseColor.g = (byte)Mathf.Clamp(baseColor.g + elevationShade, 0, 255);

            baseColor.b = (byte)Mathf.Clamp(baseColor.b + elevationShade, 0, 255);

            return baseColor;
        }


        private Color32 GetMountainRidgeShade(byte elevation)
        {
            byte shade = (byte)(elevation / 7);

            byte r = (byte)Mathf.Clamp(38 + shade, 0, 255);

            byte g = (byte)Mathf.Clamp(42 + shade, 0, 255);

            byte b = (byte)Mathf.Clamp(48 + shade, 0, 255);

            return new Color32(r, g, b, 255);
        }


        private Color32 GenerateRegionColor(ushort regionId, byte elevation)
        {
            int hash = (regionId * 1664525) + 1013904223;

            if (regionId > 180)
            {
                byte oceanBlue = (byte)(110 + ((hash >> 8) & 0x3F));

                byte oceanGreen = (byte)(60 + ((hash >> 16) & 0x2F));

                return new Color32(20, oceanGreen, oceanBlue, 255);
            }

            byte r = (byte)(80 + (hash & 0x6F));

            byte g = (byte)(80 + ((hash >> 8) & 0x6F));

            byte b = (byte)(70 + ((hash >> 16) & 0x5F));

            byte shade = (byte)(elevation / 9);

            r = (byte)Mathf.Clamp(r + shade, 0, 255);

            g = (byte)Mathf.Clamp(g + shade, 0, 255);

            b = (byte)Mathf.Clamp(b + shade, 0, 255);

            return new Color32(r, g, b, 255);
        }


        private Color32 GetTerrainBaseColor(TerrainType terrain)
        {
            switch (terrain)
            {
                case TerrainType.Plains:
                    return plainsColor;

                case TerrainType.Hills:
                    return hillsColor;

                case TerrainType.Mountains:
                    return mountainsColor;

                case TerrainType.WaterShallow:
                    return waterShallowColor;

                case TerrainType.WaterDeep:
                    return waterDeepColor;

                case TerrainType.Swamp:
                    return swampColor;

                default:
                    return plainsColor;
            }
        }


        private Color32 BlendBiomeColor(Color32 baseColor, BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.DenseForest:
                    return Color32.Lerp(baseColor, forestTint, 0.45f);

                case BiomeType.Taiga:
                    return Color32.Lerp(baseColor, taigaTint, 0.45f);

                case BiomeType.Steppe:
                    return Color32.Lerp(baseColor, steppeTint, 0.40f);

                case BiomeType.Desert:
                    return Color32.Lerp(baseColor, desertTint, 0.60f);

                case BiomeType.Tundra:
                    return Color32.Lerp(baseColor, tundraTint, 0.50f);

                default:
                    return baseColor;
            }
        }
    }
}