using System;
using System.Collections.Generic;
using System.Linq;
using VContainer;

namespace AhBearStudios.Core.DependencyInjection.Extensions.VContainer
{
    /// <summary>
    /// Main entry point for VContainer builder extensions.
    /// This class aggregates functionality from specialized extension classes to provide
    /// a single import point for all VContainer enhancement capabilities.
    /// </summary>
    public static class VContainerBuilderExtensions
    {
        /// <summary>
        /// Extension method that provides access to all registration extensions.
        /// This is a convenience method that allows fluent chaining of registration operations.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        /// <returns>The same builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        /// <example>
        /// <code>
        /// builder.WithRegistrations()
        ///        .RegisterIfNotPresent&lt;IService, Service&gt;()
        ///        .RegisterMultiple&lt;IPlugin&gt;(pluginTypes)
        ///        .RegisterLazy&lt;IExpensiveService&gt;();
        /// </code>
        /// </example>
        public static IContainerBuilder WithRegistrations(this IContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            return builder;
        }

        /// <summary>
        /// Extension method that provides access to all validation extensions.
        /// This is a convenience method for performing container validation operations.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        /// <returns>The same builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        /// <example>
        /// <code>
        /// var isValid = builder.WithValidation()
        ///                      .ValidateRegistrations()
        ///                      .IsValid();
        /// </code>
        /// </example>
        public static IContainerBuilder WithValidation(this IContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            return builder;
        }

        /// <summary>
        /// Extension method that provides access to all inspection extensions.
        /// This is a convenience method for examining container state and registrations.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        /// <returns>The same builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        /// <example>
        /// <code>
        /// var registrationCount = builder.WithInspection()
        ///                                .GetRegistrationCount();
        /// 
        /// builder.WithInspection()
        ///        .LogRegistrations(UnityEngine.LogType.Log);
        /// </code>
        /// </example>
        public static IContainerBuilder WithInspection(this IContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            return builder;
        }

        /// <summary>
        /// Performs a comprehensive setup of the container with registration, validation, and logging.
        /// This is a high-level convenience method that applies common container setup patterns.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        /// <param name="enableValidation">Whether to perform validation after setup.</param>
        /// <param name="enableLogging">Whether to log registration information.</param>
        /// <param name="throwOnValidationFailure">Whether to throw exceptions on validation failures.</param>
        /// <returns>The builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        /// <example>
        /// <code>
        /// builder.WithComprehensiveSetup(
        ///     enableValidation: true,
        ///     enableLogging: true,
        ///     throwOnValidationFailure: false
        /// );
        /// </code>
        /// </example>
        public static IContainerBuilder WithComprehensiveSetup(
            this IContainerBuilder builder,
            bool enableValidation = true,
            bool enableLogging = false,
            bool throwOnValidationFailure = false)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            try
            {
                if (enableLogging)
                {
                    UnityEngine.Debug.Log("[VContainerBuilderExtensions] Starting comprehensive container setup...");
                }

                // Perform validation if requested
                if (enableValidation)
                {
                    var isValid = builder.ValidateRegistrations(throwOnValidationFailure);
                    
                    if (enableLogging)
                    {
                        var status = isValid ? "PASSED" : "FAILED";
                        UnityEngine.Debug.Log($"[VContainerBuilderExtensions] Container validation: {status}");
                    }
                }

                // Log registration summary if requested
                if (enableLogging)
                {
                    var registrationCount = builder.GetRegistrationCount();
                    var summary = builder.GetRegistrationSummary();
                    
                    UnityEngine.Debug.Log($"[VContainerBuilderExtensions] Container setup complete:");
                    UnityEngine.Debug.Log($"  Total Registrations: {registrationCount}");
                    
                    foreach (var kvp in summary)
                    {
                        UnityEngine.Debug.Log($"  {kvp.Key}: {kvp.Value} registrations");
                    }
                }

                return builder;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[VContainerBuilderExtensions] Comprehensive setup failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Creates a validation report and optionally logs it to the console.
        /// This is a convenience method for getting detailed validation information.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        /// <param name="logReport">Whether to log the report to the Unity console.</param>
        /// <returns>A comprehensive validation report.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        /// <example>
        /// <code>
        /// var report = builder.CreateAndLogValidationReport(logReport: true);
        /// 
        /// if (!report.IsValid)
        /// {
        ///     // Handle validation failures
        ///     foreach (var error in report.Errors)
        ///     {
        ///         Debug.LogError($"Validation Error: {error}");
        ///     }
        /// }
        /// </code>
        /// </example>
        public static ContainerValidationReport CreateAndLogValidationReport(
            this IContainerBuilder builder,
            bool logReport = true)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            var report = builder.CreateValidationReport();

            if (logReport)
            {
                var logLevel = report.IsValid ? UnityEngine.LogType.Log : UnityEngine.LogType.Error;
                
                switch (logLevel)
                {
                    case UnityEngine.LogType.Error:
                        UnityEngine.Debug.LogError(report.ToString());
                        break;
                    case UnityEngine.LogType.Warning:
                        UnityEngine.Debug.LogWarning(report.ToString());
                        break;
                    default:
                        UnityEngine.Debug.Log(report.ToString());
                        break;
                }
            }

            return report;
        }

