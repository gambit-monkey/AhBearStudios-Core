using System.Threading;
using System.Threading.Tasks;

namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for a message compressor
    /// </summary>
    public interface IMessageCompressor
    {
        /// <summary>
        /// Compresses a byte array
        /// </summary>
        /// <param name="uncompressedData">The uncompressed data</param>
        /// <returns>The compressed data</returns>
        byte[] Compress(byte[] uncompressedData);
    
        /// <summary>
        /// Compresses a byte array asynchronously
        /// </summary>
        /// <param name="uncompressedData">The uncompressed data</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>The compressed data</returns>
        Task<byte[]> CompressAsync(byte[] uncompressedData, CancellationToken cancellationToken = default);
    
        /// <summary>
        /// Decompresses a byte array
        /// </summary>
        /// <param name="compressedData">The compressed data</param>
        /// <returns>The decompressed data</returns>
        byte[] Decompress(byte[] compressedData);
    
        /// <summary>
        /// Decompresses a byte array asynchronously
        /// </summary>
        /// <param name="compressedData">The compressed data</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>The decompressed data</returns>
        Task<byte[]> DecompressAsync(byte[] compressedData, CancellationToken cancellationToken = default);
    
        /// <summary>
        /// Gets the compression algorithm name
        /// </summary>
        string Algorithm { get; }
    
        /// <summary>
        /// Gets the compression level
        /// </summary>
        CompressionLevel CompressionLevel { get; }
    }
}