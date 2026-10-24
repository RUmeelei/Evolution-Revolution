using System.Collections.Generic;
using UnityEngine;
using ER.World.Grid;
using ER.World.Polity;
using ER.World.Command;

namespace ER.World.Command.Communication
{
    public class CommunicationNetworkManager : MonoBehaviour
    {
        public static CommunicationNetworkManager Instance { get; private set; }


        [Header("References")]
        [SerializeField] private PolityRegionManager polityManager;

        [SerializeField] private WorldGridManager gridManager;


        [Header("Gizmo Visualizer")]
        [SerializeField] private bool drawNetworkGizmos = false;


        private readonly List<CommunicationNodeData> nodes = new List<CommunicationNodeData>();

        private readonly List<CommunicationLineData> lines = new List<CommunicationLineData>();

        private readonly Dictionary<ushort, CommunicationNodeData> nodeLookup = new Dictionary<ushort, CommunicationNodeData>();

        private readonly Dictionary<ushort, List<ushort>> nodeConnectedLines = new Dictionary<ushort, List<ushort>>();

        private ushort nextNodeId = 1;

        private ushort nextLineId = 1;

        private bool isInitialized = false;


        public bool IsInitialized => isInitialized;

        public IReadOnlyList<CommunicationNodeData> Nodes => nodes;

        public IReadOnlyList<CommunicationLineData> Lines => lines;


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
            EnsureNetworkInitialized();
        }


        public void EnsureNetworkInitialized()
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

            if (gridManager == null || !gridManager.IsInitialized)
            {
                return;
            }

            nodes.Clear();

            lines.Clear();

            nodeLookup.Clear();

            nodeConnectedLines.Clear();

            float tileSize = gridManager.MapConfig.TileWorldSize;

            var anchors = polityManager.Anchors;

            for (int i = 0; i < anchors.Count; i++)
            {
                var anchor = anchors[i];

                float hqX = (anchor.TileX + 0.5f) * tileSize;

                float hqY = (anchor.TileY + 0.5f) * tileSize;

                RegisterNode(anchor.OwnerPolityId, anchor.AnchorId, CommunicationNodeType.CampHearth, hqX, hqY, 25.0f);
            }

            isInitialized = true;

            Debug.Log($"[CommunicationNetworkManager] Initialized {nodes.Count} primitive tribal hearth command points.");
        }


        public ushort RegisterNode(
            ushort polityId,
            ushort anchorId,
            CommunicationNodeType type,
            float worldX,
            float worldY,
            float range)
        {
            ushort id = nextNodeId++;

            CommunicationNodeData node = CommunicationNodeData.Create(id, polityId, anchorId, type, worldX, worldY, range);

            nodes.Add(node);

            nodeLookup.Add(id, node);

            nodeConnectedLines.Add(id, new List<ushort>());

            return id;
        }


        public ushort ConnectNodes(
            ushort polityId,
            ushort nodeAId,
            ushort nodeBId,
            TransmissionMedium medium)
        {
            if (!nodeLookup.TryGetValue(nodeAId, out CommunicationNodeData nodeA) ||
                !nodeLookup.TryGetValue(nodeBId, out CommunicationNodeData nodeB))
            {
                return 0;
            }

            ushort id = nextLineId++;

            float length = Vector2.Distance(
                new Vector2(nodeA.WorldPositionX, nodeA.WorldPositionY),
                new Vector2(nodeB.WorldPositionX, nodeB.WorldPositionY));

            CommunicationLineData line = CommunicationLineData.Create(id, polityId, nodeAId, nodeBId, medium, length);

            lines.Add(line);

            nodeConnectedLines[nodeAId].Add(id);

            nodeConnectedLines[nodeBId].Add(id);

            return id;
        }


        public bool TryFindFastestPath(
            ushort polityId,
            ushort anchorId,
            Vector2 targetPos,
            out TransmissionMedium networkMedium,
            out float remainingCourierDistance)
        {
            networkMedium = TransmissionMedium.AcousticVoice;

            remainingCourierDistance = float.MaxValue;

            if (!isInitialized || lines.Count == 0)
            {
                return false;
            }

            return false;
        }


        private void OnDrawGizmos()
        {
            if (!drawNetworkGizmos || !isInitialized)
            {
                return;
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                CommunicationNodeData node = nodes[i];

                Vector3 pos = new Vector3(node.WorldPositionX, node.WorldPositionY, -2.6f);

                Gizmos.color = Color.green;

                Gizmos.DrawWireSphere(pos, node.TransmissionRange);
            }
        }
    }
}