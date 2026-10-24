using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using ER.World.Grid;
using ER.World.Polity;

namespace ER.World.Culture
{
    public class ShrineManager : MonoBehaviour
    {
        public static ShrineManager Instance { get; private set; }


        [Header("Starting Shrine Setup")]
        [SerializeField] private float totemPlacementDistance = 16.0f;

        [SerializeField] private float defaultAuraRadius = 32.0f;


        [Header("System References")]
        [SerializeField] private PolityRegionManager polityManager;

        [SerializeField] private WorldGridManager gridManager;


        [Header("Debug Visualizer")]
        [SerializeField] private bool drawShrineGizmos = true;


        private NativeList<AncestralShrineData> shrines;

        private int nextShrineId = 1;

        private bool isInitialized = false;


        public bool IsInitialized => isInitialized;

        public NativeList<AncestralShrineData> RawShrines => shrines;


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

            SpawnSettlementTotems();

            isInitialized = true;
        }


        private void InitializeStorage()
        {
            if (!shrines.IsCreated)
            {
                shrines = new NativeList<AncestralShrineData>(128, Allocator.Persistent);
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
            if (shrines.IsCreated)
            {
                shrines.Dispose();
            }

            isInitialized = false;
        }


        private void SpawnSettlementTotems()
        {
            shrines.Clear();

            var anchors = polityManager.Anchors;

            float tileSize = gridManager.MapConfig.TileWorldSize;

            for (int a = 0; a < anchors.Count; a++)
            {
                PolityAnchorData anchor = anchors[a];

                float centerX = (anchor.TileX + 0.5f) * tileSize;

                float centerY = (anchor.TileY + 0.5f) * tileSize;

                float tx = centerX - totemPlacementDistance;

                float ty = centerY + totemPlacementDistance * 0.75f;

                RegisterShrine(anchor.OwnerPolityId, anchor.AnchorId, ShrineType.AncestralTotem, tx, ty, defaultAuraRadius, 15.0f);
            }

            Debug.Log($"[ShrineManager] Successfully raised {shrines.Length} ancestral shrines across settlements.");
        }


        public int RegisterShrine(
            ushort polityId,
            ushort anchorId,
            ShrineType type,
            float x,
            float y,
            float auraRadius,
            float moraleBonus)
        {
            int id = nextShrineId++;

            AncestralShrineData shrine = AncestralShrineData.Create(id, polityId, anchorId, type, x, y, auraRadius, moraleBonus);

            shrines.Add(shrine);

            return id;
        }


        public bool RecordBurial(ushort anchorId)
        {
            for (int i = 0; i < shrines.Length; i++)
            {
                if (shrines[i].HomeAnchorId == anchorId && shrines[i].IsActive)
                {
                    AncestralShrineData s = shrines[i];

                    s.BuriedAncestorsCount++;

                    shrines[i] = s;

                    return true;
                }
            }

            return false;
        }


        public bool TryGetNearestShrine(ushort anchorId, out AncestralShrineData shrine)
        {
            for (int i = 0; i < shrines.Length; i++)
            {
                if (shrines[i].HomeAnchorId == anchorId && shrines[i].IsActive)
                {
                    shrine = shrines[i];

                    return true;
                }
            }

            shrine = default;

            return false;
        }


        private void OnDrawGizmos()
        {
            if (!drawShrineGizmos || !shrines.IsCreated || shrines.Length == 0)
            {
                return;
            }

            for (int i = 0; i < shrines.Length; i++)
            {
                AncestralShrineData s = shrines[i];

                if (!s.IsActive) continue;

                Vector3 pos = new Vector3(s.WorldPositionX, s.WorldPositionY, -2.5f);

                Gizmos.color = new Color(0.6f, 0.2f, 0.8f, 0.9f);

                Gizmos.DrawCube(pos, new Vector3(1.4f, 1.4f, 3.2f));

                Gizmos.color = new Color(0.7f, 0.3f, 0.9f, 0.15f);

                Gizmos.DrawWireSphere(pos, s.SacredAuraRadius);
            }
        }
    }
}