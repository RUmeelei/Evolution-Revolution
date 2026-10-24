using System.Collections.Generic;
using UnityEngine;

namespace ER.World.Audio
{
    [CreateAssetMenu(fileName = "SpatialSoundscapeConfig", menuName = "ER/03_World/Audio/Soundscape Config")]
    public class SpatialSoundscapeConfig : ScriptableObject
    {
        [Header("Audio Variation Arrays")]
        [SerializeField] private List<AudioClip> plainsVariations = new List<AudioClip>();

        [SerializeField] private List<AudioClip> forestVariations = new List<AudioClip>();

        [SerializeField] private List<AudioClip> waterVariations = new List<AudioClip>();

        [SerializeField] private List<AudioClip> windVariations = new List<AudioClip>();

        [SerializeField] private List<AudioClip> winterWindVariations = new List<AudioClip>();


        [Header("Volume Balances")]
        [SerializeField] [Range(0.0f, 1.0f)] private float masterAmbienceVolume = 0.8f;

        [SerializeField] [Range(0.0f, 1.0f)] private float maxBiomeVolume = 0.7f;

        [SerializeField] [Range(0.0f, 1.0f)] private float maxWindVolume = 0.9f;


        [Header("Interpolation Dynamics")]
        [SerializeField] private float volumeFadeSpeed = 1.8f;

        [SerializeField] private float sampleRadiusInTiles = 12.0f;


        [Header("Altitude Attenuation (Meters)")]
        [SerializeField] private float strategicAltitudeThreshold = 3500.0f;


        public IReadOnlyList<AudioClip> PlainsVariations => plainsVariations;

        public IReadOnlyList<AudioClip> ForestVariations => forestVariations;

        public IReadOnlyList<AudioClip> WaterVariations => waterVariations;

        public IReadOnlyList<AudioClip> WindVariations => windVariations;

        public IReadOnlyList<AudioClip> WinterWindVariations => winterWindVariations;

        public float MasterAmbienceVolume => masterAmbienceVolume;

        public float MaxBiomeVolume => maxBiomeVolume;

        public float MaxWindVolume => maxWindVolume;

        public float VolumeFadeSpeed => volumeFadeSpeed;

        public float SampleRadiusInTiles => sampleRadiusInTiles;

        public float StrategicAltitudeThreshold => strategicAltitudeThreshold;


        public AudioClip GetRandomClip(IReadOnlyList<AudioClip> list)
        {
            if (list == null || list.Count == 0)
            {
                return null;
            }

            return list[UnityEngine.Random.Range(0, list.Count)];
        }
    }
}