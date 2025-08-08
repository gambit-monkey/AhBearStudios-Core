using Unity.Collections;

namespace AhBearStudios.Core.Alerting.Models
{
    /// <summary>
    /// User information for alerts.
    /// </summary>
    public sealed record AlertUserInfo
    {
        /// <summary>
        /// User identifier.
        /// </summary>
        public FixedString64Bytes UserId { get; init; }

        /// <summary>
        /// Session identifier.
        /// </summary>
        public FixedString64Bytes SessionId { get; init; }

        /// <summary>
        /// Client IP address.
        /// </summary>
        public FixedString32Bytes IpAddress { get; init; }

        /// <summary>
        /// User agent or client information.
        /// </summary>
        public FixedString512Bytes UserAgent { get; init; }
    }
}