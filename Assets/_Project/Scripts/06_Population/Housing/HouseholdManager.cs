using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using ER.World.Grid;
using ER.World.Polity;
using ER.World.Population;

namespace ER.World.Housing
{
    public class HouseholdManager : MonoBehaviour
    {
        public static HouseholdManager Instance { get; private set; }


        [Header("Starting Housing Setup")]
        [SerializeField] private int startingDwellingsPerCamp = 6;

        [SerializeField] private float minPlacementRadius = 10.0f;

        [SerializeField] private float maxPlacementRadius = 22.0f;


        [Header("System References")]
        [SerializeField] private PolityRegionManager polityManager;

        [SerializeField] private WorldGridManager gridManager;


        [Header("Debug Visualizer")]
        [SerializeField] private bool drawDwellingGizmos = true;


        private NativeList<DwellingData> dwellings;

        private NativeList<HouseholdData> households;

        private int nextDwellingId = 1;

        private int nextHouseholdId = 1;

        private bool isInitialized = false;


        public bool IsInitialized => isInitialized;

        public NativeList<DwellingData> RawDwellings => dwellings;

        public NativeList<HouseholdData> RawHouseholds => households;


        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            InitializeStorage();
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

            if (polityManager == null) polityManager = PolityRegionManager.Instance;

            if (gridManager == null) gridManager = WorldGridManager.Instance;

            if (polityManager != null)
            {
                polityManager.EnsureInitialized();
            }

            if (polityManager == null || !polityManager.IsInitialized || polityManager.Anchors.Count == 0)
            {
                return;
            }

            SpawnStartingSettlementDwellings();

            isInitialized = true;
        }


        private void InitializeStorage()
        {
            if (!dwellings.IsCreated)
            {
                dwellings = new NativeList<DwellingData>(1024, Allocator.Persistent);
            }

            if (!households.IsCreated)
            {
                households = new NativeList<HouseholdData>(1024, Allocator.Persistent);
            }
        }


        private void OnDestroy()
        {
            DisposeStorage();
        }


        private void OnApplicationQuit()
        {
            DisposeStorage();
        }


        private void DisposeStorage()
        {
            if (households.IsCreated) households.Dispose();

            if (dwellings.IsCreated) dwellings.Dispose();

            isInitialized = false;
        }


        private void SpawnStartingSettlementDwellings()
        {
            var anchors = polityManager.Anchors;

            float tileSize = gridManager.MapConfig.TileWorldSize;

            for (int a = 0; a < anchors.Count; a++)
            {
                PolityAnchorData anchor = anchors[a];

                float centerX = (anchor.TileX + 0.5f) * tileSize;

                float centerY = (anchor.TileY + 0.5f) * tileSize;

                for (int i = 0; i < startingDwellingsPerCamp; i++)
                {
                    float angle = (i / (float)startingDwellingsPerCamp) * Mathf.PI * 2.0f;

                    float dist = UnityEngine.Random.Range(minPlacementRadius, maxPlacementRadius);

                    float dx = Mathf.Cos(angle) * dist;

                    float dy = Mathf.Sin(angle) * dist;

                    RegisterDwelling(anchor.OwnerPolityId, anchor.AnchorId, DwellingType.DugoutShelter, centerX + dx, centerY + dy, 5, 18.0f);
                }
            }

            Debug.Log($"[HouseholdManager] Successfully established {dwellings.Length} primitive dwellings across settlements.");
        }


        public int RegisterDwelling(
            ushort polityId,
            ushort anchorId,
            DwellingType type,
            float x,
            float y,
            byte capacity,
            float thermalBonus)
        {
            int id = nextDwellingId++;

            DwellingData dwelling = DwellingData.Create(id, polityId, anchorId, type, x, y, capacity, thermalBonus);

            dwellings.Add(dwelling);

            return id;
        }


        public int FormHouseholdForCouple(ref PopData male, ref PopData female)
        {
            int assignedDwellingId = FindVacantDwelling(female.HomeAnchorId);

            int hId = nextHouseholdId++;

            HouseholdData household = HouseholdData.Create(
                hId,
                female.PolityId,
                female.HomeAnchorId,
                assignedDwellingId,
                male.PopId,
                female.PopId);

            households.Add(household);

            male.HouseholdId = hId;

            male.DwellingId = assignedDwellingId;

            female.HouseholdId = hId;

            female.DwellingId = assignedDwellingId;

            if (assignedDwellingId > 0)
            {
                IncrementDwellingOccupancy(assignedDwellingId, 2);
            }

            return hId;
        }


        public void AssignNewbornToHousehold(ref PopData mother, ref PopData newborn)
        {
            newborn.HouseholdId = mother.HouseholdId;

            newborn.DwellingId = mother.DwellingId;

            if (mother.HouseholdId > 0)
            {
                for (int i = 0; i < households.Length; i++)
                {
                    if (households[i].HouseholdId == mother.HouseholdId)
                    {
                        HouseholdData hh = households[i];

                        hh.ChildrenCount++;

                        households[i] = hh;

                        break;
                    }
                }
            }

            if (mother.DwellingId > 0)
            {
                IncrementDwellingOccupancy(mother.DwellingId, 1);
            }
        }


        public int FindVacantDwelling(ushort anchorId)
        {
            for (int i = 0; i < dwellings.Length; i++)
            {
                DwellingData d = dwellings[i];

                if (d.HomeAnchorId == anchorId && d.IsActive && d.OccupantCount < d.Capacity)
                {
                    return d.DwellingId;
                }
            }

            return 0;
        }


        public bool HasHousingCapacity(int dwellingId)
        {
            if (dwellingId <= 0) return false;

            for (int i = 0; i < dwellings.Length; i++)
            {
                if (dwellings[i].DwellingId == dwellingId)
                {
                    return dwellings[i].OccupantCount < dwellings[i].Capacity;
                }
            }

            return false;
        }


        private void IncrementDwellingOccupancy(int dwellingId, byte count)
        {
            for (int i = 0; i < dwellings.Length; i++)
            {
                if (dwellings[i].DwellingId == dwellingId)
                {
                    DwellingData d = dwellings[i];

                    d.OccupantCount = (byte)Mathf.Min(d.Capacity, d.OccupantCount + count);

                    dwellings[i] = d;

                    return;
                }
            }
        }


        public bool TryGetDwellingPosition(int dwellingId, out float2 position)
        {
            for (int i = 0; i < dwellings.Length; i++)
            {
                if (dwellings[i].DwellingId == dwellingId)
                {
                    position = new float2(dwellings[i].WorldPositionX, dwellings[i].WorldPositionY);

                    return true;
                }
            }

            position = float2.zero;

            return false;
        }


        private void OnDrawGizmos()
        {
            if (!drawDwellingGizmos || !dwellings.IsCreated || dwellings.Length == 0)
            {
                return;
            }

            for (int i = 0; i < dwellings.Length; i++)
            {
                DwellingData d = dwellings[i];

                if (!d.IsActive) continue;

                Vector3 pos = new Vector3(d.WorldPositionX, d.WorldPositionY, -2.1f);

                Gizmos.color = new Color(0.45f, 0.32f, 0.18f);

                Gizmos.DrawCube(pos, new Vector3(2.6f, 2.6f, 1.4f));

                Gizmos.color = new Color(0.9f, 0.6f, 0.2f, 0.25f);

                Gizmos.DrawWireSphere(pos, 3.2f);
            }
        }
    }
}