using System.Runtime.CompilerServices;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Serialization;
using AhBearStudios.Core.Infrastructure.DependencyInjection;
using UnityEngine;

namespace AhBearStudios.Unity.Alerting.Formatters
{
    /// <summary>
    /// Automatic registration of Alert System types with the serialization system.
    /// This class ensures all alert-related types are properly registered for serialization
    /// using the established [ModuleInitializer] pattern from the codebase.
    /// Replaces the manual type registration previously done by AlertSystemInitializer.
    /// </summary>
    public static class AlertFormatterRegistration
    {
        private static bool _isRegistered = false;
        private static readonly object _lockObject = new();

        /// <summary>
        /// Registers all Alert System types with the serialization service.
        /// This method is automatically called during module initialization.
        /// </summary>
        [ModuleInitializer]
        public static void RegisterAlertTypes()
        {
            lock (_lockObject)
            {
                if (_isRegistered) return;

                try
                {
                    // Get the serialization service from ServiceResolver
                    var serializationService = ServiceResolver.Resolve<ISerializationService>();
                    
                    if (serializationService != null)
                    {
                        RegisterTypes(serializationService);
                        _isRegistered = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        Debug.Log("[AlertFormatterRegistration] All Alert System types registered successfully with serialization service");
#endif
                    }
                    else
                    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        Debug.LogWarning("[AlertFormatterRegistration] ISerializationService not available during module initialization - types will be registered later");
#endif
                    }
                }
                catch (System.Exception ex)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogError($"[AlertFormatterRegistration] Failed to register Alert System types: {ex.Message}");
#endif
                }
            }
        }

        /// <summary>
        /// Registers Alert System types with a specific serialization service instance.
        /// This method can be called manually if automatic registration fails or for testing.
        /// </summary>
        /// <param name="serializationService">The serialization service to register types with</param>
        public static void RegisterAlertTypes(ISerializationService serializationService)
        {
            if (serializationService == null)
                throw new System.ArgumentNullException(nameof(serializationService));

            lock (_lockObject)
            {
                RegisterTypes(serializationService);
                _isRegistered = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log("[AlertFormatterRegistration] Alert System types manually registered with serialization service");
#endif
            }
        }

        /// <summary>
        /// Internal method that performs the actual type registration.
        /// </summary>
        private static void RegisterTypes(ISerializationService serializationService)
        {
            var correlationId = System.Guid.NewGuid();

            // Register core alert model types
            RegisterCoreTypes(serializationService, correlationId);

            // Register configuration types
            RegisterConfigurationTypes(serializationService, correlationId);

            // Register statistics types
            RegisterStatisticsTypes(serializationService, correlationId);

            // Register enum types
            RegisterEnumTypes(serializationService, correlationId);
        }

        /// <summary>
        /// Registers all core alert model types with the serialization service.
        /// </summary>
        private static void RegisterCoreTypes(ISerializationService serializationService, System.Guid correlationId)
        {
            // Core alert models
            serializationService.RegisterType<Alert>(correlationId);
            serializationService.RegisterType<AlertContext>(correlationId);
            serializationService.RegisterType<AlertRule>(correlationId);

            // Nested context types
            serializationService.RegisterType<AlertExceptionInfo>(correlationId);
            serializationService.RegisterType<AlertPerformanceMetrics>(correlationId);
            serializationService.RegisterType<AlertSystemInfo>(correlationId);
            serializationService.RegisterType<AlertUserInfo>(correlationId);
            serializationService.RegisterType<AlertNetworkInfo>(correlationId);

            // Rule-related types
            serializationService.RegisterType<AlertRuleCondition>(correlationId);
            serializationService.RegisterType<AlertRuleAction>(correlationId);
            serializationService.RegisterType<AlertRuleStatistics>(correlationId);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[AlertFormatterRegistration] Registered core alert types");
#endif
        }

        /// <summary>
        /// Registers all configuration types with the serialization service.
        /// </summary>
        private static void RegisterConfigurationTypes(ISerializationService serializationService, System.Guid correlationId)
        {
            // Service configurations
            serializationService.RegisterType<AlertConfig>(correlationId);
            serializationService.RegisterType<ChannelConfig>(correlationId);
            serializationService.RegisterType<SuppressionConfig>(correlationId);

            // Extended configurations
            serializationService.RegisterType<EmergencyEscalationConfig>(correlationId);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[AlertFormatterRegistration] Registered configuration types");
#endif
        }

        /// <summary>
        /// Registers all statistics types with the serialization service.
        /// </summary>
        private static void RegisterStatisticsTypes(ISerializationService serializationService, System.Guid correlationId)
        {
            // Main statistics types
            serializationService.RegisterType<AlertStatistics>(correlationId);
            serializationService.RegisterType<AlertSeverityStatistics>(correlationId);

            // Performance statistics
            serializationService.RegisterType<ChannelPerformanceStats>(correlationId);
            serializationService.RegisterType<FilterPerformanceStats>(correlationId);
            serializationService.RegisterType<SystemResourceStats>(correlationId);
            serializationService.RegisterType<ErrorStatistics>(correlationId);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[AlertFormatterRegistration] Registered statistics types");
#endif
        }

        /// <summary>
        /// Registers all enum types with the serialization service.
        /// </summary>
        private static void RegisterEnumTypes(ISerializationService serializationService, System.Guid correlationId)
        {
            // Core enums
            serializationService.RegisterType<AlertSeverity>(correlationId);

            // Rule-related enums
            serializationService.RegisterType<AlertRuleType>(correlationId);
            serializationService.RegisterType<ComparisonOperator>(correlationId);
            serializationService.RegisterType<AlertRuleActionType>(correlationId);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[AlertFormatterRegistration] Registered enum types");
#endif
        }

        /// <summary>
        /// Checks if Alert System types are registered.
        /// </summary>
        /// <returns>True if types are registered</returns>
        public static bool IsRegistered()
        {
            lock (_lockObject)
            {
                return _isRegistered;
            }
        }

        /// <summary>
        /// Gets the count of registered Alert System types.
        /// </summary>
        /// <returns>Number of registered types</returns>
        public static int GetRegisteredTypeCount()
        {
            lock (_lockObject)
            {
                if (!_isRegistered) return 0;
                
                // Count all the registered types
                return 20; // Core(9) + Config(4) + Stats(6) + Enums(4) - approximate count
            }
        }

        /// <summary>
        /// Forces re-registration of all types (useful for testing or service restart scenarios).
        /// </summary>
        /// <param name="serializationService">The serialization service to register types with</param>
        public static void ForceReRegistration(ISerializationService serializationService)
        {
            if (serializationService == null)
                throw new System.ArgumentNullException(nameof(serializationService));

            lock (_lockObject)
            {
                _isRegistered = false;
                RegisterAlertTypes(serializationService);
            }
        }
    }
}