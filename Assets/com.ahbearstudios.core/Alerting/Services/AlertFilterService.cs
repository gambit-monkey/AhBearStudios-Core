using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using Cysharp.Threading.Tasks;
using ZLinq;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Alerting.Filters;
using AhBearStudios.Core.Common.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Serialization;
using AhBearStudios.Core.Messaging;
using Unity.Profiling;

namespace AhBearStudios.Core.Alerting.Services
{
    /// <summary>
    /// Service responsible for managing the lifecycle of alert filters.
    /// Handles filter registration, priority ordering, performance monitoring, and execution orchestration.
    /// Designed for Unity game development with zero-allocation patterns and high performance.
    /// </summary>
    public sealed class AlertFilterService : IAlertFilterService
    {
        private readonly object _syncLock = new object();
        private readonly List<IAlertFilter> _filters = new List<IAlertFilter>();
        private readonly Dictionary<string, FilterHealth> _filterHealth = new Dictionary<string, FilterHealth>();
        private readonly Dictionary<string, FilterPerformanceData> _filterPerformance = new Dictionary<string, FilterPerformanceData>();
        private readonly ILoggingService _loggingService;
        private readonly ISerializationService _serializationService;
        private readonly IMessageBusService _messageBusService;
        
        private volatile bool _isEnabled = true;
        private volatile bool _isDisposed;
        private readonly Timer _performanceTimer;
        private readonly Timer _healthCheckTimer;
        private DateTime _lastHealthCheck = DateTime.UtcNow;
        private readonly TimeSpan _healthCheckInterval = TimeSpan.FromMinutes(5);
        private readonly TimeSpan _performanceReportInterval = TimeSpan.FromMinutes(1);

        // Performance monitoring
        private static readonly ProfilerMarker _processAlertMarker = new ProfilerMarker("AlertFilterService.ProcessAlert");
        private static readonly ProfilerMarker _filterEvaluationMarker = new ProfilerMarker("AlertFilterService.FilterEvaluation");

        /// <summary>
        /// Gets whether the filter manager is enabled.
        /// </summary>
        public bool IsEnabled => _isEnabled && !_isDisposed;

        /// <summary>
        /// Gets the count of registered filters.
        /// </summary>
        public int FilterCount
        {
            get
            {
                lock (_syncLock)
                {
                    return _filters.Count;
                }
            }
        }

        /// <summary>
        /// Gets the count of enabled filters.
        /// </summary>
        public int EnabledFilterCount
        {
            get
            {
                lock (_syncLock)
                {
                    return _filters.AsValueEnumerable().Count(f => f.IsEnabled);
                }
            }
        }



        /// <summary>
        /// Initializes a new instance of the AlertFilterService class.
        /// </summary>
        /// <param name="loggingService">Optional logging service for internal logging</param>
        /// <param name="serializationService">Optional serialization service for alert data serialization</param>
        /// <param name="messageBusService">Optional message bus service for event publishing</param>
        public AlertFilterService(ILoggingService loggingService = null, ISerializationService serializationService = null, IMessageBusService messageBusService = null)
        {
            _loggingService = loggingService;
            _serializationService = serializationService;
            _messageBusService = messageBusService;
            
            // Set up performance monitoring timer
            _performanceTimer = new Timer(MonitorPerformance, null, _performanceReportInterval, _performanceReportInterval);
            
            // Set up health check timer
            _healthCheckTimer = new Timer(PerformHealthChecks, null, _healthCheckInterval, _healthCheckInterval);
            
            LogInfo("Alert filter service initialized");
        }

        #region Filter Registration and Management

