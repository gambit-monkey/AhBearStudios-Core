using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace AhBearStudios.Core.Messaging.Jobs
{
    // Job for publishing messages to multiple subscribers
    [BurstCompile]
    public struct MessagePublishJob<T> : IJobParallelFor where T : unmanaged
    {
        [ReadOnly] public NativeArray<T> Messages;
        [ReadOnly] public NativeArray<IntPtr> Handlers;
    
        public void Execute(int index)
        {
            unsafe
            {
                var message = Messages[index % Messages.Length];
                var handler = (delegate* managed<T, void>)Handlers[index / Messages.Length];
                handler(message);
            }
        }
    }
}