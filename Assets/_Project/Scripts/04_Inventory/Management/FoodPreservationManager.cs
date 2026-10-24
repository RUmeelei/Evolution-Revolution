using System;
using UnityEngine;
using ER.Simulation.Time;
using ER.World.Inventory;
using ER.World.Polity;

namespace ER.World.Inventory
{
    public class FoodPreservationManager : MonoBehaviour
    {
        public static FoodPreservationManager Instance { get; private set; }


        [Header("Spoilage Rates (Daily % lost in warm seasons)")]
        [SerializeField] private float rawMeatSpoilRate = 0.25f;

        [SerializeField] private float berrySpoilRate = 0.15f;


        [Header("System References")]
        [SerializeField] private SimulationClockManager clockManager;

        [SerializeField] private InventoryManager inventoryManager;

        [SerializeField] private PolityRegionManager polityManager;


        private bool isInitialized = false;


        public bool IsInitialized => isInitialized;


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

            if (inventoryManager == null) inventoryManager = InventoryManager.Instance;

            if (polityManager == null) polityManager = PolityRegionManager.Instance;

            if (clockManager != null)
            {
                clockManager.OnDayPassed += HandleDailyFoodDecay;
            }

            isInitialized = true;
        }


        private void OnDestroy()
        {
            if (clockManager != null)
            {
                clockManager.OnDayPassed -= HandleDailyFoodDecay;
            }

            isInitialized = false;
        }


        private void HandleDailyFoodDecay(WorldTimeData timeData)
        {
            if (!isInitialized || inventoryManager == null || polityManager == null)
            {
                return;
            }

            if (timeData.IsFreezing)
            {
                return;
            }

            var anchors = polityManager.Anchors;

            for (int a = 0; a < anchors.Count; a++)
            {
                int storageId = anchors[a].StorageInventoryId;

                if (storageId <= 0) continue;

                uint rawMeat = inventoryManager.GetItemCount(storageId, 4);

                if (rawMeat > 0)
                {
                    uint spoiledMeat = (uint)Mathf.Ceil(rawMeat * rawMeatSpoilRate);

                    spoiledMeat = Math.Min(rawMeat, spoiledMeat);

                    inventoryManager.ExecuteTransfer(storageId, 0, 4, spoiledMeat, out _);

                    Debug.Log($"[FoodPreservationManager] Summer warmth spoiled {spoiledMeat}x raw meat in Settlement {anchors[a].AnchorId}!");
                }

                uint berries = inventoryManager.GetItemCount(storageId, 3);

                if (berries > 0)
                {
                    uint spoiledBerries = (uint)Mathf.Ceil(berries * berrySpoilRate);

                    spoiledBerries = Math.Min(berries, spoiledBerries);

                    inventoryManager.ExecuteTransfer(storageId, 0, 3, spoiledBerries, out _);
                }
            }
        }
    }
}