        /// <summary>
        /// Registers an alert filter with the manager.
        /// </summary>
        /// <param name="filter">Filter to register</param>
        /// <param name="configuration">Optional initial configuration</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>True if filter was registered successfully</returns>
        public bool RegisterFilter(IAlertFilter filter, Dictionary<string, object> configuration = null, Guid correlationId = default)
        {
            if (filter == null)
                return false;

            var filterName = filter.Name.ToString();
            
            try
            {
                lock (_syncLock)
                {
                    // Check if filter already exists
                    if (_filters.AsValueEnumerable().Any(f => f.Name.ToString() == filterName))
                    {
                        LogWarning($"Filter already registered: {filterName}", correlationId);
                        return false;
                    }

                    // Configure filter if configuration provided
                    if (configuration != null)
                    {
                        var configResult = filter.Configure(configuration, correlationId);
                        if (!configResult)
                        {
                            LogError($"Failed to configure filter during registration: {filterName}", correlationId);
                            return false;
                        }
                    }

                    // Add filter and maintain priority order
                    _filters.Add(filter);
                    _filters.Sort((x, y) => x.Priority.CompareTo(y.Priority));

                    // Initialize tracking data
                    _filterHealth[filterName] = new FilterHealth
                    {
                        FilterName = filterName,
                        IsHealthy = true,
                        LastHealthCheck = DateTime.UtcNow,
                        ConsecutiveErrors = 0
                    };

                    _filterPerformance[filterName] = new FilterPerformanceData
                    {
                        FilterName = filterName,
                        RegistrationTime = DateTime.UtcNow
                    };
                }

                // Filter events are handled via IMessage pattern - see CLAUDE.md guidelines
                // Filter registration events can be published through IMessageBusService when needed

                LogInfo($"Filter registered successfully: {filterName} (Priority: {filter.Priority})", correlationId);
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Failed to register filter {filterName}: {ex.Message}", correlationId);
                return false;
            }
        }

        /// <summary>
        /// Unregisters an alert filter from the manager.
        /// </summary>
        /// <param name="filterName">Name of filter to unregister</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>True if filter was unregistered successfully</returns>
        public bool UnregisterFilter(string filterName, Guid correlationId = default)
        {
            if (string.IsNullOrEmpty(filterName))
                return false;

            IAlertFilter filter = null;
            
            try
            {
                lock (_syncLock)
                {
                    filter = _filters.AsValueEnumerable().FirstOrDefault(f => f.Name.ToString() == filterName);
                    if (filter == null)
                        return false;

                    _filters.Remove(filter);
                    _filterHealth.Remove(filterName);
                    _filterPerformance.Remove(filterName);
                }

                // Filter events are handled via IMessage pattern - see CLAUDE.md guidelines
                
                // Dispose filter
                filter.Dispose();

                LogInfo($"Filter unregistered: {filterName}", correlationId);
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Failed to unregister filter {filterName}: {ex.Message}", correlationId);
                return false;
            }
        }

        /// <summary>
        /// Gets a registered filter by name.
        /// </summary>
        /// <param name="filterName">Name of filter to retrieve</param>
        /// <returns>Filter instance or null if not found</returns>
        public IAlertFilter GetFilter(string filterName)
        {
            if (string.IsNullOrEmpty(filterName))
                return null;

            lock (_syncLock)
            {
                return _filters.AsValueEnumerable().FirstOrDefault(f => f.Name.ToString() == filterName);
            }
        }

        /// <summary>
        /// Gets all registered filters in priority order.
        /// </summary>
        /// <returns>Collection of registered filters</returns>
        public IReadOnlyCollection<IAlertFilter> GetAllFilters()
        {
            lock (_syncLock)
            {
                return _filters.AsValueEnumerable().ToList();
            }
        }

        /// <summary>
        /// Gets enabled filters only in priority order.
        /// </summary>
        /// <returns>Collection of enabled filters</returns>
        public IReadOnlyCollection<IAlertFilter> GetEnabledFilters()
        {
            lock (_syncLock)
            {
                return _filters.AsValueEnumerable().Where(f => f.IsEnabled).ToList();
            }
        }

        #endregion

        #region Filter Execution

