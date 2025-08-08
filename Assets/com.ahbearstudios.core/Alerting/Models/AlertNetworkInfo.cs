using Unity.Collections;

namespace AhBearStudios.Core.Alerting.Models
{
    /// <summary>
    /// Network information for alerts.
    /// </summary>
    public sealed record AlertNetworkInfo
    {
        /// <summary>
        /// Request URL or endpoint.
        /// </summary>
        public FixedString512Bytes RequestUrl { get; init; }

        /// <summary>
        /// HTTP status code.
        /// </summary>
        public int HttpStatusCode { get; init; }

        /// <summary>
        /// Request duration in milliseconds.
        /// </summary>
        public double RequestDurationMs { get; init; }

        /// <summary>
        /// Response size in bytes.
        /// </summary>
        public long ResponseSizeBytes { get; init; }

        /// <summary>
        /// Remote host or server.
        /// </summary>
        public FixedString128Bytes RemoteHost { get; init; }
    }
}