using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using ER.Simulation.Time;
using ER.World.Polity;
using ER.World.Grid;
using ER.World.Command.Communication;
using ER.World.Navigation;

namespace ER.World.Command
{
    public class CommandQueueManager : MonoBehaviour
    {
        public static CommandQueueManager Instance { get; private set; }


        [Header("Physical Transmission Velocities (World Units per Tick)")]
        [SerializeField] private float acousticRangeUnits = 18.0f;

        [SerializeField] private float runnerSpeedPerTick = 0.35f;

        [SerializeField] private float horseSpeedPerTick = 1.10f;

        [SerializeField] private float heliographSpeedPerTick = 25.0f;

        [SerializeField] private float mountedCourierThresholdDistance = 40.0f;


        [Header("References")]
        [SerializeField] private SimulationClockManager clockManager;

        [SerializeField] private PolityRegionManager polityManager;

        [SerializeField] private WorldGridManager gridManager;

        [SerializeField] private CommunicationNetworkManager communicationNetwork;

        [SerializeField] private MacroPathfindingManager macroNavManager;


        private NativeList<GameCommand> activeCommands;

        private NativeQueue<GameCommand> deliveredCommandsQueue;

        private ulong nextCommandId = 1;

        private bool isInitialized = false;


        public event Action<GameCommand> OnCommandDelivered;


        public bool IsInitialized => isInitialized;

        public int ActiveCommandsInTransitCount => activeCommands.IsCreated ? activeCommands.Length : 0;


        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            InitializeQueues();
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

            if (polityManager == null) polityManager = PolityRegionManager.Instance;

            if (gridManager == null) gridManager = WorldGridManager.Instance;

            if (communicationNetwork == null) communicationNetwork = CommunicationNetworkManager.Instance;

            if (macroNavManager == null) macroNavManager = MacroPathfindingManager.Instance;

            if (clockManager != null)
            {
                clockManager.OnTick += ProcessCommandQueueOnTick;
            }

            isInitialized = true;
        }


        private void OnDestroy()
        {
            if (clockManager != null)
            {
                clockManager.OnTick -= ProcessCommandQueueOnTick;
            }

            DisposeQueues();
        }


        private void OnApplicationQuit()
        {
            DisposeQueues();
        }


        public void InitializeQueues()
        {
            if (activeCommands.IsCreated && deliveredCommandsQueue.IsCreated)
            {
                return;
            }

            activeCommands = new NativeList<GameCommand>(1024, Allocator.Persistent);

            deliveredCommandsQueue = new NativeQueue<GameCommand>(Allocator.Persistent);
        }


        public void DisposeQueues()
        {
            if (activeCommands.IsCreated)
            {
                activeCommands.Dispose();
            }

            if (deliveredCommandsQueue.IsCreated)
            {
                deliveredCommandsQueue.Dispose();
            }

            isInitialized = false;
        }


        public ulong IssueCommand(
            CommandType type,
            ushort polityId,
            ushort anchorId,
            float targetWorldX,
            float targetWorldY,
            int targetEntity = -1,
            ushort payloadId = 0,
            uint payloadQuantity = 0)
        {
            if (!isInitialized || !ValidateWorldTargetPlacement(targetWorldX, targetWorldY))
            {
                return 0;
            }

            ulong currentTick = clockManager != null ? clockManager.CurrentTime.TotalTicks : 0;

            Vector2 sourcePos = ResolveAnchorWorldPosition(anchorId);

            Vector2 targetPos = new Vector2(targetWorldX, targetWorldY);

            float totalDistance = Vector2.Distance(sourcePos, targetPos);

            ulong delayTicks = CalculateTransmissionDelay(polityId, anchorId, sourcePos, targetPos, totalDistance, out TransmissionMedium medium);

            ulong deliveryTick = currentTick + delayTicks;

            ulong commandId = nextCommandId++;

            GameCommand command = GameCommand.Create(
                commandId,
                type,
                polityId,
                anchorId,
                targetWorldX,
                targetWorldY,
                currentTick,
                deliveryTick,
                medium,
                targetEntity,
                payloadId,
                payloadQuantity);

            activeCommands.Add(command);

            return commandId;
        }


