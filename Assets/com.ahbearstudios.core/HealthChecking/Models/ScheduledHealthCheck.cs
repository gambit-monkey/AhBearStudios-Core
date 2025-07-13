using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.HealthChecking.Configs;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Internal representation of a scheduled health check
/// </summary>
internal sealed class ScheduledHealthCheck
{
    public FixedString64Bytes HealthCheckName { get; set; }
    public CheckScheduleConfig ScheduleConfig { get; set; }
    public Func<CancellationToken, Task<HealthCheckResult>> HealthCheckDelegate { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime? LastExecutionTime { get; set; }
    public DateTime NextExecutionTime { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsPaused { get; set; }
    public int ExecutionCount { get; set; }
}