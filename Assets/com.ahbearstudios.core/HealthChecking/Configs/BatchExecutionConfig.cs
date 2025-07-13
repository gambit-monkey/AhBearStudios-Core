using System.Collections.Generic;
using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.HealthCheck.Configs;

/// <summary>
/// Batch execution configuration for grouping health checks
/// </summary>
public sealed record BatchExecutionConfig
{
    /// <summary>
    /// Whether batch execution is enabled
    /// </summary>
    public bool Enabled { get; init; } = false;

    /// <summary>
    /// Maximum batch size
    /// </summary>
    public int MaxBatchSize { get; init; } = 10;

    /// <summary>
    /// Maximum time to wait for batch to fill
    /// </summary>
    public TimeSpan MaxBatchWaitTime { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Batch grouping strategy
    /// </summary>
    public BatchGroupingStrategy GroupingStrategy { get; init; } = BatchGroupingStrategy.Priority;

    /// <summary>
    /// Whether to execute batches in parallel
    /// </summary>
    public bool ParallelExecution { get; init; } = true;

    /// <summary>
    /// Maximum degree of parallelism within batch
    /// </summary>
    public int MaxDegreeOfParallelism { get; init; } = Environment.ProcessorCount;

    /// <summary>
    /// Validates batch execution configuration
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (MaxBatchSize <= 0)
            errors.Add("MaxBatchSize must be greater than zero");

        if (MaxBatchWaitTime < TimeSpan.Zero)
            errors.Add("MaxBatchWaitTime must be non-negative");

        if (!Enum.IsDefined(typeof(BatchGroupingStrategy), GroupingStrategy))
            errors.Add($"Invalid batch grouping strategy: {GroupingStrategy}");

        if (MaxDegreeOfParallelism <= 0)
            errors.Add("MaxDegreeOfParallelism must be greater than zero");

        return errors;
    }
}