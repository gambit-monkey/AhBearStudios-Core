using AhBearStudios.Core.Alerts.Interfaces;
using AhBearStudios.Core.HealthCheck.Interfaces;
using AhBearStudios.Core.Logging.Interfaces;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Profiling.Interfaces;
using Unity.Collections;

namespace AhBearStudios.Core.Bootstrap.Interfaces;

/// <summary>
/// Bootstrap context providing access to core services during installation.
/// Integrates with all core systems for comprehensive logging, monitoring, and communication.
/// </summary>
public interface IBootstrapContext
{
    /// <summary>Gets the logging service for installation activity tracking and error reporting.</summary>
    ILoggingService Logger { get; }
        
    /// <summary>Gets the message bus service for publishing installation events and system communication.</summary>
    IMessageBusService MessageBus { get; }
        
    /// <summary>Gets the profiler service for performance monitoring during installation.</summary>
    IProfilerService Profiler { get; }
        
    /// <summary>Gets the health check service for registering system health monitoring.</summary>
    IHealthCheckService HealthChecker { get; }
        
    /// <summary>Gets the alert service for critical failure notification and threshold monitoring.</summary>
    IAlertService AlertService { get; }
        
    /// <summary>Gets the current bootstrap configuration being processed.</summary>
    IBootstrapConfig Configuration { get; }
        
    /// <summary>Gets the correlation ID for tracking this bootstrap session across all systems.</summary>
    FixedString64Bytes CorrelationId { get; }
        
    /// <summary>Gets whether this is a development build with extended debugging capabilities.</summary>
    bool IsDevelopmentBuild { get; }
        
    /// <summary>Gets the target platform for platform-specific installation decisions.</summary>
    UnityEngine.RuntimePlatform TargetPlatform { get; }
}