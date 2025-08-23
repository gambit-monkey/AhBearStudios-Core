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
    /// Unity Job System integration for high-performance parallel serialization.
    /// Provides zero-allocation, burst-compatible serialization jobs optimized for 60+ FPS gameplay.
    /// Uses NativeArrays for data transfer and UniTask for async coordination.
    /// </summary>
    public struct SerializationJob<T> : IJob where T : unmanaged
    {
        /// <summary>
        /// Input data to be serialized.
        /// </summary>
        [ReadOnly]
        public NativeArray<T> InputData;

        /// <summary>
        /// Output buffer for serialized data.
        /// Must be allocated with sufficient size before job execution.
        /// </summary>
        [WriteOnly]
        public NativeArray<byte> OutputBuffer;

        /// <summary>
        /// Result information about the serialization operation.
        /// </summary>
        [WriteOnly]
        public NativeArray<SerializationJobResult> Result;

        /// <summary>
        /// Executes the serialization job.
        /// This method runs on a background thread and must be Burst-compatible.
        /// </summary>
        public void Execute()
        {
            var result = new SerializationJobResult
            {
                StartTime = DateTime.UtcNow.Ticks,
                IsSuccess = false,
                BytesWritten = 0,
                ItemsProcessed = 0
            };

            try
            {
                var outputIndex = 0;
                
                // Process each item in the input data
                for (int i = 0; i < InputData.Length; i++)
                {
                    var item = InputData[i];
                    
                    // For unmanaged types, we can use direct memory copying
                    var itemSize = Unity.Collections.LowLevel.Unsafe.UnsafeUtility.SizeOf<T>();
                    
                    // Check if we have enough space in output buffer
                    if (outputIndex + itemSize > OutputBuffer.Length)
                    {
                        result.ErrorCode = SerializationErrorCode.InsufficientBuffer;
                        break;
                    }
                    
                    // Copy the item data directly to output buffer
                    unsafe
                    {
                        var itemPtr = Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AddressOf(ref item);
                        var outputPtr = (byte*)OutputBuffer.GetUnsafePtr() + outputIndex;
                        Unity.Collections.LowLevel.Unsafe.UnsafeUtility.MemCpy(outputPtr, itemPtr, itemSize);
                    }
                    
                    outputIndex += itemSize;
                    result.ItemsProcessed++;
                }

                result.BytesWritten = outputIndex;
                result.IsSuccess = true;
                result.ErrorCode = SerializationErrorCode.None;
            }
            catch
            {
                result.IsSuccess = false;
                result.ErrorCode = SerializationErrorCode.UnknownError;
            }
            finally
            {
                result.EndTime = DateTime.UtcNow.Ticks;
                Result[0] = result;
            }
        }
    }

    /// <summary>
    /// Batch serialization job for processing multiple arrays in parallel.
    /// Optimized for scenarios where you need to serialize multiple collections simultaneously.
    /// </summary>
    public struct BatchSerializationJob<T> : IJobParallelFor where T : unmanaged
    {
        /// <summary>
        /// Input data arrays to be serialized.
        /// </summary>
        [ReadOnly]
        public NativeArray<NativeArray<T>> InputBatches;

        /// <summary>
        /// Output buffers for each batch.
        /// </summary>
        [WriteOnly]
        public NativeArray<NativeArray<byte>> OutputBuffers;

        /// <summary>
        /// Results for each batch operation.
        /// </summary>
        [WriteOnly]
        public NativeArray<SerializationJobResult> Results;

        /// <summary>
        /// Executes serialization for a single batch index.
        /// </summary>
        /// <param name="index">The batch index to process</param>
        public void Execute(int index)
        {
            var result = new SerializationJobResult
            {
                StartTime = DateTime.UtcNow.Ticks,
                IsSuccess = false,
                BytesWritten = 0,
                ItemsProcessed = 0
            };

            try
            {
                var inputBatch = InputBatches[index];
                var outputBuffer = OutputBuffers[index];
                var outputIndex = 0;
                
                // Process each item in this batch
                for (int i = 0; i < inputBatch.Length; i++)
                {
                    var item = inputBatch[i];
                    var itemSize = Unity.Collections.LowLevel.Unsafe.UnsafeUtility.SizeOf<T>();
                    
                    if (outputIndex + itemSize > outputBuffer.Length)
                    {
                        result.ErrorCode = SerializationErrorCode.InsufficientBuffer;
                        break;
                    }
                    
                    unsafe
                    {
                        var itemPtr = Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AddressOf(ref item);
                        var outputPtr = (byte*)outputBuffer.GetUnsafePtr() + outputIndex;
                        Unity.Collections.LowLevel.Unsafe.UnsafeUtility.MemCpy(outputPtr, itemPtr, itemSize);
                    }
                    
                    outputIndex += itemSize;
                    result.ItemsProcessed++;
                }

                result.BytesWritten = outputIndex;
                result.IsSuccess = true;
                result.ErrorCode = SerializationErrorCode.None;
            }
            catch
            {
                result.IsSuccess = false;
                result.ErrorCode = SerializationErrorCode.UnknownError;
            }
            finally
            {
                result.EndTime = DateTime.UtcNow.Ticks;
                Results[index] = result;
            }
        }
    }

    /// <summary>
    /// Result information for serialization jobs.
    /// Contains performance metrics and error information.
    /// </summary>
    public struct SerializationJobResult
    {
        /// <summary>
        /// Whether the serialization operation was successful.
        /// </summary>
        public bool IsSuccess;

        /// <summary>
        /// Number of bytes written to the output buffer.
        /// </summary>
        public int BytesWritten;

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
        public SerializationErrorCode ErrorCode;

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
                return seconds > 0 ? BytesWritten / seconds : 0;
            }
        }
    }

    /// <summary>
    /// Error codes for serialization operations.
    /// </summary>
    public enum SerializationErrorCode : byte
    {
        None = 0,
        InsufficientBuffer = 1,
        InvalidData = 2,
        UnknownError = 255
    }

    /// <summary>
    /// High-level service for coordinating Unity Job System serialization operations.
    /// Provides UniTask integration and automatic resource management.
    /// </summary>
    public class UnitySerializationJobService : IDisposable
    {
        private readonly ILoggingService _logger;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of UnitySerializationJobService.
        /// </summary>
        /// <param name="logger">Logging service for operation tracking</param>
        public UnitySerializationJobService(ILoggingService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Serializes data using Unity Job System with UniTask coordination.
        /// </summary>
        /// <typeparam name="T">The unmanaged type to serialize</typeparam>
        /// <param name="data">The data to serialize</param>
        /// <param name="allocator">Memory allocator for temporary arrays</param>
        /// <returns>UniTask containing the serialized data</returns>
        public async UniTask<NativeArray<byte>> SerializeAsync<T>(T[] data, Allocator allocator = Allocator.TempJob) where T : unmanaged
        {
            if (data == null || data.Length == 0)
                return new NativeArray<byte>(0, allocator);

            var correlationId = GetCorrelationId();
            _logger?.LogInfo($"Starting job-based serialization of {data.Length} items of type {typeof(T).Name}", correlationId, sourceContext: null, properties: null);

            // Create native arrays
            var inputArray = new NativeArray<T>(data, allocator);
            var itemSize = Unity.Collections.LowLevel.Unsafe.UnsafeUtility.SizeOf<T>();
            var outputArray = new NativeArray<byte>(data.Length * itemSize, allocator);
            var resultArray = new NativeArray<SerializationJobResult>(1, allocator);

            try
            {
                // Create and schedule the job
                var job = new SerializationJob<T>
                {
                    InputData = inputArray,
                    OutputBuffer = outputArray,
                    Result = resultArray
                };

                var jobHandle = job.Schedule();

                // Wait for job completion using UniTask
                await UniTask.WaitUntil(() => jobHandle.IsCompleted);
                jobHandle.Complete();

                // Check results
                var result = resultArray[0];
                if (!result.IsSuccess)
                {
                    _logger?.LogError($"Serialization job failed with error: {result.ErrorCode}", correlationId, sourceContext: null, properties: null);
                    throw new InvalidOperationException($"Serialization job failed: {result.ErrorCode}");
                }

                _logger?.LogInfo($"Serialization job completed: {result.ItemsProcessed} items, {result.BytesWritten} bytes in {result.Duration.TotalMilliseconds:F2}ms", correlationId, sourceContext: null, properties: null);

                // Create result array with actual size
                var actualOutput = new NativeArray<byte>(result.BytesWritten, allocator);
                NativeArray<byte>.Copy(outputArray, actualOutput, result.BytesWritten);

                return actualOutput;
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
        /// Serializes multiple batches in parallel using Unity Job System.
        /// </summary>
        /// <typeparam name="T">The unmanaged type to serialize</typeparam>
        /// <param name="batches">Array of data batches to serialize</param>
        /// <param name="allocator">Memory allocator for temporary arrays</param>
        /// <returns>UniTask containing arrays of serialized data for each batch</returns>
        public async UniTask<NativeArray<byte>[]> SerializeBatchesAsync<T>(T[][] batches, Allocator allocator = Allocator.TempJob) where T : unmanaged
        {
            if (batches == null || batches.Length == 0)
                return new NativeArray<byte>[0];

            var correlationId = GetCorrelationId();
            _logger?.LogInfo($"Starting batch serialization of {batches.Length} batches", correlationId, sourceContext: null, properties: null);

            // Convert to native arrays
            var inputBatches = new NativeArray<NativeArray<T>>(batches.Length, allocator);
            var outputBuffers = new NativeArray<NativeArray<byte>>(batches.Length, allocator);
            var results = new NativeArray<SerializationJobResult>(batches.Length, allocator);

            try
            {
                var itemSize = Unity.Collections.LowLevel.Unsafe.UnsafeUtility.SizeOf<T>();

                // Setup input and output arrays
                for (int i = 0; i < batches.Length; i++)
                {
                    inputBatches[i] = new NativeArray<T>(batches[i], allocator);
                    outputBuffers[i] = new NativeArray<byte>(batches[i].Length * itemSize, allocator);
                }

                // Create and schedule parallel job
                var job = new BatchSerializationJob<T>
                {
                    InputBatches = inputBatches,
                    OutputBuffers = outputBuffers,
                    Results = results
                };

                var jobHandle = job.Schedule(batches.Length, 1);

                // Wait for completion
                await UniTask.WaitUntil(() => jobHandle.IsCompleted);
                jobHandle.Complete();

                // Process results
                var outputs = new NativeArray<byte>[batches.Length];
                for (int i = 0; i < batches.Length; i++)
                {
                    var result = results[i];
                    if (result.IsSuccess)
                    {
                        outputs[i] = new NativeArray<byte>(result.BytesWritten, allocator);
                        NativeArray<byte>.Copy(outputBuffers[i], outputs[i], result.BytesWritten);
                    }
                    else
                    {
                        _logger?.LogError($"Batch {i} serialization failed: {result.ErrorCode}", correlationId, sourceContext: null, properties: null);
                        outputs[i] = new NativeArray<byte>(0, allocator);
                    }
                }

                var totalItems = results.AsValueEnumerable().Sum(r => r.ItemsProcessed);
                var totalBytes = results.AsValueEnumerable().Sum(r => r.BytesWritten);
                var maxDuration = results.AsValueEnumerable().Max(r => r.Duration.TotalMilliseconds);

                _logger?.LogInfo($"Batch serialization completed: {totalItems} items, {totalBytes} bytes, max duration {maxDuration:F2}ms", correlationId, sourceContext: null, properties: null);

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
                _logger?.LogInfo("UnitySerializationJobService disposed", GetCorrelationId(), sourceContext: null, properties: null);
            }
        }
    }
}