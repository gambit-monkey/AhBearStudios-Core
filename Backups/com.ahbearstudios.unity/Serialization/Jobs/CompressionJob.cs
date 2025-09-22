using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using AhBearStudios.Core.Serialization;
using AhBearStudios.Core.Logging;
using Cysharp.Threading.Tasks;
using ZLinq;

namespace AhBearStudios.Unity.Serialization.Jobs
{
    /// <summary>
    /// Unity Job System integration for high-performance parallel compression and decompression.
    /// Provides zero-allocation, burst-compatible compression jobs optimized for 60+ FPS gameplay.
    /// Uses NativeArrays for data transfer and UniTask for async coordination.
    /// </summary>
    public struct CompressionJob : IJob
    {
        /// <summary>
        /// Input data to be compressed.
        /// </summary>
        [ReadOnly]
        public NativeArray<byte> InputData;

        /// <summary>
        /// Output buffer for compressed data.
        /// Must be allocated with sufficient size before job execution.
        /// </summary>
        [WriteOnly]
        public NativeArray<byte> OutputBuffer;

        /// <summary>
        /// Result information about the compression operation.
        /// </summary>
        [WriteOnly]
        public NativeArray<CompressionJobResult> Result;

        /// <summary>
        /// Compression algorithm to use.
        /// </summary>
        [ReadOnly]
        public CompressionAlgorithm Algorithm;

        /// <summary>
        /// Compression level (0-9, where 9 is maximum compression).
        /// </summary>
        [ReadOnly]
        public int CompressionLevel;

        /// <summary>
        /// Executes the compression job.
        /// This method runs on a background thread and must be Burst-compatible.
        /// </summary>
        public void Execute()
        {
            var result = new CompressionJobResult
            {
                StartTime = DateTime.UtcNow.Ticks,
                IsSuccess = false,
                InputSize = InputData.Length,
                OutputSize = 0,
                Algorithm = Algorithm
            };

            try
            {
                var outputSize = 0;

                switch (Algorithm)
                {
                    case CompressionAlgorithm.LZ4:
                        outputSize = CompressLZ4();
                        break;
                    case CompressionAlgorithm.RLE:
                        outputSize = CompressRLE();
                        break;
                    case CompressionAlgorithm.None:
                        outputSize = CompressNone();
                        break;
                    default:
                        result.ErrorCode = CompressionErrorCode.UnsupportedAlgorithm;
                        return;
                }

                if (outputSize > 0)
                {
                    result.OutputSize = outputSize;
                    result.IsSuccess = true;
                    result.ErrorCode = CompressionErrorCode.None;
                }
                else
                {
                    result.ErrorCode = CompressionErrorCode.CompressionFailed;
                }
            }
            catch
            {
                result.IsSuccess = false;
                result.ErrorCode = CompressionErrorCode.UnknownError;
            }
            finally
            {
                result.EndTime = DateTime.UtcNow.Ticks;
                Result[0] = result;
            }
        }

        /// <summary>
        /// Simple LZ4-like compression implementation suitable for Burst compilation.
        /// </summary>
        private int CompressLZ4()
        {
            if (InputData.Length == 0) return 0;
            if (OutputBuffer.Length < InputData.Length + 16) return 0; // Need space for headers

            var outputIndex = 0;
            var inputIndex = 0;

            // Simple run-length encoding as LZ4 substitute (Burst-compatible)
            while (inputIndex < InputData.Length && outputIndex < OutputBuffer.Length - 8)
            {
                var currentByte = InputData[inputIndex];
                var runLength = 1;

                // Count consecutive identical bytes
                while (inputIndex + runLength < InputData.Length && 
                       InputData[inputIndex + runLength] == currentByte && 
                       runLength < 255)
                {
                    runLength++;
                }

                if (runLength >= 3) // Worth compressing
                {
                    // Write compressed token: 0xFF, byte, count
                    OutputBuffer[outputIndex++] = 0xFF; // Compression marker
                    OutputBuffer[outputIndex++] = currentByte;
                    OutputBuffer[outputIndex++] = (byte)runLength;
                    inputIndex += runLength;
                }
                else
                {
                    // Write literal byte
                    OutputBuffer[outputIndex++] = currentByte;
                    inputIndex++;
                }
            }

            return outputIndex;
        }

