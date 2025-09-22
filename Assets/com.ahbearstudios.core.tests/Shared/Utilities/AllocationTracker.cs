using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZLinq;
using Cysharp.Threading.Tasks;

namespace AhBearStudios.Core.Tests.Shared.Utilities
{
    /// <summary>
    /// Helper class for tracking memory allocations during test operations.
    /// Verifies zero-allocation patterns required for Unity game development performance.
    /// </summary>
    public sealed class AllocationTracker : IDisposable
    {
        private readonly List<AllocationResult> _allResults = new List<AllocationResult>();

        /// <summary>
        /// Gets all allocation results recorded during the test session.
        /// </summary>
        public IReadOnlyList<AllocationResult> AllResults => _allResults.AsValueEnumerable().ToList();

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
        /// </summary>
        /// <param name="operation">The async operation to measure</param>
        /// <param name="operationName">Name of the operation for reporting</param>
        /// <returns>Allocation measurement result</returns>
        public async Task<AllocationResult> MeasureAllocationsAsync(Func<UniTask> operation, string operationName)
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
        /// </summary>
        /// <param name="operation">The async operation to measure</param>
        /// <param name="operationName">Name of the operation for reporting</param>
        /// <param name="iterations">Number of iterations to perform</param>
        /// <returns>Statistical allocation analysis</returns>
        public async Task<AllocationStatistics> MeasureIterationsAsync(Func<UniTask> operation, string operationName, int iterations)
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

            return _allResults.AsValueEnumerable()
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
    }

    /// <summary>
    /// Represents the result of a single allocation measurement.
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

        public AllocationResult(string operationName, long totalBytes, int gen0Collections, int gen1Collections, int gen2Collections, DateTime timestamp)
        {
            OperationName = operationName ?? throw new ArgumentNullException(nameof(operationName));
            TotalBytes = totalBytes;
            Gen0Collections = gen0Collections;
            Gen1Collections = gen1Collections;
            Gen2Collections = gen2Collections;
            Timestamp = timestamp;
        }

        public bool IsZeroAllocation => TotalBytes == 0 && TotalAllocations == 0;

        public override string ToString()
        {
            return $"{OperationName}: {TotalBytes} bytes, GC({Gen0Collections}, {Gen1Collections}, {Gen2Collections})";
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

            var bytes = results.AsValueEnumerable().Select(r => r.TotalBytes).ToList();

            MinBytes = bytes.AsValueEnumerable().Min();
            MaxBytes = bytes.AsValueEnumerable().Max();
            AverageBytes = bytes.AsValueEnumerable().Average();
            TotalBytes = bytes.AsValueEnumerable().Sum();

            TotalGen0Collections = results.AsValueEnumerable().Sum(r => r.Gen0Collections);
            TotalGen1Collections = results.AsValueEnumerable().Sum(r => r.Gen1Collections);
            TotalGen2Collections = results.AsValueEnumerable().Sum(r => r.Gen2Collections);

            ZeroAllocationCount = results.AsValueEnumerable().Count(r => r.IsZeroAllocation);
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