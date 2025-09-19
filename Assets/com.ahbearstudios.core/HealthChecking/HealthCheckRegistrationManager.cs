using System;
using System.Collections.Generic;
using ZLinq;
using Unity.Profiling;
using AhBearStudios.Core.HealthChecking.Checks;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Common.Utilities;

namespace AhBearStudios.Core.HealthChecking;

/// <summary>
/// Manages domain health check registration following the orchestration pattern.
/// Coordinates multiple IDomainHealthCheckRegistrar instances to register their
/// health checks with the central HealthCheckService.
/// </summary>
/// <remarks>
/// This manager implements the same pattern as other orchestration managers in the system,
/// providing centralized coordination while allowing domains to manage their own registrations.
/// Supports priority-based registration and proper cleanup on shutdown.
/// </remarks>
public sealed class HealthCheckRegistrationManager : IDisposable
{
    private readonly IHealthCheckService _healthCheckService;
    private readonly ILoggingService _logger;
    private readonly List<IDomainHealthCheckRegistrar> _domainRegistrars;
    private readonly Dictionary<string, Dictionary<IHealthCheck, HealthCheckConfiguration>> _registeredHealthChecks;
    private readonly ProfilerMarker _registerMarker = new ProfilerMarker("HealthCheckRegistrationManager.Register");
    private readonly ProfilerMarker _unregisterMarker = new ProfilerMarker("HealthCheckRegistrationManager.Unregister");
    private readonly Guid _managerId;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the HealthCheckRegistrationManager
    /// </summary>
    /// <param name="healthCheckService">The health check service to register with</param>
    /// <param name="logger">Logging service for registration operations</param>
    public HealthCheckRegistrationManager(
        IHealthCheckService healthCheckService,
        ILoggingService logger)
    {
        _healthCheckService = healthCheckService ?? throw new ArgumentNullException(nameof(healthCheckService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _domainRegistrars = new List<IDomainHealthCheckRegistrar>();
        _registeredHealthChecks = new Dictionary<string, Dictionary<IHealthCheck, HealthCheckConfiguration>>();
        _managerId = DeterministicIdGenerator.GenerateCoreId("HealthCheckRegistrationManager");

        _logger.LogDebug($"HealthCheckRegistrationManager initialized with ID: {_managerId}");
    }

    /// <summary>
    /// Adds a domain health check registrar to be managed
    /// </summary>
    /// <param name="domainRegistrar">The domain registrar to add</param>
    /// <exception cref="ArgumentNullException">Thrown when domainRegistrar is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when domain is already registered or manager is disposed</exception>
    public void AddDomainRegistrar(IDomainHealthCheckRegistrar domainRegistrar)
    {
        ThrowIfDisposed();

        if (domainRegistrar == null)
            throw new ArgumentNullException(nameof(domainRegistrar));

        if (_domainRegistrars.AsValueEnumerable().Any(r => r.DomainName == domainRegistrar.DomainName))
        {
            throw new InvalidOperationException($"Domain registrar for '{domainRegistrar.DomainName}' is already added");
        }

        _domainRegistrars.Add(domainRegistrar);
        _logger.LogDebug($"Added domain registrar: {domainRegistrar.DomainName} (Priority: {domainRegistrar.RegistrationPriority})");
    }

    /// <summary>
    /// Removes a domain health check registrar
    /// </summary>
    /// <param name="domainName">The name of the domain to remove</param>
    /// <returns>True if the domain was found and removed, false otherwise</returns>
    /// <exception cref="ArgumentException">Thrown when domainName is null or empty</exception>
    /// <exception cref="ObjectDisposedException">Thrown when manager is disposed</exception>
    public bool RemoveDomainRegistrar(string domainName)
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(domainName))
            throw new ArgumentException("Domain name cannot be null or empty", nameof(domainName));

        var registrar = _domainRegistrars.AsValueEnumerable()
            .FirstOrDefault(r => r.DomainName == domainName);

        if (registrar != null)
        {
            // Unregister health checks for this domain first
            if (_registeredHealthChecks.ContainsKey(domainName))
            {
                try
                {
                    registrar.UnregisterHealthChecks(_healthCheckService);
                    _registeredHealthChecks.Remove(domainName);
                }
                catch (Exception ex)
                {
                    _logger.LogException($"Failed to unregister health checks for domain: {domainName}", ex);
                }
            }

            _domainRegistrars.Remove(registrar);
            _logger.LogDebug($"Removed domain registrar: {domainName}");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Registers all domain health checks with the health check service
    /// </summary>
    /// <param name="serviceConfig">Optional service configuration for domain-specific settings</param>
    /// <exception cref="ObjectDisposedException">Thrown when manager is disposed</exception>
    public void RegisterAllDomainHealthChecks(HealthCheckServiceConfig serviceConfig = null)
    {
        ThrowIfDisposed();

        using (_registerMarker.Auto())
        {
            try
            {
                // Sort registrars by priority (highest first)
                var sortedRegistrars = _domainRegistrars.AsValueEnumerable()
                    .OrderByDescending(r => r.RegistrationPriority)
                    .ToArray();

                var totalRegistered = 0;

                foreach (var registrar in sortedRegistrars)
                {
                    try
                    {
                        _logger.LogDebug($"Registering health checks for domain: {registrar.DomainName}");

                        var domainHealthChecks = registrar.RegisterHealthChecks(_healthCheckService, serviceConfig);
                        _registeredHealthChecks[registrar.DomainName] = domainHealthChecks;

                        totalRegistered += domainHealthChecks.Count;
                        
                        _logger.LogInfo($"Successfully registered {domainHealthChecks.Count} health checks for domain: {registrar.DomainName}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogException($"Failed to register health checks for domain: {registrar.DomainName}", ex);
                        // Continue with other domains rather than failing completely
                    }
                }

                _logger.LogInfo($"Health check registration complete. Total registered: {totalRegistered} across {sortedRegistrars.Length} domains");
            }
            catch (Exception ex)
            {
                _logger.LogException("Failed to register domain health checks", ex);
                throw;
            }
        }
    }

    /// <summary>
    /// Unregisters all domain health checks from the health check service
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when manager is disposed</exception>
    public void UnregisterAllDomainHealthChecks()
    {
        ThrowIfDisposed();

        using (_unregisterMarker.Auto())
        {
            try
            {
                var totalUnregistered = 0;

                foreach (var registrar in _domainRegistrars)
                {
                    try
                    {
                        if (_registeredHealthChecks.ContainsKey(registrar.DomainName))
                        {
                            var healthCheckCount = _registeredHealthChecks[registrar.DomainName].Count;
                            
                            _logger.LogDebug($"Unregistering health checks for domain: {registrar.DomainName}");
                            
                            registrar.UnregisterHealthChecks(_healthCheckService);
                            _registeredHealthChecks.Remove(registrar.DomainName);

                            totalUnregistered += healthCheckCount;
                            
                            _logger.LogInfo($"Successfully unregistered {healthCheckCount} health checks for domain: {registrar.DomainName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogException($"Failed to unregister health checks for domain: {registrar.DomainName}", ex);
                        // Continue with other domains rather than failing completely
                    }
                }

                _logger.LogInfo($"Health check unregistration complete. Total unregistered: {totalUnregistered} across {_domainRegistrars.Count} domains");
            }
            catch (Exception ex)
            {
                _logger.LogException("Failed to unregister domain health checks", ex);
                throw;
            }
        }
    }

    /// <summary>
    /// Gets a summary of registered health checks by domain
    /// </summary>
    /// <returns>Dictionary mapping domain names to health check counts</returns>
    /// <exception cref="ObjectDisposedException">Thrown when manager is disposed</exception>
    public Dictionary<string, int> GetRegistrationSummary()
    {
        ThrowIfDisposed();

        return _registeredHealthChecks.AsValueEnumerable()
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count);
    }

    /// <summary>
    /// Gets all registered health checks for a specific domain
    /// </summary>
    /// <param name="domainName">The name of the domain</param>
    /// <returns>Dictionary of health checks and their configurations for the specified domain</returns>
    /// <exception cref="ArgumentException">Thrown when domainName is null or empty</exception>
    /// <exception cref="ObjectDisposedException">Thrown when manager is disposed</exception>
    public Dictionary<IHealthCheck, HealthCheckConfiguration> GetDomainHealthChecks(string domainName)
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(domainName))
            throw new ArgumentException("Domain name cannot be null or empty", nameof(domainName));

        if (_registeredHealthChecks.TryGetValue(domainName, out var healthChecks))
        {
            return new Dictionary<IHealthCheck, HealthCheckConfiguration>(healthChecks);
        }

        return new Dictionary<IHealthCheck, HealthCheckConfiguration>();
    }

    /// <summary>
    /// Gets the number of registered domain registrars
    /// </summary>
    public int RegisteredDomainCount => _domainRegistrars.Count;

    /// <summary>
    /// Gets the total number of registered health checks across all domains
    /// </summary>
    public int TotalRegisteredHealthChecks => _registeredHealthChecks.Values.AsValueEnumerable().Sum(d => d.Count);

    /// <summary>
    /// Disposes the registration manager and unregisters all health checks
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed) return;

        try
        {
            _logger.LogDebug($"Disposing HealthCheckRegistrationManager: {_managerId}");
            UnregisterAllDomainHealthChecks();
        }
        catch (Exception ex)
        {
            _logger.LogException("Error during HealthCheckRegistrationManager disposal", ex);
        }
        finally
        {
            _domainRegistrars.Clear();
            _registeredHealthChecks.Clear();
            _isDisposed = true;
            
            _logger.LogDebug($"HealthCheckRegistrationManager disposed: {_managerId}");
        }
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(HealthCheckRegistrationManager));
    }
}