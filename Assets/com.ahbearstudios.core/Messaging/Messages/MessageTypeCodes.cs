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
    }
}