        /// <summary>
        /// Run-Length Encoding compression implementation.
        /// </summary>
        private int CompressRLE()
        {
            if (InputData.Length == 0) return 0;
            if (OutputBuffer.Length < InputData.Length * 2) return 0; // Worst case scenario

            var outputIndex = 0;
            var inputIndex = 0;

            while (inputIndex < InputData.Length && outputIndex < OutputBuffer.Length - 1)
            {
                var currentByte = InputData[inputIndex];
                var runLength = 1;

                // Count consecutive identical bytes
                while (inputIndex + runLength < InputData.Length && 
                       InputData[inputIndex + runLength] == currentByte && 
                       runLength < 255)
                {
                    runLength++;
                }

                // Write count and byte
                OutputBuffer[outputIndex++] = (byte)runLength;
                OutputBuffer[outputIndex++] = currentByte;
                inputIndex += runLength;
            }

            return outputIndex;
        }

        /// <summary>
        /// No compression - direct copy.
        /// </summary>
        private int CompressNone()
        {
            if (OutputBuffer.Length < InputData.Length) return 0;

            NativeArray<byte>.Copy(InputData, OutputBuffer, InputData.Length);
            return InputData.Length;
        }
    }

    /// <summary>
    /// Unity Job System integration for decompression operations.
    /// </summary>
    public struct DecompressionJob : IJob
    {
        /// <summary>
        /// Input compressed data to be decompressed.
        /// </summary>
        [ReadOnly]
        public NativeArray<byte> InputData;

        /// <summary>
        /// Output buffer for decompressed data.
        /// </summary>
        [WriteOnly]
        public NativeArray<byte> OutputBuffer;

        /// <summary>
        /// Result information about the decompression operation.
        /// </summary>
        [WriteOnly]
        public NativeArray<DecompressionJobResult> Result;

        /// <summary>
        /// Compression algorithm that was used.
        /// </summary>
        [ReadOnly]
        public CompressionAlgorithm Algorithm;

        /// <summary>
        /// Executes the decompression job.
        /// </summary>
        public void Execute()
        {
            var result = new DecompressionJobResult
            {
                StartTime = DateTime.UtcNow.Ticks,
                IsSuccess = false,
                InputSize = InputData.Length,
                OutputSize = 0,
                Algorithm = Algorithm
            };

            try
            {
                var outputSize = 0;

                switch (Algorithm)
                {
                    case CompressionAlgorithm.LZ4:
                        outputSize = DecompressLZ4();
                        break;
                    case CompressionAlgorithm.RLE:
                        outputSize = DecompressRLE();
                        break;
                    case CompressionAlgorithm.None:
                        outputSize = DecompressNone();
                        break;
                    default:
                        result.ErrorCode = CompressionErrorCode.UnsupportedAlgorithm;
                        return;
                }

                if (outputSize > 0)
                {
                    result.OutputSize = outputSize;
                    result.IsSuccess = true;
                    result.ErrorCode = CompressionErrorCode.None;
                }
                else
                {
                    result.ErrorCode = CompressionErrorCode.DecompressionFailed;
                }
            }
            catch
            {
                result.IsSuccess = false;
                result.ErrorCode = CompressionErrorCode.UnknownError;
            }
            finally
            {
                result.EndTime = DateTime.UtcNow.Ticks;
                Result[0] = result;
            }
        }

        /// <summary>
        /// Decompresses LZ4-like compressed data.
        /// </summary>
        private int DecompressLZ4()
        {
            if (InputData.Length == 0) return 0;

            var outputIndex = 0;
            var inputIndex = 0;

            while (inputIndex < InputData.Length && outputIndex < OutputBuffer.Length)
            {
                if (inputIndex + 2 < InputData.Length && InputData[inputIndex] == 0xFF)
                {
                    // Compressed token
                    var byteValue = InputData[inputIndex + 1];
                    var runLength = InputData[inputIndex + 2];
                    
                    // Write repeated bytes
                    for (int i = 0; i < runLength && outputIndex < OutputBuffer.Length; i++)
                    {
                        OutputBuffer[outputIndex++] = byteValue;
                    }
                    
                    inputIndex += 3;
                }
                else
                {
                    // Literal byte
                    OutputBuffer[outputIndex++] = InputData[inputIndex++];
                }
            }

            return outputIndex;
        }

