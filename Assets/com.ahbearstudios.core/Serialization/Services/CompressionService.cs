﻿using System.IO;
using System.IO.Compression;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Serialization.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Serialization.Services;

/// <summary>
    /// Service for compressing and decompressing serialized data.
    /// Uses efficient compression algorithms optimized for serialized data.
    /// </summary>
    public class CompressionService : ICompressionService
    {
        private readonly ILoggingService _logger;
        private double _lastCompressionRatio;
        private readonly object _ratioLock = new();

        /// <summary>
        /// Initializes a new instance of CompressionService.
        /// </summary>
        /// <param name="logger">Logging service</param>
        /// <exception cref="ArgumentNullException">Thrown when logger is null</exception>
        public CompressionService(ILoggingService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var correlationId = GetCorrelationId();
            _logger.LogInfo("CompressionService initialized", correlationId);
        }

        /// <inheritdoc />
        public byte[] Compress(byte[] data, CompressionLevel level)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            
            return Compress(data.AsSpan(), level);
        }

        /// <inheritdoc />
        public byte[] Compress(ReadOnlySpan<byte> data, CompressionLevel level)
        {
            if (level == CompressionLevel.None)
                return data.ToArray();

            var correlationId = GetCorrelationId();
            var originalSize = data.Length;

            try
            {
                using var output = new MemoryStream();
                using var compressionStream = CreateCompressionStream(output, level);
                
                compressionStream.Write(data);
                compressionStream.Flush();
                
                var compressed = output.ToArray();
                
                UpdateCompressionRatio(originalSize, compressed.Length);
                
                _logger.LogInfo($"Compressed {originalSize} bytes to {compressed.Length} bytes (ratio: {_lastCompressionRatio:P2})", correlationId);
                
                return compressed;
            }
            catch (Exception ex)
            {
                _logger.LogException($"Failed to compress {originalSize} bytes", ex, correlationId);
                throw;
            }
        }

        /// <inheritdoc />
        public byte[] Decompress(byte[] compressedData)
        {
            if (compressedData == null)
                throw new ArgumentNullException(nameof(compressedData));
            
            return Decompress(compressedData.AsSpan());
        }

        /// <inheritdoc />
        public byte[] Decompress(ReadOnlySpan<byte> compressedData)
        {
            var correlationId = GetCorrelationId();
            var compressedSize = compressedData.Length;

            try
            {
                using var input = new MemoryStream(compressedData.ToArray());
                using var decompressionStream = new GZipStream(input, System.IO.Compression.CompressionMode.Decompress);
                using var output = new MemoryStream();
                
                decompressionStream.CopyTo(output);
                var decompressed = output.ToArray();
                
                _logger.LogInfo($"Decompressed {compressedSize} bytes to {decompressed.Length} bytes", correlationId);
                
                return decompressed;
            }
            catch (Exception ex)
            {
                _logger.LogException($"Failed to decompress {compressedSize} bytes", ex, correlationId);
                throw;
            }
        }

        /// <inheritdoc />
        public double GetLastCompressionRatio()
        {
            lock (_ratioLock)
            {
                return _lastCompressionRatio;
            }
        }

        private Stream CreateCompressionStream(Stream output, CompressionLevel level)
        {
            var compressionLevel = level switch
            {
                CompressionLevel.Fastest => System.IO.Compression.CompressionLevel.Fastest,
                CompressionLevel.Optimal => System.IO.Compression.CompressionLevel.Optimal,
                CompressionLevel.SmallestSize => System.IO.Compression.CompressionLevel.SmallestSize,
                _ => System.IO.Compression.CompressionLevel.Optimal
            };

            return new GZipStream(output, compressionLevel, leaveOpen: true);
        }

        private void UpdateCompressionRatio(int originalSize, int compressedSize)
        {
            lock (_ratioLock)
            {
                _lastCompressionRatio = originalSize > 0 ? (double)compressedSize / originalSize : 1.0;
            }
        }

        private FixedString64Bytes GetCorrelationId()
        {
            return new FixedString64Bytes(Guid.NewGuid().ToString("N")[..32]);
        }
    }