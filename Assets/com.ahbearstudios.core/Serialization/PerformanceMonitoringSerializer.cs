using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Unity.Collections;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Serialization.Models;
using Cysharp.Threading.Tasks;
using ZLinq;

namespace AhBearStudios.Core.Serialization
{
    /// <summary>
    /// Decorator serializer that adds comprehensive performance monitoring to any ISerializer implementation.
    /// Tracks timing, throughput, memory usage, and error rates for all serialization operations.
    /// </summary>
    public class PerformanceMonitoringSerializer : ISerializer, IDisposable
    {
        private readonly ISerializer _innerSerializer;
        private readonly ILoggingService _logger;
        private readonly PerformanceMetricsCollector _metricsCollector;
        private readonly ConcurrentDictionary<string, OperationMetrics> _operationMetrics;
        private readonly Timer _reportingTimer;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of PerformanceMonitoringSerializer.
        /// </summary>
        /// <param name="innerSerializer">The serializer to wrap with performance monitoring</param>
        /// <param name="logger">Logging service for metrics reporting</param>
        /// <param name="reportingIntervalMs">Interval for automatic metrics reporting in milliseconds</param>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null</exception>
        public PerformanceMonitoringSerializer(
            ISerializer innerSerializer, 
            ILoggingService logger,
            int reportingIntervalMs = 60000) // Default: 1 minute
        {
            _innerSerializer = innerSerializer ?? throw new ArgumentNullException(nameof(innerSerializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _metricsCollector = new PerformanceMetricsCollector();
            _operationMetrics = new ConcurrentDictionary<string, OperationMetrics>();
            
            // Set up automatic metrics reporting
            _reportingTimer = new Timer(ReportMetrics, null, reportingIntervalMs, reportingIntervalMs);

            var correlationId = GetCorrelationId();
            _logger.LogInfo($"PerformanceMonitoringSerializer initialized wrapping {innerSerializer.GetType().Name}", correlationId: correlationId, sourceContext: null, properties: null);
        }

        /// <inheritdoc />
        public byte[] Serialize<T>(T obj)
        {
            return ExecuteWithMonitoring(
                () => _innerSerializer.Serialize(obj),
                $"Serialize_{typeof(T).Name}",
                obj);
        }

        /// <inheritdoc />
        public T Deserialize<T>(byte[] data)
        {
            return ExecuteWithMonitoring(
                () => _innerSerializer.Deserialize<T>(data),
                $"Deserialize_{typeof(T).Name}",
                data?.Length ?? 0);
        }

        /// <inheritdoc />
        public T Deserialize<T>(ReadOnlySpan<byte> data)
        {
            // ReadOnlySpan cannot be captured in lambda, so we need to handle this differently
            var startTime = DateTime.UtcNow;
            var startMemory = GC.GetTotalMemory(false);
            var correlationId = GetCorrelationId();
            var operationName = $"DeserializeSpan_{typeof(T).Name}";
            var dataLength = data.Length;

            try
            {
                var result = _innerSerializer.Deserialize<T>(data);
                
                var duration = DateTime.UtcNow - startTime;
                var endMemory = GC.GetTotalMemory(false);
                var memoryDelta = endMemory - startMemory;

                RecordSuccess(operationName, duration, memoryDelta, dataLength);
                
                // Log slow operations
                if (duration.TotalMilliseconds > 100) // Log operations over 100ms
                {
                    _logger.LogWarning($"Slow serialization operation: {operationName} took {duration.TotalMilliseconds:F2}ms", correlationId: correlationId, sourceContext: null, properties: null);
                }

                return result;
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                var endMemory = GC.GetTotalMemory(false);
                var memoryDelta = endMemory - startMemory;

                RecordFailure(operationName, duration, memoryDelta, ex);
                
                _logger.LogException($"Performance-monitored operation failed: {operationName}", ex, correlationId: correlationId, sourceContext: null, properties: null);
                throw;
            }
        }

        /// <inheritdoc />
        public bool TryDeserialize<T>(byte[] data, out T result)
        {
            T localResult = default;
            bool success = false;

            success = ExecuteWithMonitoring(
                () =>
                {
                    var innerSuccess = _innerSerializer.TryDeserialize(data, out localResult);
                    return innerSuccess;
                },
                $"TryDeserialize_{typeof(T).Name}",
                data?.Length ?? 0);

            result = localResult;
            return success;
        }

        /// <inheritdoc />
        public bool TryDeserialize<T>(ReadOnlySpan<byte> data, out T result)
        {
            // ReadOnlySpan cannot be captured in lambda, so we need to handle this differently
            var startTime = DateTime.UtcNow;
            var startMemory = GC.GetTotalMemory(false);
            var correlationId = GetCorrelationId();
            var operationName = $"TryDeserializeSpan_{typeof(T).Name}";
            var dataLength = data.Length;

            try
            {
                var success = _innerSerializer.TryDeserialize(data, out result);
                
                var duration = DateTime.UtcNow - startTime;
                var endMemory = GC.GetTotalMemory(false);
                var memoryDelta = endMemory - startMemory;

                RecordSuccess(operationName, duration, memoryDelta, dataLength);
                
                // Log slow operations
                if (duration.TotalMilliseconds > 100) // Log operations over 100ms
                {
                    _logger.LogWarning($"Slow serialization operation: {operationName} took {duration.TotalMilliseconds:F2}ms", correlationId: correlationId, sourceContext: null, properties: null);
                }

                return success;
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                var endMemory = GC.GetTotalMemory(false);
                var memoryDelta = endMemory - startMemory;

                RecordFailure(operationName, duration, memoryDelta, ex);
                
                _logger.LogException($"Performance-monitored operation failed: {operationName}", ex, correlationId: correlationId, sourceContext: null, properties: null);
                result = default;
                return false;
            }
        }

        /// <inheritdoc />
        public void RegisterType<T>()
        {
            ExecuteWithMonitoring(
                () => { _innerSerializer.RegisterType<T>(); return true; },
                $"RegisterType_{typeof(T).Name}",
                0);
        }

        /// <inheritdoc />
        public void RegisterType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            ExecuteWithMonitoring(
                () => { _innerSerializer.RegisterType(type); return true; },
                $"RegisterType_{type.Name}",
                0);
        }

        /// <inheritdoc />
        public bool IsRegistered<T>()
        {
            return ExecuteWithMonitoring(
                () => _innerSerializer.IsRegistered<T>(),
                $"IsRegistered_{typeof(T).Name}",
                0);
        }

        /// <inheritdoc />
        public bool IsRegistered(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            return ExecuteWithMonitoring(
                () => _innerSerializer.IsRegistered(type),
                $"IsRegistered_{type.Name}",
                0);
        }

        /// <inheritdoc />
        public async UniTask<byte[]> SerializeAsync<T>(T obj, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithMonitoringAsync(
                () => _innerSerializer.SerializeAsync(obj, cancellationToken),
                $"SerializeAsync_{typeof(T).Name}",
                obj);
        }

        /// <inheritdoc />
        public async UniTask<T> DeserializeAsync<T>(byte[] data, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithMonitoringAsync(
                () => _innerSerializer.DeserializeAsync<T>(data, cancellationToken),
                $"DeserializeAsync_{typeof(T).Name}",
                data?.Length ?? 0);
        }

        /// <inheritdoc />
        public void SerializeToStream<T>(T obj, Stream stream)
        {
            ExecuteWithMonitoring(
                () => { _innerSerializer.SerializeToStream(obj, stream); return true; },
                $"SerializeToStream_{typeof(T).Name}",
                obj);
        }

        /// <inheritdoc />
        public T DeserializeFromStream<T>(Stream stream)
        {
            return ExecuteWithMonitoring(
                () => _innerSerializer.DeserializeFromStream<T>(stream),
                $"DeserializeFromStream_{typeof(T).Name}",
                stream?.Length ?? 0);
        }

        /// <inheritdoc />
        public NativeArray<byte> SerializeToNativeArray<T>(T obj, Allocator allocator) where T : unmanaged
        {
            return ExecuteWithMonitoring(
                () => _innerSerializer.SerializeToNativeArray(obj, allocator),
                $"SerializeToNativeArray_{typeof(T).Name}",
                obj);
        }

        /// <inheritdoc />
        public T DeserializeFromNativeArray<T>(NativeArray<byte> data) where T : unmanaged
        {
            return ExecuteWithMonitoring(
                () => _innerSerializer.DeserializeFromNativeArray<T>(data),
                $"DeserializeFromNativeArray_{typeof(T).Name}",
                data.Length);
        }

        /// <inheritdoc />
        public SerializationStatistics GetStatistics()
        {
            var baseStats = _innerSerializer.GetStatistics();
            var performanceStats = _metricsCollector.GetStatistics();

            // Merge performance metrics with base statistics
            return baseStats with
            {
                PerformanceMetrics = performanceStats,
                TotalOperationTime = performanceStats.TotalOperationTime,
                AverageOperationTime = performanceStats.AverageOperationTime,
                PeakMemoryUsage = Math.Max(baseStats.PeakMemoryUsage, performanceStats.PeakMemoryUsage)
            };
        }

        /// <summary>
        /// Gets detailed operation metrics for specific operation types.
        /// </summary>
        /// <returns>Dictionary of operation metrics by operation name</returns>
        public IReadOnlyDictionary<string, OperationMetrics> GetOperationMetrics()
        {
            // Convert to regular dictionary since ZLinq doesn't have ToImmutableDictionary
            return _operationMetrics.AsValueEnumerable()
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        /// <summary>
        /// Gets comprehensive performance metrics.
        /// </summary>
        /// <returns>Performance metrics summary</returns>
        public PerformanceMetrics GetPerformanceMetrics()
        {
            return _metricsCollector.GetStatistics();
        }

        /// <summary>
        /// Resets all performance metrics.
        /// </summary>
        public void ResetMetrics()
        {
            _metricsCollector.Reset();
            _operationMetrics.Clear();

            var correlationId = GetCorrelationId();
            _logger.LogInfo("Performance metrics reset", correlationId: correlationId, sourceContext: null, properties: null);
        }

        private TResult ExecuteWithMonitoring<TResult>(Func<TResult> operation, string operationName, object context = null)
        {
            var startTime = DateTime.UtcNow;
            var startMemory = GC.GetTotalMemory(false);
            var correlationId = GetCorrelationId();

            try
            {
                var result = operation();
                
                var duration = DateTime.UtcNow - startTime;
                var endMemory = GC.GetTotalMemory(false);
                var memoryDelta = endMemory - startMemory;

                RecordSuccess(operationName, duration, memoryDelta, context);
                
                // Log slow operations
                if (duration.TotalMilliseconds > 100) // Log operations over 100ms
                {
                    _logger.LogWarning($"Slow serialization operation: {operationName} took {duration.TotalMilliseconds:F2}ms", correlationId: correlationId, sourceContext: null, properties: null);
                }

                return result;
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                var endMemory = GC.GetTotalMemory(false);
                var memoryDelta = endMemory - startMemory;

                RecordFailure(operationName, duration, memoryDelta, ex);
                
                _logger.LogException($"Performance-monitored operation failed: {operationName}", ex, correlationId: correlationId, sourceContext: null, properties: null);
                throw;
            }
        }

        private async UniTask<TResult> ExecuteWithMonitoringAsync<TResult>(Func<UniTask<TResult>> operation, string operationName, object context = null)
        {
            var startTime = DateTime.UtcNow;
            var startMemory = GC.GetTotalMemory(false);
            var correlationId = GetCorrelationId();

            try
            {
                var result = await operation();
                
                var duration = DateTime.UtcNow - startTime;
                var endMemory = GC.GetTotalMemory(false);
                var memoryDelta = endMemory - startMemory;

                RecordSuccess(operationName, duration, memoryDelta, context);
                
                // Log slow operations
                if (duration.TotalMilliseconds > 100) // Log operations over 100ms
                {
                    _logger.LogWarning($"Slow async serialization operation: {operationName} took {duration.TotalMilliseconds:F2}ms", correlationId: correlationId, sourceContext: null, properties: null);
                }

                return result;
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                var endMemory = GC.GetTotalMemory(false);
                var memoryDelta = endMemory - startMemory;

                RecordFailure(operationName, duration, memoryDelta, ex);
                
                _logger.LogException($"Performance-monitored async operation failed: {operationName}", ex, correlationId: correlationId, sourceContext: null, properties: null);
                throw;
            }
        }

        private void RecordSuccess(string operationName, TimeSpan duration, long memoryDelta, object context)
        {
            _metricsCollector.RecordOperation(operationName, duration, memoryDelta, true);
            
            var metrics = _operationMetrics.AddOrUpdate(operationName, 
                new OperationMetrics { OperationName = operationName },
                (key, existing) => existing);
                
            metrics.RecordSuccess(duration, memoryDelta, GetDataSize(context));
        }

        private void RecordFailure(string operationName, TimeSpan duration, long memoryDelta, Exception exception)
        {
            _metricsCollector.RecordOperation(operationName, duration, memoryDelta, false);
            
            var metrics = _operationMetrics.AddOrUpdate(operationName, 
                new OperationMetrics { OperationName = operationName },
                (key, existing) => existing);
                
            metrics.RecordFailure(duration, memoryDelta, exception);
        }

        private static long GetDataSize(object context)
        {
            return context switch
            {
                byte[] bytes => bytes.Length,
                int size => size,
                long size => size,
                _ => 0
            };
        }

        private void ReportMetrics(object state)
        {
            try
            {
                var correlationId = GetCorrelationId();
                var metrics = _metricsCollector.GetStatistics();
                
                _logger.LogInfo($"Performance Metrics Report - Operations: {metrics.TotalOperations}, " +
                              $"Avg Time: {metrics.AverageOperationTime.TotalMilliseconds:F2}ms, " +
                              $"Success Rate: {metrics.SuccessRate:P2}, " +
                              $"Peak Memory: {metrics.PeakMemoryUsage:N0} bytes", correlationId: correlationId, sourceContext: null, properties: null);

                // Report top 5 slowest operations
                var slowestOps = _operationMetrics.Values.AsValueEnumerable()
                    .OrderByDescending(m => m.AverageExecutionTime.TotalMilliseconds)
                    .Take(5)
                    .ToList();

                foreach (var op in slowestOps)
                {
                    _logger.LogInfo($"  {op.OperationName}: {op.TotalExecutions} calls, " +
                                  $"avg {op.AverageExecutionTime.TotalMilliseconds:F2}ms, " +
                                  $"success rate {op.SuccessRate:P2}", correlationId: correlationId, sourceContext: null, properties: null);
                }
            }
            catch (Exception ex)
            {
                // Don't let metrics reporting crash the application
                var correlationId = GetCorrelationId();
                _logger.LogException("Failed to report performance metrics", ex, correlationId: correlationId, sourceContext: null, properties: null);
            }
        }

        private FixedString64Bytes GetCorrelationId()
        {
            return new FixedString64Bytes(Guid.NewGuid().ToString("N")[..32]);
        }

        /// <summary>
        /// Disposes the performance monitoring serializer and releases resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _reportingTimer?.Dispose();
                _metricsCollector?.Dispose();
                
                // Report final metrics before disposal
                ReportMetrics(null);
                
                // Dispose inner serializer if it implements IDisposable
                if (_innerSerializer is IDisposable disposableSerializer)
                {
                    disposableSerializer.Dispose();
                }

                _disposed = true;

                var correlationId = GetCorrelationId();
                _logger.LogInfo("PerformanceMonitoringSerializer disposed", correlationId: correlationId, sourceContext: null, properties: null);
            }
        }
    }

