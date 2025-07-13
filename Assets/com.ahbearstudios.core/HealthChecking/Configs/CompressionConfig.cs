using System.Collections.Generic;
using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.HealthCheck.Configs;

 /// <summary>
    /// Data compression configuration
    /// </summary>
    public sealed record CompressionConfig
    {
        /// <summary>
        /// Whether compression is enabled
        /// </summary>
        public bool Enabled { get; init; } = true;

        /// <summary>
        /// Compression algorithm to use
        /// </summary>
        public CompressionAlgorithm Algorithm { get; init; } = CompressionAlgorithm.Gzip;

        /// <summary>
        /// Compression level (1-9, higher = better compression but slower)
        /// </summary>
        public int CompressionLevel { get; init; } = 6;

        /// <summary>
        /// Age threshold for compressing data
        /// </summary>
        public TimeSpan CompressionThreshold { get; init; } = TimeSpan.FromDays(7);

        /// <summary>
        /// Creates compression configuration for production environments
        /// </summary>
        /// <returns>Production compression configuration</returns>
        public static CompressionConfig ForProduction()
        {
            return new CompressionConfig
            {
                Enabled = true,
                Algorithm = CompressionAlgorithm.Lz4,
                CompressionLevel = 4,
                CompressionThreshold = TimeSpan.FromDays(1)
            };
        }

        /// <summary>
        /// Validates compression configuration
        /// </summary>
        /// <returns>List of validation errors</returns>
        public List<string> Validate()
        {
            var errors = new List<string>();

            if (!Enum.IsDefined(typeof(CompressionAlgorithm), Algorithm))
                errors.Add($"Invalid compression algorithm: {Algorithm}");

            if (CompressionLevel < 1 || CompressionLevel > 9)
                errors.Add("CompressionLevel must be between 1 and 9");

            if (CompressionThreshold < TimeSpan.Zero)
                errors.Add("CompressionThreshold must be non-negative");

            return errors;
        }
    }