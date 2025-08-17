using System;
using System.Collections.Generic;
using Unity.Collections;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Alerting.Messages;
using AhBearStudios.Core.Messaging;

namespace AhBearStudios.Core.Alerting.Filters
{
    /// <summary>
    /// Abstract base class for alert filter implementations.
    /// Provides common functionality for filter statistics, configuration, and lifecycle management.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    public abstract class BaseAlertFilter : IAlertFilter
    {
        private readonly object _syncLock = new object();
        private readonly IMessageBusService _messageBusService;
        private volatile bool _isEnabled = true;
        private volatile bool _isDisposed;
        private FilterStatistics _statistics = FilterStatistics.Empty;
        private Dictionary<string, object> _configuration = new Dictionary<string, object>();
        
        /// <summary>
        /// Gets the unique name identifier for this filter.
        /// </summary>
        public abstract FixedString64Bytes Name { get; }

        /// <summary>
        /// Gets or sets whether this filter is currently enabled.
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled && !_isDisposed;
            set => _isEnabled = value;
        }

        /// <summary>
        /// Gets or sets the priority order for this filter.
        /// </summary>
        public int Priority { get; set; } = 100;

        /// <summary>
        /// Gets the filter execution statistics.
        /// </summary>
        public FilterStatistics Statistics => _statistics;

        /// <summary>
        /// Initializes a new instance of the BaseAlertFilter class.
        /// </summary>
        /// <param name="messageBusService">Message bus service for publishing filter events</param>
        protected BaseAlertFilter(IMessageBusService messageBusService)
        {
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
        }

        /// <summary>
        /// Evaluates whether an alert should be allowed through the filter.
        /// </summary>
        /// <param name="alert">The alert to evaluate</param>
        /// <param name="context">Additional filtering context</param>
        /// <returns>Filter decision result</returns>
        public virtual FilterResult Evaluate(Alert alert, FilterContext context = default)
        {
            if (!IsEnabled)
                return FilterResult.Allow("Filter is disabled");

            if (alert == null)
                return FilterResult.Allow("Null alert passed through");

            var startTime = DateTime.UtcNow;
            
            try
            {
                // Perform filter-specific evaluation
                var result = EvaluateCore(alert, context);
                
                var duration = DateTime.UtcNow - startTime;
                UpdateStatistics(result.Decision, duration);
                
                return result;
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                UpdateStatistics(FilterDecision.Allow, duration); // Allow on error to avoid blocking
                
                return FilterResult.Allow($"Filter error: {ex.Message}", duration);
            }
        }

        /// <summary>
        /// Determines if this filter can handle the specified alert type.
        /// </summary>
        /// <param name="alert">The alert to check</param>
        /// <returns>True if this filter applies to the alert</returns>
        public virtual bool CanHandle(Alert alert)
        {
            if (!IsEnabled || alert == null)
                return false;

            return CanHandleCore(alert);
        }

        /// <summary>
        /// Configures the filter with new settings.
        /// </summary>
        /// <param name="configuration">Filter configuration data</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>True if configuration was applied successfully</returns>
        public virtual bool Configure(Dictionary<string, object> configuration, Guid correlationId = default)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(Name.ToString());

            var validationResult = ValidateConfiguration(configuration);
            if (!validationResult.IsValid)
                return false;

            var previousConfig = _configuration;
            _configuration = configuration ?? new Dictionary<string, object>();

            // Handle common properties
            if (_configuration.TryGetValue("Priority", out var priorityValue))
            {
                if (int.TryParse(priorityValue.ToString(), out var priority))
                {
                    Priority = priority;
                }
            }

            // Apply configuration
            var result = ConfigureCore(_configuration, correlationId);
            
            if (result)
            {
                var message = FilterConfigurationChangedMessage.Create(
                    filterName: Name,
                    changeSummary: "Filter configuration updated successfully",
                    previousConfig: previousConfig,
                    newConfig: _configuration,
                    wasSuccessful: true,
                    isEnabled: IsEnabled,
                    priority: Priority,
                    source: "BaseAlertFilter",
                    correlationId: correlationId);
                
                _messageBusService.PublishMessage(message);
            }
            else
            {
                // Rollback on failure
                _configuration = previousConfig;
                
                var failureMessage = FilterConfigurationChangedMessage.Create(
                    filterName: Name,
                    changeSummary: "Filter configuration update failed",
                    previousConfig: previousConfig,
                    newConfig: _configuration,
                    wasSuccessful: false,
                    isEnabled: IsEnabled,
                    priority: Priority,
                    source: "BaseAlertFilter",
                    correlationId: correlationId);
                
                _messageBusService.PublishMessage(failureMessage);
            }

