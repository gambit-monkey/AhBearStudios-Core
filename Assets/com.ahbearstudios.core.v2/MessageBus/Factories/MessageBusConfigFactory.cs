using AhBearStudios.Core.MessageBus.Builders;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.MessageBus.Configuration;

namespace AhBearStudios.Core.MessageBus.Factories
{
    /// <summary>
    /// Factory for creating common message bus configurations using the builder pattern.
    /// </summary>
    public static class MessageBusConfigFactory
    {
        /// <summary>
        /// Creates a balanced configuration suitable for most general-purpose use cases.
        /// Provides good performance with moderate resource usage and diagnostic capabilities.
        /// </summary>
        /// <param name="configId">Optional configuration ID</param>
        /// <returns>A balanced message bus configuration for general use</returns>
        public static IMessageBusConfig CreateDefaultConfig(string configId = "DefaultMessageBusConfig")
        {
            return new MessageBusConfigBuilder()
                .WithConfigId(configId)
                .WithMaxMessagesPerFrame(120)
                .WithInitialQueueCapacity(100)
                .WithProcessingTimeSlice(0.014f)
                .WithMessagePooling(true, 150, 1500)
                .WithSerialization(true, false, true)
                .WithReliableDelivery(true, 3, 8f, 2f)
                .WithStatistics(true, true, false)
                .WithLogging(false, true, true)
                .WithThreading(true, 1, true)
                .OptimizeForCurrentPlatform()
                .Build();
        }
        
        /// <summary>
        /// Creates a high-performance configuration for production environments.
        /// </summary>
        /// <param name="configId">Optional configuration ID</param>
        /// <returns>A message bus configuration optimized for production</returns>
        public static IMessageBusConfig CreateProductionConfig(string configId = "ProductionMessageBusConfig")
        {
            return new MessageBusConfigBuilder()
                .WithConfigId(configId)
                .WithMaxMessagesPerFrame(100)
                .WithInitialQueueCapacity(100)
                .WithProcessingTimeSlice(0.012f)
                .WithMessagePooling(true, 200, 2000)
                .WithSerialization(true, false, true)
                .WithReliableDelivery(true, 3, 5f, 2f)
                .WithStatistics(false, true, false)
                .WithLogging(false, false, true)
                .WithThreading(true, 2, true)
                .OptimizeForCurrentPlatform()
                .Build();
        }

        /// <summary>
        /// Creates a development-focused configuration with comprehensive logging and metrics.
        /// </summary>
        /// <param name="configId">Optional configuration ID</param>
        /// <returns>A message bus configuration optimized for development</returns>
        public static IMessageBusConfig CreateDevelopmentConfig(string configId = "DevelopmentMessageBusConfig")
        {
            return new MessageBusConfigBuilder()
                .WithConfigId(configId)
                .WithMaxMessagesPerFrame(200)
                .WithInitialQueueCapacity(100)
                .WithProcessingTimeSlice(0.016f)
                .WithMessagePooling(true, 100, 1000)
                .WithSerialization(true, true, true)
                .WithReliableDelivery(true, 5, 10f, 1.5f)
                .WithStatistics(true, true, true)
                .WithLogging(true, true, true)
                .WithThreading(true, 2, true)
                .Build();
        }

        /// <summary>
        /// Creates a minimal configuration suitable for testing or low-resource environments.
        /// </summary>
        /// <param name="configId">Optional configuration ID</param>
        /// <returns>A minimal message bus configuration</returns>
        public static IMessageBusConfig CreateMinimalConfig(string configId = "MinimalMessageBusConfig")
        {
            return new MessageBusConfigBuilder()
                .WithConfigId(configId)
                .WithMaxMessagesPerFrame(50)
                .WithInitialQueueCapacity(20)
                .WithProcessingTimeSlice(0.020f)
                .WithMessagePooling(true, 50, 200)
                .WithSerialization(true, false, false)
                .WithReliableDelivery(false)
                .WithStatistics(false, false, false)
                .WithLogging(false, false, true)
                .WithThreading(false, 1, true)
                .OptimizeForCurrentPlatform()
                .Build();
        }

        /// <summary>
        /// Creates a network-optimized configuration for distributed systems.
        /// </summary>
        /// <param name="configId">Optional configuration ID</param>
        /// <returns>A network-optimized message bus configuration</returns>
        public static IMessageBusConfig CreateNetworkConfig(string configId = "NetworkMessageBusConfig")
        {
            return new MessageBusConfigBuilder()
                .WithConfigId(configId)
                .WithMaxMessagesPerFrame(150)
                .WithInitialQueueCapacity(100)
                .WithProcessingTimeSlice(0.016f)
                .WithMessagePooling(true, 200, 2000)
                .WithSerialization(true, true, true)
                .WithReliableDelivery(true, 5, 10f, 2f)
                .WithStatistics(true, true, true)
                .WithLogging(false, false, true)
                .WithThreading(true, 2, true)
                .Build();
        }
    }
}