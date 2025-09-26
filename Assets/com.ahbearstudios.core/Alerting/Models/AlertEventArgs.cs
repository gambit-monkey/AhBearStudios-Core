using System;
using AhBearStudios.Core.Alerting.Configs;
using Unity.Collections;
using AhBearStudios.Core.Alerting.Models;

namespace AhBearStudios.Core.Alerting.Models
{
    /// <summary>
    /// Base event arguments for alert system events.
    /// Provides common properties for all alert-related events.
    /// </summary>
    public abstract class AlertEventArgsBase : EventArgs
    {
        /// <summary>
        /// Gets the correlation ID for tracking this event across systems.
        /// </summary>
        public Guid CorrelationId { get; init; }

        /// <summary>
        /// Gets the timestamp when this event occurred.
        /// </summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Gets the source component that raised this event.
        /// </summary>
        public FixedString64Bytes Source { get; init; }

        /// <summary>
        /// Initializes a new instance of the AlertEventArgsBase class.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="source">Source component</param>
        protected AlertEventArgsBase(Guid correlationId = default, FixedString64Bytes source = default)
        {
            CorrelationId = correlationId == default ? DeterministicIdGenerator.GenerateCorrelationId("AlertEventArgs", source.ToString()) : correlationId;
            Source = source.IsEmpty ? "AlertSystem" : source;
        }
    }

    /// <summary>
    /// Event arguments for alert raised, acknowledged, and resolved events.
    /// Contains the alert instance and additional contextual information.
    /// </summary>
    public sealed class AlertEventArgs : AlertEventArgsBase
    {
        /// <summary>
        /// Gets the alert associated with this event.
        /// </summary>
        public Alert Alert { get; init; }

        /// <summary>
        /// Gets the previous state of the alert (for state change events).
        /// </summary>
        public AlertState? PreviousState { get; init; }

        /// <summary>
        /// Gets the user or system that performed the action.
        /// </summary>
        public FixedString64Bytes ActionBy { get; init; }

        /// <summary>
        /// Gets additional context about the event.
        /// </summary>
        public string EventContext { get; init; }

        /// <summary>
        /// Gets the channels that will receive or have received this alert.
        /// </summary>
        public string[] TargetChannels { get; init; }

        /// <summary>
        /// Initializes a new instance of the AlertEventArgs class.
        /// </summary>
        /// <param name="alert">The alert associated with this event</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="source">Source component</param>
        /// <param name="previousState">Previous alert state</param>
        /// <param name="actionBy">User or system performing the action</param>
        /// <param name="context">Additional event context</param>
        /// <param name="targetChannels">Target channels for the alert</param>
        public AlertEventArgs(
            Alert alert,
            Guid correlationId = default,
            FixedString64Bytes source = default,
            AlertState? previousState = null,
            FixedString64Bytes actionBy = default,
            string context = null,
            string[] targetChannels = null)
            : base(correlationId, source)
        {
            Alert = alert ?? throw new ArgumentNullException(nameof(alert));
            PreviousState = previousState;
            ActionBy = actionBy;
            EventContext = context;
            TargetChannels = targetChannels ?? Array.Empty<string>();
        }

        /// <summary>
        /// Creates event args for alert raised event.
        /// </summary>
        /// <param name="alert">The raised alert</param>
        /// <param name="correlationId">Correlation ID</param>
        /// <param name="targetChannels">Target delivery channels</param>
        /// <returns>Alert event arguments</returns>
        public static AlertEventArgs ForAlertRaised(
            Alert alert,
            Guid correlationId = default,
            string[] targetChannels = null)
        {
            return new AlertEventArgs(
                alert: alert,
                correlationId: correlationId,
                source: "AlertService",
                context: "Alert raised",
                targetChannels: targetChannels);
        }

        /// <summary>
        /// Creates event args for alert acknowledged event.
        /// </summary>
        /// <param name="alert">The acknowledged alert</param>
        /// <param name="acknowledgedBy">User or system that acknowledged</param>
        /// <param name="correlationId">Correlation ID</param>
        /// <returns>Alert event arguments</returns>
        public static AlertEventArgs ForAlertAcknowledged(
            Alert alert,
            FixedString64Bytes acknowledgedBy,
            Guid correlationId = default)
        {
            return new AlertEventArgs(
                alert: alert,
                correlationId: correlationId,
                source: "AlertService",
                previousState: AlertState.Active,
                actionBy: acknowledgedBy,
                context: "Alert acknowledged");
        }

