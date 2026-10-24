using System.Collections.Generic;
using UnityEngine;

namespace ER.World.Audio
{
    [CreateAssetMenu(fileName = "AdaptiveMusicConfig", menuName = "ER/03_World/Audio/Adaptive Music Config")]
    public class AdaptiveMusicConfig : ScriptableObject
    {
        [Header("Mood Playlists")]
        [SerializeField] private List<AudioClip> peaceTracks = new List<AudioClip>();

        [SerializeField] private List<AudioClip> tensionTracks = new List<AudioClip>();

        [SerializeField] private List<AudioClip> warTracks = new List<AudioClip>();


        [Header("Crossfade and Volume")]
        [SerializeField] [Range(0.0f, 1.0f)] private float masterMusicVolume = 0.60f;

        [SerializeField] private float crossfadeDuration = 3.0f;


        public IReadOnlyList<AudioClip> PeaceTracks => peaceTracks;

        public IReadOnlyList<AudioClip> TensionTracks => tensionTracks;

        public IReadOnlyList<AudioClip> WarTracks => warTracks;

        public float MasterMusicVolume => masterMusicVolume;

        public float CrossfadeDuration => crossfadeDuration;


        public IReadOnlyList<AudioClip> GetPlaylistByMood(MusicMood mood)
        {
            switch (mood)
            {
                case MusicMood.Peace:
                    return peaceTracks;

                case MusicMood.Tension:
                    return tensionTracks;

                case MusicMood.War:
                    return warTracks;

                default:
                    return peaceTracks;
            }
        }
    }
}