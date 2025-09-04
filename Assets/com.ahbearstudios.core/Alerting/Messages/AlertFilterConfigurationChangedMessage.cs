using System;
using System.Collections.Generic;
using Unity.Collections;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Alerting.Messages
{
    /// <summary>
    /// Message published when an alert filter's configuration changes.
    /// Implements IMessage for integration with the messaging bus and correlation tracking.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    public readonly record struct AlertFilterConfigurationChangedMessage : IMessage
    {
        #region IMessage Implementation

        /// <summary>
        /// Gets the unique identifier for this message instance.
        /// </summary>
        public Guid Id { get; init; }

        /// <summary>
        /// Gets the timestamp when this message was created, in UTC ticks.
        /// </summary>
        public long TimestampTicks { get; init; }

        /// <summary>
        /// Gets the message type code for efficient routing and filtering.
        /// </summary>
        public ushort TypeCode { get; init; }

        /// <summary>
        /// Gets the source system or component that created this message.
        /// </summary>
        public FixedString64Bytes Source { get; init; }

        /// <summary>
        /// Gets the priority level for message processing.
        /// </summary>
        public MessagePriority Priority { get; init; }

        /// <summary>
        /// Gets optional correlation ID for message tracing across systems.
        /// </summary>
        public Guid CorrelationId { get; init; }

        #endregion

        #region Message-Specific Properties
        /// <summary>
        /// Gets the name of the filter whose configuration changed.
        /// </summary>
        public FixedString64Bytes FilterName { get; init; }

        /// <summary>
        /// Gets a summary of the configuration changes.
        /// </summary>
        public FixedString512Bytes ConfigurationChangeSummary { get; init; }

        /// <summary>
        /// Gets the previous configuration settings as JSON string.
        /// </summary>
        public FixedString512Bytes PreviousConfigurationJson { get; init; }

        /// <summary>
        /// Gets the new configuration settings as JSON string.
        /// </summary>
        public FixedString512Bytes NewConfigurationJson { get; init; }

        /// <summary>
        /// Gets whether the configuration change was successful.
        /// </summary>
        public bool WasSuccessful { get; init; }

        /// <summary>
        /// Gets whether the filter is now enabled.
        /// </summary>
        public bool IsFilterEnabled { get; init; }

        /// <summary>
        /// Gets the new priority value for the filter.
        /// </summary>
        public int NewPriority { get; init; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the DateTime representation of the message timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a new AlertFilterConfigurationChangedMessage with proper validation and defaults.
        /// </summary>
        /// <param name="filterName">Name of the filter</param>
        /// <param name="changeSummary">Summary of changes</param>
        /// <param name="previousConfig">Previous configuration</param>
        /// <param name="newConfig">New configuration</param>
        /// <param name="wasSuccessful">Whether the change was successful</param>
        /// <param name="isEnabled">Whether the filter is enabled</param>
        /// <param name="priority">New priority value</param>
        /// <param name="source">Source component creating this message</param>
        /// <param name="correlationId">Optional correlation ID</param>
        /// <returns>New AlertFilterConfigurationChangedMessage instance</returns>
        public static AlertFilterConfigurationChangedMessage Create(
            FixedString64Bytes filterName,
            string changeSummary,
            Dictionary<string, object> previousConfig,
            Dictionary<string, object> newConfig,
            bool wasSuccessful,
            bool isEnabled,
            int priority,
            FixedString64Bytes source = default,
            Guid correlationId = default)
        {
            // Simplified JSON serialization - in real implementation would use proper JSON serializer
            var prevJson = SerializeDictionaryToJson(previousConfig);
            var newJson = SerializeDictionaryToJson(newConfig);

            // ID generation with explicit parameters to avoid ambiguity
            var sourceString = source.IsEmpty ? "AlertFilterService" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId("AlertFilterConfigurationChangedMessage", sourceString, correlationId: null);
            var finalCorrelationId = correlationId == default 
                ? DeterministicIdGenerator.GenerateCorrelationId("AlertFilterConfigChange", filterName.ToString())
                : correlationId;

            return new AlertFilterConfigurationChangedMessage
            {
                Id = messageId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.AlertFilterConfigurationChangedMessage,
                Source = source.IsEmpty ? "AlertFilterService" : source,
                Priority = wasSuccessful ? MessagePriority.Low : MessagePriority.Normal,
                CorrelationId = finalCorrelationId,
                FilterName = filterName,
                ConfigurationChangeSummary = changeSummary?.Length <= 256 ? changeSummary : changeSummary?[..256] ?? "Configuration updated",
                PreviousConfigurationJson = prevJson.Length <= 512 ? prevJson : prevJson[..512],
                NewConfigurationJson = newJson.Length <= 512 ? newJson : newJson[..512],
                WasSuccessful = wasSuccessful,
                IsFilterEnabled = isEnabled,
                NewPriority = priority
            };
        }

        /// <summary>
        /// Simple dictionary to JSON converter for configuration serialization.
        /// </summary>
        /// <param name="dictionary">Dictionary to serialize</param>
        /// <returns>JSON string representation</returns>
        private static string SerializeDictionaryToJson(Dictionary<string, object> dictionary)
        {
            if (dictionary == null || dictionary.Count == 0)
                return "{}";

            var items = new List<string>();
            foreach (var kvp in dictionary)
            {
                var value = kvp.Value?.ToString() ?? "null";
                items.Add($"\"{kvp.Key}\":\"{value}\"");
            }

            return "{" + string.Join(",", items) + "}";
        }

        #endregion

        #region String Representation

        /// <summary>
        /// Returns a string representation of this message for debugging.
        /// </summary>
        /// <returns>Filter configuration change message string representation</returns>
        public override string ToString()
        {
            var filterText = FilterName.IsEmpty ? "Unknown" : FilterName.ToString();
            var summaryText = ConfigurationChangeSummary.IsEmpty ? "No summary" : ConfigurationChangeSummary.ToString();
            var status = WasSuccessful ? "successfully updated" : "failed to update";
            return $"FilterConfigurationChanged: {filterText} {status} - {summaryText}";
        }

        #endregion
    }
}