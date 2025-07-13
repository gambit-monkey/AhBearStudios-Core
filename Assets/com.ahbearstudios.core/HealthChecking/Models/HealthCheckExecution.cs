using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.HealthChecking.Services;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Health check execution context
/// </summary>
internal sealed class HealthCheckExecution
{
    public FixedString64Bytes HealthCheckName { get; set; }
    public DateTime ScheduledTime { get; set; }
    public ExecutionType ExecutionType { get; set; }
    public int Priority { get; set; }
    public Func<CancellationToken, Task<HealthCheckResult>> HealthCheckDelegate { get; set; }
}