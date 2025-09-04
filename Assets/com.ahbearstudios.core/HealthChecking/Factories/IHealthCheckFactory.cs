using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.HealthChecking.Checks;
using AhBearStudios.Core.HealthChecking.Configs;

namespace AhBearStudios.Core.HealthChecking.Factories;

/// <summary>
/// Factory interface for creating individual health check instances
/// </summary>
/// <remarks>
/// Provides type-safe creation of health checks with proper configuration and dependency injection
/// </remarks>
public interface IHealthCheckFactory
{
    /// <summary>
    /// Creates a health check instance of the specified type asynchronously
    /// </summary>
    /// <typeparam name="T">Type of health check to create</typeparam>
    /// <param name="config">Optional configuration for the health check</param>
    /// <returns>Configured health check instance</returns>
    /// <exception cref="InvalidOperationException">Thrown when type cannot be created</exception>
    UniTask<T> CreateHealthCheckAsync<T>(HealthCheckConfiguration config = null) where T : class, IHealthCheck;
    
    /// <summary>
    /// Creates a health check instance by type name asynchronously
    /// </summary>
    /// <param name="typeName">Full type name of the health check</param>
    /// <param name="config">Optional configuration for the health check</param>
    /// <returns>Configured health check instance</returns>
    /// <exception cref="ArgumentException">Thrown when type name is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when type cannot be created</exception>
    UniTask<IHealthCheck> CreateHealthCheckAsync(string typeName, HealthCheckConfiguration config = null);
    
    /// <summary>
    /// Creates multiple health checks from a collection of types asynchronously
    /// </summary>
    /// <param name="healthCheckTypes">Types of health checks to create</param>
    /// <param name="defaultConfig">Default configuration for all health checks</param>
    /// <returns>Dictionary of created health checks indexed by type name</returns>
    UniTask<Dictionary<string, IHealthCheck>> CreateHealthChecksAsync(
        IEnumerable<Type> healthCheckTypes, 
        HealthCheckConfiguration defaultConfig = null);
    
    /// <summary>
    /// Validates that a health check type can be created
    /// </summary>
    /// <param name="healthCheckType">Type to validate</param>
    /// <returns>True if the type can be instantiated</returns>
    bool CanCreateHealthCheck(Type healthCheckType);
    
    /// <summary>
    /// Gets all available health check types that can be created
    /// </summary>
    /// <returns>Collection of available health check types</returns>
    IEnumerable<Type> GetAvailableHealthCheckTypes();
}