        /// <summary>
        /// Performs a quick health check on the container and returns a summary.
        /// This is useful for runtime diagnostics and monitoring.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        /// <returns>A health check summary.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        /// <example>
        /// <code>
        /// var health = builder.PerformHealthCheck();
        /// 
        /// if (health.Status != ContainerHealthStatus.Healthy)
        /// {
        ///     Debug.LogWarning($"Container health: {health.Status} - {health.Message}");
        /// }
        /// </code>
        /// </example>
        public static ContainerHealthSummary PerformHealthCheck(this IContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            try
            {
                var registrationCount = builder.GetRegistrationCount();
                var hasCircularDependencies = builder.HasCircularDependencies();
                var duplicates = builder.FindDuplicateRegistrations();
                var isValid = builder.IsValid();

                if (!isValid)
                {
                    return new ContainerHealthSummary
                    {
                        Status = ContainerHealthStatus.Critical,
                        Message = "Container has critical validation errors",
                        RegistrationCount = registrationCount,
                        HasCircularDependencies = hasCircularDependencies,
                        DuplicateRegistrationCount = duplicates.Count
                    };
                }

                if (hasCircularDependencies || duplicates.Count > 0)
                {
                    return new ContainerHealthSummary
                    {
                        Status = ContainerHealthStatus.Warning,
                        Message = $"Container has warnings: {(hasCircularDependencies ? "circular dependencies" : "")} {(duplicates.Count > 0 ? $"{duplicates.Count} duplicates" : "")}".Trim(),
                        RegistrationCount = registrationCount,
                        HasCircularDependencies = hasCircularDependencies,
                        DuplicateRegistrationCount = duplicates.Count
                    };
                }

                return new ContainerHealthSummary
                {
                    Status = ContainerHealthStatus.Healthy,
                    Message = "Container is healthy",
                    RegistrationCount = registrationCount,
                    HasCircularDependencies = false,
                    DuplicateRegistrationCount = 0
                };
            }
            catch (Exception ex)
            {
                return new ContainerHealthSummary
                {
                    Status = ContainerHealthStatus.Critical,
                    Message = $"Health check failed: {ex.Message}",
                    RegistrationCount = 0,
                    HasCircularDependencies = false,
                    DuplicateRegistrationCount = 0
                };
            }
        }

        /// <summary>
        /// Applies a series of configuration actions to the builder with error handling.
        /// This allows for robust container configuration with graceful error handling.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        /// <param name="configurations">The configuration actions to apply.</param>
        /// <param name="continueOnError">Whether to continue applying configurations if one fails.</param>
        /// <returns>The builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder or configurations is null.</exception>
        /// <example>
        /// <code>
        /// builder.ApplyConfigurations(
        ///     continueOnError: true,
        ///     b => b.RegisterSingleton&lt;IService, Service&gt;(),
        ///     b => b.RegisterTransient&lt;IRepository, Repository&gt;(),
        ///     b => b.RegisterMultiple&lt;IPlugin&gt;(pluginTypes)
        /// );
        /// </code>
        /// </example>
        public static IContainerBuilder ApplyConfigurations(
            this IContainerBuilder builder,
            bool continueOnError,
            params Action<IContainerBuilder>[] configurations)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (configurations == null) throw new ArgumentNullException(nameof(configurations));

            var errors = new List<Exception>();

