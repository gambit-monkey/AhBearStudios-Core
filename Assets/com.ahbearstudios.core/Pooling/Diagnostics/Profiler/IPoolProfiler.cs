using System;
using System.Collections.Generic;
using Unity.Profiling;

namespace AhBearStudios.Core.Pooling.Diagnostics
{
    /// <summary>
    /// Interface for profiling and reporting on pool performance with Unity Profiler integration.
    /// Supports custom profiler markers for high-performance tracking and debugging of pool operations.
    /// Primarily identifies pools by their unique GUID with name as a secondary identifier.
    /// </summary>
    public interface IPoolProfiler
    {
        /// <summary>
        /// Creates a disposable profiling scope that automatically begins timing when created
        /// and ends when disposed. Designed to be used with the 'using' statement.
        /// </summary>
        /// <param name="operationType">Type of operation being profiled</param>
        /// <returns>A disposable object that ends the profiling sample when disposed</returns>
        ProfilerSampleScope Sample(string operationType);

        /// <summary>
        /// Creates a disposable profiling scope for a specific pool that automatically begins timing
        /// when created and ends when disposed. Designed to be used with the 'using' statement.
        /// </summary>
        /// <param name="operationType">Type of operation being profiled</param>
        /// <param name="poolId">Unique identifier of the pool</param>
        /// <param name="poolName">Name of the pool (for human readability)</param>
        /// <param name="activeCount">Current active count</param>
        /// <param name="freeCount">Current free count</param>
        /// <returns>A disposable object that ends the profiling sample when disposed</returns>
        ProfilerSampleScope Sample(string operationType, Guid poolId, string poolName, int activeCount, int freeCount);
        
        /// <summary>
        /// Begins profiling an operation
        /// </summary>
        /// <param name="operationType">Type of operation (Acquire/Release/Create/Clear/Expand)</param>
        void BeginSample(string operationType);

        /// <summary>
        /// Begins profiling an operation for a specific pool
        /// </summary>
        /// <param name="operationType">Type of operation (Acquire/Release/Create/Clear/Expand)</param>
        /// <param name="poolId">Unique identifier of the pool</param>
        /// <param name="poolName">Name of the pool (for human readability)</param>
        void BeginSample(string operationType, Guid poolId, string poolName = null);

        /// <summary>
        /// Ends profiling an operation and records the sample
        /// </summary>
        /// <param name="operationType">Type of operation</param>
        /// <param name="poolId">Unique identifier of the pool</param>
        /// <param name="poolName">Name of the pool (for human readability)</param>
        /// <param name="activeCount">Current active count</param>
        /// <param name="freeCount">Current free count</param>
        void EndSample(string operationType, Guid poolId, string poolName, int activeCount, int freeCount);

        /// <summary>
        /// Wraps an action with profiling. Useful for profiling complete pool operations.
        /// </summary>
        /// <param name="operationType">Type of operation</param>
        /// <param name="poolId">Unique identifier of the pool</param>
        /// <param name="poolName">Name of the pool (for human readability)</param>
        /// <param name="activeCount">Current active count</param>
        /// <param name="freeCount">Current free count</param>
        /// <param name="action">Action to profile</param>
        void SampleAction(string operationType, Guid poolId, string poolName, int activeCount, int freeCount, System.Action action);

        /// <summary>
        /// Creates a standalone profiler marker
        /// </summary>
        /// <param name="name">Marker name</param>
        /// <returns>ProfilerMarker</returns>
        ProfilerMarker CreateMarker(string name);

        /// <summary>
        /// Gets all samples
        /// </summary>
        /// <returns>List of samples</returns>
        List<ProfileSample> GetSamples();

        /// <summary>
        /// Gets stats for operations by type
        /// </summary>
        /// <param name="operationType">Type of operation</param>
        /// <returns>Min, max, and average times in milliseconds</returns>
        (float min, float max, float avg) GetOperationStats(string operationType);

        /// <summary>
        /// Gets stats for a specific pool by ID
        /// </summary>
        /// <param name="poolId">Unique identifier of the pool</param>
        /// <returns>Min, max, and average times in milliseconds across all operations</returns>
        (float min, float max, float avg) GetPoolStats(Guid poolId);

        /// <summary>
        /// Gets stats for a specific pool by name
        /// </summary>
        /// <param name="poolName">Name of the pool</param>
        /// <returns>Min, max, and average times in milliseconds across all operations</returns>
        (float min, float max, float avg) GetPoolStatsByName(string poolName);

        /// <summary>
        /// Clears all samples
        /// </summary>
        void ClearSamples();
        
        /// <summary>
        /// Creates a disposable profiling scope for a pool using only its name (legacy support).
        /// Designed to be used with the 'using' statement.
        /// </summary>
        /// <param name="operationType">Type of operation being profiled</param>
        /// <param name="poolName">Name of the pool</param>
        /// <param name="activeCount">Current active count</param>
        /// <param name="freeCount">Current free count</param>
        /// <returns>A disposable object that ends the profiling sample when disposed</returns>
        ProfilerSampleScope SampleByName(string operationType, string poolName, int activeCount, int freeCount);
        
        /// <summary>
        /// Gets operations that exceed the specified duration threshold
        /// </summary>
        /// <param name="thresholdMs">Threshold in milliseconds</param>
        /// <returns>List of slow operations</returns>
        List<ProfileSample> GetSlowOperations(float thresholdMs);
        
        /// <summary>
        /// Begins profiling an operation for a pool using only its name (legacy support)
        /// </summary>
        /// <param name="operationType">Type of operation (Acquire/Release/Create/Clear/Expand)</param>
        /// <param name="poolName">Name of the pool</param>
        void BeginSampleByName(string operationType, string poolName = null);
        
        /// <summary>
        /// Ends profiling an operation for a pool using only its name (legacy support)
        /// </summary>
        /// <param name="operationType">Type of operation</param>
        /// <param name="poolName">Name of the pool</param>
        /// <param name="activeCount">Current active count</param>
        /// <param name="freeCount">Current free count</param>
        void EndSampleByName(string operationType, string poolName, int activeCount, int freeCount);
    }
}