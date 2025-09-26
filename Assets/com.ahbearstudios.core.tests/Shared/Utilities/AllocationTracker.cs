using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using ZLinq;
using Cysharp.Threading.Tasks;

namespace AhBearStudios.Core.Tests.Shared.Utilities
{
    /// <summary>
    /// Helper class for tracking memory allocations during test operations.
    /// Verifies zero-allocation patterns required for Unity game development performance.
    /// Enhanced with TDD test double integration and Unity Collections validation.
    /// Unity Test Runner compatible for both Edit Mode and Play Mode tests.
    /// </summary>
    public sealed class AllocationTracker : IDisposable
    {
        private readonly List<AllocationResult> _allResults = new List<AllocationResult>();

        /// <summary>
        /// Gets all allocation results recorded during the test session.
        /// </summary>
        public IReadOnlyList<AllocationResult> AllResults => _allResults;

        /// <summary>
        /// Gets the total number of allocation measurements taken.
        /// </summary>
        public int MeasurementCount => _allResults.Count;

        /// <summary>
        /// Measures memory allocations during a synchronous operation.
        /// </summary>
        /// <param name="operation">The operation to measure</param>
        /// <param name="operationName">Name of the operation for reporting</param>
        /// <returns>Allocation measurement result</returns>
        public AllocationResult MeasureAllocations(Action operation, string operationName)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));
            if (string.IsNullOrEmpty(operationName))
                throw new ArgumentException("Operation name cannot be null or empty", nameof(operationName));

            // Force garbage collection to get accurate baseline
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var initialMemory = GC.GetTotalMemory(false);
            var initialGen0 = GC.CollectionCount(0);
            var initialGen1 = GC.CollectionCount(1);
            var initialGen2 = GC.CollectionCount(2);

            try
            {
                operation();
            }
            finally
            {
                // Force another collection to ensure all allocations are counted
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            var finalMemory = GC.GetTotalMemory(false);
            var finalGen0 = GC.CollectionCount(0);
            var finalGen1 = GC.CollectionCount(1);
            var finalGen2 = GC.CollectionCount(2);

            var result = new AllocationResult(
                operationName: operationName,
                totalBytes: Math.Max(0, finalMemory - initialMemory),
                gen0Collections: finalGen0 - initialGen0,
                gen1Collections: finalGen1 - initialGen1,
                gen2Collections: finalGen2 - initialGen2,
                timestamp: DateTime.UtcNow);

            _allResults.Add(result);
            return result;
        }

        /// <summary>
        /// Measures memory allocations during an asynchronous operation.
        /// Unity Test Runner compatible async operation using UniTask.
        /// </summary>
        /// <param name="operation">The async operation to measure</param>
        /// <param name="operationName">Name of the operation for reporting</param>
        /// <returns>Allocation measurement result</returns>
        public async UniTask<AllocationResult> MeasureAllocationsAsync(Func<UniTask> operation, string operationName)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));
            if (string.IsNullOrEmpty(operationName))
                throw new ArgumentException("Operation name cannot be null or empty", nameof(operationName));

            // Force garbage collection to get accurate baseline
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var initialMemory = GC.GetTotalMemory(false);
            var initialGen0 = GC.CollectionCount(0);
            var initialGen1 = GC.CollectionCount(1);
            var initialGen2 = GC.CollectionCount(2);

            try
            {
                await operation();
            }
            finally
            {
                // Force another collection to ensure all allocations are counted
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            var finalMemory = GC.GetTotalMemory(false);
            var finalGen0 = GC.CollectionCount(0);
            var finalGen1 = GC.CollectionCount(1);
            var finalGen2 = GC.CollectionCount(2);

            var result = new AllocationResult(
                operationName: operationName,
                totalBytes: Math.Max(0, finalMemory - initialMemory),
                gen0Collections: finalGen0 - initialGen0,
                gen1Collections: finalGen1 - initialGen1,
                gen2Collections: finalGen2 - initialGen2,
                timestamp: DateTime.UtcNow);

            _allResults.Add(result);
            return result;
        }

        /// <summary>
        /// Measures allocations for multiple iterations and provides statistical analysis.
        /// </summary>
        /// <param name="operation">The operation to measure</param>
        /// <param name="operationName">Name of the operation for reporting</param>
        /// <param name="iterations">Number of iterations to perform</param>
        /// <returns>Statistical allocation analysis</returns>
        public AllocationStatistics MeasureIterations(Action operation, string operationName, int iterations)
        {
            if (iterations <= 0)
                throw new ArgumentException("Iterations must be positive", nameof(iterations));

            var results = new List<AllocationResult>();

            for (int i = 0; i < iterations; i++)
            {
                var result = MeasureAllocations(operation, $"{operationName}_Iteration_{i}");
                results.Add(result);
            }

            return new AllocationStatistics(operationName, results);
        }

        /// <summary>
        /// Measures allocations for multiple async iterations and provides statistical analysis.
        /// Unity Test Runner compatible async operation using UniTask.
        /// </summary>
        /// <param name="operation">The async operation to measure</param>
        /// <param name="operationName">Name of the operation for reporting</param>
        /// <param name="iterations">Number of iterations to perform</param>
        /// <returns>Statistical allocation analysis</returns>
        public async UniTask<AllocationStatistics> MeasureIterationsAsync(Func<UniTask> operation, string operationName, int iterations)
        {
            if (iterations <= 0)
                throw new ArgumentException("Iterations must be positive", nameof(iterations));

            var results = new List<AllocationResult>();

            for (int i = 0; i < iterations; i++)
            {
                var result = await MeasureAllocationsAsync(operation, $"{operationName}_Iteration_{i}");
                results.Add(result);
            }

            return new AllocationStatistics(operationName, results);
        }

        /// <summary>
        /// Gets allocation results filtered by operation name pattern.
        /// </summary>
        /// <param name="namePattern">Pattern to match operation names (supports wildcards)</param>
        /// <returns>Filtered allocation results</returns>
        public IEnumerable<AllocationResult> GetResultsByPattern(string namePattern)
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
        /// Gets overall allocation statistics for all measurements.
        /// </summary>
        /// <returns>Overall allocation statistics</returns>
        public AllocationStatistics GetOverallStatistics()
        {
            return new AllocationStatistics("Overall", _allResults);
        }

        /// <summary>
        /// Clears all recorded allocation results.
        /// </summary>
        public void Clear()
        {
            _allResults.Clear();
        }

        /// <summary>
        /// Disposes of the tracker and clears all data.
        /// </summary>
        public void Dispose()
        {
            Clear();
        }

        #region CLAUDETESTS.md Compliance - Enhanced Allocation Tracking Methods

        /// <summary>
        /// Measures allocations with correlation tracking for distributed system debugging.
        /// Essential for correlating allocation measurements with specific test operations.
        /// </summary>
        public AllocationResult MeasureAllocationsWithCorrelation(Action operation, string operationName, Guid correlationId)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));
            if (string.IsNullOrEmpty(operationName))
                throw new ArgumentException("Operation name cannot be null or empty", nameof(operationName));

            // Force garbage collection to get accurate baseline
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var initialMemory = GC.GetTotalMemory(false);
            var initialGen0 = GC.CollectionCount(0);
            var initialGen1 = GC.CollectionCount(1);
            var initialGen2 = GC.CollectionCount(2);

            try
            {
                operation();
            }
            finally
            {
                // Force another collection to ensure all allocations are counted
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            var finalMemory = GC.GetTotalMemory(false);
            var finalGen0 = GC.CollectionCount(0);
            var finalGen1 = GC.CollectionCount(1);
            var finalGen2 = GC.CollectionCount(2);

            var result = new AllocationResult(
                operationName: operationName,
                totalBytes: Math.Max(0, finalMemory - initialMemory),
                gen0Collections: finalGen0 - initialGen0,
                gen1Collections: finalGen1 - initialGen1,
                gen2Collections: finalGen2 - initialGen2,
                timestamp: DateTime.UtcNow,
                correlationId: correlationId);

            _allResults.Add(result);
            return result;
        }

        /// <summary>
        /// Measures allocations with correlation tracking for async operations.
        /// Unity Test Runner compatible with correlation tracking support.
        /// </summary>
        public async UniTask<AllocationResult> MeasureAllocationsWithCorrelationAsync(Func<UniTask> operation, string operationName, Guid correlationId)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));
            if (string.IsNullOrEmpty(operationName))
                throw new ArgumentException("Operation name cannot be null or empty", nameof(operationName));

            // Force garbage collection to get accurate baseline
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var initialMemory = GC.GetTotalMemory(false);
            var initialGen0 = GC.CollectionCount(0);
            var initialGen1 = GC.CollectionCount(1);
            var initialGen2 = GC.CollectionCount(2);

            try
            {
                await operation();
            }
            finally
            {
                // Force another collection to ensure all allocations are counted
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            var finalMemory = GC.GetTotalMemory(false);
            var finalGen0 = GC.CollectionCount(0);
            var finalGen1 = GC.CollectionCount(1);
            var finalGen2 = GC.CollectionCount(2);

            var result = new AllocationResult(
                operationName: operationName,
                totalBytes: Math.Max(0, finalMemory - initialMemory),
                gen0Collections: finalGen0 - initialGen0,
                gen1Collections: finalGen1 - initialGen1,
                gen2Collections: finalGen2 - initialGen2,
                timestamp: DateTime.UtcNow,
                correlationId: correlationId);

            _allResults.Add(result);
            return result;
        }

        /// <summary>
        /// Validates Unity Collections zero-allocation patterns.
        /// Specifically designed for FixedString, NativeArray, and other Unity Collections.
        /// Critical for Unity game development performance requirements.
        /// </summary>
        public AllocationResult ValidateUnityCollectionsPattern(Action operation, string operationName, bool enforceZeroAllocation = true)
        {
            var result = MeasureAllocations(operation, $"UnityCollections_{operationName}");

            if (enforceZeroAllocation && !result.IsZeroAllocation)
            {
                throw new InvalidOperationException(
                    $"Unity Collections operation '{operationName}' should produce zero allocations " +
                    $"but allocated {result.TotalBytes} bytes and triggered {result.TotalAllocations} GC collections. " +
                    $"Consider using FixedString instead of string, NativeArray instead of managed arrays, " +
                    $"or other Unity.Collections types for zero-allocation patterns.");
            }

            return result;
        }

        /// <summary>
        /// Validates Unity Collections zero-allocation patterns for async operations.
        /// Unity Test Runner compatible async validation.
        /// </summary>
        public async UniTask<AllocationResult> ValidateUnityCollectionsPatternAsync(Func<UniTask> operation, string operationName, bool enforceZeroAllocation = true)
        {
            var result = await MeasureAllocationsAsync(operation, $"UnityCollections_{operationName}");

            if (enforceZeroAllocation && !result.IsZeroAllocation)
            {
                throw new InvalidOperationException(
                    $"Unity Collections operation '{operationName}' should produce zero allocations " +
                    $"but allocated {result.TotalBytes} bytes and triggered {result.TotalAllocations} GC collections. " +
                    $"Consider using FixedString instead of string, NativeArray instead of managed arrays, " +
                    $"or other Unity.Collections types for zero-allocation patterns.");
            }

            return result;
        }

        /// <summary>
        /// Validates frame budget compliance with allocation limits.
        /// Ensures that allocation measurement operations don't exceed Unity's 16.67ms frame budget.
        /// Critical for maintaining 60 FPS performance in Unity game development.
        /// </summary>
        public AllocationResult ValidateFrameBudgetCompliance(Action operation, string operationName, TimeSpan maxDuration)
        {
            var startTime = DateTime.UtcNow;
            var result = MeasureAllocations(operation, operationName);
            var measurementDuration = DateTime.UtcNow - startTime;

            if (measurementDuration > maxDuration)
            {
                throw new InvalidOperationException(
                    $"Allocation measurement for '{operationName}' took {measurementDuration.TotalMilliseconds:F2}ms, " +
                    $"exceeding frame budget limit of {maxDuration.TotalMilliseconds:F2}ms. " +
                    $"Operation is too slow for 60 FPS Unity game development requirements.");
            }

            return result;
        }

        /// <summary>
        /// Gets allocation results filtered by correlation ID.
        /// Essential for tracking allocation measurements across distributed test operations.
        /// </summary>
        public IEnumerable<AllocationResult> GetResultsByCorrelation(Guid correlationId)
        {
            return _allResults.Where(r => r.CorrelationId == correlationId).ToList();
        }

        /// <summary>
        /// Validates acceptable allocation limits for non-critical operations.
        /// Provides thresholds for operations that cannot achieve zero allocations.
        /// </summary>
        public void ValidateAcceptableAllocations(AllocationResult result, long maxBytes = 1024, int maxCollections = 1)
        {
            if (result.TotalBytes > maxBytes)
            {
                throw new InvalidOperationException(
                    $"Operation '{result.OperationName}' allocated {result.TotalBytes} bytes, " +
                    $"exceeding acceptable limit of {maxBytes} bytes for non-critical operations.");
            }

            if (result.TotalAllocations > maxCollections)
            {
                throw new InvalidOperationException(
                    $"Operation '{result.OperationName}' triggered {result.TotalAllocations} GC collections, " +
                    $"exceeding acceptable limit of {maxCollections} collections for non-critical operations.");
            }
        }

        /// <summary>
        /// Measures allocations for stress testing scenarios.
        /// Validates allocation patterns under realistic game load (1000+ operations).
        /// </summary>
        public async UniTask<AllocationStatistics> MeasureStressTestAllocationsAsync(
            Func<UniTask> operation,
            string operationName,
            int iterations = 1000,
            long maxTotalBytes = 1024 * 1024) // 1MB default limit
        {
            var statistics = await MeasureIterationsAsync(operation, operationName, iterations);

            if (statistics.TotalBytes > maxTotalBytes)
            {
                throw new InvalidOperationException(
                    $"Stress test '{operationName}' allocated {statistics.TotalBytes} bytes total, " +
                    $"exceeding stress test limit of {maxTotalBytes} bytes. " +
                    $"This indicates potential memory leaks or excessive allocation patterns.");
            }

            // Validate that zero-allocation percentage is acceptable for performance-critical code
            if (statistics.ZeroAllocationPercentage < 95.0) // 95% should be zero-allocation
            {
                throw new InvalidOperationException(
                    $"Stress test '{operationName}' achieved only {statistics.ZeroAllocationPercentage:F1}% zero-allocation rate, " +
                    $"but Unity game development requires at least 95% zero-allocation for performance-critical operations.");
            }

            return statistics;
        }

        #endregion
    }

    /// <summary>
    /// Represents the result of a single allocation measurement.
    /// Enhanced with correlation tracking for distributed system debugging.
    /// </summary>
    public sealed class AllocationResult
    {
        public string OperationName { get; }
        public long TotalBytes { get; }
        public int Gen0Collections { get; }
        public int Gen1Collections { get; }
        public int Gen2Collections { get; }
        public int TotalAllocations => Gen0Collections + Gen1Collections + Gen2Collections;
        public DateTime Timestamp { get; }
        public Guid CorrelationId { get; }

        public AllocationResult(string operationName, long totalBytes, int gen0Collections, int gen1Collections, int gen2Collections, DateTime timestamp, Guid correlationId = default)
        {
            OperationName = operationName ?? throw new ArgumentNullException(nameof(operationName));
            TotalBytes = totalBytes;
            Gen0Collections = gen0Collections;
            Gen1Collections = gen1Collections;
            Gen2Collections = gen2Collections;
            Timestamp = timestamp;
            CorrelationId = correlationId;
        }

        public bool IsZeroAllocation => TotalBytes == 0 && TotalAllocations == 0;

        /// <summary>
        /// Indicates if this is a Unity Collections compliant allocation pattern.
        /// True if zero allocations, false if any managed memory was allocated.
        /// </summary>
        public bool IsUnityCollectionsCompliant => IsZeroAllocation;

        /// <summary>
        /// Indicates if this allocation result exceeds acceptable limits for non-critical operations.
        /// </summary>
        public bool ExceedsAcceptableLimits(long maxBytes = 1024, int maxCollections = 1)
        {
            return TotalBytes > maxBytes || TotalAllocations > maxCollections;
        }

        public override string ToString()
        {
            var correlationInfo = CorrelationId != Guid.Empty ? $", Correlation: {CorrelationId}" : "";
            return $"{OperationName}: {TotalBytes} bytes, GC({Gen0Collections}, {Gen1Collections}, {Gen2Collections}){correlationInfo}";
        }
    }

    /// <summary>
    /// Provides statistical analysis of multiple allocation measurements.
    /// </summary>
    public sealed class AllocationStatistics
    {
        public string OperationName { get; }
        public int SampleCount { get; }
        public long MinBytes { get; }
        public long MaxBytes { get; }
        public double AverageBytes { get; }
        public long TotalBytes { get; }
        public int TotalGen0Collections { get; }
        public int TotalGen1Collections { get; }
        public int TotalGen2Collections { get; }
        public int ZeroAllocationCount { get; }
        public double ZeroAllocationPercentage { get; }

        public AllocationStatistics(string operationName, IList<AllocationResult> results)
        {
            OperationName = operationName ?? throw new ArgumentNullException(nameof(operationName));

            if (results == null || results.Count == 0)
            {
                SampleCount = 0;
                return;
            }

            SampleCount = results.Count;

            var bytes = results.Select(r => r.TotalBytes).ToList();

            MinBytes = bytes.Count > 0 ? bytes.Min() : 0;
            MaxBytes = bytes.Count > 0 ? bytes.Max() : 0;
            AverageBytes = bytes.Count > 0 ? bytes.Average() : 0.0;
            TotalBytes = bytes.Sum();

            TotalGen0Collections = results.Sum(r => r.Gen0Collections);
            TotalGen1Collections = results.Sum(r => r.Gen1Collections);
            TotalGen2Collections = results.Sum(r => r.Gen2Collections);

            ZeroAllocationCount = results.Count(r => r.IsZeroAllocation);
            ZeroAllocationPercentage = SampleCount > 0 ? (double)ZeroAllocationCount / SampleCount * 100.0 : 0.0;
        }

        public override string ToString()
        {
            if (SampleCount == 0)
                return $"{OperationName}: No samples";

            return $"{OperationName} ({SampleCount} samples):\n" +
                   $"  Bytes: min={MinBytes}, max={MaxBytes}, avg={AverageBytes:F2}, total={TotalBytes}\n" +
                   $"  GC: Gen0={TotalGen0Collections}, Gen1={TotalGen1Collections}, Gen2={TotalGen2Collections}\n" +
                   $"  Zero Allocation: {ZeroAllocationCount}/{SampleCount} ({ZeroAllocationPercentage:F1}%)";
        }
    }
}