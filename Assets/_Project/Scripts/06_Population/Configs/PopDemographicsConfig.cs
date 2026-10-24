using UnityEngine;

namespace ER.World.Population
{
    [CreateAssetMenu(fileName = "PopDemographicsConfig", menuName = "ER/06_Population/Demographics Config")]
    public class PopDemographicsConfig : ScriptableObject
    {
        [Header("Life Stage Thresholds (Age in Days)")]
        [SerializeField] private ushort infantMaxDays = 1080;

        [SerializeField] private ushort childMaxDays = 4320;

        [SerializeField] private ushort youthMaxDays = 6480;

        [SerializeField] private ushort adultMaxDays = 18000;


        [Header("Reproduction Dynamics")]
        [SerializeField] private ushort pregnancyDurationDays = 270;

        [SerializeField] private ushort postPartumCooldownDays = 180;

        [SerializeField] private float conceptionMinCalories = 1600.0f;

        [SerializeField] private float conceptionMinBodyTemp = 35.0f;

        [SerializeField] private byte conceptionMinHealth = 180;

        [SerializeField] [Range(0.01f, 1.0f)] private float dailyConceptionProbability = 0.05f;


        [Header("Senescence & Natural Mortality")]
        [SerializeField] private ushort elderBaseMortalityAgeDays = 18000;

        [SerializeField] private float baseSenescenceDailyChance = 0.0008f;

        [SerializeField] private float senescenceAgeCurveMultiplier = 0.00025f;


        public ushort InfantMaxDays => infantMaxDays;

        public ushort ChildMaxDays => childMaxDays;

        public ushort YouthMaxDays => youthMaxDays;

        public ushort AdultMaxDays => adultMaxDays;

        public ushort PregnancyDurationDays => pregnancyDurationDays;

        public ushort PostPartumCooldownDays => postPartumCooldownDays;

        public float ConceptionMinCalories => conceptionMinCalories;

        public float ConceptionMinBodyTemp => conceptionMinBodyTemp;

        public byte ConceptionMinHealth => conceptionMinHealth;

        public float DailyConceptionProbability => dailyConceptionProbability;

        public ushort ElderBaseMortalityAgeDays => elderBaseMortalityAgeDays;

        public float BaseSenescenceDailyChance => baseSenescenceDailyChance;

        public float SenescenceAgeCurveMultiplier => senescenceAgeCurveMultiplier;
    }
}