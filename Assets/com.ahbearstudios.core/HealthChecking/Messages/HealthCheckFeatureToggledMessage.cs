using System;
using Unity.Collections;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Common.Utilities;

namespace AhBearStudios.Core.HealthChecking.Messages
{
    /// <summary>
    /// Message published when a health check feature is toggled on or off.
    /// Implements IMessage for integration with the messaging bus and correlation tracking.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    public readonly record struct HealthCheckFeatureToggledMessage : IMessage
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
        /// Gets the name of the feature that was toggled.
        /// </summary>
        public FixedString64Bytes FeatureName { get; init; }

        /// <summary>
        /// Gets whether the feature is now enabled.
        /// </summary>
        public bool IsEnabled { get; init; }

        /// <summary>
        /// Gets the degradation level that triggered the toggle.
        /// </summary>
        public DegradationLevel TriggeringDegradationLevel { get; init; }

        /// <summary>
        /// Gets the reason for the feature toggle.
        /// </summary>
        public FixedString512Bytes Reason { get; init; }

        /// <summary>
        /// Gets whether this toggle was automatic or manual.
        /// </summary>
        public bool IsAutomatic { get; init; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the DateTime representation of the message timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a new HealthCheckFeatureToggledMessage following CLAUDE.md patterns.
        /// </summary>
        /// <param name="featureName">Name of the feature being toggled</param>
        /// <param name="isEnabled">Whether the feature is now enabled</param>
        /// <param name="degradationLevel">Degradation level that triggered the toggle</param>
        /// <param name="reason">Reason for the toggle</param>
        /// <param name="isAutomatic">Whether the toggle was automatic</param>
        /// <param name="source">Source component creating this message</param>
        /// <param name="correlationId">Optional correlation ID for tracking</param>
        /// <returns>New HealthCheckFeatureToggledMessage instance</returns>
        public static HealthCheckFeatureToggledMessage Create(
            string featureName,
            bool isEnabled,
            DegradationLevel degradationLevel = DegradationLevel.None,
            string reason = null,
            bool isAutomatic = false,
            FixedString64Bytes source = default,
            Guid correlationId = default)
        {
            // Input validation
            if (string.IsNullOrEmpty(featureName))
                throw new ArgumentException("Feature name cannot be null or empty", nameof(featureName));

            // ID generation with explicit parameters to avoid ambiguity
            var sourceString = source.IsEmpty ? "HealthDegradationManager" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId("HealthCheckFeatureToggledMessage", sourceString, correlationId: null);
            var finalCorrelationId = correlationId == default
                ? DeterministicIdGenerator.GenerateCorrelationId("FeatureToggle", $"{featureName}-{isEnabled}")
                : correlationId;

            return new HealthCheckFeatureToggledMessage
            {
                Id = messageId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.HealthCheckFeatureToggledMessage,
                Source = source.IsEmpty ? "HealthDegradationManager" : source,
                Priority = isEnabled ? MessagePriority.Normal : MessagePriority.High,
                CorrelationId = finalCorrelationId,
                FeatureName = featureName.Length <= 64 ? featureName : featureName[..64],
                IsEnabled = isEnabled,
                TriggeringDegradationLevel = degradationLevel,
                Reason = reason?.Length <= 512 ? reason : reason?[..512] ?? "No reason provided",
                IsAutomatic = isAutomatic
            };
        }

        #endregion

        #region String Representation

        /// <summary>
        /// Returns a string representation of this message for debugging.
        /// </summary>
        /// <returns>Feature toggled message string representation</returns>
        public override string ToString()
        {
            var stateText = IsEnabled ? "Enabled" : "Disabled";
            var typeText = IsAutomatic ? "Auto" : "Manual";
            return $"FeatureToggled: {FeatureName} {stateText} ({typeText}, Level: {TriggeringDegradationLevel})";
        }

        #endregion
    }
}