using System;
using AhBearStudios.Core.Messaging.Configs;

namespace AhBearStudios.Core.Messaging.Builders
{
    /// <summary>
    /// Builder for creating MessageSubscriptionConfig instances.
    /// Provides a fluent API for configuring message subscription behavior.
    /// </summary>
    public sealed class MessageSubscriptionConfigBuilder
    {
        private int _maxSubscribers = 10000;
        private int _maxConcurrentHandlers = Environment.ProcessorCount * 2;
        private TimeSpan _handlerTimeout = TimeSpan.FromSeconds(30);
        private bool _asyncEnabled = true;
        private bool _filteringEnabled = true;
        private bool _priorityRouting = true;
        private bool _scopedSubscriptions = true;
        private bool _performanceMonitoringEnabled = false;

        /// <summary>
        /// Sets the maximum number of subscribers.
        /// </summary>
        /// <param name="maxSubscribers">Maximum subscribers</param>
        /// <returns>The builder instance for method chaining</returns>
        public MessageSubscriptionConfigBuilder WithMaxSubscribers(int maxSubscribers)
        {
            _maxSubscribers = maxSubscribers;
            return this;
        }

        /// <summary>
        /// Sets the maximum number of concurrent handlers.
        /// </summary>
        /// <param name="maxConcurrentHandlers">Maximum concurrent handlers</param>
        /// <returns>The builder instance for method chaining</returns>
        public MessageSubscriptionConfigBuilder WithMaxConcurrentHandlers(int maxConcurrentHandlers)
        {
            _maxConcurrentHandlers = maxConcurrentHandlers;
            return this;
        }

        /// <summary>
        /// Sets the handler timeout.
        /// </summary>
        /// <param name="handlerTimeout">Handler timeout</param>
        /// <returns>The builder instance for method chaining</returns>
        public MessageSubscriptionConfigBuilder WithHandlerTimeout(TimeSpan handlerTimeout)
        {
            _handlerTimeout = handlerTimeout;
            return this;
        }

        /// <summary>
        /// Enables or disables async handlers.
        /// </summary>
        /// <param name="enabled">Whether async handlers are enabled</param>
        /// <returns>The builder instance for method chaining</returns>
        public MessageSubscriptionConfigBuilder WithAsyncEnabled(bool enabled)
        {
            _asyncEnabled = enabled;
            return this;
        }

        /// <summary>
        /// Enables or disables message filtering.
        /// </summary>
        /// <param name="enabled">Whether filtering is enabled</param>
        /// <returns>The builder instance for method chaining</returns>
        public MessageSubscriptionConfigBuilder WithFilteringEnabled(bool enabled)
        {
            _filteringEnabled = enabled;
            return this;
        }

        /// <summary>
        /// Enables or disables priority routing.
        /// </summary>
        /// <param name="enabled">Whether priority routing is enabled</param>
        /// <returns>The builder instance for method chaining</returns>
        public MessageSubscriptionConfigBuilder WithPriorityRouting(bool enabled)
        {
            _priorityRouting = enabled;
            return this;
        }

        /// <summary>
        /// Enables or disables scoped subscriptions.
        /// </summary>
        /// <param name="enabled">Whether scoped subscriptions are enabled</param>
        /// <returns>The builder instance for method chaining</returns>
        public MessageSubscriptionConfigBuilder WithScopedSubscriptions(bool enabled)
        {
            _scopedSubscriptions = enabled;
            return this;
        }

        /// <summary>
        /// Enables or disables performance monitoring.
        /// </summary>
        /// <param name="enabled">Whether performance monitoring is enabled</param>
        /// <returns>The builder instance for method chaining</returns>
        public MessageSubscriptionConfigBuilder WithPerformanceMonitoring(bool enabled)
        {
            _performanceMonitoringEnabled = enabled;
            return this;
        }

        /// <summary>
        /// Builds the MessageSubscriptionConfig instance with the configured values.
        /// </summary>
        /// <returns>A new MessageSubscriptionConfig instance</returns>
        public MessageSubscriptionConfig Build()
        {
            return new MessageSubscriptionConfig
            {
                MaxTotalSubscriptions = _maxSubscribers,
                MaxConcurrentHandlers = _maxConcurrentHandlers,
                ProcessingTimeout = _handlerTimeout,
                AsyncHandlingEnabled = _asyncEnabled,
                FilteringEnabled = _filteringEnabled,
                PriorityRoutingEnabled = _priorityRouting,
                PerformanceMonitoringEnabled = _performanceMonitoringEnabled
            };
        }
    }
}