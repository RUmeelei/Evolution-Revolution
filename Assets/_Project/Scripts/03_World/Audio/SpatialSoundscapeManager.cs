using UnityEngine;
using ER.World.Grid;
using ER.Core.Cameras;
using ER.Simulation.Time;

namespace ER.World.Audio
{
    public class SpatialSoundscapeManager : MonoBehaviour
    {
        public static SpatialSoundscapeManager Instance { get; private set; }


        [Header("Configuration")]
        [SerializeField] private SpatialSoundscapeConfig soundConfig;


        [Header("System References")]
        [SerializeField] private StrategicCameraController cameraController;

        [SerializeField] private WorldGridManager gridManager;

        [SerializeField] private SimulationClockManager clockManager;


        private AudioSource plainsSource;

        private AudioSource forestSource;

        private AudioSource waterSource;

        private AudioSource windSource;


        private float targetPlainsVolume = 0.0f;

        private float targetForestVolume = 0.0f;

        private float targetWaterVolume = 0.0f;

        private float targetWindVolume = 0.0f;


        private float analysisTimer = 0.0f;

        private const float AnalysisInterval = 0.25f;

        private bool wasWinterActive = false;


        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            InitializeAudioSources();
        }


        private void Start()
        {
            if (cameraController == null)
            {
                cameraController = StrategicCameraController.Instance != null 
                    ? StrategicCameraController.Instance 
                    : FindFirstObjectByType<StrategicCameraController>();
            }

            if (gridManager == null)
            {
                gridManager = WorldGridManager.Instance;
            }

            if (clockManager == null)
            {
                clockManager = SimulationClockManager.Instance;
            }
        }


        private void InitializeAudioSources()
        {
            plainsSource = CreateChannel("Ambience_Plains");

            forestSource = CreateChannel("Ambience_Forest");

            waterSource = CreateChannel("Ambience_Water");

            windSource = CreateChannel("Ambience_Wind");

            if (soundConfig == null)
            {
                return;
            }

            AssignClipToChannel(plainsSource, soundConfig.GetRandomClip(soundConfig.PlainsVariations));

            AssignClipToChannel(forestSource, soundConfig.GetRandomClip(soundConfig.ForestVariations));

            AssignClipToChannel(waterSource, soundConfig.GetRandomClip(soundConfig.WaterVariations));

            AssignClipToChannel(windSource, soundConfig.GetRandomClip(soundConfig.WindVariations));
        }


        private AudioSource CreateChannel(string channelName)
        {
            GameObject channelObj = new GameObject(channelName);

            channelObj.transform.SetParent(transform);

            AudioSource source = channelObj.AddComponent<AudioSource>();

            source.loop = true;

            source.playOnAwake = false;

            source.volume = 0.0f;

            source.spatialBlend = 0.0f;

            return source;
        }


        private void AssignClipToChannel(AudioSource source, AudioClip clip)
        {
            if (clip != null)
            {
                source.clip = clip;

                source.Play();
            }
        }


        private void Update()
        {
            if (soundConfig == null || cameraController == null || gridManager == null || !gridManager.IsInitialized)
            {
                return;
            }

            analysisTimer += UnityEngine.Time.unscaledDeltaTime;

            if (analysisTimer >= AnalysisInterval)
            {
                AnalyzeViewportLandscape();

                analysisTimer = 0.0f;
            }

            InterpolateAudioVolumes();
        }


