using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Health check execution context
/// </summary>
public sealed class HealthCheckExecution
{
    public FixedString64Bytes HealthCheckName { get; set; }
    public DateTime ScheduledTime { get; set; }
    public ExecutionType ExecutionType { get; set; }
    public int Priority { get; set; }
    public Func<CancellationToken, UniTask<HealthCheckResult>> HealthCheckDelegate { get; set; }
}