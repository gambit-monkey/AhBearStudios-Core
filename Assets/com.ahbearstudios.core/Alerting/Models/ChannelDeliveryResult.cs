using System;
using Unity.Collections;

namespace AhBearStudios.Core.Alerting.Models
{
    /// <summary>
    /// Result of alert delivery to a specific channel.
    /// Contains success status, timing, and error information.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    public sealed record ChannelDeliveryResult
    {
        /// <summary>
        /// Gets the channel name.
        /// </summary>
        public FixedString64Bytes ChannelName { get; init; }

        /// <summary>
        /// Gets whether the delivery was successful.
        /// </summary>
        public bool IsSuccess { get; init; }

        /// <summary>
        /// Gets the delivery time.
        /// </summary>
        public TimeSpan DeliveryTime { get; init; }

        /// <summary>
        /// Gets any error message if delivery failed.
        /// </summary>
        public string Error { get; init; }

        /// <summary>
        /// Gets the timestamp when delivery was attempted.
        /// </summary>
        public DateTime AttemptTimestamp { get; init; }

        /// <summary>
        /// Gets the number of retry attempts made.
        /// </summary>
        public int RetryCount { get; init; }

        /// <summary>
        /// Creates a successful delivery result.
        /// </summary>
        /// <param name="channelName">Channel name</param>
        /// <param name="deliveryTime">Time taken for delivery</param>
        /// <param name="retryCount">Number of retries needed</param>
        /// <returns>Successful delivery result</returns>
        public static ChannelDeliveryResult Success(FixedString64Bytes channelName, TimeSpan deliveryTime, int retryCount = 0)
        {
            return new ChannelDeliveryResult
            {
                ChannelName = channelName,
                IsSuccess = true,
                DeliveryTime = deliveryTime,
                Error = null,
                AttemptTimestamp = DateTime.UtcNow,
                RetryCount = retryCount
            };
        }

        /// <summary>
        /// Creates a failed delivery result.
        /// </summary>
        /// <param name="channelName">Channel name</param>
        /// <param name="error">Error message</param>
        /// <param name="deliveryTime">Time taken for failed delivery attempt</param>
        /// <param name="retryCount">Number of retries attempted</param>
        /// <returns>Failed delivery result</returns>
        public static ChannelDeliveryResult Failure(FixedString64Bytes channelName, string error, TimeSpan deliveryTime, int retryCount = 0)
        {
            return new ChannelDeliveryResult
            {
                ChannelName = channelName,
                IsSuccess = false,
                DeliveryTime = deliveryTime,
                Error = error ?? "Unknown delivery error",
                AttemptTimestamp = DateTime.UtcNow,
                RetryCount = retryCount
            };
        }

        /// <summary>
        /// Creates a failed delivery result from an exception.
        /// </summary>
        /// <param name="channelName">Channel name</param>
        /// <param name="exception">Exception that caused the failure</param>
        /// <param name="deliveryTime">Time taken for failed delivery attempt</param>
        /// <param name="retryCount">Number of retries attempted</param>
        /// <returns>Failed delivery result</returns>
        public static ChannelDeliveryResult FromException(FixedString64Bytes channelName, Exception exception, TimeSpan deliveryTime, int retryCount = 0)
        {
            return new ChannelDeliveryResult
            {
                ChannelName = channelName,
                IsSuccess = false,
                DeliveryTime = deliveryTime,
                Error = exception?.Message ?? "Unknown exception",
                AttemptTimestamp = DateTime.UtcNow,
                RetryCount = retryCount
            };
        }

        /// <summary>
        /// Gets a string representation for debugging.
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            var status = IsSuccess ? "SUCCESS" : "FAILED";
            var retryInfo = RetryCount > 0 ? $" (after {RetryCount} retries)" : "";
            var errorInfo = !IsSuccess && !string.IsNullOrEmpty(Error) ? $" - {Error}" : "";
            
            return $"{ChannelName}: {status} in {DeliveryTime.TotalMilliseconds:F1}ms{retryInfo}{errorInfo}";
        }
    }
}