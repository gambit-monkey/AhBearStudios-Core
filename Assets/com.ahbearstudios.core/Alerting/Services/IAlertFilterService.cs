using System;
using System.Collections.Generic;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Alerting.Filters;

namespace AhBearStudios.Core.Alerting.Services
{
    /// <summary>
    /// Defines the contract for managing alert filter lifecycle and execution orchestration.
    /// Handles filter registration, priority ordering, performance monitoring, and filter chain execution.
    /// Designed for Unity game development with zero-allocation patterns and high performance.
    /// </summary>
    public interface IAlertFilterService : IDisposable
    {
        /// <summary>
        /// Gets whether the filter service is enabled and operational.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Gets the count of registered filters.
        /// </summary>
        int FilterCount { get; }

        /// <summary>
        /// Gets the count of enabled filters.
        /// </summary>
        int EnabledFilterCount { get; }

        /// <summary>
        /// Registers an alert filter with the service.
        /// </summary>
        /// <param name="filter">Filter to register</param>
        /// <param name="configuration">Optional initial configuration</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>True if filter was registered successfully</returns>
        bool RegisterFilter(IAlertFilter filter, Dictionary<string, object> configuration = null, Guid correlationId = default);

        /// <summary>
        /// Unregisters an alert filter from the service.
        /// </summary>
        /// <param name="filterName">Name of filter to unregister</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>True if filter was unregistered successfully</returns>
        bool UnregisterFilter(string filterName, Guid correlationId = default);

        /// <summary>
        /// Gets a registered filter by name.
        /// </summary>
        /// <param name="filterName">Name of filter to retrieve</param>
        /// <returns>Filter instance or null if not found</returns>
        IAlertFilter GetFilter(string filterName);

        /// <summary>
        /// Gets all registered filters in priority order.
        /// </summary>
        /// <returns>Collection of registered filters</returns>
        IReadOnlyCollection<IAlertFilter> GetAllFilters();

        /// <summary>
        /// Gets enabled filters only in priority order.
        /// </summary>
        /// <returns>Collection of enabled filters</returns>
        IReadOnlyCollection<IAlertFilter> GetEnabledFilters();

        /// <summary>
        /// Processes an alert through the filter chain.
        /// </summary>
        /// <param name="alert">Alert to process</param>
        /// <param name="context">Filtering context</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>Filter chain result</returns>
        FilterChainResult ProcessAlert(Alert alert, FilterContext context = default, Guid correlationId = default);

        /// <summary>
        /// Processes multiple alerts through the filter chain.
        /// </summary>
        /// <param name="alerts">Alerts to process</param>
        /// <param name="context">Filtering context</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>Collection of filter chain results</returns>
        IEnumerable<FilterChainResult> ProcessAlerts(IEnumerable<Alert> alerts, FilterContext context = default, Guid correlationId = default);

        /// <summary>
        /// Updates configuration for a specific filter.
        /// </summary>
        /// <param name="filterName">Name of filter to configure</param>
        /// <param name="configuration">New configuration</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>True if configuration was applied successfully</returns>
        bool ConfigureFilter(string filterName, Dictionary<string, object> configuration, Guid correlationId = default);

        /// <summary>
        /// Enables a specific filter.
        /// </summary>
        /// <param name="filterName">Name of filter to enable</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>True if filter was enabled</returns>
        bool EnableFilter(string filterName, Guid correlationId = default);

        /// <summary>
        /// Disables a specific filter.
        /// </summary>
        /// <param name="filterName">Name of filter to disable</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>True if filter was disabled</returns>
        bool DisableFilter(string filterName, Guid correlationId = default);

        /// <summary>
        /// Updates the priority of a specific filter and re-sorts the chain.
        /// </summary>
        /// <param name="filterName">Name of filter to update</param>
        /// <param name="newPriority">New priority value</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>True if priority was updated</returns>
        bool UpdateFilterPriority(string filterName, int newPriority, Guid correlationId = default);

        /// <summary>
        /// Gets comprehensive performance metrics for all filters.
        /// </summary>
        /// <returns>Filter service performance metrics</returns>
        FilterManagerMetrics GetPerformanceMetrics();

        /// <summary>
        /// Gets health information for all filters.
        /// </summary>
        /// <returns>Collection of filter health information</returns>
        IReadOnlyCollection<FilterHealth> GetFilterHealthInfo();

        /// <summary>
        /// Gets diagnostics information for a specific filter.
        /// </summary>
        /// <param name="filterName">Name of filter</param>
        /// <returns>Filter diagnostics or null if not found</returns>
        FilterDiagnostics GetFilterDiagnostics(string filterName);

        /// <summary>
        /// Resets performance metrics for all filters.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void ResetPerformanceMetrics(Guid correlationId = default);
    }
}