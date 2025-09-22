using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using ZLinq;
using Cysharp.Threading.Tasks;

namespace AhBearStudios.Core.Tests.Shared.Utilities
{
    /// <summary>
    /// Helper class for measuring performance in tests, ensuring Unity frame budget compliance.
    /// Provides comprehensive performance measurement and analysis capabilities.
    /// </summary>
    public sealed class PerformanceTestHelper : IDisposable
    {
        private readonly List<PerformanceResult> _allResults = new List<PerformanceResult>();
        private readonly Stopwatch _stopwatch = new Stopwatch();

        /// <summary>
        /// Gets all performance results recorded during the test session.
        /// </summary>
        public IReadOnlyList<PerformanceResult> AllResults => _allResults.AsValueEnumerable().ToList();

        /// <summary>
        /// Gets the total number of measurements taken.
        /// </summary>
        public int MeasurementCount => _allResults.Count;

        /// <summary>
        /// Measures the performance of a synchronous operation.
        /// </summary>
        /// <param name="operation">The operation to measure</param>
        /// <param name="operationName">Name of the operation for reporting</param>
        /// <returns>Performance measurement result</returns>
        public PerformanceResult Measure(Action operation, string operationName)
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
                timestamp: DateTime.UtcNow);

            _allResults.Add(result);
            return result;
        }

        /// <summary>
        /// Measures the performance of an asynchronous operation.
        /// </summary>
        /// <param name="operation">The async operation to measure</param>
        /// <param name="operationName">Name of the operation for reporting</param>
        /// <returns>Performance measurement result</returns>
        public async Task<PerformanceResult> MeasureAsync(Func<UniTask> operation, string operationName)
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
                timestamp: DateTime.UtcNow);

            _allResults.Add(result);
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
        public async Task<PerformanceStatistics> MeasureIterationsAsync(Func<UniTask> operation, string operationName, int iterations)
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

            return _allResults.AsValueEnumerable()
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
        /// Disposes of the helper and clears all data.
        /// </summary>
        public void Dispose()
        {
            Clear();
        }
    }

    /// <summary>
    /// Represents the result of a single performance measurement.
    /// </summary>
    public sealed class PerformanceResult
    {
        public string OperationName { get; }
        public TimeSpan Duration { get; }
        public double MemoryUsed { get; }
        public DateTime Timestamp { get; }

        public PerformanceResult(string operationName, TimeSpan duration, double memoryUsed, DateTime timestamp)
        {
            OperationName = operationName ?? throw new ArgumentNullException(nameof(operationName));
            Duration = duration;
            MemoryUsed = memoryUsed;
            Timestamp = timestamp;
        }

        public override string ToString()
        {
            return $"{OperationName}: {Duration.TotalMilliseconds:F2}ms, {MemoryUsed:F2}MB";
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

            var durations = results.AsValueEnumerable().Select(r => r.Duration).OrderBy(d => d).ToList();
            var memories = results.AsValueEnumerable().Select(r => r.MemoryUsed).ToList();

            MinDuration = durations.First();
            MaxDuration = durations.Last();
            AverageDuration = TimeSpan.FromMilliseconds(durations.AsValueEnumerable().Average(d => d.TotalMilliseconds));
            MedianDuration = durations[durations.Count / 2];

            MinMemory = memories.AsValueEnumerable().Min();
            MaxMemory = memories.AsValueEnumerable().Max();
            AverageMemory = memories.AsValueEnumerable().Average();
            TotalMemory = memories.AsValueEnumerable().Sum();
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
            BatchResults = batchResults?.AsValueEnumerable().ToList() ?? throw new ArgumentNullException(nameof(batchResults));
            TotalItems = totalItems;
            TotalDuration = TimeSpan.FromMilliseconds(batchResults.AsValueEnumerable().Sum(r => r.Duration.TotalMilliseconds));
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

        public StressTestResult(string operationName, IList<PerformanceResult> iterationResults, int totalIterations)
        {
            OperationName = operationName ?? throw new ArgumentNullException(nameof(operationName));
            IterationResults = iterationResults?.AsValueEnumerable().ToList() ?? throw new ArgumentNullException(nameof(iterationResults));
            TotalIterations = totalIterations;
            FailureCount = Math.Max(0, totalIterations - iterationResults.Count);
            TotalDuration = TimeSpan.FromMilliseconds(iterationResults.AsValueEnumerable().Sum(r => r.Duration.TotalMilliseconds));
            Statistics = new PerformanceStatistics(operationName, iterationResults);
        }
    }
}