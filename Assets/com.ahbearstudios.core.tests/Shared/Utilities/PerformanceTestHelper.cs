using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ZLinq;
using Unity.Collections;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Tests.Shared.TestDoubles.Stubs;
using AhBearStudios.Core.Common.Models;

namespace AhBearStudios.Core.Tests.Shared.Utilities
{
    /// <summary>
    /// Helper class for measuring performance in tests, ensuring Unity frame budget compliance.
    /// Provides comprehensive performance measurement and analysis capabilities.
    /// Strictly follows CLAUDETESTS.md guidelines with TDD test double integration.
    /// Unity Test Runner compatible for both Edit Mode and Play Mode tests.
    /// </summary>
    public sealed class PerformanceTestHelper : IDisposable
    {
        private readonly List<PerformanceResult> _allResults = new List<PerformanceResult>();
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private StubLoggingService _loggingService;

        /// <summary>
        /// Initializes a new instance of PerformanceTestHelper.
        /// </summary>
        /// <param name="loggingService">Optional logging service for test double integration</param>
        public PerformanceTestHelper(StubLoggingService loggingService = null)
        {
            _loggingService = loggingService;
        }

        /// <summary>
        /// Gets all performance results recorded during the test session.
        /// </summary>
        public IReadOnlyList<PerformanceResult> AllResults => _allResults;

        /// <summary>
        /// Gets the total number of measurements taken.
        /// </summary>
        public int MeasurementCount => _allResults.Count;