        /// <summary>
        /// Creates event args for alert resolved event.
        /// </summary>
        /// <param name="alert">The resolved alert</param>
        /// <param name="resolvedBy">User or system that resolved</param>
        /// <param name="correlationId">Correlation ID</param>
        /// <returns>Alert event arguments</returns>
        public static AlertEventArgs ForAlertResolved(
            Alert alert,
            FixedString64Bytes resolvedBy,
            Guid correlationId = default)
        {
            return new AlertEventArgs(
                alert: alert,
                correlationId: correlationId,
                source: "AlertService",
                previousState: alert.IsAcknowledged ? AlertState.Acknowledged : AlertState.Active,
                actionBy: resolvedBy,
                context: "Alert resolved");
        }
    }

    /// <summary>
    /// Event arguments for alert system health changes.
    /// Provides information about system health status and metrics.
    /// </summary>
    public sealed class AlertSystemHealthEventArgs : AlertEventArgsBase
    {
        /// <summary>
        /// Gets the previous health status.
        /// </summary>
        public bool PreviousHealthStatus { get; init; }

        /// <summary>
        /// Gets the current health status.
        /// </summary>
        public bool CurrentHealthStatus { get; init; }

        /// <summary>
        /// Gets the health score (0-100).
        /// </summary>
        public double HealthScore { get; init; }

        /// <summary>
        /// Gets the reason for the health change.
        /// </summary>
        public FixedString512Bytes HealthChangeReason { get; init; }

        /// <summary>
        /// Gets the current system statistics.
        /// </summary>
        public AlertStatistics Statistics { get; init; }

        /// <summary>
        /// Gets specific health issues detected.
        /// </summary>
        public string[] HealthIssues { get; init; }

        /// <summary>
        /// Gets recommendations for improving health.
        /// </summary>
        public string[] Recommendations { get; init; }

        /// <summary>
        /// Initializes a new instance of the AlertSystemHealthEventArgs class.
        /// </summary>
        /// <param name="previousHealthStatus">Previous health status</param>
        /// <param name="currentHealthStatus">Current health status</param>
        /// <param name="healthScore">Health score (0-100)</param>
        /// <param name="reason">Reason for health change</param>
        /// <param name="statistics">Current system statistics</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="source">Source component</param>
        /// <param name="healthIssues">Specific health issues</param>
        /// <param name="recommendations">Health recommendations</param>
        public AlertSystemHealthEventArgs(
            bool previousHealthStatus,
            bool currentHealthStatus,
            double healthScore,
            string reason,
            AlertStatistics statistics,
            Guid correlationId = default,
            FixedString64Bytes source = default,
            string[] healthIssues = null,
            string[] recommendations = null)
            : base(correlationId, source.IsEmpty ? "AlertSystemHealthMonitor" : source)
        {
            PreviousHealthStatus = previousHealthStatus;
            CurrentHealthStatus = currentHealthStatus;
            HealthScore = Math.Max(0, Math.Min(100, healthScore));
            HealthChangeReason = reason ?? "Health status changed";
            Statistics = statistics;
            HealthIssues = healthIssues ?? Array.Empty<string>();
            Recommendations = recommendations ?? Array.Empty<string>();
        }

        /// <summary>
        /// Gets whether this represents a health improvement.
        /// </summary>
        public bool IsHealthImprovement => CurrentHealthStatus && !PreviousHealthStatus;

        /// <summary>
        /// Gets whether this represents a health degradation.
        /// </summary>
        public bool IsHealthDegradation => !CurrentHealthStatus && PreviousHealthStatus;

        /// <summary>
        /// Gets the severity of the health change.
        /// </summary>
        public HealthChangeSeverity Severity => HealthScore switch
        {
            >= 90 => HealthChangeSeverity.Good,
            >= 70 => HealthChangeSeverity.Warning,
            >= 50 => HealthChangeSeverity.Concerning,
            _ => HealthChangeSeverity.Critical
        };
    }

    /// <summary>
    /// Severity levels for health changes.
    /// </summary>
    public enum HealthChangeSeverity : byte
    {
        /// <summary>
        /// System health is good.
        /// </summary>
        Good = 0,

        /// <summary>
        /// System health has warnings but is functional.
        /// </summary>
        Warning = 1,

        /// <summary>
        /// System health is concerning and needs attention.
        /// </summary>
        Concerning = 2,

        /// <summary>
        /// System health is critical and requires immediate action.
        /// </summary>
        Critical = 3
    }

    /// <summary>
    /// Event arguments for channel-related events (registration, health changes, etc.).
    /// </summary>
    public sealed class ChannelEventArgs : AlertEventArgsBase
    {
        /// <summary>
        /// Gets the name of the channel.
        /// </summary>
        public FixedString64Bytes ChannelName { get; init; }

