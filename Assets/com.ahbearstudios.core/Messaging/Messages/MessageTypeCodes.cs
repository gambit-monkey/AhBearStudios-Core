namespace AhBearStudios.Core.Messaging.Messages
{
    /// <summary>
    /// Predefined type codes for different message types in the system.
    /// Used for efficient message routing and identification.
    /// </summary>
    public static class MessageTypeCodes
    {
        /// <summary>
        /// Type code for log messages.
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
        /// Type code for health check messages.
        /// </summary>
        public const ushort HealthCheck = 1003;
        
        /// <summary>
        /// Type code for alert messages.
        /// </summary>
        public const ushort Alert = 1004;
        
        // Logging System Message Type Codes (1100-1199)
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
    }
}