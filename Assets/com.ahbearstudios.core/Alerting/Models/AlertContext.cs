using System;
using System.Collections.Generic;
using Unity.Collections;

namespace AhBearStudios.Core.Alerting.Models;
    /// <summary>
    /// Zero-allocation context container for alert contextual information.
    /// Uses Unity.Collections for high-performance with minimal memory overhead.
    /// Serialization is handled through ISerializationService.
    /// Designed for game development with minimal memory overhead.
    /// </summary>
    public sealed partial record AlertContext : IDisposable
    {
        /// <summary>
        /// Additional properties for structured data using efficient collections.
        /// </summary>
        public Dictionary<string, object> Properties { get; init; } = new();

        /// <summary>
        /// Exception information if the alert is related to an error.
        /// </summary>
        public AlertExceptionInfo Exception { get; init; }

        /// <summary>
        /// Performance metrics associated with this alert.
        /// </summary>
        public AlertPerformanceMetrics Performance { get; init; }

        /// <summary>
        /// System resource information when the alert was raised.
        /// </summary>
        public AlertSystemInfo System { get; init; }

        /// <summary>
        /// User or session information for user-related alerts.
        /// </summary>
        public AlertUserInfo User { get; init; }

        /// <summary>
        /// Network-related information for network alerts.
        /// </summary>
        public AlertNetworkInfo Network { get; init; }

        /// <summary>
        /// Creates an empty alert context.
        /// </summary>
        public static AlertContext Empty => new();

        /// <summary>
        /// Creates a context with basic properties.
        /// </summary>
        /// <param name="properties">Context properties</param>
        /// <returns>Alert context with properties</returns>
        public static AlertContext WithProperties(Dictionary<string, object> properties)
        {
            return new AlertContext { Properties = properties ?? new Dictionary<string, object>() };
        }

        /// <summary>
        /// Creates a context with exception information.
        /// </summary>
        /// <param name="exception">Exception to include</param>
        /// <param name="additionalInfo">Additional context</param>
        /// <returns>Alert context with exception</returns>
        public static AlertContext WithException(Exception exception, Dictionary<string, object> additionalInfo = null)
        {
            return new AlertContext
            {
                Exception = AlertExceptionInfo.FromException(exception),
                Properties = additionalInfo ?? new Dictionary<string, object>()
            };
        }

        /// <summary>
        /// Creates a context with performance metrics.
        /// </summary>
        /// <param name="duration">Operation duration</param>
        /// <param name="memoryUsage">Memory usage in bytes</param>
        /// <param name="additionalMetrics">Additional performance data</param>
        /// <returns>Alert context with performance metrics</returns>
        public static AlertContext WithPerformance(
            TimeSpan duration,
            long memoryUsage = 0,
            Dictionary<string, double> additionalMetrics = null)
        {
            return new AlertContext
            {
                Performance = new AlertPerformanceMetrics
                {
                    DurationTicks = duration.Ticks,
                    MemoryUsageBytes = memoryUsage,
                    AdditionalMetrics = additionalMetrics ?? new Dictionary<string, double>()
                }
            };
        }

        /// <summary>
        /// Adds a property to the context (creates a new instance).
        /// </summary>
        /// <param name="key">Property key</param>
        /// <param name="value">Property value</param>
        /// <returns>New context with added property</returns>
        public AlertContext WithProperty(string key, object value)
        {
            var newProperties = new Dictionary<string, object>(Properties)
            {
                [key] = value
            };

            return this with { Properties = newProperties };
        }

        /// <summary>
        /// Gets a property value with optional default.
        /// </summary>
        /// <typeparam name="T">Property type</typeparam>
        /// <param name="key">Property key</param>
        /// <param name="defaultValue">Default value if not found</param>
        /// <returns>Property value or default</returns>
        public T GetProperty<T>(string key, T defaultValue = default)
        {
            return Properties.TryGetValue(key, out var value) && value is T typedValue
                ? typedValue
                : defaultValue;
        }

        /// <summary>
        /// Checks if a property exists.
        /// </summary>
        /// <param name="key">Property key</param>
        /// <returns>True if property exists</returns>
        public bool HasProperty(string key)
        {
            return Properties.ContainsKey(key);
        }

        /// <summary>
        /// Disposes resources associated with this context.
        /// </summary>
        public void Dispose()
        {
            // Dispose any disposable properties
            if (Properties != null)
            {
                foreach (var value in Properties.Values)
                {
                    if (value is IDisposable disposable)
                        disposable.Dispose();
                }
                Properties.Clear();
            }
        }
    }