using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace ER.World.Command
{
    [BurstCompile]
    public struct CommandDispatchJob : IJob
    {
        public NativeList<GameCommand> ActiveCommands;

        public NativeQueue<GameCommand>.ParallelWriter ReadyCommandsQueue;

        public ulong CurrentTick;


        public void Execute()
        {
            for (int i = 0; i < ActiveCommands.Length; i++)
            {
                GameCommand cmd = ActiveCommands[i];

                if (cmd.Status == CommandStatus.QueuedInTransit)
                {
                    if (CurrentTick >= cmd.DeliveryTick)
                    {
                        cmd.Status = CommandStatus.Delivered;

                        ActiveCommands[i] = cmd;

                        ReadyCommandsQueue.Enqueue(cmd);
                    }
                }
            }
        }
    }
}