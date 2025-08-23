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
    /// Unity Job System integration for high-performance parallel deserialization.
    /// Provides zero-allocation, burst-compatible deserialization jobs optimized for 60+ FPS gameplay.
    /// Uses NativeArrays for data transfer and UniTask for async coordination.
    /// </summary>
    public struct DeserializationJob<T> : IJob where T : unmanaged
    {
        /// <summary>
        /// Input serialized data to be deserialized.
        /// </summary>
        [ReadOnly]
        public NativeArray<byte> InputData;

        /// <summary>
        /// Output buffer for deserialized items.
        /// Must be allocated with sufficient size before job execution.
        /// </summary>
        [WriteOnly]
        public NativeArray<T> OutputBuffer;

        /// <summary>
        /// Result information about the deserialization operation.
        /// </summary>
        [WriteOnly]
        public NativeArray<DeserializationJobResult> Result;

        /// <summary>
        /// Expected item size for validation.
        /// </summary>
        [ReadOnly]
        public int ItemSize;

        /// <summary>
        /// Executes the deserialization job.
        /// This method runs on a background thread and must be Burst-compatible.
        /// </summary>
        public void Execute()
        {
            var result = new DeserializationJobResult
            {
                StartTime = DateTime.UtcNow.Ticks,
                IsSuccess = false,
                BytesRead = 0,
                ItemsProcessed = 0
            };

            try
            {
                var inputIndex = 0;
                var outputIndex = 0;
                
                // Calculate expected number of items
                var expectedItems = InputData.Length / ItemSize;
                
                // Validate we have enough output space
                if (expectedItems > OutputBuffer.Length)
                {
                    result.ErrorCode = DeserializationErrorCode.InsufficientBuffer;
                    return;
                }

                // Process each item in the input data
                while (inputIndex + ItemSize <= InputData.Length && outputIndex < OutputBuffer.Length)
                {
                    // For unmanaged types, we can use direct memory copying
                    unsafe
                    {
                        var inputPtr = (byte*)InputData.GetUnsafeReadOnlyPtr() + inputIndex;
                        var outputPtr = Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AddressOf(ref OutputBuffer.ElementAt(outputIndex));
                        Unity.Collections.LowLevel.Unsafe.UnsafeUtility.MemCpy(outputPtr, inputPtr, ItemSize);
                    }
                    
                    inputIndex += ItemSize;
                    outputIndex++;
                    result.ItemsProcessed++;
                }

                result.BytesRead = inputIndex;
                result.IsSuccess = true;
                result.ErrorCode = DeserializationErrorCode.None;
            }
            catch
            {
                result.IsSuccess = false;
                result.ErrorCode = DeserializationErrorCode.UnknownError;
            }
            finally
            {
                result.EndTime = DateTime.UtcNow.Ticks;
                Result[0] = result;
            }
        }
    }

    /// <summary>
    /// Batch deserialization job for processing multiple arrays in parallel.
    /// Optimized for scenarios where you need to deserialize multiple collections simultaneously.
    /// </summary>
    public struct BatchDeserializationJob<T> : IJobParallelFor where T : unmanaged
    {
        /// <summary>
        /// Input serialized data arrays to be deserialized.
        /// </summary>
        [ReadOnly]
        public NativeArray<NativeArray<byte>> InputBatches;

        /// <summary>
        /// Output buffers for each batch.
        /// </summary>
        [WriteOnly]
        public NativeArray<NativeArray<T>> OutputBuffers;

        /// <summary>
        /// Results for each batch operation.
        /// </summary>
        [WriteOnly]
        public NativeArray<DeserializationJobResult> Results;

        /// <summary>
        /// Expected item size for validation.
        /// </summary>
        [ReadOnly]
        public int ItemSize;

        /// <summary>
        /// Executes deserialization for a single batch index.
        /// </summary>
        /// <param name="index">The batch index to process</param>
        public void Execute(int index)
        {
            var result = new DeserializationJobResult
            {
                StartTime = DateTime.UtcNow.Ticks,
                IsSuccess = false,
                BytesRead = 0,
                ItemsProcessed = 0
            };

            try
            {
                var inputBatch = InputBatches[index];
                var outputBuffer = OutputBuffers[index];
                var inputIndex = 0;
                var outputIndex = 0;
                
                // Process each item in this batch
                while (inputIndex + ItemSize <= inputBatch.Length && outputIndex < outputBuffer.Length)
                {
                    unsafe
                    {
                        var inputPtr = (byte*)inputBatch.GetUnsafeReadOnlyPtr() + inputIndex;
                        var outputPtr = Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AddressOf(ref outputBuffer.ElementAt(outputIndex));
                        Unity.Collections.LowLevel.Unsafe.UnsafeUtility.MemCpy(outputPtr, inputPtr, ItemSize);
                    }
                    
                    inputIndex += ItemSize;
                    outputIndex++;
                    result.ItemsProcessed++;
                }

                result.BytesRead = inputIndex;
                result.IsSuccess = true;
                result.ErrorCode = DeserializationErrorCode.None;
            }
            catch
            {
                result.IsSuccess = false;
                result.ErrorCode = DeserializationErrorCode.UnknownError;
            }
            finally
            {
                result.EndTime = DateTime.UtcNow.Ticks;
                Results[index] = result;
            }
        }
    }

    /// <summary>
    /// Result information for deserialization jobs.
    /// Contains performance metrics and error information.
    /// </summary>
    public struct DeserializationJobResult
    {
        /// <summary>
        /// Whether the deserialization operation was successful.
        /// </summary>
        public bool IsSuccess;

        /// <summary>
        /// Number of bytes read from the input buffer.
        /// </summary>
        public int BytesRead;

        /// <summary>
        /// Number of items successfully processed.
        /// </summary>
        public int ItemsProcessed;

        /// <summary>
        /// Start time of the operation in ticks.
        /// </summary>
        public long StartTime;

        /// <summary>
        /// End time of the operation in ticks.
        /// </summary>
        public long EndTime;

        /// <summary>
        /// Error code if the operation failed.
        /// </summary>
        public DeserializationErrorCode ErrorCode;

        /// <summary>
        /// Gets the duration of the operation.
        /// </summary>
        public TimeSpan Duration => new TimeSpan(EndTime - StartTime);

        /// <summary>
        /// Gets the throughput in items per second.
        /// </summary>
        public double ItemsPerSecond
        {
            get
            {
                var seconds = Duration.TotalSeconds;
                return seconds > 0 ? ItemsProcessed / seconds : 0;
            }
        }

        /// <summary>
        /// Gets the throughput in bytes per second.
        /// </summary>
        public double BytesPerSecond
        {
            get
            {
                var seconds = Duration.TotalSeconds;
                return seconds > 0 ? BytesRead / seconds : 0;
            }
        }
    }

    /// <summary>
    /// Error codes for deserialization operations.
    /// </summary>
    public enum DeserializationErrorCode : byte
    {
        None = 0,
        InsufficientBuffer = 1,
        InvalidData = 2,
        CorruptedData = 3,
        UnknownError = 255
    }

    /// <summary>
    /// High-level service for coordinating Unity Job System deserialization operations.
    /// Provides UniTask integration and automatic resource management.
    /// </summary>
    public class UnityDeserializationJobService : IDisposable
    {
        private readonly ILoggingService _logger;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of UnityDeserializationJobService.
        /// </summary>
        /// <param name="logger">Logging service for operation tracking</param>
        public UnityDeserializationJobService(ILoggingService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Deserializes data using Unity Job System with UniTask coordination.
        /// </summary>
        /// <typeparam name="T">The unmanaged type to deserialize</typeparam>
        /// <param name="data">The serialized data to deserialize</param>
        /// <param name="expectedCount">Expected number of items to deserialize</param>
        /// <param name="allocator">Memory allocator for temporary arrays</param>
        /// <returns>UniTask containing the deserialized data</returns>
        public async UniTask<T[]> DeserializeAsync<T>(byte[] data, int expectedCount, Allocator allocator = Allocator.TempJob) where T : unmanaged
        {
            if (data == null || data.Length == 0)
                return new T[0];

            var correlationId = GetCorrelationId();
            _logger?.LogInfo($"Starting job-based deserialization of {data.Length} bytes to {expectedCount} items of type {typeof(T).Name}", correlationId, sourceContext: null, properties: null);

            // Create native arrays
            var inputArray = new NativeArray<byte>(data, allocator);
            var outputArray = new NativeArray<T>(expectedCount, allocator);
            var resultArray = new NativeArray<DeserializationJobResult>(1, allocator);

            try
            {
                // Create and schedule the job
                var itemSize = Unity.Collections.LowLevel.Unsafe.UnsafeUtility.SizeOf<T>();
                var job = new DeserializationJob<T>
                {
                    InputData = inputArray,
                    OutputBuffer = outputArray,
                    Result = resultArray,
                    ItemSize = itemSize
                };

                var jobHandle = job.Schedule();

                // Wait for job completion using UniTask
                await UniTask.WaitUntil(() => jobHandle.IsCompleted);
                jobHandle.Complete();

                // Check results
                var result = resultArray[0];
                if (!result.IsSuccess)
                {
                    _logger?.LogError($"Deserialization job failed with error: {result.ErrorCode}", correlationId, sourceContext: null, properties: null);
                    throw new InvalidOperationException($"Deserialization job failed: {result.ErrorCode}");
                }

                _logger?.LogInfo($"Deserialization job completed: {result.ItemsProcessed} items, {result.BytesRead} bytes in {result.Duration.TotalMilliseconds:F2}ms", correlationId, sourceContext: null, properties: null);

                // Convert to managed array
                var managedArray = new T[result.ItemsProcessed];
                NativeArray<T>.Copy(outputArray, managedArray, result.ItemsProcessed);

                return managedArray;
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
        /// Deserializes multiple batches in parallel using Unity Job System.
        /// </summary>
        /// <typeparam name="T">The unmanaged type to deserialize</typeparam>
        /// <param name="batches">Array of serialized data batches to deserialize</param>
        /// <param name="expectedCounts">Expected counts for each batch</param>
        /// <param name="allocator">Memory allocator for temporary arrays</param>
        /// <returns>UniTask containing arrays of deserialized data for each batch</returns>
        public async UniTask<T[][]> DeserializeBatchesAsync<T>(byte[][] batches, int[] expectedCounts, Allocator allocator = Allocator.TempJob) where T : unmanaged
        {
            if (batches == null || batches.Length == 0)
                return new T[0][];

            if (expectedCounts == null || expectedCounts.Length != batches.Length)
                throw new ArgumentException("Expected counts array must match batches array length");

            var correlationId = GetCorrelationId();
            _logger?.LogInfo($"Starting batch deserialization of {batches.Length} batches", correlationId, sourceContext: null, properties: null);

            // Convert to native arrays
            var inputBatches = new NativeArray<NativeArray<byte>>(batches.Length, allocator);
            var outputBuffers = new NativeArray<NativeArray<T>>(batches.Length, allocator);
            var results = new NativeArray<DeserializationJobResult>(batches.Length, allocator);

            try
            {
                var itemSize = Unity.Collections.LowLevel.Unsafe.UnsafeUtility.SizeOf<T>();

                // Setup input and output arrays
                for (int i = 0; i < batches.Length; i++)
                {
                    inputBatches[i] = new NativeArray<byte>(batches[i], allocator);
                    outputBuffers[i] = new NativeArray<T>(expectedCounts[i], allocator);
                }

                // Create and schedule parallel job
                var job = new BatchDeserializationJob<T>
                {
                    InputBatches = inputBatches,
                    OutputBuffers = outputBuffers,
                    Results = results,
                    ItemSize = itemSize
                };

                var jobHandle = job.Schedule(batches.Length, 1);

                // Wait for completion
                await UniTask.WaitUntil(() => jobHandle.IsCompleted);
                jobHandle.Complete();

                // Process results
                var outputs = new T[batches.Length][];
                for (int i = 0; i < batches.Length; i++)
                {
                    var result = results[i];
                    if (result.IsSuccess)
                    {
                        outputs[i] = new T[result.ItemsProcessed];
                        NativeArray<T>.Copy(outputBuffers[i], outputs[i], result.ItemsProcessed);
                    }
                    else
                    {
                        _logger?.LogError($"Batch {i} deserialization failed: {result.ErrorCode}", correlationId, sourceContext: null, properties: null);
                        outputs[i] = new T[0];
                    }
                }

                var totalItems = results.AsValueEnumerable().Sum(r => r.ItemsProcessed);
                var totalBytes = results.AsValueEnumerable().Sum(r => r.BytesRead);
                var maxDuration = results.AsValueEnumerable().Max(r => r.Duration.TotalMilliseconds);

                _logger?.LogInfo($"Batch deserialization completed: {totalItems} items, {totalBytes} bytes, max duration {maxDuration:F2}ms", correlationId, sourceContext: null, properties: null);

                return outputs;
            }
            finally
            {
                // Clean up arrays
                for (int i = 0; i < inputBatches.Length; i++)
                {
                    if (inputBatches[i].IsCreated) inputBatches[i].Dispose();
                    if (outputBuffers[i].IsCreated) outputBuffers[i].Dispose();
                }
                if (inputBatches.IsCreated) inputBatches.Dispose();
                if (outputBuffers.IsCreated) outputBuffers.Dispose();
                if (results.IsCreated) results.Dispose();
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
                _logger?.LogInfo("UnityDeserializationJobService disposed", GetCorrelationId(), sourceContext: null, properties: null);
            }
        }
    }
}