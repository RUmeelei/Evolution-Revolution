using UnityEngine;

namespace ER.World.Population
{
    [CreateAssetMenu(fileName = "PopMetabolismConfig", menuName = "ER/06_Population/Metabolism Config")]
    public class PopMetabolismConfig : ScriptableObject
    {
        [Header("Calorie Consumption")]
        [SerializeField] private float idleCalorieBurn = 0.04f;

        [SerializeField] private float movingCalorieBurn = 0.08f;

        [SerializeField] private float workingCalorieBurn = 0.16f;

        [SerializeField] private float hungerThreshold = 1000.0f;

        [SerializeField] private float shiveringCalorieCost = 0.06f;


        [Header("Hydration Dynamics")]
        [SerializeField] private float baseHydrationDecay = 0.02f;

        [SerializeField] private float workHydrationMultiplier = 1.6f;


        [Header("Thermoregulation & Homeostasis")]
        [SerializeField] private float nominalBodyTemp = 36.6f;

        [SerializeField] private float baseComfortTemp = 22.0f;

        [SerializeField] private float activeWarmingRate = 1.4f;

        [SerializeField] private float passiveCoolingRate = 0.0018f;

        [SerializeField] private float workMetabolicHeatBoost = 8.5f;

        [SerializeField] private float hearthHeatRadius = 24.0f;

        [SerializeField] private float hearthMaxHeatBonus = 54.0f;

        [SerializeField] private float hypothermiaThreshold = 33.5f;

        [SerializeField] private float hypothermiaCriticalTemp = 29.0f;


        [Header("Damage and Recovery Rates")]
        [SerializeField] private byte starvationDamage = 1;

        [SerializeField] private byte dehydrationDamage = 2;

        [SerializeField] private byte hypothermiaDamage = 2;

        [SerializeField] private byte naturalHealingRate = 1;


        public float IdleCalorieBurn => idleCalorieBurn;

        public float MovingCalorieBurn => movingCalorieBurn;

        public float WorkingCalorieBurn => workingCalorieBurn;

        public float HungerThreshold => hungerThreshold;

        public float ShiveringCalorieCost => shiveringCalorieCost;

        public float BaseHydrationDecay => baseHydrationDecay;

        public float WorkHydrationMultiplier => workHydrationMultiplier;

        public float NominalBodyTemp => nominalBodyTemp;

        public float BaseComfortTemp => baseComfortTemp;

        public float ActiveWarmingRate => activeWarmingRate;

        public float PassiveCoolingRate => passiveCoolingRate;

        public float WorkMetabolicHeatBoost => workMetabolicHeatBoost;

        public float HearthHeatRadius => hearthHeatRadius;

        public float HearthMaxHeatBonus => hearthMaxHeatBonus;

        public float HypothermiaThreshold => hypothermiaThreshold;

        public float HypothermiaCriticalTemp => hypothermiaCriticalTemp;

        public byte StarvationDamage => starvationDamage;

        public byte DehydrationDamage => dehydrationDamage;

        public byte HypothermiaDamage => hypothermiaDamage;

        public byte NaturalHealingRate => naturalHealingRate;
    }
}