using AhBearStudios.Core.HealthChecking.Configs;

namespace AhBearStudios.Core.HealthChecking.Factories;

/// <summary>
/// Factory interface for creating HealthCheckService instances with proper dependency management
/// </summary>
/// <remarks>
/// Provides controlled creation of health check services with configuration validation,
/// dependency resolution, and proper lifecycle management
/// </remarks>
public interface IHealthCheckServiceFactory
{
    /// <summary>
    /// Creates a new HealthCheckService instance with validated configuration
    /// </summary>
    /// <param name="config">Service configuration</param>
    /// <returns>Configured and validated health check service</returns>
    /// <exception cref="ArgumentNullException">Thrown when config is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when configuration validation fails</exception>
    IHealthCheckService CreateService(HealthCheckServiceConfig config);
    
    /// <summary>
    /// Creates a health check service with default configuration
    /// </summary>
    /// <returns>Health check service with system defaults</returns>
    IHealthCheckService CreateServiceWithDefaults();
    
    /// <summary>
    /// Validates that all required dependencies are available for service creation
    /// </summary>
    /// <returns>True if all dependencies are resolved</returns>
    bool ValidateDependencies();
    
    /// <summary>
    /// Gets the default configuration used when none is specified
    /// </summary>
    /// <returns>Default health check service configuration</returns>
    HealthCheckServiceConfig GetDefaultConfiguration();
}