            return result;
        }

        /// <summary>
        /// Validates the filter configuration without applying changes.
        /// </summary>
        /// <param name="configuration">Configuration to validate</param>
        /// <returns>Validation result</returns>
        public virtual FilterValidationResult ValidateConfiguration(Dictionary<string, object> configuration)
        {
            if (configuration == null)
                return FilterValidationResult.Valid(); // Null config is valid (uses defaults)

            return ValidateConfigurationCore(configuration);
        }

        /// <summary>
        /// Resets filter statistics and internal state.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        public virtual void Reset(Guid correlationId = default)
        {
            lock (_syncLock)
            {
                _statistics = FilterStatistics.Empty;
                ResetCore(correlationId);
            }

            var message = FilterStatisticsUpdatedMessage.Create(
                filterName: Name,
                statistics: _statistics,
                source: "BaseAlertFilter",
                correlationId: correlationId);
            
            _messageBusService.PublishMessage(message);
        }

        /// <summary>
        /// Gets detailed information about the filter's current state and configuration.
        /// </summary>
        /// <returns>Filter diagnostic information</returns>
        public virtual FilterDiagnostics GetDiagnostics()
        {
            return new FilterDiagnostics
            {
                FilterName = Name,
                IsEnabled = IsEnabled,
                Priority = Priority,
                Configuration = new Dictionary<string, object>(_configuration),
                Statistics = Statistics,
                LastEvaluation = Statistics.LastUpdated == default ? null : Statistics.LastUpdated,
                RecentEvaluationTimes = GetRecentEvaluationTimes()
            };
        }

        /// <summary>
        /// Disposes of the filter resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of the filter resources.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            if (disposing)
            {
                _isEnabled = false;
                _configuration?.Clear();
                DisposeCore();
            }

            _isDisposed = true;
        }

        #region Abstract Methods

        /// <summary>
        /// Core implementation of alert evaluation.
        /// </summary>
        /// <param name="alert">Alert to evaluate</param>
        /// <param name="context">Filtering context</param>
        /// <returns>Filter result</returns>
        protected abstract FilterResult EvaluateCore(Alert alert, FilterContext context);

        /// <summary>
        /// Core implementation to determine if filter can handle an alert.
        /// </summary>
        /// <param name="alert">Alert to check</param>
        /// <returns>True if filter can handle the alert</returns>
        protected abstract bool CanHandleCore(Alert alert);

        /// <summary>
        /// Core implementation of configuration application.
        /// </summary>
        /// <param name="configuration">Configuration to apply</param>
        /// <param name="correlationId">Correlation ID</param>
        /// <returns>True if configuration was applied</returns>
        protected abstract bool ConfigureCore(Dictionary<string, object> configuration, Guid correlationId);

        /// <summary>
        /// Core implementation of configuration validation.
        /// </summary>
        /// <param name="configuration">Configuration to validate</param>
        /// <returns>Validation result</returns>
        protected abstract FilterValidationResult ValidateConfigurationCore(Dictionary<string, object> configuration);

        #endregion

        #region Protected Helper Methods

        /// <summary>
        /// Core implementation of filter reset.
        /// </summary>
        /// <param name="correlationId">Correlation ID</param>
        protected virtual void ResetCore(Guid correlationId)
        {
            // Override in derived classes for custom reset behavior
        }

        /// <summary>
        /// Core implementation of resource disposal.
        /// </summary>
        protected virtual void DisposeCore()
        {
            // Override in derived classes for custom disposal
        }

        /// <summary>
        /// Gets recent evaluation times for diagnostics.
        /// </summary>
        /// <returns>List of recent evaluation times</returns>
        protected virtual IReadOnlyList<double> GetRecentEvaluationTimes()
        {
            // Default implementation returns empty list
            // Derived classes can override to track recent times
            return Array.Empty<double>();
        }

        /// <summary>
        /// Updates filter statistics after an evaluation.
        /// </summary>
        /// <param name="decision">Filter decision made</param>
        /// <param name="duration">Evaluation duration</param>
        protected void UpdateStatistics(FilterDecision decision, TimeSpan duration)
        {
            lock (_syncLock)
            {
                var total = _statistics.TotalEvaluations + 1;
                var allowed = decision == FilterDecision.Allow ? _statistics.AllowedCount + 1 : _statistics.AllowedCount;
                var suppressed = decision == FilterDecision.Suppress ? _statistics.SuppressedCount + 1 : _statistics.SuppressedCount;
                var modified = decision == FilterDecision.Modify ? _statistics.ModifiedCount + 1 : _statistics.ModifiedCount;
                var deferred = decision == FilterDecision.Defer ? _statistics.DeferredCount + 1 : _statistics.DeferredCount;
                
                var avgTime = (_statistics.AverageEvaluationTimeMs * _statistics.TotalEvaluations + duration.TotalMilliseconds) / total;
                var maxTime = Math.Max(_statistics.MaxEvaluationTimeMs, duration.TotalMilliseconds);
                
                _statistics = new FilterStatistics
                {
                    TotalEvaluations = total,
                    AllowedCount = allowed,
                    SuppressedCount = suppressed,
                    ModifiedCount = modified,
                    DeferredCount = deferred,
                    AverageEvaluationTimeMs = avgTime,
                    MaxEvaluationTimeMs = maxTime,
                    LastUpdated = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Gets a configuration value with type checking.
        /// </summary>
        /// <typeparam name="T">Expected value type</typeparam>
        /// <param name="key">Configuration key</param>
        /// <param name="defaultValue">Default value if not found</param>
        /// <returns>Configuration value or default</returns>
        protected T GetConfigValue<T>(string key, T defaultValue = default)
        {
            if (_configuration == null || !_configuration.TryGetValue(key, out var value))
                return defaultValue;

            if (value is T typedValue)
                return typedValue;

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        #endregion
    }
}