    /// <summary>
    /// Collects and aggregates performance metrics for serialization operations.
    /// Thread-safe implementation for high-concurrency scenarios.
    /// </summary>
    internal class PerformanceMetricsCollector : IDisposable
    {
        private long _totalOperations;
        private long _successfulOperations;
        private long _failedOperations;
        private long _totalOperationTimeTicks;
        private long _peakMemoryUsage;
        private readonly ConcurrentQueue<OperationRecord> _recentOperations;
        private readonly object _lockObject = new();
        private bool _disposed;

        public PerformanceMetricsCollector()
        {
            _recentOperations = new ConcurrentQueue<OperationRecord>();
        }

        public void RecordOperation(string operationName, TimeSpan duration, long memoryDelta, bool success)
        {
            if (_disposed) return;

            lock (_lockObject)
            {
                Interlocked.Increment(ref _totalOperations);
                
                if (success)
                    Interlocked.Increment(ref _successfulOperations);
                else
                    Interlocked.Increment(ref _failedOperations);

                Interlocked.Add(ref _totalOperationTimeTicks, duration.Ticks);

                // Update peak memory usage
                var currentMemory = GC.GetTotalMemory(false);
                if (currentMemory > _peakMemoryUsage)
                {
                    Interlocked.Exchange(ref _peakMemoryUsage, currentMemory);
                }

                // Keep recent operations for trend analysis (limit to last 1000)
                _recentOperations.Enqueue(new OperationRecord
                {
                    OperationName = operationName,
                    Timestamp = DateTime.UtcNow,
                    Duration = duration,
                    MemoryDelta = memoryDelta,
                    Success = success
                });

                // Trim old records
                while (_recentOperations.Count > 1000)
                {
                    _recentOperations.TryDequeue(out _);
                }
            }
        }