            foreach (var configuration in configurations)
            {
                if (configuration == null) continue;

                try
                {
                    configuration(builder);
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                    
                    UnityEngine.Debug.LogError($"[VContainerBuilderExtensions] Configuration failed: {ex.Message}");
                    
                    if (!continueOnError)
                    {
                        throw;
                    }
                }
            }

            if (errors.Count > 0 && !continueOnError)
            {
                throw new AggregateException("One or more container configurations failed", errors);
            }

            return builder;
        }

        /// <summary>
        /// Convenience method to register multiple services with validation and logging.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        /// <param name="logProgress">Whether to log registration progress.</param>
        /// <param name="validateAfterRegistration">Whether to validate after each registration.</param>
        /// <param name="configurations">The configuration actions to apply.</param>
        /// <returns>The builder for method chaining.</returns>
        /// <example>
        /// <code>
        /// builder.RegisterMultipleWithValidation(
        ///     logProgress: true,
        ///     validateAfterRegistration: false,
        ///     b => b.RegisterSingleton&lt;IService, Service&gt;(),
        ///     b => b.RegisterTransient&lt;IRepository, Repository&gt;()
        /// );
        /// </code>
        /// </example>
        public static IContainerBuilder RegisterMultipleWithValidation(
            this IContainerBuilder builder,
            bool logProgress = false,
            bool validateAfterRegistration = false,
            params Action<IContainerBuilder>[] configurations)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (configurations == null) throw new ArgumentNullException(nameof(configurations));

            var initialCount = builder.GetRegistrationCount();

            if (logProgress)
            {
                UnityEngine.Debug.Log($"[VContainerBuilderExtensions] Starting registration of {configurations.Length} services...");
            }

            for (int i = 0; i < configurations.Length; i++)
            {
                var configuration = configurations[i];
                if (configuration == null) continue;

                try
                {
                    configuration(builder);

                    if (logProgress)
                    {
                        UnityEngine.Debug.Log($"[VContainerBuilderExtensions] Registered service {i + 1}/{configurations.Length}");
                    }

                    if (validateAfterRegistration && !builder.IsValid())
                    {
                        UnityEngine.Debug.LogWarning($"[VContainerBuilderExtensions] Validation failed after registration {i + 1}");
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[VContainerBuilderExtensions] Failed to register service {i + 1}: {ex.Message}");
                    throw;
                }
            }

            var finalCount = builder.GetRegistrationCount();
            var addedCount = finalCount - initialCount;

            if (logProgress)
            {
                UnityEngine.Debug.Log($"[VContainerBuilderExtensions] Registration complete. Added {addedCount} registrations (Total: {finalCount})");
            }

            return builder;
        }
    }

    /// <summary>
    /// Summary of container health status.
    /// </summary>
    public sealed class ContainerHealthSummary
    {
        /// <summary>
        /// Gets or sets the overall health status.
        /// </summary>
        public ContainerHealthStatus Status { get; set; }
        
        /// <summary>
        /// Gets or sets a descriptive message about the health status.
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// Gets or sets the total number of registrations.
        /// </summary>
        public int RegistrationCount { get; set; }
        
        /// <summary>
        /// Gets or sets whether circular dependencies were detected.
        /// </summary>
        public bool HasCircularDependencies { get; set; }
        
        /// <summary>
        /// Gets or sets the number of duplicate registrations.
        /// </summary>
        public int DuplicateRegistrationCount { get; set; }
        
        /// <summary>
        /// Gets whether the container is considered healthy.
        /// </summary>
        public bool IsHealthy => Status == ContainerHealthStatus.Healthy;
        
        /// <summary>
        /// Returns a string representation of the health summary.
        /// </summary>
        /// <returns>A formatted health summary.</returns>
        public override string ToString()
        {
            return $"Container Health: {Status} - {Message} " +
                   $"(Registrations: {RegistrationCount}, " +
                   $"Circular Dependencies: {HasCircularDependencies}, " +
                   $"Duplicates: {DuplicateRegistrationCount})";
        }
    }

    /// <summary>
    /// Container health status levels.
    /// </summary>
    public enum ContainerHealthStatus
    {
        /// <summary>
        /// Container is functioning normally.
        /// </summary>
        Healthy,
        
        /// <summary>
        /// Container has warnings but is functional.
        /// </summary>
        Warning,
        
        /// <summary>
        /// Container has critical issues.
        /// </summary>
        Critical
    }
}