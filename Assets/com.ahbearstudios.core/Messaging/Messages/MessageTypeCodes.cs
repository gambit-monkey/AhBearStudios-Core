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
                >= 1400 and <= 1499 => "Alerting System",
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