        public PerformanceMetrics GetStatistics()
        {
            if (_disposed)
                return new PerformanceMetrics();

            lock (_lockObject)
            {
                var totalOps = _totalOperations;
                var avgTime = totalOps > 0 
                    ? new TimeSpan(_totalOperationTimeTicks / totalOps) 
                    : TimeSpan.Zero;

                return new PerformanceMetrics
                {
                    TotalOperations = totalOps,
                    SuccessfulOperations = _successfulOperations,
                    FailedOperations = _failedOperations,
                    SuccessRate = totalOps > 0 ? (double)_successfulOperations / totalOps : 0.0,
                    TotalOperationTime = new TimeSpan(_totalOperationTimeTicks),
                    AverageOperationTime = avgTime,
                    PeakMemoryUsage = _peakMemoryUsage,
                    RecentOperations = _recentOperations.ToArray()
                };
            }
        }

        public void Reset()
        {
            if (_disposed) return;

            lock (_lockObject)
            {
                _totalOperations = 0;
                _successfulOperations = 0;
                _failedOperations = 0;
                _totalOperationTimeTicks = 0;
                _peakMemoryUsage = 0;
                
                while (_recentOperations.TryDequeue(out _)) { }
            }
        }

        public void Dispose()
        {
            _disposed = true;
            lock (_lockObject)
            {
                while (_recentOperations.TryDequeue(out _)) { }
            }
        }
    }