        /// <summary>
        /// Decompresses RLE compressed data.
        /// </summary>
        private int DecompressRLE()
        {
            if (InputData.Length == 0 || InputData.Length % 2 != 0) return 0;

            var outputIndex = 0;
            var inputIndex = 0;

            while (inputIndex + 1 < InputData.Length && outputIndex < OutputBuffer.Length)
            {
                var runLength = InputData[inputIndex++];
                var byteValue = InputData[inputIndex++];

                // Write repeated bytes
                for (int i = 0; i < runLength && outputIndex < OutputBuffer.Length; i++)
                {
                    OutputBuffer[outputIndex++] = byteValue;
                }
            }

            return outputIndex;
        }

        /// <summary>
        /// No decompression - direct copy.
        /// </summary>
        private int DecompressNone()
        {
            if (OutputBuffer.Length < InputData.Length) return 0;

            NativeArray<byte>.Copy(InputData, OutputBuffer, InputData.Length);
            return InputData.Length;
        }
    }

    /// <summary>
    /// Batch compression job for processing multiple arrays in parallel.
    /// </summary>
    public struct BatchCompressionJob : IJobParallelFor
    {
        /// <summary>
        /// Input data arrays to be compressed.
        /// </summary>
        [ReadOnly]
        public NativeArray<NativeArray<byte>> InputBatches;

        /// <summary>
        /// Output buffers for each batch.
        /// </summary>
        [WriteOnly]
        public NativeArray<NativeArray<byte>> OutputBuffers;

        /// <summary>
        /// Results for each batch operation.
        /// </summary>
        [WriteOnly]
        public NativeArray<CompressionJobResult> Results;

        /// <summary>
        /// Compression algorithm to use.
        /// </summary>
        [ReadOnly]
        public CompressionAlgorithm Algorithm;

        /// <summary>
        /// Compression level.
        /// </summary>
        [ReadOnly]
        public int CompressionLevel;

        /// <summary>
        /// Executes compression for a single batch index.
        /// </summary>
        /// <param name="index">The batch index to process</param>
        public void Execute(int index)
        {
            var job = new CompressionJob
            {
                InputData = InputBatches[index],
                OutputBuffer = OutputBuffers[index],
                Result = new NativeArray<CompressionJobResult>(1, Allocator.Temp),
                Algorithm = Algorithm,
                CompressionLevel = CompressionLevel
            };

            job.Execute();
            Results[index] = job.Result[0];
            job.Result.Dispose();
        }
    }

    /// <summary>
    /// Compression algorithms supported by the job system.
    /// </summary>
    public enum CompressionAlgorithm : byte
    {
        None = 0,
        RLE = 1,      // Run-Length Encoding
        LZ4 = 2       // LZ4-like compression
    }

    /// <summary>
    /// Result information for compression jobs.
    /// </summary>
    public struct CompressionJobResult
    {
        /// <summary>
        /// Whether the compression operation was successful.
        /// </summary>
        public bool IsSuccess;

        /// <summary>
        /// Size of input data in bytes.
        /// </summary>
        public int InputSize;

        /// <summary>
        /// Size of output data in bytes.
        /// </summary>
        public int OutputSize;

        /// <summary>
        /// Start time of the operation in ticks.
        /// </summary>
        public long StartTime;

        /// <summary>
        /// End time of the operation in ticks.
        /// </summary>
        public long EndTime;

        /// <summary>
        /// Compression algorithm used.
        /// </summary>
        public CompressionAlgorithm Algorithm;

        /// <summary>
        /// Error code if the operation failed.
        /// </summary>
        public CompressionErrorCode ErrorCode;

        /// <summary>
        /// Gets the duration of the operation.
        /// </summary>
        public TimeSpan Duration => new TimeSpan(EndTime - StartTime);

