using CompressionLevel = AhBearStudios.Core.Serialization.Models.CompressionLevel;

namespace AhBearStudios.Core.Serialization.Services;

/// <summary>
/// Interface for data compression services.
/// </summary>
public interface ICompressionService
{
    /// <summary>
    /// Compresses data using the specified compression level.
    /// </summary>
    /// <param name="data">Data to compress</param>
    /// <param name="level">Compression level</param>
    /// <returns>Compressed data</returns>
    byte[] Compress(byte[] data, CompressionLevel level);

    /// <summary>
    /// Compresses data from a ReadOnlySpan.
    /// </summary>
    /// <param name="data">Data to compress</param>
    /// <param name="level">Compression level</param>
    /// <returns>Compressed data</returns>
    byte[] Compress(ReadOnlySpan<byte> data, CompressionLevel level);

    /// <summary>
    /// Decompresses data.
    /// </summary>
    /// <param name="compressedData">Compressed data</param>
    /// <returns>Decompressed data</returns>
    byte[] Decompress(byte[] compressedData);

    /// <summary>
    /// Decompresses data from a ReadOnlySpan.
    /// </summary>
    /// <param name="compressedData">Compressed data</param>
    /// <returns>Decompressed data</returns>
    byte[] Decompress(ReadOnlySpan<byte> compressedData);

    /// <summary>
    /// Gets the compression ratio for the last operation.
    /// </summary>
    /// <returns>Compression ratio (0.0 to 1.0)</returns>
    double GetLastCompressionRatio();
}