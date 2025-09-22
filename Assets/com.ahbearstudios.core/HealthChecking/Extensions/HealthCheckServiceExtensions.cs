// using System;
// using AhBearStudios.Core.HealthChecking.Configs;
// using AhBearStudios.Core.Database.HealthChecks;
// using AhBearStudios.Core.HealthChecking.Checks;
// using AhBearStudios.Core.Network.HealthChecks;
// using AhBearStudios.Core.Logging;
//
// namespace AhBearStudios.Core.HealthChecking.Extensions;
//
// /// <summary>
// /// Extension methods for IHealthCheckService to support domain self-registration pattern.
// /// Provides convenient methods for registering all domain health checks automatically.
// /// </summary>
// public static class HealthCheckServiceExtensions
// {
//     /// <summary>
//     /// Registers all core domain health checks with the health check service.
//     /// This includes Database, Network, and System domain health checks.
//     /// </summary>
//     /// <param name="healthCheckService">The health check service to register with</param>
//     /// <param name="logger">Logging service for registration operations</param>
//     /// <param name="serviceConfig">Optional service configuration for domain-specific settings</param>
//     /// <returns>HealthCheckRegistrationManager for managing the registered health checks</returns>
//     /// <exception cref="ArgumentNullException">Thrown when healthCheckService or logger is null</exception>
//     public static HealthCheckRegistrationManager RegisterAllCoreDomainHealthChecks(
//         this IHealthCheckService healthCheckService,
//         ILoggingService logger,
//         HealthCheckServiceConfig serviceConfig = null)
//     {
//         if (healthCheckService == null)
//             throw new ArgumentNullException(nameof(healthCheckService));
//         if (logger == null)
//             throw new ArgumentNullException(nameof(logger));
//
//         var registrationManager = new HealthCheckRegistrationManager(healthCheckService, logger);
//
//         try
//         {
//             // Add core domain registrars
//             registrationManager.AddDomainRegistrar(new SystemHealthCheckRegistrar(logger));
//             registrationManager.AddDomainRegistrar(new DatabaseHealthCheckRegistrar(logger));
//             registrationManager.AddDomainRegistrar(new NetworkHealthCheckRegistrar(logger));
//
//             // Register all health checks
//             registrationManager.RegisterAllDomainHealthChecks(serviceConfig);
//
//             logger.LogInfo("Successfully registered all core domain health checks");
//             return registrationManager;
//         }
//         catch
//         {
//             // Clean up on failure
//             registrationManager.Dispose();
//             throw;
//         }
//     }
//
//     /// <summary>
//     /// Creates and configures a HealthCheckRegistrationManager with the specified domain registrars.
//     /// Allows for custom domain registration scenarios.
//     /// </summary>
//     /// <param name="healthCheckService">The health check service to register with</param>
//     /// <param name="logger">Logging service for registration operations</param>
//     /// <param name="domainRegistrars">The domain registrars to include</param>
//     /// <param name="serviceConfig">Optional service configuration for domain-specific settings</param>
//     /// <returns>HealthCheckRegistrationManager for managing the registered health checks</returns>
//     /// <exception cref="ArgumentNullException">Thrown when healthCheckService, logger, or domainRegistrars is null</exception>
//     public static HealthCheckRegistrationManager RegisterDomainHealthChecks(
//         this IHealthCheckService healthCheckService,
//         ILoggingService logger,
//         IDomainHealthCheckRegistrar[] domainRegistrars,
//         HealthCheckServiceConfig serviceConfig = null)
//     {
//         if (healthCheckService == null)
//             throw new ArgumentNullException(nameof(healthCheckService));
//         if (logger == null)
//             throw new ArgumentNullException(nameof(logger));
//         if (domainRegistrars == null)
//             throw new ArgumentNullException(nameof(domainRegistrars));
//
//         var registrationManager = new HealthCheckRegistrationManager(healthCheckService, logger);
//
//         try
//         {
//             // Add provided domain registrars
//             foreach (var registrar in domainRegistrars)
//             {
//                 if (registrar != null)
//                 {
//                     registrationManager.AddDomainRegistrar(registrar);
//                 }
//             }
//
//             // Register all health checks
//             registrationManager.RegisterAllDomainHealthChecks(serviceConfig);
//
//             logger.LogInfo("Successfully registered {Count} domain health check registrars", domainRegistrars.Length);
//             return registrationManager;
//         }
//         catch
//         {
//             // Clean up on failure
//             registrationManager.Dispose();
//             throw;
//         }
//     }
//
//     /// <summary>
//     /// Registers system health checks only (CPU, Memory, Disk, Frame Rate).
//     /// Useful for minimal system monitoring scenarios.
//     /// </summary>
//     /// <param name="healthCheckService">The health check service to register with</param>
//     /// <param name="logger">Logging service for registration operations</param>
//     /// <param name="serviceConfig">Optional service configuration for domain-specific settings</param>
//     /// <returns>HealthCheckRegistrationManager for managing the registered health checks</returns>
//     /// <exception cref="ArgumentNullException">Thrown when healthCheckService or logger is null</exception>
//     public static HealthCheckRegistrationManager RegisterSystemHealthChecksOnly(
//         this IHealthCheckService healthCheckService,
//         ILoggingService logger,
//         HealthCheckServiceConfig serviceConfig = null)
//     {
//         if (healthCheckService == null)
//             throw new ArgumentNullException(nameof(healthCheckService));
//         if (logger == null)
//             throw new ArgumentNullException(nameof(logger));
//
//         var registrationManager = new HealthCheckRegistrationManager(healthCheckService, logger);
//
//         try
//         {
//             // Add only system domain registrar
//             registrationManager.AddDomainRegistrar(new SystemHealthCheckRegistrar(logger));
//
//             // Register system health checks
//             registrationManager.RegisterAllDomainHealthChecks(serviceConfig);
//
//             logger.LogInfo("Successfully registered system health checks only");
//             return registrationManager;
//         }
//         catch
//         {
//             // Clean up on failure
//             registrationManager.Dispose();
//             throw;
//         }
//     }
//
//     /// <summary>
//     /// Creates a HealthCheckRegistrationManager without registering any health checks.
//     /// Useful for scenarios where you want to add registrars manually.
//     /// </summary>
//     /// <param name="healthCheckService">The health check service to register with</param>
//     /// <param name="logger">Logging service for registration operations</param>
//     /// <returns>Empty HealthCheckRegistrationManager ready for manual configuration</returns>
//     /// <exception cref="ArgumentNullException">Thrown when healthCheckService or logger is null</exception>
//     public static HealthCheckRegistrationManager CreateRegistrationManager(
//         this IHealthCheckService healthCheckService,
//         ILoggingService logger)
//     {
//         if (healthCheckService == null)
//             throw new ArgumentNullException(nameof(healthCheckService));
//         if (logger == null)
//             throw new ArgumentNullException(nameof(logger));
//
//         return new HealthCheckRegistrationManager(healthCheckService, logger);
//     }
// }