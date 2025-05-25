using Unity.Burst;
using Unity.Jobs;

namespace AhBearStudios.Core.Pooling.Diagnostics
{
    /// <summary>
    /// Job that updates pool metrics data atomically
    /// </summary>
    [BurstCompile]
    public struct UpdatePoolMetricsJob : IJob
    {
        public PoolMetricsParallelWriter Writer;
        
        /// <summary>
        /// Executes the job, updating metrics based on the writer's configured operation
        /// </summary>
        public unsafe void Execute()
        {
            // The actual implementation is in NativePoolMetricsExtensions.ExecuteUpdate
            // This is just a wrapper to enable job scheduling
            
            if (Writer._metricsBuffer == null)
                return;
                
            // Call into the static helper method to perform the actual work
            NativePoolMetricsExtensions.ExecuteUpdate(ref this);
        }
    }
}