        /// <summary>
        /// Processes an alert through the filter chain.
        /// </summary>
        /// <param name="alert">Alert to process</param>
        /// <param name="context">Filtering context</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>Filter chain result</returns>
        public FilterChainResult ProcessAlert(Alert alert, FilterContext context = default, Guid correlationId = default)
        {
            using (_processAlertMarker.Auto())
            {
                if (!IsEnabled || alert == null)
                    return FilterChainResult.Allow(alert, "Filter manager disabled");

                var startTime = DateTime.UtcNow;
                var currentAlert = alert;
                var appliedFilters = new List<FilterApplicationResult>();
                var effectiveContext = context.CorrelationId == default 
                    ? FilterContext.WithCorrelation(correlationId) 
                    : context;

                try
                {
                    var enabledFilters = GetEnabledFilters();
                
                foreach (var filter in enabledFilters)
                {
                    var filterName = filter.Name.ToString();
                    var filterStartTime = DateTime.UtcNow;
                    
                    try
                    {
                        // Check if filter can handle this alert
                        if (!filter.CanHandle(currentAlert))
                        {
                            appliedFilters.Add(new FilterApplicationResult
                            {
                                FilterName = filterName,
                                Applied = false,
                                Reason = "Filter cannot handle alert type",
                                ProcessingTime = TimeSpan.Zero
                            });
                            continue;
                        }

                        // Evaluate alert against filter
                        FilterResult filterResult;
                        using (_filterEvaluationMarker.Auto())
                        {
                            filterResult = filter.Evaluate(currentAlert, effectiveContext);
                        }
                        var filterDuration = DateTime.UtcNow - filterStartTime;

                        // Update performance tracking
                        UpdateFilterPerformance(filterName, filterResult.Decision, filterDuration);

                        // Record filter application
                        appliedFilters.Add(new FilterApplicationResult
                        {
                            FilterName = filterName,
                            Applied = true,
                            Decision = filterResult.Decision,
                            Reason = filterResult.Reason.ToString(),
                            ProcessingTime = filterDuration
                        });

                        // Handle filter decision
                        switch (filterResult.Decision)
                        {
                            case FilterDecision.Allow:
                                continue; // Continue to next filter
                                
                            case FilterDecision.Suppress:
                                LogDebug($"Alert suppressed by filter: {filterName}", correlationId);
                                return FilterChainResult.Suppress(appliedFilters, filterResult.Reason.ToString());
                                
                            case FilterDecision.Modify:
                                if (filterResult.ModifiedAlert != null)
                                {
                                    currentAlert = filterResult.ModifiedAlert;
                                    LogDebug($"Alert modified by filter: {filterName}", correlationId);
                                }
                                continue;
                                
                            case FilterDecision.Defer:
                                LogDebug($"Alert deferred by filter: {filterName}", correlationId);
                                return FilterChainResult.Defer(currentAlert, appliedFilters, filterResult.Reason.ToString());
                                
                            default:
                                continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        var filterDuration = DateTime.UtcNow - filterStartTime;
                        
                        // Update error tracking
                        UpdateFilterError(filterName, ex);
                        
                        // Record failed filter application
                        appliedFilters.Add(new FilterApplicationResult
                        {
                            FilterName = filterName,
                            Applied = false,
                            Reason = $"Filter error: {ex.Message}",
                            ProcessingTime = filterDuration,
                            Error = ex
                        });

                        // Filter evaluation errors are handled via IMessage pattern

                        // Continue with next filter (don't let one filter break the chain)
                        LogWarning($"Filter evaluation error in {filterName}: {ex.Message}", correlationId);
                    }
                }

                    // All filters passed or modified alert
                    var totalDuration = DateTime.UtcNow - startTime;
                    LogDebug($"Alert processed through {appliedFilters.Count} filters in {totalDuration.TotalMilliseconds:F2}ms", correlationId);
                    
                    return FilterChainResult.Allow(currentAlert, "Passed all filters", appliedFilters);
                }
                catch (Exception ex)
                {
                    LogError($"Critical error in filter chain processing: {ex.Message}", correlationId);
                    return FilterChainResult.Allow(alert, "Filter chain error - allowing alert", appliedFilters);
                }
            }
        }

        /// <summary>
        /// Processes multiple alerts through the filter chain.
        /// </summary>
        /// <param name="alerts">Alerts to process</param>
        /// <param name="context">Filtering context</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>Collection of filter chain results</returns>
        public IEnumerable<FilterChainResult> ProcessAlerts(IEnumerable<Alert> alerts, FilterContext context = default, Guid correlationId = default)
        {
            if (alerts == null)
                return Array.Empty<FilterChainResult>();

            return alerts.AsValueEnumerable().Select(alert => ProcessAlert(alert, context, correlationId)).ToList();
        }

        #endregion

        #region Configuration Management

        /// <summary>
        /// Updates configuration for a specific filter.
        /// </summary>
        /// <param name="filterName">Name of filter to configure</param>
        /// <param name="configuration">New configuration</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>True if configuration was applied successfully</returns>
        public bool ConfigureFilter(string filterName, Dictionary<string, object> configuration, Guid correlationId = default)
        {
            var filter = GetFilter(filterName);
            if (filter == null || configuration == null)
                return false;

            try
            {
                var result = filter.Configure(configuration, correlationId);
                
                if (result)
                {
                    // Re-sort filters if priority changed
                    if (configuration.ContainsKey("Priority"))
                    {
                        lock (_syncLock)
                        {
                            _filters.Sort((x, y) => x.Priority.CompareTo(y.Priority));
                        }
                    }
                    
                    // Filter configuration changed events are handled via IMessage pattern
                    
                    LogInfo($"Filter configuration updated: {filterName}", correlationId);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                LogError($"Failed to configure filter {filterName}: {ex.Message}", correlationId);
                return false;
            }
        }

        /// <summary>
        /// Enables a specific filter.
        /// </summary>
        /// <param name="filterName">Name of filter to enable</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>True if filter was enabled</returns>
        public bool EnableFilter(string filterName, Guid correlationId = default)
        {
            var filter = GetFilter(filterName);
            if (filter == null)
                return false;

            try
            {
                filter.IsEnabled = true;
                LogInfo($"Filter enabled: {filterName}", correlationId);
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Failed to enable filter {filterName}: {ex.Message}", correlationId);
                return false;
            }
        }

        /// <summary>
        /// Disables a specific filter.
        /// </summary>
        /// <param name="filterName">Name of filter to disable</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>True if filter was disabled</returns>
        public bool DisableFilter(string filterName, Guid correlationId = default)
        {
            var filter = GetFilter(filterName);
            if (filter == null)
                return false;

            try
            {
                filter.IsEnabled = false;
                LogInfo($"Filter disabled: {filterName}", correlationId);
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Failed to disable filter {filterName}: {ex.Message}", correlationId);
                return false;
            }
        }

        /// <summary>
        /// Updates the priority of a specific filter and re-sorts the chain.
        /// </summary>
        /// <param name="filterName">Name of filter to update</param>
        /// <param name="newPriority">New priority value</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>True if priority was updated</returns>
        public bool UpdateFilterPriority(string filterName, int newPriority, Guid correlationId = default)
        {
            var filter = GetFilter(filterName);
            if (filter == null)
                return false;

            try
            {
                var oldPriority = filter.Priority;
                filter.Priority = newPriority;
                
                // Re-sort filter chain
                lock (_syncLock)
                {
                    _filters.Sort((x, y) => x.Priority.CompareTo(y.Priority));
                }
                
                LogInfo($"Filter priority updated: {filterName} ({oldPriority} -> {newPriority})", correlationId);
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Failed to update filter priority {filterName}: {ex.Message}", correlationId);
                return false;
            }
        }

        #endregion

        #region Performance and Health Monitoring

        /// <summary>
        /// Gets comprehensive performance metrics for all filters.
        /// </summary>
        /// <returns>Filter manager performance metrics</returns>
        public FilterManagerMetrics GetPerformanceMetrics()
        {
            lock (_syncLock)
            {
                return new FilterManagerMetrics
                {
                    TotalFilters = _filters.Count,
                    EnabledFilters = _filters.AsValueEnumerable().Count(f => f.IsEnabled),
                    HealthyFilters = _filterHealth.Values.AsValueEnumerable().Count(h => h.IsHealthy),
                    FilterPerformanceData = _filterPerformance.Values.AsValueEnumerable().ToList(),
                    LastUpdated = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Gets health information for all filters.
        /// </summary>
        /// <returns>Collection of filter health information</returns>
        public IReadOnlyCollection<FilterHealth> GetFilterHealthInfo()
        {
            lock (_syncLock)
            {
                return _filterHealth.Values.AsValueEnumerable().ToList();
            }
        }

        /// <summary>
        /// Gets diagnostics information for a specific filter.
        /// </summary>
        /// <param name="filterName">Name of filter</param>
        /// <returns>Filter diagnostics or null if not found</returns>
        public FilterDiagnostics GetFilterDiagnostics(string filterName)
        {
            var filter = GetFilter(filterName);
            if (filter == null)
                return new FilterDiagnostics(); // Return empty diagnostics

            return filter.GetDiagnostics();
        }

        /// <summary>
        /// Resets performance metrics for all filters.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        public void ResetPerformanceMetrics(Guid correlationId = default)
        {
            lock (_syncLock)
            {
                foreach (var filter in _filters)
                {
                    filter.Reset(correlationId);
                }
                
                foreach (var performance in _filterPerformance.Values)
                {
                    performance.ResetCounters();
                }
            }
            
            LogInfo("Filter performance metrics reset", correlationId);
        }

        #endregion

        #region Private Methods

        // Filter events are now handled via IMessage pattern - see IAlertFilter interface
        // FilterConfigurationChangedMessage and FilterStatisticsUpdatedMessage
        // are published through IMessageBusService

        private void UpdateFilterPerformance(string filterName, FilterDecision decision, TimeSpan duration)
        {
            lock (_syncLock)
            {
                if (_filterPerformance.TryGetValue(filterName, out var performance))
                {
                    performance.TotalEvaluations++;
                    performance.TotalProcessingTime += duration;
                    performance.LastEvaluation = DateTime.UtcNow;

                    switch (decision)
                    {
                        case FilterDecision.Allow:
                            performance.AllowCount++;
                            break;
                        case FilterDecision.Suppress:
                            performance.SuppressCount++;
                            break;
                        case FilterDecision.Modify:
                            performance.ModifyCount++;
                            break;
                        case FilterDecision.Defer:
                            performance.DeferCount++;
                            break;
                    }

                    // Update max processing time
                    if (duration > performance.MaxProcessingTime)
                        performance.MaxProcessingTime = duration;
                }
            }
        }

        private void UpdateFilterPerformanceFromStatistics(string filterName, FilterStatistics statistics)
        {
            lock (_syncLock)
            {
                if (_filterPerformance.TryGetValue(filterName, out var performance))
                {
                    // Sync with filter's own statistics
                    performance.TotalEvaluations = statistics.TotalEvaluations;
                    performance.AllowCount = statistics.AllowedCount;
                    performance.SuppressCount = statistics.SuppressedCount;
                    performance.ModifyCount = statistics.ModifiedCount;
                    performance.DeferCount = statistics.DeferredCount;
                    performance.LastEvaluation = statistics.LastUpdated;
                }
            }
        }

        private void UpdateFilterError(string filterName, Exception ex)
        {
            lock (_syncLock)
            {
                if (_filterHealth.TryGetValue(filterName, out var health))
                {
                    health.ConsecutiveErrors++;
                    health.LastError = DateTime.UtcNow;
                    health.LastErrorMessage = ex.Message;
                    
                    // Mark as unhealthy if too many consecutive errors
                    if (health.ConsecutiveErrors >= 5)
                    {
                        health.IsHealthy = false;
                    }
                }

                if (_filterPerformance.TryGetValue(filterName, out var performance))
                {
                    performance.ErrorCount++;
                }
            }
        }

        private void MonitorPerformance(object state)
        {
            if (_isDisposed)
                return;

            try
            {
                var metrics = GetPerformanceMetrics();
                
                // Check for performance issues
                foreach (var filterPerf in metrics.FilterPerformanceData)
                {
                    if (filterPerf.AverageProcessingTimeMs > 100) // 100ms threshold
                    {
                        // Filter performance alerts are handled via IMessage pattern
                        LogWarning($"High average processing time for filter {filterPerf.FilterName}: {filterPerf.AverageProcessingTimeMs:F2}ms");
                    }

                    if (filterPerf.ErrorRate > 10) // 10% error rate threshold
                    {
                        // Filter performance alerts are handled via IMessage pattern
                        LogError($"High error rate for filter {filterPerf.FilterName}: {filterPerf.ErrorRate:F2}%");
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Error during performance monitoring: {ex.Message}");
            }
        }

        private void PerformHealthChecks(object state)
        {
            if (_isDisposed || DateTime.UtcNow - _lastHealthCheck < _healthCheckInterval)
                return;

            _lastHealthCheck = DateTime.UtcNow;
            
            try
            {
                var filters = GetAllFilters();
                foreach (var filter in filters)
                {
                    var filterName = filter.Name.ToString();
                    
                    lock (_syncLock)
                    {
                        if (_filterHealth.TryGetValue(filterName, out var health))
                        {
                            health.LastHealthCheck = DateTime.UtcNow;
                            
                            // Reset consecutive errors if filter has been stable
                            if (health.ConsecutiveErrors > 0 && DateTime.UtcNow - health.LastError > TimeSpan.FromMinutes(10))
                            {
                                health.ConsecutiveErrors = 0;
                                health.IsHealthy = true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Error during health checks: {ex.Message}");
            }
        }

        private void LogDebug(string message, Guid correlationId = default)
        {
            _loggingService?.LogDebug($"[AlertFilterService] {message}", correlationId.ToString(), "AlertFilterService");
        }

        private void LogInfo(string message, Guid correlationId = default)
        {
            _loggingService?.LogInfo($"[AlertFilterService] {message}", correlationId.ToString(), "AlertFilterService");
        }

        private void LogWarning(string message, Guid correlationId = default)
        {
            _loggingService?.LogWarning($"[AlertFilterService] {message}", correlationId.ToString(), "AlertFilterService");
        }

        private void LogError(string message, Guid correlationId = default)
        {
            _loggingService?.LogError($"[AlertFilterService] {message}", correlationId.ToString(), "AlertFilterService");
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Disposes of the filter manager resources.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isEnabled = false;
            _isDisposed = true;

            _performanceTimer?.Dispose();
            _healthCheckTimer?.Dispose();

            lock (_syncLock)
            {
                foreach (var filter in _filters)
                {
                    try
                    {
                        filter.Dispose();
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error disposing filter {filter.Name}: {ex.Message}");
                    }
                }

                _filters.Clear();
                _filterHealth.Clear();
                _filterPerformance.Clear();
            }

            LogInfo("Alert filter service disposed");
        }

        #endregion
    }

    #region Supporting Types

    /// <summary>
    /// Result of processing an alert through the filter chain.
    /// </summary>
    public sealed class FilterChainResult
    {
        public FilterChainDecision Decision { get; set; }
        public Alert ProcessedAlert { get; set; }
        public string Reason { get; set; }
        public List<FilterApplicationResult> AppliedFilters { get; set; } = new List<FilterApplicationResult>();
        public TimeSpan TotalProcessingTime { get; set; }

        public static FilterChainResult Allow(Alert alert, string reason, List<FilterApplicationResult> appliedFilters = null)
        {
            return new FilterChainResult
            {
                Decision = FilterChainDecision.Allow,
                ProcessedAlert = alert,
                Reason = reason,
                AppliedFilters = appliedFilters ?? new List<FilterApplicationResult>()
            };
        }

        public static FilterChainResult Suppress(List<FilterApplicationResult> appliedFilters, string reason)
        {
            return new FilterChainResult
            {
                Decision = FilterChainDecision.Suppress,
                ProcessedAlert = null,
                Reason = reason,
                AppliedFilters = appliedFilters ?? new List<FilterApplicationResult>()
            };
        }

        public static FilterChainResult Defer(Alert alert, List<FilterApplicationResult> appliedFilters, string reason)
        {
            return new FilterChainResult
            {
                Decision = FilterChainDecision.Defer,
                ProcessedAlert = alert,
                Reason = reason,
                AppliedFilters = appliedFilters ?? new List<FilterApplicationResult>()
            };
        }
    }

    /// <summary>
    /// Decision made by the filter chain.
    /// </summary>
    public enum FilterChainDecision
    {
        Allow = 0,
        Suppress = 1,
        Defer = 2
    }

    /// <summary>
    /// Result of applying a single filter to an alert.
    /// </summary>
    public sealed class FilterApplicationResult
    {
        public string FilterName { get; set; }
        public bool Applied { get; set; }
        public FilterDecision Decision { get; set; }
        public string Reason { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public Exception Error { get; set; }
    }

    /// <summary>
    /// Health information for a filter.
    /// </summary>
    public sealed class FilterHealth
    {
        public string FilterName { get; set; }
        public bool IsHealthy { get; set; } = true;
        public DateTime LastHealthCheck { get; set; }
        public int ConsecutiveErrors { get; set; }
        public DateTime? LastError { get; set; }
        public string LastErrorMessage { get; set; }
    }

    /// <summary>
    /// Performance data for a filter.
    /// </summary>
    public sealed class FilterPerformanceData
    {
        public string FilterName { get; set; }
        public DateTime RegistrationTime { get; set; }
        public long TotalEvaluations { get; set; }
        public long AllowCount { get; set; }
        public long SuppressCount { get; set; }
        public long ModifyCount { get; set; }
        public long DeferCount { get; set; }
        public long ErrorCount { get; set; }
        public TimeSpan TotalProcessingTime { get; set; }
        public TimeSpan MaxProcessingTime { get; set; }
        public DateTime? LastEvaluation { get; set; }

        public double AverageProcessingTimeMs => TotalEvaluations > 0 
            ? TotalProcessingTime.TotalMilliseconds / TotalEvaluations 
            : 0;

        public double SuppressionRate => TotalEvaluations > 0 
            ? (double)SuppressCount / TotalEvaluations * 100 
            : 0;

        public double ErrorRate => TotalEvaluations > 0 
            ? (double)ErrorCount / TotalEvaluations * 100 
            : 0;

        public void ResetCounters()
        {
            TotalEvaluations = 0;
            AllowCount = 0;
            SuppressCount = 0;
            ModifyCount = 0;
            DeferCount = 0;
            ErrorCount = 0;
            TotalProcessingTime = TimeSpan.Zero;
            MaxProcessingTime = TimeSpan.Zero;
        }
    }

    /// <summary>
    /// Comprehensive metrics for the filter manager.
    /// </summary>
    public sealed class FilterManagerMetrics
    {
        public int TotalFilters { get; set; }
        public int EnabledFilters { get; set; }
        public int HealthyFilters { get; set; }
        public List<FilterPerformanceData> FilterPerformanceData { get; set; } = new List<FilterPerformanceData>();
        public DateTime LastUpdated { get; set; }

        public double HealthRate => TotalFilters > 0 
            ? (double)HealthyFilters / TotalFilters * 100 
            : 0;
    }


    #endregion
}