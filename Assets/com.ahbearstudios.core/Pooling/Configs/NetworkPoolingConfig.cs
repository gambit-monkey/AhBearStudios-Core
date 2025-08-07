using System;
using System.Collections.Generic;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Pooling.Services;

namespace AhBearStudios.Core.Pooling.Configs
{
    /// <summary>
    /// Immutable configuration record for network-related object pools.
    /// Defines buffer sizes and pooling strategies optimized for network serialization.
    /// Use with-expressions to create modified copies.
    /// </summary>
    public record NetworkPoolingConfig
    {
        /// <summary>
        /// Configuration for small network buffers (up to 1KB).
        /// Used for simple types like primitives, Vector3, Quaternion.
        /// </summary>
        public PoolConfiguration SmallBufferPoolConfig { get; init; }

        /// <summary>
        /// Configuration for medium network buffers (up to 16KB).
        /// Used for medium complexity objects and collections.
        /// </summary>
        public PoolConfiguration MediumBufferPoolConfig { get; init; }

        /// <summary>
        /// Configuration for large network buffers (up to 64KB).
        /// Used for complex objects, large collections, and compressed data.
        /// </summary>
        public PoolConfiguration LargeBufferPoolConfig { get; init; }

        /// <summary>
        /// Configuration for compression working buffers.
        /// Used by the compression service for network payload optimization.
        /// </summary>
        public PoolConfiguration CompressionBufferPoolConfig { get; init; }

        /// <summary>
        /// Creates a default network pooling configuration.
        /// </summary>
        /// <returns>Default network pooling configuration</returns>
        public static NetworkPoolingConfig CreateDefault()
        {
            var validationService = new PoolValidationService();
            
            return new NetworkPoolingConfig
            {
                SmallBufferPoolConfig = new PoolConfiguration
                {
                    Name = "SmallNetworkBuffer",
                    InitialCapacity = 100,
                    MaxCapacity = 500,
                    Factory = null, // Will be set by factory
                    ResetAction = buffer => validationService.ResetPooledObject(buffer),
                    ValidationFunc = buffer => validationService.ValidatePooledObject(buffer),
                    ValidationInterval = TimeSpan.FromMinutes(2),
                    MaxIdleTime = TimeSpan.FromMinutes(10),
                    EnableValidation = true,
                    EnableStatistics = true,
                    DisposalPolicy = PoolDisposalPolicy.PoolDecision
                },
                MediumBufferPoolConfig = new PoolConfiguration
                {
                    Name = "MediumNetworkBuffer",
                    InitialCapacity = 50,
                    MaxCapacity = 200,
                    Factory = null, // Will be set by factory
                    ResetAction = buffer => validationService.ResetPooledObject(buffer),
                    ValidationFunc = buffer => validationService.ValidatePooledObject(buffer),
                    ValidationInterval = TimeSpan.FromMinutes(3),
                    MaxIdleTime = TimeSpan.FromMinutes(15),
                    EnableValidation = true,
                    EnableStatistics = true,
                    DisposalPolicy = PoolDisposalPolicy.PoolDecision
                },
                LargeBufferPoolConfig = new PoolConfiguration
                {
                    Name = "LargeNetworkBuffer",
                    InitialCapacity = 20,
                    MaxCapacity = 100,
                    Factory = null, // Will be set by factory
                    ResetAction = buffer => validationService.ResetPooledObject(buffer),
                    ValidationFunc = buffer => validationService.ValidatePooledObject(buffer),
                    ValidationInterval = TimeSpan.FromMinutes(5),
                    MaxIdleTime = TimeSpan.FromMinutes(20),
                    EnableValidation = true,
                    EnableStatistics = true,
                    DisposalPolicy = PoolDisposalPolicy.PoolDecision
                },
                CompressionBufferPoolConfig = new PoolConfiguration
                {
                    Name = "CompressionBuffer",
                    InitialCapacity = 25,
                    MaxCapacity = 100,
                    Factory = null, // Will be set by factory
                    ResetAction = buffer => validationService.ResetPooledObject(buffer),
                    ValidationFunc = buffer => validationService.ValidatePooledObject(buffer),
                    ValidationInterval = TimeSpan.FromMinutes(3),
                    MaxIdleTime = TimeSpan.FromMinutes(10),
                    EnableValidation = true,
                    EnableStatistics = true,
                    DisposalPolicy = PoolDisposalPolicy.PoolDecision
                }
            };
        }

