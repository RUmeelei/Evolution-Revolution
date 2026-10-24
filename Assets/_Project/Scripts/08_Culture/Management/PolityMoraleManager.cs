using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using ER.Simulation.Time;
using ER.World.Population;
using ER.World.Polity;

namespace ER.World.Culture
{
    public class PolityMoraleManager : MonoBehaviour
    {
        public static PolityMoraleManager Instance { get; private set; }


        [Header("Universal Morale Tuning")]
        [SerializeField] private float nominalMorale = 70.0f;

        [SerializeField] private float unburiedCorpsePenalty = 6.0f;

        [SerializeField] private float maxCorpseMoraleDrop = 35.0f;

        [SerializeField] private float sacredSiteProximityBonus = 12.0f;


        [Header("System References")]
        [SerializeField] private SimulationClockManager clockManager;

        [SerializeField] private PopulationManager populationManager;

        [SerializeField] private ShrineManager shrineManager;

        [SerializeField] private PolityRegionManager polityManager;


        private readonly Dictionary<ushort, float> polityMoraleMap = new Dictionary<ushort, float>();

        private int unburiedCorpseCount = 0;

        private bool isInitialized = false;


        public bool IsInitialized => isInitialized;

        public int UnburiedCorpseCount => unburiedCorpseCount;


        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }


        private void Start()
        {
            EnsureInitialized();
        }


        public void EnsureInitialized()
        {
            if (isInitialized)
            {
                return;
            }

            if (clockManager == null) clockManager = SimulationClockManager.Instance;

            if (populationManager == null) populationManager = PopulationManager.Instance;

            if (shrineManager == null) shrineManager = ShrineManager.Instance;

            if (polityManager == null) polityManager = PolityRegionManager.Instance;

            if (shrineManager != null)
            {
                shrineManager.EnsureInitialized();
            }

            if (clockManager != null)
            {
                clockManager.OnDayPassed += HandleDailyMoraleEvaluation;
            }

            polityMoraleMap[1] = nominalMorale;

            polityMoraleMap[2] = nominalMorale;

            polityMoraleMap[3] = nominalMorale;

            isInitialized = true;
        }


        private void OnDestroy()
        {
            if (clockManager != null)
            {
                clockManager.OnDayPassed -= HandleDailyMoraleEvaluation;
            }

            isInitialized = false;
        }


        private void HandleDailyMoraleEvaluation(WorldTimeData timeData)
        {
            if (!isInitialized || populationManager == null || !populationManager.IsInitialized)
            {
                return;
            }

            NativeArray<PopData> pops = populationManager.ActivePops;

            int totalPops = populationManager.TotalPopulationCount;

            int deadCount = 0;

            int starvingCount = 0;

            int freezingCount = 0;

            int totalAlive = 0;

            for (int i = 0; i < totalPops; i++)
            {
                PopData pop = pops[i];

                if (!pop.IsAlive)
                {
                    deadCount++;

                    continue;
                }

                totalAlive++;

                if (pop.CurrentCalories < 800.0f) starvingCount++;

                if (pop.BodyTemperature < 33.5f) freezingCount++;
            }

            unburiedCorpseCount = deadCount;

            float baseScore = nominalMorale;

            float corpseImpact = math.min(maxCorpseMoraleDrop, deadCount * unburiedCorpsePenalty);

            baseScore -= corpseImpact;

            if (totalAlive > 0)
            {
                float hardshipRatio = (starvingCount + freezingCount) / (float)totalAlive;

                baseScore -= hardshipRatio * 25.0f;
            }

            if (shrineManager != null && shrineManager.IsInitialized)
            {
                baseScore += sacredSiteProximityBonus;
            }

            baseScore = math.clamp(baseScore, 5.0f, 100.0f);

            polityMoraleMap[1] = baseScore;

            if (deadCount > 0)
            {
                Debug.Log($"[PolityMoraleManager] Polity Morale: {baseScore:F0}% ({deadCount} unburied dead disturbing the settlement).");
            }
        }


        public float GetPolityMorale(ushort polityId)
        {
            if (polityMoraleMap.TryGetValue(polityId, out float morale))
            {
                return morale;
            }

            return nominalMorale;
        }


        public void ApplyDirectMoraleShock(ushort polityId, float delta)
        {
            if (polityMoraleMap.ContainsKey(polityId))
            {
                polityMoraleMap[polityId] = math.clamp(polityMoraleMap[polityId] + delta, 0.0f, 100.0f);
            }
        }
    }
}