        /// <summary>
        /// Gets the type of channel event.
        /// </summary>
        public ChannelEventType EventType { get; init; }

        /// <summary>
        /// Gets additional event data specific to the event type.
        /// </summary>
        public object EventData { get; init; }

        /// <summary>
        /// Gets the channel configuration (for registration/configuration events).
        /// </summary>
        public ChannelConfig Configuration { get; init; }

        /// <summary>
        /// Gets error information (for error events).
        /// </summary>
        public Exception Exception { get; init; }

        /// <summary>
        /// Initializes a new instance of the ChannelEventArgs class.
        /// </summary>
        /// <param name="channelName">Name of the channel</param>
        /// <param name="eventType">Type of channel event</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="source">Source component</param>
        /// <param name="eventData">Additional event data</param>
        /// <param name="configuration">Channel configuration</param>
        /// <param name="exception">Error information</param>
        public ChannelEventArgs(
            FixedString64Bytes channelName,
            ChannelEventType eventType,
            Guid correlationId = default,
            FixedString64Bytes source = default,
            object eventData = null,
            ChannelConfig configuration = null,
            Exception exception = null)
            : base(correlationId, source.IsEmpty ? "ChannelManager" : source)
        {
            ChannelName = channelName;
            EventType = eventType;
            EventData = eventData;
            Configuration = configuration;
            Exception = exception;
        }
    }

    /// <summary>
    /// Types of channel events.
    /// </summary>
    public enum ChannelEventType : byte
    {
        /// <summary>
        /// Channel was registered with the system.
        /// </summary>
        Registered = 0,

        /// <summary>
        /// Channel was unregistered from the system.
        /// </summary>
        Unregistered = 1,

        /// <summary>
        /// Channel health status changed.
        /// </summary>
        HealthChanged = 2,

        /// <summary>
        /// Channel configuration was updated.
        /// </summary>
        ConfigurationChanged = 3,

        /// <summary>
        /// Channel encountered an error.
        /// </summary>
        Error = 4,

        /// <summary>
        /// Channel was enabled.
        /// </summary>
        Enabled = 5,

        /// <summary>
        /// Channel was disabled.
        /// </summary>
        Disabled = 6
    }

    /// <summary>
    /// Event arguments for filter-related events.
    /// </summary>
    public sealed class FilterEventArgs : AlertEventArgsBase
    {
        /// <summary>
        /// Gets the name of the filter.
        /// </summary>
        public FixedString64Bytes FilterName { get; init; }

        /// <summary>
        /// Gets the type of filter event.
        /// </summary>
        public FilterEventType EventType { get; init; }

        /// <summary>
        /// Gets the alert that was being filtered (if applicable).
        /// </summary>
        public Alert Alert { get; init; }

        /// <summary>
        /// Gets the filter decision result (if applicable).
        /// </summary>
        public object FilterResult { get; init; }

        /// <summary>
        /// Gets error information (for error events).
        /// </summary>
        public Exception Exception { get; init; }

        /// <summary>
        /// Initializes a new instance of the FilterEventArgs class.
        /// </summary>
        /// <param name="filterName">Name of the filter</param>
        /// <param name="eventType">Type of filter event</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="source">Source component</param>
        /// <param name="alert">Alert being filtered</param>
        /// <param name="filterResult">Filter decision result</param>
        /// <param name="exception">Error information</param>
        public FilterEventArgs(
            FixedString64Bytes filterName,
            FilterEventType eventType,
            Guid correlationId = default,
            FixedString64Bytes source = default,
            Alert alert = null,
            object filterResult = null,
            Exception exception = null)
            : base(correlationId, source.IsEmpty ? "FilterManager" : source)
        {
            FilterName = filterName;
            EventType = eventType;
            Alert = alert;
            FilterResult = filterResult;
            Exception = exception;
        }
    }

    /// <summary>
    /// Types of filter events.
    /// </summary>
    public enum FilterEventType : byte
    {
        /// <summary>
        /// Filter was added to the system.
        /// </summary>
        Added = 0,

        /// <summary>
        /// Filter was removed from the system.
        /// </summary>
        Removed = 1,

        /// <summary>
        /// Filter configuration was updated.
        /// </summary>
        ConfigurationChanged = 2,

        /// <summary>
        /// Filter suppressed an alert.
        /// </summary>
        AlertSuppressed = 3,

        /// <summary>
        /// Filter modified an alert.
        /// </summary>
        AlertModified = 4,

