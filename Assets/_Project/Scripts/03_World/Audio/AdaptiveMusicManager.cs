using System.Collections.Generic;
using UnityEngine;

namespace ER.World.Audio
{
    public class AdaptiveMusicManager : MonoBehaviour
    {
        public static AdaptiveMusicManager Instance { get; private set; }


        [Header("Configuration Asset")]
        [SerializeField] private AdaptiveMusicConfig musicConfig;


        [Header("Dynamic State")]
        [SerializeField] private MusicMood currentMood = MusicMood.Peace;

        [SerializeField] [Range(0.0f, 1.0f)] private float tensionLevel = 0.0f;

        [SerializeField] [Range(0.0f, 1.0f)] private float warThreatLevel = 0.0f;


        private AudioSource sourceA;

        private AudioSource sourceB;

        private bool isSourceAActive = true;


        private float crossfadeTimer = 0.0f;

        private bool isCrossfading = false;


        private int lastPlayedClipIndex = -1;

        private AudioClip activeClip;


        public MusicMood CurrentMood => currentMood;

        public float TensionLevel => tensionLevel;

        public float WarThreatLevel => warThreatLevel;


        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            InitializeDualChannels();
        }


        private void Start()
        {
            EvaluateMoodState();

            PlayNextTrack(true);
        }


        private void InitializeDualChannels()
        {
            sourceA = CreateAudioChannel("MusicChannel_A");

            sourceB = CreateAudioChannel("MusicChannel_B");
        }


        private AudioSource CreateAudioChannel(string channelName)
        {
            GameObject channelObj = new GameObject(channelName);

            channelObj.transform.SetParent(transform);

            AudioSource source = channelObj.AddComponent<AudioSource>();

            source.loop = false;

            source.playOnAwake = false;

            source.volume = 0.0f;

            source.spatialBlend = 0.0f;

            return source;
        }


        private void Update()
        {
            if (musicConfig == null)
            {
                return;
            }

            HandleMoodEvaluation();

            HandleCrossfadeProgression();

            HandleTrackEndDetection();
        }


        private void HandleMoodEvaluation()
        {
            MusicMood evaluatedMood = EvaluateMoodState();

            if (evaluatedMood != currentMood)
            {
                currentMood = evaluatedMood;

                PlayNextTrack(false);
            }
        }


        private MusicMood EvaluateMoodState()
        {
            if (warThreatLevel >= 0.5f)
            {
                return MusicMood.War;
            }

            if (tensionLevel >= 0.4f)
            {
                return MusicMood.Tension;
            }

            return MusicMood.Peace;
        }


        private void HandleCrossfadeProgression()
        {
            if (!isCrossfading)
            {
                return;
            }

            crossfadeTimer += UnityEngine.Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(crossfadeTimer / Mathf.Max(musicConfig.CrossfadeDuration, 0.1f));

            float master = musicConfig.MasterMusicVolume;

            AudioSource activeSource = isSourceAActive ? sourceA : sourceB;

            AudioSource fadingSource = isSourceAActive ? sourceB : sourceA;

            activeSource.volume = progress * master;

            fadingSource.volume = (1.0f - progress) * master;

            if (progress >= 1.0f)
            {
                fadingSource.Stop();

                fadingSource.clip = null;

                isCrossfading = false;

                crossfadeTimer = 0.0f;
            }
        }


        private void HandleTrackEndDetection()
        {
            if (isCrossfading)
            {
                return;
            }

            AudioSource activeSource = isSourceAActive ? sourceA : sourceB;

            if (activeSource.clip != null && !activeSource.isPlaying && activeSource.time >= (activeSource.clip.length - 0.2f))
            {
                PlayNextTrack(false);
            }
        }


        public void PlayNextTrack(bool immediate)
        {
            if (musicConfig == null)
            {
                return;
            }

            IReadOnlyList<AudioClip> playlist = musicConfig.GetPlaylistByMood(currentMood);

            if (playlist == null || playlist.Count == 0)
            {
                playlist = musicConfig.PeaceTracks;
            }

            if (playlist == null || playlist.Count == 0)
            {
                return;
            }

            AudioClip nextClip = SelectNextClip(playlist);

            if (nextClip == null)
            {
                return;
            }

            if (immediate)
            {
                sourceA.Stop();

                sourceB.Stop();

                sourceA.clip = nextClip;

                sourceA.volume = musicConfig.MasterMusicVolume;

                sourceA.Play();

                isSourceAActive = true;

                isCrossfading = false;

                activeClip = nextClip;
            }
            else
            {
                AudioSource incomingSource = isSourceAActive ? sourceB : sourceA;

                incomingSource.Stop();

                incomingSource.clip = nextClip;

                incomingSource.volume = 0.0f;

                incomingSource.Play();

                isSourceAActive = !isSourceAActive;

                isCrossfading = true;

                crossfadeTimer = 0.0f;

                activeClip = nextClip;
            }
        }


        private AudioClip SelectNextClip(IReadOnlyList<AudioClip> playlist)
        {
            if (playlist.Count == 1)
            {
                lastPlayedClipIndex = 0;

                return playlist[0];
            }

            int candidateIndex;

            int attempts = 0;

            do
            {
                candidateIndex = Random.Range(0, playlist.Count);

                attempts++;
            }
            while (candidateIndex == lastPlayedClipIndex && attempts < 10);

            lastPlayedClipIndex = candidateIndex;

            return playlist[candidateIndex];
        }


        public void SetTensionLevel(float value)
        {
            tensionLevel = Mathf.Clamp01(value);
        }


        public void SetWarThreatLevel(float value)
        {
            warThreatLevel = Mathf.Clamp01(value);
        }
    }
}