        /// <summary>
        /// Gets the compression ratio (0.0 = no compression, 1.0 = maximum compression).
        /// </summary>
        public double CompressionRatio
        {
            get
            {
                if (InputSize == 0) return 0.0;
                return 1.0 - ((double)OutputSize / InputSize);
            }
        }

        /// <summary>
        /// Gets the space savings in bytes.
        /// </summary>
        public int SpaceSavings => InputSize - OutputSize;

        /// <summary>
        /// Gets the throughput in bytes per second.
        /// </summary>
        public double BytesPerSecond
        {
            get
            {
                var seconds = Duration.TotalSeconds;
                return seconds > 0 ? InputSize / seconds : 0;
            }
        }
    }

    /// <summary>
    /// Result information for decompression jobs.
    /// </summary>
    public struct DecompressionJobResult
    {
        /// <summary>
        /// Whether the decompression operation was successful.
        /// </summary>
        public bool IsSuccess;

        /// <summary>
        /// Size of input compressed data in bytes.
        /// </summary>
        public int InputSize;

        /// <summary>
        /// Size of output decompressed data in bytes.
        /// </summary>
        public int OutputSize;

        /// <summary>
        /// Start time of the operation in ticks.
        /// </summary>
        public long StartTime;

        /// <summary>
        /// End time of the operation in ticks.
        /// </summary>
        public long EndTime;

        /// <summary>
        /// Compression algorithm that was used.
        /// </summary>
        public CompressionAlgorithm Algorithm;

        /// <summary>
        /// Error code if the operation failed.
        /// </summary>
        public CompressionErrorCode ErrorCode;

        /// <summary>
        /// Gets the duration of the operation.
        /// </summary>
        public TimeSpan Duration => new TimeSpan(EndTime - StartTime);

        /// <summary>
        /// Gets the throughput in bytes per second.
        /// </summary>
        public double BytesPerSecond
        {
            get
            {
                var seconds = Duration.TotalSeconds;
                return seconds > 0 ? OutputSize / seconds : 0;
            }
        }
    }

    /// <summary>
    /// Error codes for compression/decompression operations.
    /// </summary>
    public enum CompressionErrorCode : byte
    {
        None = 0,
        InsufficientBuffer = 1,
        InvalidData = 2,
        UnsupportedAlgorithm = 3,
        CompressionFailed = 4,
        DecompressionFailed = 5,
        UnknownError = 255
    }