        /// <summary>
        /// Gets the appropriate buffer pool configuration based on expected data size.
        /// </summary>
        /// <param name="expectedSize">Expected data size in bytes</param>
        /// <returns>Appropriate pool configuration</returns>
        public PoolConfiguration GetBufferPoolConfig(int expectedSize)
        {
            return expectedSize switch
            {
                <= 1024 => SmallBufferPoolConfig,
                <= 16384 => MediumBufferPoolConfig,
                _ => LargeBufferPoolConfig
            };
        }

        /// <summary>
        /// Gets health monitoring thresholds for network buffer pools.
        /// </summary>
        /// <returns>Health monitoring thresholds</returns>
        public static NetworkBufferHealthThresholds GetHealthThresholds()
        {
            return new NetworkBufferHealthThresholds
            {
                MaxConsecutiveFailures = 5,
                MaxValidationErrors = 10,
                CorruptionThresholdPercentage = 0.25,
                WarningMemoryUsageBytes = 64 * 1024 * 1024, // 64MB
                CriticalMemoryUsageBytes = 128 * 1024 * 1024, // 128MB
                MaxIdleTimeBeforeCleanup = TimeSpan.FromMinutes(30),
                ValidationFrequency = TimeSpan.FromMinutes(2)
            };
        }

        /// <summary>
        /// Validates the configuration and returns any validation errors.
        /// </summary>
        /// <returns>A list of validation errors, empty if configuration is valid</returns>
        public List<string> Validate()
        {
            var errors = new List<string>();

            // Validate Small Buffer Pool Config
            var smallErrors = ValidatePoolConfiguration(SmallBufferPoolConfig, "SmallBufferPool");
            errors.AddRange(smallErrors);

            // Validate Medium Buffer Pool Config
            var mediumErrors = ValidatePoolConfiguration(MediumBufferPoolConfig, "MediumBufferPool");
            errors.AddRange(mediumErrors);

            // Validate Large Buffer Pool Config
            var largeErrors = ValidatePoolConfiguration(LargeBufferPoolConfig, "LargeBufferPool");
            errors.AddRange(largeErrors);

            // Validate Compression Buffer Pool Config
            var compressionErrors = ValidatePoolConfiguration(CompressionBufferPoolConfig, "CompressionBufferPool");
            errors.AddRange(compressionErrors);

            // Validate size hierarchy
            if (SmallBufferPoolConfig != null && MediumBufferPoolConfig != null)
            {
                if (SmallBufferPoolConfig.MaxCapacity > MediumBufferPoolConfig.MaxCapacity)
                {
                    errors.Add("Small buffer pool max capacity should not exceed medium buffer pool max capacity");
                }
            }

            if (MediumBufferPoolConfig != null && LargeBufferPoolConfig != null)
            {
                if (MediumBufferPoolConfig.MaxCapacity > LargeBufferPoolConfig.MaxCapacity)
                {
                    errors.Add("Medium buffer pool max capacity should not exceed large buffer pool max capacity");
                }
            }

            return errors;
        }

        /// <summary>
        /// Validates an individual pool configuration.
        /// </summary>
        /// <param name="config">Pool configuration to validate</param>
        /// <param name="configName">Name of the configuration for error messages</param>
        /// <returns>List of validation errors</returns>
        private List<string> ValidatePoolConfiguration(PoolConfiguration config, string configName)
        {
            var errors = new List<string>();

            if (config == null)
            {
                errors.Add($"{configName} configuration is null");
                return errors;
            }

            if (string.IsNullOrWhiteSpace(config.Name))
            {
                errors.Add($"{configName} name cannot be null or empty");
            }

            if (config.InitialCapacity < 0)
            {
                errors.Add($"{configName} initial capacity must be non-negative");
            }

            if (config.MaxCapacity <= 0)
            {
                errors.Add($"{configName} max capacity must be greater than zero");
            }

            if (config.InitialCapacity > config.MaxCapacity)
            {
                errors.Add($"{configName} initial capacity cannot exceed max capacity");
            }

            if (config.Factory == null)
            {
                errors.Add($"{configName} factory cannot be null");
            }

            if (config.ValidationInterval <= TimeSpan.Zero)
            {
                errors.Add($"{configName} validation interval must be greater than zero");
            }

            if (config.MaxIdleTime <= TimeSpan.Zero)
            {
                errors.Add($"{configName} max idle time must be greater than zero");
            }


            return errors;
        }
    }
}