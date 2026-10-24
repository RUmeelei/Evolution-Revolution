using System;
using UnityEngine;

namespace ER.World.Audio
{
    [Serializable]
    public class MusicTheme
    {
        [SerializeField] private string themeName = "New Theme";

        [SerializeField] private AudioClip baseAmbientLayer;

        [SerializeField] private AudioClip culturalMelodyLayer;

        [SerializeField] private AudioClip tensionPerilLayer;

        [SerializeField] private AudioClip warCrisisLayer;


        public string ThemeName => themeName;

        public AudioClip BaseAmbientLayer => baseAmbientLayer;

        public AudioClip CulturalMelodyLayer => culturalMelodyLayer;

        public AudioClip TensionPerilLayer => tensionPerilLayer;

        public AudioClip WarCrisisLayer => warCrisisLayer;

        public bool IsValid => baseAmbientLayer != null;

        public float Duration => baseAmbientLayer != null ? baseAmbientLayer.length : 0.0f;
    }
}