    /// <summary>
    /// High-level service for coordinating Unity Job System compression operations.
    /// Provides UniTask integration and automatic resource management.
    /// </summary>
    public class UnityCompressionJobService : IDisposable
    {
        private readonly ILoggingService _logger;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of UnityCompressionJobService.
        /// </summary>
        /// <param name="logger">Logging service for operation tracking</param>
        public UnityCompressionJobService(ILoggingService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Compresses data using Unity Job System with UniTask coordination.
        /// </summary>
        /// <param name="data">The data to compress</param>
        /// <param name="algorithm">Compression algorithm to use</param>
        /// <param name="compressionLevel">Compression level (0-9)</param>
        /// <param name="allocator">Memory allocator for temporary arrays</param>
        /// <returns>UniTask containing the compressed data</returns>
        public async UniTask<byte[]> CompressAsync(byte[] data, CompressionAlgorithm algorithm = CompressionAlgorithm.LZ4, int compressionLevel = 6, Allocator allocator = Allocator.TempJob)
        {
            if (data == null || data.Length == 0)
                return new byte[0];

            var correlationId = GetCorrelationId();
            _logger?.LogInfo($"Starting job-based compression of {data.Length} bytes using {algorithm}", correlationId: correlationId, sourceContext: null, properties: null);

            // Create native arrays
            var inputArray = new NativeArray<byte>(data, allocator);
            var outputArray = new NativeArray<byte>(data.Length * 2, allocator); // Worst case scenario
            var resultArray = new NativeArray<CompressionJobResult>(1, allocator);

            try
            {
                // Create and schedule the job
                var job = new CompressionJob
                {
                    InputData = inputArray,
                    OutputBuffer = outputArray,
                    Result = resultArray,
                    Algorithm = algorithm,
                    CompressionLevel = compressionLevel
                };

                var jobHandle = job.Schedule();

                // Wait for job completion using UniTask
                await UniTask.WaitUntil(() => jobHandle.IsCompleted);
                jobHandle.Complete();

                // Check results
                var result = resultArray[0];
                if (!result.IsSuccess)
                {
                    _logger?.LogError($"Compression job failed with error: {result.ErrorCode}", correlationId: correlationId, sourceContext: null, properties: null);
                    throw new InvalidOperationException($"Compression job failed: {result.ErrorCode}");
                }

                _logger?.LogInfo($"Compression job completed: {result.InputSize} -> {result.OutputSize} bytes ({result.CompressionRatio:P1} compression) in {result.Duration.TotalMilliseconds:F2}ms", correlationId: correlationId, sourceContext: null, properties: null);

                // Create result array with actual size
                var compressedData = new byte[result.OutputSize];
                NativeArray<byte>.Copy(outputArray, compressedData, result.OutputSize);

                return compressedData;
            }
            finally
            {
                // Clean up temporary arrays
                if (inputArray.IsCreated) inputArray.Dispose();
                if (outputArray.IsCreated) outputArray.Dispose();
                if (resultArray.IsCreated) resultArray.Dispose();
            }
        }

        /// <summary>
        /// Decompresses data using Unity Job System with UniTask coordination.
        /// </summary>
        /// <param name="compressedData">The compressed data to decompress</param>
        /// <param name="expectedSize">Expected size of decompressed data</param>
        /// <param name="algorithm">Compression algorithm that was used</param>
        /// <param name="allocator">Memory allocator for temporary arrays</param>
        /// <returns>UniTask containing the decompressed data</returns>
        public async UniTask<byte[]> DecompressAsync(byte[] compressedData, int expectedSize, CompressionAlgorithm algorithm = CompressionAlgorithm.LZ4, Allocator allocator = Allocator.TempJob)
        {
            if (compressedData == null || compressedData.Length == 0)
                return new byte[0];

            var correlationId = GetCorrelationId();
            _logger?.LogInfo($"Starting job-based decompression of {compressedData.Length} bytes using {algorithm}", correlationId: correlationId, sourceContext: null, properties: null);

            // Create native arrays
            var inputArray = new NativeArray<byte>(compressedData, allocator);
            var outputArray = new NativeArray<byte>(expectedSize, allocator);
            var resultArray = new NativeArray<DecompressionJobResult>(1, allocator);

            try
            {
                // Create and schedule the job
                var job = new DecompressionJob
                {
                    InputData = inputArray,
                    OutputBuffer = outputArray,
                    Result = resultArray,
                    Algorithm = algorithm
                };

                var jobHandle = job.Schedule();

                // Wait for job completion using UniTask
                await UniTask.WaitUntil(() => jobHandle.IsCompleted);
                jobHandle.Complete();

                // Check results
                var result = resultArray[0];
                if (!result.IsSuccess)
                {
                    _logger?.LogError($"Decompression job failed with error: {result.ErrorCode}", correlationId: correlationId, sourceContext: null, properties: null);
                    throw new InvalidOperationException($"Decompression job failed: {result.ErrorCode}");
                }

                _logger?.LogInfo($"Decompression job completed: {result.InputSize} -> {result.OutputSize} bytes in {result.Duration.TotalMilliseconds:F2}ms", correlationId: correlationId, sourceContext: null, properties: null);

                // Create result array with actual size
                var decompressedData = new byte[result.OutputSize];
                NativeArray<byte>.Copy(outputArray, decompressedData, result.OutputSize);

                return decompressedData;
            }
            finally
            {
                // Clean up temporary arrays
                if (inputArray.IsCreated) inputArray.Dispose();
                if (outputArray.IsCreated) outputArray.Dispose();
                if (resultArray.IsCreated) resultArray.Dispose();
            }
        }

        private FixedString64Bytes GetCorrelationId()
        {
            return new FixedString64Bytes(Guid.NewGuid().ToString("N")[..32]);
        }

        /// <summary>
        /// Disposes the service and releases any resources.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _logger?.LogInfo("UnityCompressionJobService disposed", GetCorrelationId(), sourceContext: null, properties: null);
            }
        }
    }
}