        /// <summary>
        /// Measures the performance of a synchronous operation with optional correlation tracking.
        /// Validates Unity frame budget compliance and integrates with TDD test doubles.
        /// </summary>
        /// <param name="operation">The operation to measure</param>
        /// <param name="operationName">Name of the operation for reporting</param>
        /// <param name="correlationId">Optional correlation ID for tracking across test doubles</param>
        /// <returns>Performance measurement result</returns>
        public PerformanceResult Measure(Action operation, string operationName, Guid correlationId = default)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));
            if (string.IsNullOrEmpty(operationName))
                throw new ArgumentException("Operation name cannot be null or empty", nameof(operationName));

            // Force garbage collection before measurement
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var initialMemory = GC.GetTotalMemory(false);

            _stopwatch.Restart();

            try
            {
                operation();
            }
            finally
            {
                _stopwatch.Stop();
            }

            var finalMemory = GC.GetTotalMemory(false);
            var memoryUsed = Math.Max(0, finalMemory - initialMemory) / (1024.0 * 1024.0); // Convert to MB

            var result = new PerformanceResult(
                operationName: operationName,
                duration: _stopwatch.Elapsed,
                memoryUsed: memoryUsed,
                timestamp: DateTime.UtcNow,
                correlationId: correlationId);

            _allResults.Add(result);

            // Log performance metrics using test double if available
            LogPerformanceMetrics(result);

            return result;
        }

        /// <summary>
        /// Measures the performance of an asynchronous operation with optional correlation tracking.
        /// Validates Unity frame budget compliance and integrates with TDD test doubles.
        /// </summary>
        /// <param name="operation">The async operation to measure</param>
        /// <param name="operationName">Name of the operation for reporting</param>
        /// <param name="correlationId">Optional correlation ID for tracking across test doubles</param>
        /// <returns>Performance measurement result</returns>
        public async UniTask<PerformanceResult> MeasureAsync(Func<UniTask> operation, string operationName, Guid correlationId = default)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));
            if (string.IsNullOrEmpty(operationName))
                throw new ArgumentException("Operation name cannot be null or empty", nameof(operationName));

            // Force garbage collection before measurement
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var initialMemory = GC.GetTotalMemory(false);

            _stopwatch.Restart();

            try
            {
                await operation();
            }
            finally
            {
                _stopwatch.Stop();
            }

            var finalMemory = GC.GetTotalMemory(false);
            var memoryUsed = Math.Max(0, finalMemory - initialMemory) / (1024.0 * 1024.0); // Convert to MB

            var result = new PerformanceResult(
                operationName: operationName,
                duration: _stopwatch.Elapsed,
                memoryUsed: memoryUsed,
                timestamp: DateTime.UtcNow,
                correlationId: correlationId);

            _allResults.Add(result);

            // Log performance metrics using test double if available
            LogPerformanceMetrics(result);

            return result;
        }

        /// <summary>
        /// Measures multiple iterations of an operation and provides statistical analysis.
        /// </summary>
        /// <param name="operation">The operation to measure</param>
        /// <param name="operationName">Name of the operation for reporting</param>
        /// <param name="iterations">Number of iterations to perform</param>
        /// <returns>Statistical performance analysis</returns>
        public PerformanceStatistics MeasureIterations(Action operation, string operationName, int iterations)
        {
            if (iterations <= 0)
                throw new ArgumentException("Iterations must be positive", nameof(iterations));

            var results = new List<PerformanceResult>();

            for (int i = 0; i < iterations; i++)
            {
                var result = Measure(operation, $"{operationName}_Iteration_{i}");
                results.Add(result);
            }

            return new PerformanceStatistics(operationName, results);
        }

        /// <summary>
        /// Measures multiple iterations of an async operation and provides statistical analysis.
        /// </summary>
        /// <param name="operation">The async operation to measure</param>
        /// <param name="operationName">Name of the operation for reporting</param>
        /// <param name="iterations">Number of iterations to perform</param>
        /// <returns>Statistical performance analysis</returns>
        public async UniTask<PerformanceStatistics> MeasureIterationsAsync(Func<UniTask> operation, string operationName, int iterations)
        {
            if (iterations <= 0)
                throw new ArgumentException("Iterations must be positive", nameof(iterations));

            var results = new List<PerformanceResult>();

            for (int i = 0; i < iterations; i++)
            {
                var result = await MeasureAsync(operation, $"{operationName}_Iteration_{i}");
                results.Add(result);
            }

            return new PerformanceStatistics(operationName, results);
        }

        /// <summary>
        /// Gets performance statistics for all measurements taken.
        /// </summary>
        /// <returns>Overall performance statistics</returns>
        public PerformanceStatistics GetOverallStatistics()
        {
            return new PerformanceStatistics("Overall", _allResults);
        }

        /// <summary>
        /// Gets performance results filtered by operation name pattern.
        /// </summary>
        /// <param name="namePattern">Pattern to match operation names (supports wildcards)</param>
        /// <returns>Filtered performance results</returns>
        public IEnumerable<PerformanceResult> GetResultsByPattern(string namePattern)
        {
            if (string.IsNullOrEmpty(namePattern))
                return _allResults;

            var isWildcard = namePattern.EndsWith("*");
            var pattern = isWildcard ? namePattern.Substring(0, namePattern.Length - 1) : namePattern;

            return _allResults
                .Where(r => isWildcard ? r.OperationName.StartsWith(pattern) : r.OperationName == pattern)
                .ToList();
        }

        /// <summary>
        /// Clears all recorded performance results.
        /// </summary>
        public void Clear()
        {
            _allResults.Clear();
        }

        /// <summary>
        /// Validates that an operation completes within Unity's frame budget (16.67ms).
        /// Critical for maintaining 60 FPS performance following CLAUDETESTS.md guidelines.
        /// </summary>
        /// <param name="operation">The operation to validate</param>
        /// <param name="operationName">Name of the operation for reporting</param>
        /// <param name="correlationId">Optional correlation ID for tracking</param>
        /// <returns>Performance result with frame budget validation</returns>
        public PerformanceResult ValidateFrameBudgetCompliance(Action operation, string operationName, Guid correlationId = default)
        {
            var result = Measure(operation, operationName, correlationId);

            if (result.Duration > TestConstants.FrameBudget)
            {
                var message = $"Operation '{operationName}' took {result.Duration.TotalMilliseconds:F2}ms, " +
                             $"exceeding 60 FPS frame budget of {TestConstants.FrameBudget.TotalMilliseconds:F2}ms";
                _loggingService?.LogError(message);
                throw new InvalidOperationException(message);
            }

            _loggingService?.LogInfo($"Frame Budget Compliance: '{operationName}' completed in {result.Duration.TotalMilliseconds:F2}ms");
            return result;
        }

        /// <summary>
        /// Validates that an async operation completes within Unity's frame budget (16.67ms).
        /// </summary>
        /// <param name="operation">The async operation to validate</param>
        /// <param name="operationName">Name of the operation for reporting</param>
        /// <param name="correlationId">Optional correlation ID for tracking</param>
        /// <returns>Performance result with frame budget validation</returns>
        public async UniTask<PerformanceResult> ValidateFrameBudgetComplianceAsync(Func<UniTask> operation, string operationName, Guid correlationId = default)
        {
            var result = await MeasureAsync(operation, operationName, correlationId);

            if (result.Duration > TestConstants.FrameBudget)
            {
                var message = $"Operation '{operationName}' took {result.Duration.TotalMilliseconds:F2}ms, " +
                             $"exceeding 60 FPS frame budget of {TestConstants.FrameBudget.TotalMilliseconds:F2}ms";
                _loggingService?.LogError(message);
                throw new InvalidOperationException(message);
            }

            _loggingService?.LogInfo($"Frame Budget Compliance: '{operationName}' completed in {result.Duration.TotalMilliseconds:F2}ms");
            return result;
        }

        /// <summary>
        /// Validates Unity Collections zero-allocation patterns for performance-critical operations.
        /// Essential for Unity game development performance requirements.
        /// </summary>
        /// <param name="operation">The operation to validate</param>
        /// <param name="operationName">Name of the operation for reporting</param>
        /// <param name="correlationId">Optional correlation ID for tracking</param>
        /// <returns>Performance result with allocation validation</returns>
        public PerformanceResult ValidateZeroAllocationPattern(Action operation, string operationName, Guid correlationId = default)
        {
            // Force cleanup before measurement
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var initialMemory = GC.GetTotalMemory(false);
            var result = Measure(operation, operationName, correlationId);
            var finalMemory = GC.GetTotalMemory(false);
            var allocatedBytes = Math.Max(0, finalMemory - initialMemory);

            if (allocatedBytes > 0)
            {
                var message = $"Operation '{operationName}' should produce zero allocations but allocated {allocatedBytes} bytes";
                _loggingService?.LogWarning(message);
            }
            else
            {
                _loggingService?.LogInfo($"Zero Allocation Validation: '{operationName}' passed with 0 bytes allocated");
            }

            return result;
        }

        /// <summary>
        /// Performs stress testing with realistic game load (1000+ operations).
        /// Validates system performance under load following CLAUDETESTS.md guidelines.
        /// </summary>
        /// <param name="operation">The operation to stress test</param>
        /// <param name="operationName">Name of the operation for reporting</param>
        /// <param name="iterations">Number of iterations (default 1000 for realistic game load)</param>
        /// <param name="correlationId">Optional correlation ID for tracking</param>
        /// <returns>Stress test results with performance analysis</returns>
        public StressTestResult PerformStressTest(Action operation, string operationName, int iterations = 1000, Guid correlationId = default)
        {
            var results = new List<PerformanceResult>();
            var failures = 0;
            var stopwatch = Stopwatch.StartNew();

            _loggingService?.LogInfo($"Starting stress test '{operationName}' with {iterations} iterations");

            for (int i = 0; i < iterations; i++)
            {
                try
                {
                    var result = Measure(operation, $"{operationName}_StressIteration_{i}", correlationId);
                    results.Add(result);

                    // Check frame budget compliance
                    if (result.Duration > TestConstants.FrameBudget)
                    {
                        _loggingService?.LogWarning($"Stress test iteration {i} exceeded frame budget: {result.Duration.TotalMilliseconds:F2}ms");
                    }
                }
                catch (Exception ex)
                {
                    failures++;
                    _loggingService?.LogError($"Stress test iteration {i} failed: {ex.Message}");
                }
            }

            stopwatch.Stop();
            var stressResult = new StressTestResult(operationName, results, iterations, failures, stopwatch.Elapsed);

            _loggingService?.LogInfo($"Stress test '{operationName}' completed: {results.Count} successes, {failures} failures in {stopwatch.Elapsed.TotalSeconds:F2}s");

            return stressResult;
        }

        /// <summary>
        /// Logs performance metrics using the configured test double logging service.
        /// Integrates with TDD patterns for consistent performance tracking.
        /// </summary>
        /// <param name="result">The performance result to log</param>
        private void LogPerformanceMetrics(PerformanceResult result)
        {
            if (_loggingService == null) return;

            var frameBudgetStatus = result.Duration < TestConstants.FrameBudget ? "PASS" : "FAIL";
            var metricsMessage = $"Performance Metrics: {result.OperationName} - " +
                               $"Duration: {result.Duration.TotalMilliseconds:F2}ms, " +
                               $"Memory: {result.MemoryUsed:F2}MB, " +
                               $"Frame Budget: {frameBudgetStatus}";

            if (result.CorrelationId != Guid.Empty)
            {
                metricsMessage += $", CorrelationId: {result.CorrelationId}";
            }

            _loggingService.LogInfo(metricsMessage);
        }

        /// <summary>
        /// Disposes of the helper and clears all data.
        /// </summary>
        public void Dispose()
        {
            Clear();
        }
    }

    /// <summary>
    /// Represents the result of a single performance measurement with correlation tracking.
    /// Enhanced for CLAUDETESTS.md compliance and TDD test double integration.
    /// </summary>
    public sealed class PerformanceResult
    {
        public string OperationName { get; }
        public TimeSpan Duration { get; }
        public double MemoryUsed { get; }
        public DateTime Timestamp { get; }
        public Guid CorrelationId { get; }

        public PerformanceResult(string operationName, TimeSpan duration, double memoryUsed, DateTime timestamp, Guid correlationId = default)
        {
            OperationName = operationName ?? throw new ArgumentNullException(nameof(operationName));
            Duration = duration;
            MemoryUsed = memoryUsed;
            Timestamp = timestamp;
            CorrelationId = correlationId;
        }

        public override string ToString()
        {
            var result = $"{OperationName}: {Duration.TotalMilliseconds:F2}ms, {MemoryUsed:F2}MB";
            if (CorrelationId != Guid.Empty)
            {
                result += $", CorrelationId: {CorrelationId}";
            }
            return result;
        }
    }

    /// <summary>
    /// Provides statistical analysis of multiple performance measurements.
    /// </summary>
    public sealed class PerformanceStatistics
    {
        public string OperationName { get; }
        public int SampleCount { get; }
        public TimeSpan MinDuration { get; }
        public TimeSpan MaxDuration { get; }
        public TimeSpan AverageDuration { get; }
        public TimeSpan MedianDuration { get; }
        public double MinMemory { get; }
        public double MaxMemory { get; }
        public double AverageMemory { get; }
        public double TotalMemory { get; }

        public PerformanceStatistics(string operationName, IList<PerformanceResult> results)
        {
            OperationName = operationName ?? throw new ArgumentNullException(nameof(operationName));

            if (results == null || results.Count == 0)
            {
                SampleCount = 0;
                return;
            }

            SampleCount = results.Count;

            var durations = results.Select(r => r.Duration).OrderBy(d => d).ToList();
            var memories = results.Select(r => r.MemoryUsed).ToList();

            MinDuration = durations.First();
            MaxDuration = durations.Last();
            AverageDuration = TimeSpan.FromMilliseconds(durations.Average(d => d.TotalMilliseconds));
            MedianDuration = durations[durations.Count / 2];

            MinMemory = memories.Count > 0 ? memories.Min() : 0;
            MaxMemory = memories.Count > 0 ? memories.Max() : 0;
            AverageMemory = memories.Count > 0 ? memories.Average() : 0;
            TotalMemory = memories.Sum();
        }

        public override string ToString()
        {
            if (SampleCount == 0)
                return $"{OperationName}: No samples";

            return $"{OperationName} ({SampleCount} samples):\n" +
                   $"  Duration: min={MinDuration.TotalMilliseconds:F2}ms, max={MaxDuration.TotalMilliseconds:F2}ms, avg={AverageDuration.TotalMilliseconds:F2}ms\n" +
                   $"  Memory: min={MinMemory:F2}MB, max={MaxMemory:F2}MB, avg={AverageMemory:F2}MB";
        }
    }

    /// <summary>
    /// Represents the result of a bulk operation performance test.
    /// </summary>
    public sealed class BulkPerformanceResult
    {
        public string OperationName { get; }
        public IReadOnlyList<PerformanceResult> BatchResults { get; }
        public int TotalItems { get; }
        public TimeSpan TotalDuration { get; }
        public double ThroughputPerSecond { get; }

        public BulkPerformanceResult(string operationName, IList<PerformanceResult> batchResults, int totalItems)
        {
            OperationName = operationName ?? throw new ArgumentNullException(nameof(operationName));
            BatchResults = batchResults?.ToList() ?? throw new ArgumentNullException(nameof(batchResults));
            TotalItems = totalItems;
            TotalDuration = TimeSpan.FromMilliseconds(batchResults.Sum(r => r.Duration.TotalMilliseconds));
            ThroughputPerSecond = TotalDuration.TotalSeconds > 0 ? TotalItems / TotalDuration.TotalSeconds : 0;
        }
    }

    /// <summary>
    /// Represents the result of a stress test.
    /// </summary>
    public sealed class StressTestResult
    {
        public string OperationName { get; }
        public IReadOnlyList<PerformanceResult> IterationResults { get; }
        public int TotalIterations { get; }
        public int FailureCount { get; }
        public TimeSpan TotalDuration { get; }
        public PerformanceStatistics Statistics { get; }

        public StressTestResult(string operationName, IList<PerformanceResult> iterationResults, int totalIterations, int failureCount, TimeSpan totalDuration)
        {
            OperationName = operationName ?? throw new ArgumentNullException(nameof(operationName));
            IterationResults = iterationResults?.ToList() ?? throw new ArgumentNullException(nameof(iterationResults));
            TotalIterations = totalIterations;
            FailureCount = failureCount;
            TotalDuration = totalDuration;
            Statistics = new PerformanceStatistics(operationName, iterationResults);
        }

        // Legacy constructor for backward compatibility
        public StressTestResult(string operationName, IList<PerformanceResult> iterationResults, int totalIterations)
            : this(operationName, iterationResults, totalIterations,
                   Math.Max(0, totalIterations - (iterationResults?.Count ?? 0)),
                   TimeSpan.FromMilliseconds(iterationResults?.Sum(r => r.Duration.TotalMilliseconds) ?? 0))
        {
        }
    }
}