    /// <summary>
    /// Metrics for a specific operation type.
    /// </summary>
    public class OperationMetrics
    {
        private readonly object _lock = new();
        private long _totalExecutions;
        private long _successfulExecutions;
        private long _failedExecutions;
        private long _totalExecutionTimeTicks;
        private long _totalDataSize;
        private long _totalMemoryUsed;

        public string OperationName { get; init; }
        public long TotalExecutions => _totalExecutions;
        public long SuccessfulExecutions => _successfulExecutions;
        public long FailedExecutions => _failedExecutions;
        public double SuccessRate => _totalExecutions > 0 ? (double)_successfulExecutions / _totalExecutions : 0.0;
        public TimeSpan TotalExecutionTime => new(_totalExecutionTimeTicks);
        public TimeSpan AverageExecutionTime => _totalExecutions > 0 ? new(_totalExecutionTimeTicks / _totalExecutions) : TimeSpan.Zero;
        public long TotalDataSize => _totalDataSize;
        public long AverageDataSize => _totalExecutions > 0 ? _totalDataSize / _totalExecutions : 0;
        public long TotalMemoryUsed => _totalMemoryUsed;

        public void RecordSuccess(TimeSpan duration, long memoryDelta, long dataSize)
        {
            lock (_lock)
            {
                Interlocked.Increment(ref _totalExecutions);
                Interlocked.Increment(ref _successfulExecutions);
                Interlocked.Add(ref _totalExecutionTimeTicks, duration.Ticks);
                Interlocked.Add(ref _totalDataSize, dataSize);
                Interlocked.Add(ref _totalMemoryUsed, Math.Max(0, memoryDelta));
            }
        }