        private void AnalyzeViewportLandscape()
        {
            Vector3 cameraPosition = cameraController.transform.position;

            WorldMapConfig mapConfig = gridManager.MapConfig;

            int centerTileX = Mathf.RoundToInt(cameraPosition.x / mapConfig.TileWorldSize);

            int centerTileY = Mathf.RoundToInt(cameraPosition.y / mapConfig.TileWorldSize);

            int radius = Mathf.RoundToInt(soundConfig.SampleRadiusInTiles);

            int waterCount = 0;

            int forestCount = 0;

            int plainsCount = 0;

            int mountainCount = 0;

            int totalSamples = 0;

            for (int dy = -radius; dy <= radius; dy += 2)
            {
                for (int dx = -radius; dx <= radius; dx += 2)
                {
                    int sampleX = centerTileX + dx;

                    int sampleY = centerTileY + dy;

                    if (!mapConfig.IsCoordsValid(sampleX, sampleY))
                    {
                        continue;
                    }

                    TileData tile = gridManager.GetTile(sampleX, sampleY);

                    totalSamples++;

                    if (tile.Terrain == TerrainType.WaterShallow || tile.Terrain == TerrainType.WaterDeep)
                    {
                        waterCount++;
                    }
                    else if (tile.Terrain == TerrainType.Mountains)
                    {
                        mountainCount++;
                    }
                    else if (tile.Biome == BiomeType.DenseForest || tile.Biome == BiomeType.Taiga)
                    {
                        forestCount++;
                    }
                    else
                    {
                        plainsCount++;
                    }
                }
            }

            if (totalSamples == 0)
            {
                return;
            }

            float waterFraction = (float)waterCount / totalSamples;

            float forestFraction = (float)forestCount / totalSamples;

            float plainsFraction = (float)plainsCount / totalSamples;

            float mountainFraction = (float)mountainCount / totalSamples;

            float altitude = cameraController.CurrentAltitude;

            float zoomRatio = Mathf.Clamp01(altitude / soundConfig.StrategicAltitudeThreshold);

            bool isWinter = clockManager != null && clockManager.CurrentTime.CurrentSeason == Season.Winter;

            CheckWinterAudioTransition(isWinter);

            float groundFade = 1.0f - (zoomRatio * 0.85f);

            targetWaterVolume = waterFraction * soundConfig.MaxBiomeVolume * groundFade;

            targetForestVolume = forestFraction * soundConfig.MaxBiomeVolume * groundFade;

            targetPlainsVolume = plainsFraction * soundConfig.MaxBiomeVolume * groundFade;

            float altitudeWind = mountainFraction * 0.5f;

            float zoomWind = zoomRatio * 0.75f;

            targetWindVolume = Mathf.Clamp01(altitudeWind + zoomWind) * soundConfig.MaxWindVolume;

            if (isWinter)
            {
                targetForestVolume *= 0.2f;

                targetWaterVolume *= 0.3f;

                targetWindVolume = Mathf.Clamp01(targetWindVolume * 1.35f);
            }

            float master = soundConfig.MasterAmbienceVolume;

            targetWaterVolume *= master;

            targetForestVolume *= master;

            targetPlainsVolume *= master;

            targetWindVolume *= master;
        }


        private void CheckWinterAudioTransition(bool isWinter)
        {
            if (isWinter == wasWinterActive)
            {
                return;
            }

            wasWinterActive = isWinter;

            if (isWinter && soundConfig.WinterWindVariations.Count > 0)
            {
                AssignClipToChannel(windSource, soundConfig.GetRandomClip(soundConfig.WinterWindVariations));
            }
            else if (!isWinter && soundConfig.WindVariations.Count > 0)
            {
                AssignClipToChannel(windSource, soundConfig.GetRandomClip(soundConfig.WindVariations));
            }
        }


        private void InterpolateAudioVolumes()
        {
            float step = soundConfig.VolumeFadeSpeed * UnityEngine.Time.unscaledDeltaTime;

            plainsSource.volume = Mathf.MoveTowards(plainsSource.volume, targetPlainsVolume, step);

            forestSource.volume = Mathf.MoveTowards(forestSource.volume, targetForestVolume, step);

            waterSource.volume = Mathf.MoveTowards(waterSource.volume, targetWaterVolume, step);

            windSource.volume = Mathf.MoveTowards(windSource.volume, targetWindVolume, step);
        }
    }
}