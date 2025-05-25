using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace AhBearStudios.Pooling.Pools.Native
{
    /// <summary>
    /// Provides thread-safe parallel write access to a native pool from multiple jobs.
    /// This class is designed to be used with Unity's job system for concurrent operations.
    /// </summary>
    [BurstCompile]
    public struct NativePoolParallelWriter
    {
        // The pool ID for registry access
        internal readonly int PoolId;

        // Job handle for synchronization
        internal JobHandle DependsOn;

        /// <summary>
        /// Creates a new parallel writer for a native pool
        /// </summary>
        /// <param name="poolId">ID of the pool in the registry</param>
        /// <param name="dependsOn">Optional job handle to depend on</param>
        public NativePoolParallelWriter(int poolId, JobHandle dependsOn = default)
        {
            PoolId = poolId;
            DependsOn = dependsOn;
        }

        /// <summary>
        /// Updates the dependencies for this writer
        /// </summary>
        /// <param name="handle">Job handle to depend on</param>
        public void AddDependency(JobHandle handle)
        {
            DependsOn = JobHandle.CombineDependencies(DependsOn, handle);
        }

        /// <summary>
        /// Gets the current job handle dependencies
        /// </summary>
        public JobHandle Dependencies => DependsOn;
    }

    /// <summary>
    /// Type-specific parallel writer for native pools that allows thread-safe operations
    /// from multiple jobs simultaneously.
    /// </summary>
    /// <typeparam name="T">Type of items in the native pool (must be unmanaged)</typeparam>
    [BurstCompile]
    public struct NativePoolParallelWriter<T> where T : unmanaged
    {
        // The base writer
        internal NativePoolParallelWriter Writer;

        // Atomic counter for thread-safe index acquisition
        [NativeDisableUnsafePtrRestriction]
        internal readonly AtomicSafetyHandle SafetyHandle;

        /// <summary>
        /// Creates a new type-specific parallel writer for a native pool
        /// </summary>
        /// <param name="poolId">ID of the pool in the registry</param>
        /// <param name="dependsOn">Optional job handle to depend on</param>
        public NativePoolParallelWriter(int poolId, JobHandle dependsOn = default)
        {
            Writer = new NativePoolParallelWriter(poolId, dependsOn);
            unsafe
            {
                SafetyHandle = NativePoolRegistry.Instance.GetSafetyHandle(poolId);
            }
        }

        /// <summary>
        /// Acquires an index from the pool in a thread-safe manner.
        /// </summary>
        /// <returns>The acquired index, or -1 if the pool is full</returns>
        [BurstCompile]
        public int AcquireIndex()
        {
            return NativePoolRegistry.Instance.AcquireIndexThreadSafe(Writer.PoolId);
        }

        /// <summary>
        /// Releases an index back to the pool in a thread-safe manner.
        /// </summary>
        /// <param name="index">The index to release</param>
        [BurstCompile]
        public void ReleaseIndex(int index)
        {
            NativePoolRegistry.Instance.ReleaseIndexThreadSafe(Writer.PoolId, index);
        }

        /// <summary>
        /// Sets a value at the specified index in a thread-safe manner.
        /// </summary>
        /// <param name="index">The index to set</param>
        /// <param name="value">The value to set</param>
        [BurstCompile]
        public void SetValue(int index, T value)
        {
            NativePoolRegistry.Instance.SetValueThreadSafe(Writer.PoolId, index, value);
        }

        /// <summary>
        /// Gets a value from the specified index in a thread-safe manner.
        /// </summary>
        /// <param name="index">The index to get</param>
        /// <returns>The value at the specified index</returns>
        [BurstCompile]
        public T GetValue(int index)
        {
            return NativePoolRegistry.Instance.GetValueThreadSafe<T>(Writer.PoolId, index);
        }

        /// <summary>
        /// Checks if an index is active in the pool in a thread-safe manner.
        /// </summary>
        /// <param name="index">The index to check</param>
        /// <returns>True if the index is active, false otherwise</returns>
        [BurstCompile]
        public bool IsIndexActive(int index)
        {
            return NativePoolRegistry.Instance.IsIndexActiveThreadSafe(Writer.PoolId, index);
        }

        /// <summary>
        /// Updates the dependencies for this writer
        /// </summary>
        /// <param name="handle">Job handle to depend on</param>
        public void AddDependency(JobHandle handle)
        {
            Writer.AddDependency(handle);
        }

        /// <summary>
        /// Gets the current job handle dependencies
        /// </summary>
        public JobHandle Dependencies => Writer.Dependencies;
    }
}