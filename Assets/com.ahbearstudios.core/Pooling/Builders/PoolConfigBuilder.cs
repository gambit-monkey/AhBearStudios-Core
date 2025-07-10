using AhBearStudios.Core.Pooling.Configurations;

namespace AhBearStudios.Core.Pooling.Builders
{
    /// <summary>
    /// Builder for standard pool configurations
    /// </summary>
    public class PoolConfigBuilder : PoolConfigBuilderBase<PoolConfig, PoolConfigBuilder>
    {
        /// <summary>
        /// Creates a new pool config builder with default settings
        /// </summary>
        public PoolConfigBuilder()
        {
            Config = new PoolConfig();
        }
        
        /// <summary>
        /// Creates a new pool config builder with the specified initial capacity
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        public PoolConfigBuilder(int initialCapacity)
        {
            Config = new PoolConfig(initialCapacity);
        }
        
        /// <summary>
        /// Sets whether to use exponential growth when expanding the pool
        /// </summary>
        /// <param name="useExponentialGrowth">Whether to use exponential growth</param>
        /// <returns>The builder instance for method chaining</returns>
        public PoolConfigBuilder WithExponentialGrowth(bool useExponentialGrowth)
        {
            Config.UseExponentialGrowth = useExponentialGrowth;
            return this;
        }
        
        /// <summary>
        /// Sets the growth factor for exponential pool expansion
        /// </summary>
        /// <param name="growthFactor">The growth factor (e.g., 2.0 for doubling)</param>
        /// <returns>The builder instance for method chaining</returns>
        public PoolConfigBuilder WithGrowthFactor(float growthFactor)
        {
            Config.GrowthFactor = growthFactor;
            return this;
        }
        
        /// <summary>
        /// Sets the linear growth increment for pool expansion
        /// </summary>
        /// <param name="increment">The number of items to add when expanding</param>
        /// <returns>The builder instance for method chaining</returns>
        public PoolConfigBuilder WithGrowthIncrement(int increment)
        {
            Config.GrowthIncrement = increment;
            return this;
        }
        
        /// <summary>
        /// Sets whether to throw an exception when exceeding maximum pool size
        /// </summary>
        /// <param name="throwIfExceeding">Whether to throw an exception</param>
        /// <returns>The builder instance for method chaining</returns>
        public PoolConfigBuilder WithExceptionOnExceedingMaxCount(bool throwIfExceeding)
        {
            Config.ThrowIfExceedingMaxCount = throwIfExceeding;
            return this;
        }
        
        /// <inheritdoc />
        public override PoolConfig Build()
        {
            return Config.Clone() as PoolConfig;
        }
    }
}