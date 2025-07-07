using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Core.HealthCheck.Models;
using AhBearStudios.Core.MessageBus.Interfaces;
using Unity.Collections;

namespace AhBearStudios.Core.HealthCheck.Messages;

/// <summary>
/// Message published when a health check factory is cleared of its registered components.
/// Provides comprehensive information about the clearing operation, affected components,
/// and cleanup results for auditing, monitoring, and troubleshooting purposes.
/// </summary>
/// <param name="Id">Unique identifier for this message instance.</param>
/// <param name="TimestampTicks">UTC timestamp when the factory was cleared, in ticks since Unix epoch.</param>
/// <param name="TypeCode">Message type code for efficient routing and filtering.</param>
/// <param name="FactoryId">Unique identifier of the health check factory that was cleared.</param>
/// <param name="FactoryName">Human-readable name of the health check factory.</param>
/// <param name="ClearOperation">Type of clear operation performed.</param>
/// <param name="ClearedHealthCheckCount">Number of health checks that were cleared from the factory.</param>
/// <param name="ClearedConfigurationCount">Number of configurations that were cleared from the factory.</param>
/// <param name="ClearedBuilderCount">Number of builders that were cleared from the factory.</param>
/// <param name="ClearDurationMs">Time taken to complete the clear operation in milliseconds.</param>
/// <param name="FailedClearCount">Number of components that failed to clear properly.</param>
/// <param name="FailureReasons">Comma-separated list of failure reasons if any components failed to clear.</param>
/// <param name="MemoryReleasedBytes">Amount of memory released during the clear operation in bytes.</param>
/// <param name="ResourcesDisposed">Number of disposable resources that were properly disposed.</param>
/// <param name="ForceCleared">Whether the clear operation was forced (bypassing normal cleanup).</param>
/// <param name="PreClearHealthStatus">Overall health status of the factory before clearing.</param>
/// <param name="PostClearHealthStatus">Overall health status of the factory after clearing.</param>
/// <param name="ClearReason">Reason why the factory was cleared.</param>
/// <param name="InitiatedBy">Identity of the user or system that initiated the clear operation.</param>
/// <param name="EnvironmentName">Name of the environment where the factory clearing occurred.</param>
/// <param name="CorrelationId">Correlation identifier for tracking related operations.</param>
public readonly record struct HealthCheckFactoryClearedMessage(
    Guid Id,
    long TimestampTicks,
    ushort TypeCode,
    FixedString128Bytes FactoryId,
    FixedString64Bytes FactoryName,
    FactoryClearOperation ClearOperation,
    int ClearedHealthCheckCount,
    int ClearedConfigurationCount,
    int ClearedBuilderCount,
    float ClearDurationMs,
    int FailedClearCount,
    FixedString512Bytes FailureReasons,
    long MemoryReleasedBytes,
    int ResourcesDisposed,
    bool ForceCleared,
    FactoryHealthStatus PreClearHealthStatus,
    FactoryHealthStatus PostClearHealthStatus,
    FixedString128Bytes ClearReason,
    FixedString64Bytes InitiatedBy,
    FixedString64Bytes EnvironmentName,
    FixedString64Bytes CorrelationId
) : IMessage
{
    /// <summary>
    /// Gets whether the clear operation completed successfully without any failures.
    /// </summary>
    public bool IsSuccessfulClear => FailedClearCount == 0;

    /// <summary>
    /// Gets whether the clear operation was completed quickly (under 1000ms).
    /// </summary>
    public bool IsFastClear => ClearDurationMs < 1000f;

    /// <summary>
    /// Gets whether any components were actually cleared.
    /// </summary>
    public bool HasClearedComponents => TotalClearedCount > 0;

    /// <summary>
    /// Gets the total number of components that were cleared.
    /// </summary>
    public int TotalClearedCount => ClearedHealthCheckCount + ClearedConfigurationCount + ClearedBuilderCount;

    /// <summary>
    /// Gets whether significant memory was released (over 1MB).
    /// </summary>
    public bool HasSignificantMemoryRelease => MemoryReleasedBytes > 1_048_576; // 1MB

    /// <summary>
    /// Gets whether the factory health improved after clearing.
    /// </summary>
    public bool HealthImproved => PostClearHealthStatus > PreClearHealthStatus;

    /// <summary>
    /// Gets whether there were any failures during the clear operation.
    /// </summary>
    public bool HasFailures => FailedClearCount > 0 || !FailureReasons.IsEmpty;

    /// <summary>
    /// Gets whether this was an emergency clear operation.
    /// </summary>
    public bool IsEmergencyClear => ForceCleared || ClearReason.ToString().Contains("emergency", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the clear operation performance category.
    /// </summary>
    public ClearOperationPerformance GetClearPerformance()
    {
        if (!IsSuccessfulClear)
            return ClearOperationPerformance.Failed;

        return ClearDurationMs switch
        {
            < 100f => ClearOperationPerformance.Excellent,
            < 500f => ClearOperationPerformance.Good,
            < 1000f => ClearOperationPerformance.Acceptable,
            < 5000f => ClearOperationPerformance.Slow,
            _ => ClearOperationPerformance.Poor
        };
    }

    /// <summary>
    /// Gets the clear operation scope based on the number of components cleared.
    /// </summary>
    public ClearOperationScope GetClearScope()
    {
        return TotalClearedCount switch
        {
            0 => ClearOperationScope.None,
            <= 5 => ClearOperationScope.Minimal,
            <= 20 => ClearOperationScope.Moderate,
            <= 50 => ClearOperationScope.Extensive,
            _ => ClearOperationScope.Complete
        };
    }

    /// <summary>
    /// Gets the memory release category based on bytes released.
    /// </summary>
    public MemoryReleaseCategory GetMemoryReleaseCategory()
    {
        return MemoryReleasedBytes switch
        {
            0 => MemoryReleaseCategory.None,
            < 1024 => MemoryReleaseCategory.Minimal,
            < 1_048_576 => MemoryReleaseCategory.Small,
            < 10_485_760 => MemoryReleaseCategory.Moderate,
            < 104_857_600 => MemoryReleaseCategory.Large,
            _ => MemoryReleaseCategory.Massive
        };
    }

    /// <summary>
    /// Gets a formatted summary of the factory clearing operation.
    /// </summary>
    /// <returns>A human-readable summary of the clearing operation.</returns>
    public string GetFormattedSummary()
    {
        var performance = GetClearPerformance();
        var scope = GetClearScope();
        var memoryCategory = GetMemoryReleaseCategory();
        var successText = IsSuccessfulClear ? "successfully" : $"with {FailedClearCount} failures";
        var forceText = ForceCleared ? " (forced)" : "";
        var memoryText = MemoryReleasedBytes > 0 ? $", released {GetFormattedMemorySize()}" : "";
        
        return $"Health Check Factory '{FactoryName}' cleared {successText}{forceText}. " +
               $"{TotalClearedCount} components removed ({scope} scope, {performance} performance)" +
               $"{memoryText}. Operation completed in {ClearDurationMs:F1}ms.";
    }

    /// <summary>
    /// Gets a human-readable formatted memory size.
    /// </summary>
    /// <returns>Formatted memory size string.</returns>
    public string GetFormattedMemorySize()
    {
        return MemoryReleasedBytes switch
        {
            < 1024 => $"{MemoryReleasedBytes} bytes",
            < 1_048_576 => $"{MemoryReleasedBytes / 1024.0:F1} KB",
            < 1_073_741_824 => $"{MemoryReleasedBytes / 1_048_576.0:F1} MB",
            _ => $"{MemoryReleasedBytes / 1_073_741_824.0:F1} GB"
        };
    }

    /// <summary>
    /// Gets the failure reasons as a parsed list.
    /// </summary>
    /// <returns>A list of failure reason descriptions.</returns>
    public IReadOnlyList<string> GetFailureReasonsList()
    {
        if (FailureReasons.IsEmpty)
            return Array.Empty<string>();

        return FailureReasons.ToString()
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(reason => reason.Trim())
            .Where(reason => !string.IsNullOrEmpty(reason))
            .ToList();
    }

    /// <summary>
    /// Gets a dictionary representation of key clearing metrics.
    /// </summary>
    /// <returns>Dictionary containing key metrics for monitoring and reporting.</returns>
    public IReadOnlyDictionary<string, object> GetMetrics()
    {
        return new Dictionary<string, object>
        {
            ["factory_id"] = FactoryId.ToString(),
            ["factory_name"] = FactoryName.ToString(),
            ["clear_operation"] = ClearOperation.ToString(),
            ["total_cleared_count"] = TotalClearedCount,
            ["cleared_health_check_count"] = ClearedHealthCheckCount,
            ["cleared_configuration_count"] = ClearedConfigurationCount,
            ["cleared_builder_count"] = ClearedBuilderCount,
            ["clear_duration_ms"] = ClearDurationMs,
            ["failed_clear_count"] = FailedClearCount,
            ["memory_released_bytes"] = MemoryReleasedBytes,
            ["resources_disposed"] = ResourcesDisposed,
            ["force_cleared"] = ForceCleared,
            ["pre_clear_health_status"] = PreClearHealthStatus.ToString(),
            ["post_clear_health_status"] = PostClearHealthStatus.ToString(),
            ["clear_reason"] = ClearReason.ToString(),
            ["initiated_by"] = InitiatedBy.ToString(),
            ["environment_name"] = EnvironmentName.ToString(),
            ["is_successful_clear"] = IsSuccessfulClear,
            ["is_fast_clear"] = IsFastClear,
            ["has_significant_memory_release"] = HasSignificantMemoryRelease,
            ["health_improved"] = HealthImproved,
            ["is_emergency_clear"] = IsEmergencyClear,
            ["clear_performance"] = GetClearPerformance().ToString(),
            ["clear_scope"] = GetClearScope().ToString(),
            ["memory_release_category"] = GetMemoryReleaseCategory().ToString(),
            ["timestamp"] = DateTimeOffset.FromUnixTimeSeconds(TimestampTicks / TimeSpan.TicksPerSecond).ToString("O")
        };
    }

    /// <summary>
    /// Validates that all required fields are properly set and within acceptable ranges.
    /// </summary>
    /// <returns>True if the message is valid; otherwise, false.</returns>
    public bool IsValid()
    {
        return Id != Guid.Empty
               && TimestampTicks > 0
               && !FactoryId.IsEmpty
               && !FactoryName.IsEmpty
               && ClearDurationMs >= 0
               && ClearedHealthCheckCount >= 0
               && ClearedConfigurationCount >= 0
               && ClearedBuilderCount >= 0
               && FailedClearCount >= 0
               && MemoryReleasedBytes >= 0
               && ResourcesDisposed >= 0
               && !ClearReason.IsEmpty
               && !InitiatedBy.IsEmpty
               && !EnvironmentName.IsEmpty
               && !CorrelationId.IsEmpty
               && Enum.IsDefined(typeof(FactoryClearOperation), ClearOperation)
               && Enum.IsDefined(typeof(FactoryHealthStatus), PreClearHealthStatus)
               && Enum.IsDefined(typeof(FactoryHealthStatus), PostClearHealthStatus);
    }

    /// <summary>
    /// Gets the DateTime representation of the TimestampTicks value.
    /// </summary>
    public DateTime GetTimestamp() => DateTimeOffset.FromUnixTimeSeconds(TimestampTicks / TimeSpan.TicksPerSecond).DateTime;

    /// <summary>
    /// Creates a new message with an updated correlation ID.
    /// </summary>
    /// <param name="correlationId">The new correlation ID.</param>
    /// <returns>A new message with the updated correlation ID.</returns>
    public HealthCheckFactoryClearedMessage WithCorrelationId(FixedString64Bytes correlationId)
    {
        return this with { CorrelationId = correlationId };
    }

    /// <summary>
    /// Creates a condensed version of this message with only essential information.
    /// Useful for high-frequency logging or storage-constrained scenarios.
    /// </summary>
    /// <returns>A condensed message containing only essential fields.</returns>
    public HealthCheckFactoryClearedMessage GetCondensedVersion()
    {
        return this with 
        { 
            FailureReasons = new FixedString512Bytes(""), // Clear large text fields
            ClearReason = new FixedString128Bytes(ClearReason.ToString().Length > 32 ? 
                ClearReason.ToString()[..32] : ClearReason.ToString()) // Truncate if too long
        };
    }

    /// <summary>
    /// Gets the efficiency score of the clear operation (0-100).
    /// </summary>
    /// <returns>Efficiency score based on performance, success rate, and resource management.</returns>
    public float GetEfficiencyScore()
    {
        float successScore = IsSuccessfulClear ? 40f : Math.Max(0f, 40f - (FailedClearCount * 5f));
        float performanceScore = GetClearPerformance() switch
        {
            ClearOperationPerformance.Excellent => 30f,
            ClearOperationPerformance.Good => 25f,
            ClearOperationPerformance.Acceptable => 20f,
            ClearOperationPerformance.Slow => 10f,
            ClearOperationPerformance.Poor => 5f,
            ClearOperationPerformance.Failed => 0f,
            _ => 0f
        };
        float resourceScore = HasSignificantMemoryRelease ? 20f : Math.Min(20f, MemoryReleasedBytes / 52428.8f); // Scale to MB
        float healthScore = HealthImproved ? 10f : 5f;

        return Math.Min(100f, successScore + performanceScore + resourceScore + healthScore);
    }
}