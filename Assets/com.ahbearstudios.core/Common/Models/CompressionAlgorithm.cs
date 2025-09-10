using System;

namespace AhBearStudios.Core.Common.Models;

/// <summary>
/// Defines the available compression algorithms for data compression operations.
/// Used across health checking, serialization, and data storage systems.
/// </summary>
public enum CompressionAlgorithm : byte
{
    /// <summary>
    /// No compression applied
    /// </summary>
    None = 0,

    /// <summary>
    /// GZip compression algorithm - good balance of compression ratio and speed
    /// </summary>
    Gzip = 1,

    /// <summary>
    /// LZ4 compression algorithm - optimized for speed with reasonable compression
    /// </summary>
    Lz4 = 2,

    /// <summary>
    /// Deflate compression algorithm - similar to GZip but without headers
    /// </summary>
    Deflate = 3,

    /// <summary>
    /// Brotli compression algorithm - high compression ratio but slower
    /// </summary>
    Brotli = 4
}