        public bool ValidateWorldTargetPlacement(float worldX, float worldY)
        {
            if (gridManager == null || !gridManager.IsInitialized)
            {
                return false;
            }

            WorldMapConfig config = gridManager.MapConfig;

            int tileX = Mathf.FloorToInt(worldX / config.TileWorldSize);

            int tileY = Mathf.FloorToInt(worldY / config.TileWorldSize);

            if (!config.IsCoordsValid(tileX, tileY))
            {
                return false;
            }

            TileData tile = gridManager.GetTile(tileX, tileY);

            if (tile.Terrain == TerrainType.WaterDeep)
            {
                return false;
            }

            return true;
        }


        private Vector2 ResolveAnchorWorldPosition(ushort anchorId)
        {
            if (polityManager == null || gridManager == null)
            {
                return Vector2.zero;
            }

            var anchors = polityManager.Anchors;

            float tileSize = gridManager.MapConfig.TileWorldSize;

            for (int i = 0; i < anchors.Count; i++)
            {
                if (anchors[i].AnchorId == anchorId)
                {
                    return new Vector2((anchors[i].TileX + 0.5f) * tileSize, (anchors[i].TileY + 0.5f) * tileSize);
                }
            }

            return Vector2.zero;
        }


        private ulong CalculateTransmissionDelay(
            ushort polityId,
            ushort anchorId,
            Vector2 sourcePos,
            Vector2 targetPos,
            float directDistance,
            out TransmissionMedium usedMedium)
        {
            if (directDistance <= acousticRangeUnits)
            {
                usedMedium = TransmissionMedium.AcousticVoice;

                return 0;
            }

            float physicalTravelDistance = directDistance;

            if (macroNavManager == null)
            {
                macroNavManager = MacroPathfindingManager.Instance;
            }

            if (macroNavManager != null && macroNavManager.IsInitialized)
            {
                var macroPath = macroNavManager.FindMacroPathBetweenPositions(sourcePos, targetPos);

                if (macroPath.Success && macroPath.TotalDistanceMeters > 1.0f)
                {
                    physicalTravelDistance = macroPath.TotalDistanceMeters;
                }
            }

            if (communicationNetwork != null && communicationNetwork.TryFindFastestPath(polityId, anchorId, targetPos, out TransmissionMedium networkMedium, out float remainingCourierDistance))
            {
                usedMedium = networkMedium;

                ulong networkDelay = networkMedium == TransmissionMedium.ElectricTelegraph ? 1UL : (ulong)Mathf.Max(1.0f, physicalTravelDistance / heliographSpeedPerTick);

                float courierSpeed = remainingCourierDistance > mountedCourierThresholdDistance ? horseSpeedPerTick : runnerSpeedPerTick;

                ulong courierDelay = (ulong)Mathf.Max(0.0f, remainingCourierDistance / Mathf.Max(0.01f, courierSpeed));

                return networkDelay + courierDelay;
            }

            if (physicalTravelDistance > mountedCourierThresholdDistance)
            {
                usedMedium = TransmissionMedium.MountedMessenger;

                return (ulong)Mathf.Max(1.0f, physicalTravelDistance / Mathf.Max(0.01f, horseSpeedPerTick));
            }

            usedMedium = TransmissionMedium.RunnerCourier;

            return (ulong)Mathf.Max(1.0f, physicalTravelDistance / Mathf.Max(0.01f, runnerSpeedPerTick));
        }


        private void ProcessCommandQueueOnTick(WorldTimeData timeData)
        {
            if (!isInitialized || activeCommands.Length == 0)
            {
                return;
            }

            CommandDispatchJob job = new CommandDispatchJob
            {
                ActiveCommands = activeCommands,
                ReadyCommandsQueue = deliveredCommandsQueue.AsParallelWriter(),
                CurrentTick = timeData.TotalTicks
            };

            JobHandle handle = job.Schedule();

            handle.Complete();

            while (deliveredCommandsQueue.TryDequeue(out GameCommand deliveredCmd))
            {
                RemoveCompletedCommandFromList(deliveredCmd.CommandId);

                OnCommandDelivered?.Invoke(deliveredCmd);
            }
        }


        private void RemoveCompletedCommandFromList(ulong commandId)
        {
            for (int i = 0; i < activeCommands.Length; i++)
            {
                if (activeCommands[i].CommandId == commandId)
                {
                    activeCommands.RemoveAtSwapBack(i);

                    break;
                }
            }
        }
    }
}