        /// <summary>
        /// Filter encountered an error during evaluation.
        /// </summary>
        EvaluationError = 5,

        /// <summary>
        /// Filter was enabled.
        /// </summary>
        Enabled = 6,

        /// <summary>
        /// Filter was disabled.
        /// </summary>
        Disabled = 7
    }

    /// <summary>
    /// Event arguments for alert delivery events.
    /// </summary>
    public sealed class AlertDeliveryEventArgs : AlertEventArgsBase
    {
        /// <summary>
        /// Gets the alert being delivered.
        /// </summary>
        public Alert Alert { get; init; }

        /// <summary>
        /// Gets the channel used for delivery.
        /// </summary>
        public FixedString64Bytes ChannelName { get; init; }

        /// <summary>
        /// Gets whether the delivery was successful.
        /// </summary>
        public bool IsSuccessful { get; init; }

        /// <summary>
        /// Gets the delivery duration.
        /// </summary>
        public TimeSpan DeliveryDuration { get; init; }

        /// <summary>
        /// Gets error information (for failed deliveries).
        /// </summary>
        public Exception Exception { get; init; }

        /// <summary>
        /// Gets the retry count (for retry scenarios).
        /// </summary>
        public int RetryCount { get; init; }

        /// <summary>
        /// Gets whether this was the final delivery attempt.
        /// </summary>
        public bool IsFinalAttempt { get; init; }

        /// <summary>
        /// Initializes a new instance of the AlertDeliveryEventArgs class.
        /// </summary>
        /// <param name="alert">Alert being delivered</param>
        /// <param name="channelName">Channel used for delivery</param>
        /// <param name="isSuccessful">Whether delivery was successful</param>
        /// <param name="deliveryDuration">Delivery duration</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="source">Source component</param>
        /// <param name="exception">Error information</param>
        /// <param name="retryCount">Retry count</param>
        /// <param name="isFinalAttempt">Whether this was the final attempt</param>
        public AlertDeliveryEventArgs(
            Alert alert,
            FixedString64Bytes channelName,
            bool isSuccessful,
            TimeSpan deliveryDuration,
            Guid correlationId = default,
            FixedString64Bytes source = default,
            Exception exception = null,
            int retryCount = 0,
            bool isFinalAttempt = true)
            : base(correlationId, source.IsEmpty ? "AlertDeliveryService" : source)
        {
            Alert = alert ?? throw new ArgumentNullException(nameof(alert));
            ChannelName = channelName;
            IsSuccessful = isSuccessful;
            DeliveryDuration = deliveryDuration;
            Exception = exception;
            RetryCount = retryCount;
            IsFinalAttempt = isFinalAttempt;
        }

        /// <summary>
        /// Creates event args for successful delivery.
        /// </summary>
        /// <param name="alert">Delivered alert</param>
        /// <param name="channelName">Delivery channel</param>
        /// <param name="deliveryDuration">Delivery duration</param>
        /// <param name="correlationId">Correlation ID</param>
        /// <returns>Delivery event arguments</returns>
        public static AlertDeliveryEventArgs ForSuccessfulDelivery(
            Alert alert,
            FixedString64Bytes channelName,
            TimeSpan deliveryDuration,
            Guid correlationId = default)
        {
            return new AlertDeliveryEventArgs(
                alert: alert,
                channelName: channelName,
                isSuccessful: true,
                deliveryDuration: deliveryDuration,
                correlationId: correlationId);
        }

        /// <summary>
        /// Creates event args for failed delivery.
        /// </summary>
        /// <param name="alert">Alert that failed to deliver</param>
        /// <param name="channelName">Channel that failed</param>
        /// <param name="exception">Delivery error</param>
        /// <param name="deliveryDuration">Attempted delivery duration</param>
        /// <param name="retryCount">Current retry count</param>
        /// <param name="isFinalAttempt">Whether this was the final attempt</param>
        /// <param name="correlationId">Correlation ID</param>
        /// <returns>Delivery event arguments</returns>
        public static AlertDeliveryEventArgs ForFailedDelivery(
            Alert alert,
            FixedString64Bytes channelName,
            Exception exception,
            TimeSpan deliveryDuration,
            int retryCount = 0,
            bool isFinalAttempt = true,
            Guid correlationId = default)
        {
            return new AlertDeliveryEventArgs(
                alert: alert,
                channelName: channelName,
                isSuccessful: false,
                deliveryDuration: deliveryDuration,
                correlationId: correlationId,
                exception: exception,
                retryCount: retryCount,
                isFinalAttempt: isFinalAttempt);
        }
    }
}