        public void RecordFailure(TimeSpan duration, long memoryDelta, Exception exception)
        {
            lock (_lock)
            {
                Interlocked.Increment(ref _totalExecutions);
                Interlocked.Increment(ref _failedExecutions);
                Interlocked.Add(ref _totalExecutionTimeTicks, duration.Ticks);
                Interlocked.Add(ref _totalMemoryUsed, Math.Max(0, memoryDelta));
            }
        }
    }

    /// <summary>
    /// Comprehensive performance metrics for serialization operations.
    /// </summary>
    public record PerformanceMetrics
    {
        public long TotalOperations { get; init; }
        public long SuccessfulOperations { get; init; }
        public long FailedOperations { get; init; }
        public double SuccessRate { get; init; }
        public TimeSpan TotalOperationTime { get; init; }
        public TimeSpan AverageOperationTime { get; init; }
        public long PeakMemoryUsage { get; init; }
        public OperationRecord[] RecentOperations { get; init; } = Array.Empty<OperationRecord>();
    }

    /// <summary>
    /// Record of a single serialization operation for trend analysis.
    /// </summary>
    public record OperationRecord
    {
        public string OperationName { get; init; }
        public DateTime Timestamp { get; init; }
        public TimeSpan Duration { get; init; }
        public long MemoryDelta { get; init; }
        public bool Success { get; init; }
    }
}