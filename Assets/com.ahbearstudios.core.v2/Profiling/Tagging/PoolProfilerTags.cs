using Unity.Profiling;

namespace AhBearStudios.Core.Profiling.Tagging
{
    /// <summary>
    /// Predefined profiler tag for pool operations
    /// </summary>
    public static class PoolProfilerTags
    {
        // Pool category
        private static readonly ProfilerCategory PoolCategory = ProfilerCategory.Scripts;
        
        // Common operation tag
        public static readonly ProfilerTag PoolAcquire = new ProfilerTag(PoolCategory, "Pool.Acquire");
        public static readonly ProfilerTag PoolRelease = new ProfilerTag(PoolCategory, "Pool.Release");
        public static readonly ProfilerTag PoolCreate = new ProfilerTag(PoolCategory, "Pool.Create");
        public static readonly ProfilerTag PoolClear = new ProfilerTag(PoolCategory, "Pool.Clear");
        public static readonly ProfilerTag PoolExpand = new ProfilerTag(PoolCategory, "Pool.Expand");
        
        /// <summary>
        /// Creates a pool-specific profiler tag
        /// </summary>
        /// <param name="operationType">Type of operation</param>
        /// <param name="poolId">Pool identifier (shortened GUID)</param>
        /// <returns>A profiler tag for the specific pool operation</returns>
        public static ProfilerTag ForPool(string operationType, System.Guid poolId)
        {
            string guidPrefix = poolId.ToString().Substring(0, 8);
            return new ProfilerTag(PoolCategory, $"Pool.{guidPrefix}.{operationType}");
        }
        
        /// <summary>
        /// Creates a pool-specific profiler tag using only the pool name
        /// </summary>
        /// <param name="operationType">Type of operation</param>
        /// <param name="poolName">Name of the pool</param>
        /// <returns>A profiler tag for the specific pool operation</returns>
        public static ProfilerTag ForPoolName(string operationType, string poolName)
        {
            return new ProfilerTag(PoolCategory, $"Pool.{poolName}.{operationType}");
        }
        
        /// <summary>
        /// Gets the appropriate tag for the given operation type
        /// </summary>
        /// <param name="operationType">Type of operation</param>
        /// <returns>A profiler tag for the operation</returns>
        public static ProfilerTag ForOperation(string operationType)
        {
            switch (operationType.ToLowerInvariant())
            {
                case "acquire":
                    return PoolAcquire;
                case "release":
                    return PoolRelease;
                case "create":
                    return PoolCreate;
                case "clear":
                    return PoolClear;
                case "expand":
                    return PoolExpand;
                default:
                    return new ProfilerTag(PoolCategory, $"Pool.{operationType}");
            }
        }
    }
}