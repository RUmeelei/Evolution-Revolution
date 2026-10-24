using UnityEngine;

namespace ER.World.Population
{
    [CreateAssetMenu(fileName = "PopDemographicsConfig", menuName = "ER/06_Population/Demographics Config")]
    public class PopDemographicsConfig : ScriptableObject
    {
        [Header("Life Stage Thresholds (in Days)")]
        [SerializeField] private ushort infantMaxDays = 64;

        [SerializeField] private ushort childMaxDays = 256;

        [SerializeField] private ushort youthMaxDays = 768;

        [SerializeField] private ushort adultMaxDays = 2880;

        [SerializeField] private ushort elderBaseMortalityAgeDays = 2880;


        [Header("Starting Population Age Range (in Days)")]
        [SerializeField] private ushort startingAdultMinAgeDays = 1280;

        [SerializeField] private ushort startingAdultMaxAgeDays = 2240;


        [Header("Senescence & Mortality")]
        [SerializeField] private float baseSenescenceDailyChance = 0.012f;

        [SerializeField] private float senescenceAgeCurveMultiplier = 0.0008f;


        [Header("Reproduction & Matrimony")]
        [SerializeField] private ushort pregnancyDurationDays = 48;

        [SerializeField] private ushort postPartumCooldownDays = 16;

        [SerializeField] private float dailyConceptionProbability = 0.04f;

        [SerializeField] private float conceptionMinHealth = 180.0f;

        [SerializeField] private float conceptionMinCalories = 1200.0f;

        [SerializeField] private float conceptionMinBodyTemp = 35.5f;


        public ushort InfantMaxDays => infantMaxDays;

        public ushort ChildMaxDays => childMaxDays;

        public ushort YouthMaxDays => youthMaxDays;

        public ushort AdultMaxDays => adultMaxDays;

        public ushort ElderBaseMortalityAgeDays => elderBaseMortalityAgeDays;

        public ushort StartingAdultMinAgeDays => startingAdultMinAgeDays;

        public ushort StartingAdultMaxAgeDays => startingAdultMaxAgeDays;

        public float BaseSenescenceDailyChance => baseSenescenceDailyChance;

        public float SenescenceAgeCurveMultiplier => senescenceAgeCurveMultiplier;

        public ushort PregnancyDurationDays => pregnancyDurationDays;

        public ushort PostPartumCooldownDays => postPartumCooldownDays;

        public float DailyConceptionProbability => dailyConceptionProbability;

        public float ConceptionMinHealth => conceptionMinHealth;

        public float ConceptionMinCalories => conceptionMinCalories;

        public float ConceptionMinBodyTemp => conceptionMinBodyTemp;


        public PopLifeStage ResolveLifeStage(ushort ageInDays)
        {
            if (ageInDays < infantMaxDays) return PopLifeStage.Infant;

            if (ageInDays < childMaxDays) return PopLifeStage.Child;

            if (ageInDays < youthMaxDays) return PopLifeStage.Youth;

            if (ageInDays < adultMaxDays) return PopLifeStage.Adult;

            return PopLifeStage.Elder;
        }
    }
}