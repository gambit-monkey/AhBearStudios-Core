namespace AhBearStudios.Core.Messaging.Messages
{
    /// <summary>
    /// Centralized TypeCode management for all message types in the AhBearStudios Core system.
    /// Provides organized ranges to prevent conflicts and enable efficient message routing.
    /// 
    /// TypeCode Range Allocation:
    /// - Core System: 1000-1049 (System startup, shutdown, general operations)
    /// - Messaging System: 1050-1099 (Message bus, routing, subscriptions)
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
    /// Naming Convention:
    /// All type codes follow the pattern: {System}{Action}Message
    /// Examples: CoreStartupMessage, MessageBusHealthChangedMessage, PoolExpansionMessage
    /// 
    /// To request a new range allocation, update this documentation and register
    /// the range in the MessageRegistry system.
    /// </summary>
    public static class MessageTypeCodes
    {
        #region Core System Messages (1000-1049)
        
        /// <summary>
        /// Type code for general log messages from core system.
        /// </summary>
        public const ushort CoreLogMessage = 1000;
        
        /// <summary>
        /// Type code for system startup messages.
        /// </summary>
        public const ushort CoreStartupMessage = 1001;
        
        /// <summary>
        /// Type code for system shutdown messages.
        /// </summary>
        public const ushort CoreShutdownMessage = 1002;
        
        /// <summary>
        /// Type code for system error messages.
        /// </summary>
        public const ushort CoreErrorMessage = 1003;
        
        /// <summary>
        /// Type code for performance metric messages from core system.
        /// </summary>
        public const ushort CorePerformanceMetricMessage = 1004;
        
        /// <summary>
        /// Type code for general alert messages from core system.
        /// </summary>
        public const ushort CoreAlertMessage = 1005;
        
        #endregion
        
        #region Messaging System Messages (1050-1099)
        
        /// <summary>
        /// Type code for message bus configuration messages.
        /// </summary>
        public const ushort MessageBusConfigurationMessage = 1050;
        
        /// <summary>
        /// Type code for message bus statistics messages.
        /// </summary>
        public const ushort MessageBusStatisticsMessage = 1051;
        
        /// <summary>
        /// Type code for message bus health status messages.
        /// </summary>
        public const ushort MessageBusHealthStatusMessage = 1052;
        
        /// <summary>
        /// Type code for message routing status messages.
        /// </summary>
        public const ushort MessageBusRoutingStatusMessage = 1053;
        
        /// <summary>
        /// Type code for message processing failed messages.
        /// </summary>
        public const ushort MessageBusProcessingFailedMessage = 1054;
        
        /// <summary>
        /// Type code for message published messages.
        /// </summary>
        public const ushort MessageBusPublishedMessage = 1055;
        
        /// <summary>
        /// Type code for message subscriber registered messages.
        /// </summary>
        public const ushort MessageBusSubscriberRegisteredMessage = 1056;
        
        /// <summary>
        /// Type code for message subscriber unregistered messages.
        /// </summary>
        public const ushort MessageBusSubscriberUnregisteredMessage = 1057;
        
        /// <summary>
        /// Type code for message metadata created messages.
        /// </summary>
        public const ushort MessageBusMetadataCreatedMessage = 1058;
        
        /// <summary>
        /// Type code for message metadata updated messages.
        /// </summary>
        public const ushort MessageBusMetadataUpdatedMessage = 1059;
        
        /// <summary>
        /// Type code for message metadata validation failed messages.
        /// </summary>
        public const ushort MessageBusMetadataValidationFailedMessage = 1060;
        
        /// <summary>
        /// Type code for message bus health changed messages.
        /// </summary>
        public const ushort MessageBusHealthChangedMessage = 1061;
        
        /// <summary>
        /// Type code for message bus circuit breaker state changed messages.
        /// </summary>
        public const ushort MessageBusCircuitBreakerStateChangedMessage = 1062;
        
        /// <summary>
        /// Type code for message processed messages.
        /// </summary>
        public const ushort MessageBusProcessedMessage = 1063;
        
        /// <summary>
        /// Type code for subscription created messages.
        /// </summary>
        public const ushort MessageBusSubscriptionCreatedMessage = 1064;
        
        /// <summary>
        /// Type code for subscription disposed messages.
        /// </summary>
        public const ushort MessageBusSubscriptionDisposedMessage = 1065;
        
        /// <summary>
        /// Type code for message routed messages.
        /// </summary>
        public const ushort MessageBusRoutedMessage = 1066;
        
        /// <summary>
        /// Type code for routing rule changed messages.
        /// </summary>
        public const ushort MessageBusRoutingRuleChangedMessage = 1067;
        
        /// <summary>
        /// Type code for route handler changed messages.
        /// </summary>
        public const ushort MessageBusRouteHandlerChangedMessage = 1068;
        
        /// <summary>
        /// Type code for routes cleared messages.
        /// </summary>
        public const ushort MessageBusRoutesClearedMessage = 1069;
        
        /// <summary>
        /// Type code for message type registered messages.
        /// </summary>
        public const ushort MessageBusTypeRegisteredMessage = 1070;
        
        /// <summary>
        /// Type code for message type unregistered messages.
        /// </summary>
        public const ushort MessageBusTypeUnregisteredMessage = 1071;
        
        /// <summary>
        /// Type code for registry cleared messages.
        /// </summary>
        public const ushort MessageBusRegistryClearedMessage = 1072;
        
        /// <summary>
        /// Type code for MessagePipe publish succeeded messages.
        /// </summary>
        public const ushort MessagePipePublishSucceededMessage = 1073;
        
        /// <summary>
        /// Type code for MessagePipe publish failed messages.
        /// </summary>
        public const ushort MessagePipePublishFailedMessage = 1074;
        
        /// <summary>
        /// Type code for MessagePipe publish cancelled messages.
        /// </summary>
        public const ushort MessagePipePublishCancelledMessage = 1075;
        
        /// <summary>
        /// Type code for MessagePipe subscription created messages.
        /// </summary>
        public const ushort MessagePipeSubscriptionCreatedMessage = 1076;
        
        /// <summary>
        /// Type code for MessagePipe subscription disposed messages.
        /// </summary>
        public const ushort MessagePipeSubscriptionDisposedMessage = 1077;
        
        /// <summary>
        /// Type code for MessagePipe health changed messages.
        /// </summary>
        public const ushort MessagePipeHealthChangedMessage = 1078;
        
        /// <summary>
        /// Type code for message bus publish failed messages.
        /// </summary>
        public const ushort MessageBusPublishFailedMessage = 1079;
        
        /// <summary>
        /// Type code for message registry type registered messages.
        /// </summary>
        public const ushort MessageRegistryTypeRegisteredMessage = 1080;
        
        /// <summary>
        /// Type code for message registry type unregistered messages.
        /// </summary>
        public const ushort MessageRegistryTypeUnregisteredMessage = 1081;
        
        /// <summary>
        /// Type code for message registry cache cleared messages.
        /// </summary>
        public const ushort MessageRegistryCacheClearedMessage = 1082;
        
        /// <summary>
        /// Type code for message registry statistics reset messages.
        /// </summary>
        public const ushort MessageRegistryStatisticsResetMessage = 1083;
        
        /// <summary>
        /// Type code for message pipe adapter health status messages.
        /// </summary>
        public const ushort MessagePipeAdapterHealthStatusMessage = 1084;
        
        /// <summary>
        /// Type code for message pipe adapter statistics messages.
        /// </summary>
        public const ushort MessagePipeAdapterStatisticsMessage = 1085;
        
        /// <summary>
        /// Type code for message subscriber created messages.
        /// Sent when a new message subscriber is created.
        /// </summary>
        public const ushort MessageBusSubscriberCreatedMessage = 1086;
        
        /// <summary>
        /// Type code for message subscriber disposed messages.
        /// Sent when a message subscriber is disposed.
        /// </summary>
        public const ushort MessageBusSubscriberDisposedMessage = 1087;
        
        /// <summary>
        /// Type code for subscription processed messages.
        /// Sent when a subscription successfully processes a message.
        /// </summary>
        public const ushort MessageBusSubscriptionProcessedMessage = 1088;
        
        /// <summary>
        /// Type code for subscription failed messages.
        /// Sent when a subscription fails to process a message.
        /// </summary>
        public const ushort MessageBusSubscriptionFailedMessage = 1089;
        
        #endregion
        
        #region Logging System Messages (1100-1199)
        
        /// <summary>
        /// Type code for log target error messages.
        /// </summary>
        public const ushort LoggingTargetErrorMessage = 1100;
        
        /// <summary>
        /// Type code for log scope completed messages.
        /// </summary>
        public const ushort LoggingScopeCompletedMessage = 1101;
        
        /// <summary>
        /// Type code for logging system health messages.
        /// </summary>
        public const ushort LoggingSystemHealthMessage = 1102;
        
        /// <summary>
        /// Type code for log configuration changed messages.
        /// </summary>
        public const ushort LoggingConfigurationChangedMessage = 1103;
        
        #endregion
        
        #region Health System Messages (1200-1299)
        
        /// <summary>
        /// Type code for health check completed messages.
        /// </summary>
        public const ushort HealthCheckCompletedMessage = 1200;
        
        /// <summary>
        /// Type code for health check test messages.
        /// </summary>
        public const ushort HealthCheckTestMessage = 1201;
        
        /// <summary>
        /// Type code for health status changed messages.
        /// </summary>
        public const ushort HealthCheckStatusChangedMessage = 1202;
        
        /// <summary>
        /// Type code for health monitoring alert messages.
        /// </summary>
        public const ushort HealthCheckAlertMessage = 1203;
        
        /// <summary>
        /// Type code for health check degradation change messages.
        /// </summary>
        public const ushort HealthCheckDegradationChangeMessage = 1204;
        
        /// <summary>
        /// Type code for health check circuit breaker state changed messages.
        /// Sent when circuit breakers managed by the health check system change state.
        /// </summary>
        public const ushort HealthCheckCircuitBreakerStateChangedMessage = 1205;
        
        #endregion
        
        #region Pooling System Messages (1300-1399)
        
        /// <summary>
        /// Type code for pool expansion messages.
        /// Sent when a pool increases its size due to demand.
        /// </summary>
        public const ushort PoolExpansionMessage = 1300;
        
        /// <summary>
        /// Type code for network spike detected messages.
        /// Sent when adaptive network strategies detect traffic spikes.
        /// </summary>
        public const ushort PoolNetworkSpikeDetectedMessage = 1301;
        
        /// <summary>
        /// Type code for pool contraction messages.
        /// Sent when a pool reduces its size due to low utilization.
        /// </summary>
        public const ushort PoolContractionMessage = 1302;
        
        /// <summary>
        /// Type code for buffer exhaustion messages.
        /// Sent when a pool runs out of available objects.
        /// </summary>
        public const ushort PoolBufferExhaustionMessage = 1303;
        
        /// <summary>
        /// Type code for pool circuit breaker state changed messages.
        /// Sent when pool circuit breaker strategies change state.
        /// </summary>
        public const ushort PoolCircuitBreakerStateChangedMessage = 1304;
        
        /// <summary>
        /// Type code for pool object retrieved messages.
        /// Sent when an object is retrieved from a pool.
        /// </summary>
        public const ushort PoolObjectRetrievedMessage = 1305;
        
        /// <summary>
        /// Type code for pool object returned messages.
        /// Sent when an object is returned to a pool.
        /// </summary>
        public const ushort PoolObjectReturnedMessage = 1306;
        
        /// <summary>
        /// Type code for pool capacity reached messages.
        /// Sent when a pool reaches its capacity limits.
        /// </summary>
        public const ushort PoolCapacityReachedMessage = 1307;
        
        /// <summary>
        /// Type code for pool validation issues messages.
        /// Sent when pool validation detects issues.
        /// </summary>
        public const ushort PoolValidationIssuesMessage = 1308;
        
        /// <summary>
        /// Type code for pool operation started messages.
        /// Sent when a pool operation begins for performance monitoring.
        /// </summary>
        public const ushort PoolOperationStartedMessage = 1310;
        
        /// <summary>
        /// Type code for pool operation completed messages.
        /// Sent when a pool operation completes successfully.
        /// </summary>
        public const ushort PoolOperationCompletedMessage = 1311;
        
        /// <summary>
        /// Type code for pool operation failed messages.
        /// Sent when a pool operation encounters an error.
        /// </summary>
        public const ushort PoolOperationFailedMessage = 1312;
        
        /// <summary>
        /// Type code for pool strategy health status messages.
        /// Sent periodically to report strategy health metrics.
        /// </summary>
        public const ushort PoolStrategyHealthStatusMessage = 1313;
        
        #endregion
        
        #region Alerting System Messages (1400-1499)
        
        /// <summary>
        /// Type code for alert raised messages.
        /// Sent when an alert is raised in the system.
        /// </summary>
        public const ushort AlertRaisedMessage = 1401;
        
        /// <summary>
        /// Type code for alert acknowledged messages.
        /// Sent when an alert is acknowledged by a user or system.
        /// </summary>
        public const ushort AlertAcknowledgedMessage = 1402;
        
        /// <summary>
        /// Type code for alert resolved messages.
        /// Sent when an alert is resolved and no longer active.
        /// </summary>
        public const ushort AlertResolvedMessage = 1403;
        
        /// <summary>
        /// Type code for alert system health changed messages.
        /// Sent when alerting system health status changes.
        /// </summary>
        public const ushort AlertSystemHealthChangedMessage = 1404;
        
        /// <summary>
        /// Type code for alert suppression messages.
        /// Sent when alerts are suppressed by filtering rules.
        /// </summary>
        public const ushort AlertSuppressedMessage = 1405;
        
        /// <summary>
        /// Type code for alert channel status messages.
        /// Sent when alert channels change status or fail.
        /// </summary>
        public const ushort AlertChannelStatusMessage = 1406;
        
        /// <summary>
        /// Type code for alert statistics messages.
        /// Sent periodically with alerting system statistics.
        /// </summary>
        public const ushort AlertStatisticsMessage = 1407;

        /// <summary>
        /// Type code for channel health changed messages.
        /// Sent when alert channel health status changes.
        /// </summary>
        public const ushort AlertChannelHealthChangedMessage = 1408;

        /// <summary>
        /// Type code for alert delivery failed messages.
        /// Sent when an alert fails to be delivered through a channel.
        /// </summary>
        public const ushort AlertDeliveryFailedMessage = 1409;

        /// <summary>
        /// Type code for filter configuration changed messages.
        /// Sent when filter configuration is updated.
        /// </summary>
        public const ushort AlertFilterConfigurationChangedMessage = 1410;

        /// <summary>
        /// Type code for filter statistics updated messages.
        /// Sent when filter statistics are updated.
        /// </summary>
        public const ushort AlertFilterStatisticsUpdatedMessage = 1411;

        /// <summary>
        /// Type code for channel configuration changed messages.
        /// Sent when channel configuration is updated.
        /// </summary>
        public const ushort AlertChannelConfigurationChangedMessage = 1412;

        /// <summary>
        /// Type code for channel registered messages.
        /// Sent when a channel is registered with the alert service.
        /// </summary>
        public const ushort AlertChannelRegisteredMessage = 1413;

        /// <summary>
        /// Type code for channel unregistered messages.
        /// Sent when a channel is unregistered from the alert service.
        /// </summary>
        public const ushort AlertChannelUnregisteredMessage = 1414;

        /// <summary>
        /// Type code for alert service health check messages.
        /// Sent when comprehensive health checks are performed.
        /// </summary>
        public const ushort AlertServiceHealthCheckMessage = 1415;

        /// <summary>
        /// Type code for alert service configuration updated messages.
        /// Sent when service configuration is hot-reloaded.
        /// </summary>
        public const ushort AlertServiceConfigurationUpdatedMessage = 1416;

        /// <summary>
        /// Type code for emergency mode status messages.
        /// Sent when emergency mode is enabled or disabled.
        /// </summary>
        public const ushort AlertEmergencyModeStatusMessage = 1417;

        /// <summary>
        /// Type code for bulk operation completed messages.
        /// Sent when bulk alert operations complete.
        /// </summary>
        public const ushort AlertBulkOperationCompletedMessage = 1418;
        
        #endregion
        
        #region Range Validation Constants
        
        /// <summary>
        /// Minimum TypeCode value for core system messages.
        /// </summary>
        public const ushort CoreSystemRangeStart = 1000;
        
        /// <summary>
        /// Maximum TypeCode value for core system messages.
        /// </summary>
        public const ushort CoreSystemRangeEnd = 1049;
        
        /// <summary>
        /// Minimum TypeCode value for messaging system messages.
        /// </summary>
        public const ushort MessagingSystemRangeStart = 1050;
        
        /// <summary>
        /// Maximum TypeCode value for messaging system messages.
        /// </summary>
        public const ushort MessagingSystemRangeEnd = 1099;
        
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
                >= MessagingSystemRangeStart and <= MessagingSystemRangeEnd => "Messaging System",
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