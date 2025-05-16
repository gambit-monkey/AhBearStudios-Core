using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace A.hBearStudios.Core.Messaging.Jobs
{
    [BurstCompile]
    public struct MessageProcessingJob<T> : IJob where T : unmanaged
    {
        [ReadOnly] public NativeArray<T> Messages;
        public int MessageCount;
    
        // Function pointer to the handler
        [NativeDisableUnsafePtrRestriction]
        public unsafe delegate* managed<T, void> Handler;
    
        public void Execute()
        {
            unsafe
            {
                for (int i = 0; i < MessageCount; i++)
                {
                    Handler(Messages[i]);
                }
            }
        }
    }
}