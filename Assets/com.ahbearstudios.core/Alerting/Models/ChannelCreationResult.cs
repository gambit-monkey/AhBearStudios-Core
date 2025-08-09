using System;
using AhBearStudios.Core.Alerting.Channels;
using AhBearStudios.Core.Alerting.Configs;

namespace AhBearStudios.Core.Alerting.Models
{
    /// <summary>
    /// Result of channel creation operation.
    /// </summary>
    public sealed class ChannelCreationResult
    {
        /// <summary>
        /// Gets or sets whether the creation was successful.
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Gets or sets the created channel instance.
        /// </summary>
        public IAlertChannel Channel { get; set; }

        /// <summary>
        /// Gets or sets any error that occurred during creation.
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Gets or sets the configuration used for creation.
        /// </summary>
        public ChannelConfig Configuration { get; set; }

        /// <summary>
        /// Gets or sets the time taken for creation.
        /// </summary>
        public TimeSpan CreationTime { get; set; }

        /// <summary>
        /// Creates a successful result.
        /// </summary>
        /// <param name="channel">Created channel</param>
        /// <param name="configuration">Configuration used</param>
        /// <param name="creationTime">Time taken</param>
        /// <returns>Successful creation result</returns>
        public static ChannelCreationResult Success(IAlertChannel channel, ChannelConfig configuration, TimeSpan creationTime)
        {
            return new ChannelCreationResult
            {
                IsSuccessful = true,
                Channel = channel,
                Configuration = configuration,
                CreationTime = creationTime
            };
        }

        /// <summary>
        /// Creates a failure result.
        /// </summary>
        /// <param name="error">Error message</param>
        /// <param name="configuration">Configuration that failed</param>
        /// <param name="creationTime">Time taken before failure</param>
        /// <returns>Failed creation result</returns>
        public static ChannelCreationResult Failure(string error, ChannelConfig configuration = null, TimeSpan creationTime = default)
        {
            return new ChannelCreationResult
            {
                IsSuccessful = false,
                Error = error,
                Configuration = configuration,
                CreationTime = creationTime
            };
        }
    }
}