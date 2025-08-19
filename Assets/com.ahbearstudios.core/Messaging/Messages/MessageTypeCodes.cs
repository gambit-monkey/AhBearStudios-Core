namespace AhBearStudios.Core.Messaging.Messages
{
    /// <summary>
    /// Centralized TypeCode management for all message types in the AhBearStudios Core system.
    /// Provides organized ranges to prevent conflicts and enable efficient message routing.
    /// 
    /// TypeCode Range Allocation:
    /// - Core System: 1000-1099 (General messaging, startup, shutdown)
    /// - Logging System: 1100-1199 (Logging infrastructure)
    /// - Health System: 1200-1299 (Health checks and monitoring)
    /// - Pooling System: 1300-1399 (Object pooling strategies)
    /// - Alerting System: 1400-1499 (Alert and notification messages)
    /// - Profiling System: 1500-1599 (Performance profiling)
    /// - Serialization System: 1600-1699 (Serialization infrastructure)
    /// - Authentication System: 1700-1799 (Auth and security)
    /// - Networking System: 1800-1899 (Network communication)
    /// - User Interface System: 1900-1999 (UI and interaction)
    /// - Game Systems: 2000-2999 (Game-specific messages)
    /// - Custom/Third-party: 3000-64999 (Custom integrations)
    /// - Reserved/Testing: 65000-65535 (Special cases and testing)
    /// 
    /// To request a new range allocation, update this documentation and register
    /// the range in the MessageRegistry system.
    /// </summary>
    public static class MessageTypeCodes
    {
        #region Core System Messages (1000-1099)
        
        /// <summary>
        /// Type code for general log messages.
        /// </summary>
        public const ushort LogMessage = 1000;
        
        /// <summary>
        /// Type code for system startup messages.
        /// </summary>
        public const ushort SystemStartup = 1001;
        
        /// <summary>
        /// Type code for system shutdown messages.
        /// </summary>
        public const ushort SystemShutdown = 1002;
        
        /// <summary>
        /// Type code for system error messages.
        /// </summary>
        public const ushort SystemError = 1003;
        
        /// <summary>
        /// Type code for performance metric messages.
        /// </summary>
        public const ushort PerformanceMetric = 1004;
        
        /// <summary>
        /// Type code for general alert messages.
        /// </summary>
        public const ushort Alert = 1005;
        
        /// <summary>
        /// Type code for message bus configuration messages.
        /// </summary>
        public const ushort MessageBusConfiguration = 1006;
        
        /// <summary>
        /// Type code for message bus statistics messages.
        /// </summary>
        public const ushort MessageBusStatistics = 1007;
        
        /// <summary>
        /// Type code for message bus health status messages.
        /// </summary>
        public const ushort MessageBusHealthStatus = 1008;
        
        /// <summary>
        /// Type code for message routing status messages.
        /// </summary>
        public const ushort MessageRoutingStatus = 1009;
        
        /// <summary>
        /// Type code for message processing failed messages.
        /// </summary>
        public const ushort MessageProcessingFailed = 1010;
        
        /// <summary>
        /// Type code for message published event messages.
        /// </summary>
        public const ushort MessagePublished = 1011;
        
        /// <summary>
        /// Type code for message subscriber registered messages.
        /// </summary>
        public const ushort MessageSubscriberRegistered = 1012;
        
        /// <summary>
        /// Type code for message subscriber unregistered messages.
        /// </summary>
        public const ushort MessageSubscriberUnregistered = 1013;
        
        #endregion
        
        #region Logging System Messages (1100-1199)
        
        /// <summary>
        /// Type code for log target error messages.
        /// </summary>
        public const ushort LogTargetError = 1100;
        
        /// <summary>
        /// Type code for log scope completed messages.
        /// </summary>
        public const ushort LogScopeCompleted = 1101;
        
        /// <summary>
        /// Type code for logging system health messages.
        /// </summary>
        public const ushort LoggingSystemHealth = 1102;
        
        /// <summary>
        /// Type code for log configuration changed messages.
        /// </summary>
        public const ushort LogConfigurationChanged = 1103;
        
        #endregion
        
        #region Health System Messages (1200-1299)
        
        /// <summary>
        /// Type code for health check messages.
        /// </summary>
        public const ushort HealthCheck = 1200;
        
        /// <summary>
        /// Type code for health check test messages.
        /// </summary>
        public const ushort HealthCheckTest = 1201;
        
        /// <summary>
        /// Type code for health status changed messages.
        /// </summary>
        public const ushort HealthStatusChanged = 1202;
        
        /// <summary>
        /// Type code for health monitoring alert messages.
        /// </summary>
        public const ushort HealthAlert = 1203;
        
        #endregion
        
        #region Pooling System Messages (1300-1399)
        
        /// <summary>
        /// Type code for pool expansion messages.
        /// Sent when a pool increases its size due to demand.
        /// </summary>
        public const ushort PoolExpansion = 1300;
        
        /// <summary>
        /// Type code for network spike detected messages.
        /// Sent when adaptive network strategies detect traffic spikes.
        /// </summary>
        public const ushort NetworkSpikeDetected = 1301;
        
        /// <summary>
        /// Type code for pool contraction messages.
        /// Sent when a pool reduces its size due to low utilization.
        /// </summary>
        public const ushort PoolContraction = 1302;
        
        /// <summary>
        /// Type code for buffer exhaustion messages.
        /// Sent when a pool runs out of available objects.
        /// </summary>
        public const ushort BufferExhaustion = 1303;
        
        /// <summary>
        /// Type code for circuit breaker state changed messages.
        /// Sent when circuit breaker strategies change state.
        /// </summary>
        public const ushort CircuitBreakerStateChanged = 1304;
        
        /// <summary>
        /// Type code for pool object retrieved messages.
        /// Sent when an object is retrieved from a pool.
        /// </summary>
        public const ushort PoolObjectRetrieved = 1305;
        
        /// <summary>
        /// Type code for pool object returned messages.
        /// Sent when an object is returned to a pool.
        /// </summary>
        public const ushort PoolObjectReturned = 1306;
        
        /// <summary>
        /// Type code for pool capacity reached messages.
        /// Sent when a pool reaches its capacity limits.
        /// </summary>
        public const ushort PoolCapacityReached = 1307;
        
        /// <summary>
        /// Type code for pool validation issues messages.
        /// Sent when pool validation detects issues.
        /// </summary>
        public const ushort PoolValidationIssues = 1308;
        
        #endregion
        
        #region Alerting System Messages (1400-1499)
        
        /// <summary>
        /// Type code for alert raised messages.
        /// Sent when an alert is raised in the system.
        /// </summary>
        public const ushort AlertRaised = 1401;
        
        /// <summary>
        /// Type code for alert acknowledged messages.
        /// Sent when an alert is acknowledged by a user or system.
        /// </summary>
        public const ushort AlertAcknowledged = 1402;
        
        /// <summary>
        /// Type code for alert resolved messages.
        /// Sent when an alert is resolved and no longer active.
        /// </summary>
        public const ushort AlertResolved = 1403;
        
        /// <summary>
        /// Type code for alert system health changed messages.
        /// Sent when alerting system health status changes.
        /// </summary>
        public const ushort AlertSystemHealthChanged = 1404;
        
        /// <summary>
        /// Type code for alert suppression messages.
        /// Sent when alerts are suppressed by filtering rules.
        /// </summary>
        public const ushort AlertSuppressed = 1405;
        
        /// <summary>
        /// Type code for alert channel status messages.
        /// Sent when alert channels change status or fail.
        /// </summary>
        public const ushort AlertChannelStatus = 1406;
        
        /// <summary>
        /// Type code for alert statistics messages.
        /// Sent periodically with alerting system statistics.
        /// </summary>
        public const ushort AlertStatistics = 1407;

        /// <summary>
        /// Type code for channel health changed messages.
        /// Sent when alert channel health status changes.
        /// </summary>
        public const ushort ChannelHealthChanged = 1408;

        /// <summary>
        /// Type code for alert delivery failed messages.
        /// Sent when an alert fails to be delivered through a channel.
        /// </summary>
        public const ushort AlertDeliveryFailed = 1409;

        /// <summary>
        /// Type code for filter configuration changed messages.
        /// Sent when filter configuration is updated.
        /// </summary>
        public const ushort FilterConfigurationChanged = 1410;

        /// <summary>
        /// Type code for filter statistics updated messages.
        /// Sent when filter statistics are updated.
        /// </summary>
        public const ushort FilterStatisticsUpdated = 1411;

        /// <summary>
        /// Type code for channel configuration changed messages.
        /// Sent when channel configuration is updated.
        /// </summary>
        public const ushort ChannelConfigurationChanged = 1412;

        /// <summary>
        /// Type code for channel registered messages.
        /// Sent when a channel is registered with the alert service.
        /// </summary>
        public const ushort ChannelRegistered = 1413;

        /// <summary>
        /// Type code for channel unregistered messages.
        /// Sent when a channel is unregistered from the alert service.
        /// </summary>
        public const ushort ChannelUnregistered = 1414;

        /// <summary>
        /// Type code for alert service health check messages.
        /// Sent when comprehensive health checks are performed.
        /// </summary>
        public const ushort AlertServiceHealthCheck = 1415;

        /// <summary>
        /// Type code for alert service configuration updated messages.
        /// Sent when service configuration is hot-reloaded.
        /// </summary>
        public const ushort AlertServiceConfigurationUpdated = 1416;

        /// <summary>
        /// Type code for emergency mode status messages.
        /// Sent when emergency mode is enabled or disabled.
        /// </summary>
        public const ushort EmergencyModeStatus = 1417;

        /// <summary>
        /// Type code for bulk operation completed messages.
        /// Sent when bulk alert operations complete.
        /// </summary>
        public const ushort BulkOperationCompleted = 1418;
        
        #endregion
        
        #region Range Validation Constants
        
        /// <summary>
        /// Minimum TypeCode value for core system messages.
        /// </summary>
        public const ushort CoreSystemRangeStart = 1000;
        
        /// <summary>
        /// Maximum TypeCode value for core system messages.
        /// </summary>
        public const ushort CoreSystemRangeEnd = 1099;
        
        /// <summary>
        /// Minimum TypeCode value for logging system messages.
        /// </summary>
        public const ushort LoggingSystemRangeStart = 1100;
        
        /// <summary>
        /// Maximum TypeCode value for logging system messages.
        /// </summary>
        public const ushort LoggingSystemRangeEnd = 1199;
        
        /// <summary>
        /// Minimum TypeCode value for health system messages.
        /// </summary>
        public const ushort HealthSystemRangeStart = 1200;
        
        /// <summary>
        /// Maximum TypeCode value for health system messages.
        /// </summary>
        public const ushort HealthSystemRangeEnd = 1299;
        
        /// <summary>
        /// Minimum TypeCode value for pooling system messages.
        /// </summary>
        public const ushort PoolingSystemRangeStart = 1300;
        
        /// <summary>
        /// Maximum TypeCode value for pooling system messages.
        /// </summary>
        public const ushort PoolingSystemRangeEnd = 1399;
        
        /// <summary>
        /// Minimum TypeCode value for alerting system messages.
        /// </summary>
        public const ushort AlertingSystemRangeStart = 1400;
        
        /// <summary>
        /// Maximum TypeCode value for alerting system messages.
        /// </summary>
        public const ushort AlertingSystemRangeEnd = 1499;
        
        /// <summary>
        /// Special TypeCode reserved for testing and validation messages.
        /// </summary>
        public const ushort Reserved_Testing = 65535;
        
        #endregion
        
        #region Validation Methods
        
        /// <summary>
        /// Validates that a TypeCode is within the allowed range for a specific system.
        /// </summary>
        /// <param name="typeCode">The TypeCode to validate</param>
        /// <param name="systemRangeStart">The start of the system's allocated range</param>
        /// <param name="systemRangeEnd">The end of the system's allocated range</param>
        /// <returns>True if the TypeCode is within the specified range</returns>
        public static bool IsTypeCodeInRange(ushort typeCode, ushort systemRangeStart, ushort systemRangeEnd)
        {
            return typeCode >= systemRangeStart && typeCode <= systemRangeEnd;
        }
        
        /// <summary>
        /// Gets the system name for a given TypeCode based on its range.
        /// </summary>
        /// <param name="typeCode">The TypeCode to identify</param>
        /// <returns>The system name or "Unknown" if not in a recognized range</returns>
        public static string GetSystemForTypeCode(ushort typeCode)
        {
            return typeCode switch
            {
                >= CoreSystemRangeStart and <= CoreSystemRangeEnd => "Core System",
                >= LoggingSystemRangeStart and <= LoggingSystemRangeEnd => "Logging System",
                >= HealthSystemRangeStart and <= HealthSystemRangeEnd => "Health System",
                >= PoolingSystemRangeStart and <= PoolingSystemRangeEnd => "Pooling System",
                >= AlertingSystemRangeStart and <= AlertingSystemRangeEnd => "Alerting System",
                >= 1500 and <= 1599 => "Profiling System",
                >= 1600 and <= 1699 => "Serialization System",
                >= 2000 and <= 2999 => "Game System",
                >= 3000 and <= 64999 => "Custom System",
                Reserved_Testing => "Testing",
                _ => "Unknown"
            };
        }
        
        #endregion
    }
}