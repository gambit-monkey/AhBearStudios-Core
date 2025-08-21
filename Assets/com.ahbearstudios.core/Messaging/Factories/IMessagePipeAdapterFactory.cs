using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.HealthChecking;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Configs;
using AhBearStudios.Core.Messaging.Services;
using AhBearStudios.Core.Pooling;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Serialization;
using Cysharp.Threading.Tasks;

namespace AhBearStudios.Core.Messaging.Factories;

/// <summary>
/// Factory interface for creating MessagePipe adapter services.
/// Follows CLAUDE.md Builder → Config → Factory → Service pattern.
/// </summary>
public interface IMessagePipeAdapterFactory
{
    /// <summary>
    /// Creates a MessagePipe adapter service instance.
    /// </summary>
    /// <param name="config">Configuration for the service</param>
    /// <param name="logger">Logging service dependency (required)</param>
    /// <param name="alertService">Alert service dependency (optional)</param>
    /// <param name="healthCheckService">Health check service dependency (optional)</param>
    /// <param name="poolingService">Pooling service dependency (optional)</param>
    /// <param name="profilerService">Profiler service dependency (optional)</param>
    /// <param name="serializationService">Serialization service dependency (optional)</param>
    /// <returns>Configured MessagePipe adapter service</returns>
    UniTask<IMessageBusAdapter> CreateServiceAsync(
        MessagePipeAdapterConfig config,
        ILoggingService logger,
        IAlertService alertService = null,
        IHealthCheckService healthCheckService = null,
        IPoolingService poolingService = null,
        IProfilerService profilerService = null,
        ISerializationService serializationService = null);

    /// <summary>
    /// Creates a MessagePipe adapter service with default configuration.
    /// </summary>
    /// <param name="logger">Logging service dependency (required)</param>
    /// <param name="alertService">Alert service dependency (optional)</param>
    /// <param name="healthCheckService">Health check service dependency (optional)</param>
    /// <param name="poolingService">Pooling service dependency (optional)</param>
    /// <param name="profilerService">Profiler service dependency (optional)</param>
    /// <param name="serializationService">Serialization service dependency (optional)</param>
    /// <returns>MessagePipe adapter service with default configuration</returns>
    UniTask<IMessageBusAdapter> CreateDefaultServiceAsync(
        ILoggingService logger,
        IAlertService alertService = null,
        IHealthCheckService healthCheckService = null,
        IPoolingService poolingService = null,
        IProfilerService profilerService = null,
        ISerializationService serializationService = null);
}