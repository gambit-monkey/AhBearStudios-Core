namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Compression algorithms available
/// </summary>
public enum CompressionAlgorithm
{
    Gzip,
    Deflate,
    Lz4,
    Brotli,
    Zstd
}