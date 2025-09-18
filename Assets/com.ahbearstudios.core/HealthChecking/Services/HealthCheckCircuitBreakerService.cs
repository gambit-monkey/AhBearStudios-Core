using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Unity.Collections;
using ZLinq;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging;

namespace AhBearStudios.Core.HealthChecking.Services
{
    /// <summary>
    /// Production implementation of health check circuit breaker service.
    /// Manages circuit breaker states for health check operations with thread-safe operations.
    /// Uses ZLinq for zero-allocation operations and follows CLAUDE.md patterns.
    /// </summary>
    public sealed class HealthCheckCircuitBreakerService : IHealthCheckCircuitBreakerService
    {
        private readonly ILoggingService _logger;
        private readonly Guid _serviceId;

        // Thread-safe collections for circuit breaker state
        private readonly ConcurrentDictionary<FixedString64Bytes, ICircuitBreaker> _circuitBreakers;
        private readonly ConcurrentDictionary<FixedString64Bytes, CircuitBreakerStatistics> _statistics;
        private readonly object _statsLock = new object();

        /// <summary>
        /// Event raised when a circuit breaker state changes.
        /// </summary>
        public event EventHandler<CircuitBreakerStateChangedEventArgs> CircuitBreakerStateChanged;

        /// <summary>
        /// Initializes a new instance of the HealthCheckCircuitBreakerService.
        /// </summary>
        /// <param name="logger">Logging service for circuit breaker operations</param>
        public HealthCheckCircuitBreakerService(ILoggingService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _serviceId = DeterministicIdGenerator.GenerateCoreId("HealthCheckCircuitBreakerService");
            _circuitBreakers = new ConcurrentDictionary<FixedString64Bytes, ICircuitBreaker>();
            _statistics = new ConcurrentDictionary<FixedString64Bytes, CircuitBreakerStatistics>();

            _logger.LogDebug("HealthCheckCircuitBreakerService initialized with ID: {ServiceId}", _serviceId);
        }

        /// <inheritdoc />
        public CircuitBreakerState GetCircuitBreakerState(FixedString64Bytes operationName)
        {
            if (operationName.IsEmpty)
                return CircuitBreakerState.Closed;

            if (_circuitBreakers.TryGetValue(operationName, out var circuitBreaker))
            {
                return circuitBreaker.State;
            }

            return CircuitBreakerState.Closed; // Default to closed if not registered
        }

        /// <inheritdoc />
        public Dictionary<FixedString64Bytes, CircuitBreakerState> GetAllCircuitBreakerStates()
        {
            return _circuitBreakers.AsValueEnumerable()
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.State);
        }

        /// <inheritdoc />
        public void RecordSuccess(FixedString64Bytes operationName, TimeSpan executionTime)
        {
            if (operationName.IsEmpty)
                return;

            if (_circuitBreakers.TryGetValue(operationName, out var circuitBreaker))
            {
                var oldState = circuitBreaker.State;
                circuitBreaker.RecordSuccess();
                var newState = circuitBreaker.State;

                UpdateStatistics(operationName, success: true, executionTime);

                if (oldState != newState)
                {
                    OnCircuitBreakerStateChanged(operationName, oldState, newState, "Success threshold reached");
                }

                _logger.LogDebug("Recorded success for {Operation}, state: {State}", operationName, newState);
            }
        }

        /// <inheritdoc />
        public void RecordFailure(FixedString64Bytes operationName, Exception exception, TimeSpan executionTime)
        {
            if (operationName.IsEmpty)
                return;

            if (_circuitBreakers.TryGetValue(operationName, out var circuitBreaker))
            {
                var oldState = circuitBreaker.State;
                circuitBreaker.RecordFailure(exception);
                var newState = circuitBreaker.State;

                UpdateStatistics(operationName, success: false, executionTime);

                if (oldState != newState)
                {
                    OnCircuitBreakerStateChanged(operationName, oldState, newState, $"Failure threshold exceeded: {exception?.GetType().Name}");
                }

                _logger.LogWarning("Recorded failure for {Operation}, state: {State}, exception: {Exception}",
                    operationName, newState, exception?.Message);
            }
        }

        /// <inheritdoc />
        public void ForceCircuitBreakerOpen(FixedString64Bytes operationName, string reason)
        {
            if (operationName.IsEmpty)
                return;

            if (_circuitBreakers.TryGetValue(operationName, out var circuitBreaker))
            {
                var oldState = circuitBreaker.State;
                circuitBreaker.ForceOpen();
                var newState = circuitBreaker.State;

                if (oldState != newState)
                {
                    OnCircuitBreakerStateChanged(operationName, oldState, newState, $"Forced open: {reason}");
                }

                _logger.LogWarning("Forced circuit breaker open for {Operation}: {Reason}", operationName, reason);
            }
        }

        /// <inheritdoc />
        public void ForceCircuitBreakerClosed(FixedString64Bytes operationName, string reason)
        {
            if (operationName.IsEmpty)
                return;

            if (_circuitBreakers.TryGetValue(operationName, out var circuitBreaker))
            {
                var oldState = circuitBreaker.State;
                circuitBreaker.ForceClosed();
                var newState = circuitBreaker.State;

                if (oldState != newState)
                {
                    OnCircuitBreakerStateChanged(operationName, oldState, newState, $"Forced closed: {reason}");
                }

                _logger.LogInfo("Forced circuit breaker closed for {Operation}: {Reason}", operationName, reason);
            }
        }

        /// <inheritdoc />
        public bool CanExecuteOperation(FixedString64Bytes operationName)
        {
            if (operationName.IsEmpty)
                return true;

            if (_circuitBreakers.TryGetValue(operationName, out var circuitBreaker))
            {
                return circuitBreaker.CanExecute();
            }

            return true; // Default to allowing execution if not registered
        }

        /// <inheritdoc />
        public void RegisterCircuitBreaker(
            FixedString64Bytes operationName,
            int failureThreshold,
            TimeSpan timeout,
            int successThreshold = 1)
        {
            if (operationName.IsEmpty)
                throw new ArgumentException("Operation name cannot be empty", nameof(operationName));

            if (failureThreshold < 1)
                throw new ArgumentOutOfRangeException(nameof(failureThreshold), "Must be at least 1");

            if (timeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeout), "Must be greater than zero");

            if (successThreshold < 1)
                throw new ArgumentOutOfRangeException(nameof(successThreshold), "Must be at least 1");

            var circuitBreaker = new CircuitBreaker(
                operationName.ToString(),
                failureThreshold,
                timeout,
                successThreshold);

            if (_circuitBreakers.TryAdd(operationName, circuitBreaker))
            {
                InitializeStatistics(operationName);
                _logger.LogInfo("Registered circuit breaker for {Operation} with failure threshold {FailureThreshold}",
                    operationName, failureThreshold);
            }
            else
            {
                throw new InvalidOperationException($"Circuit breaker for operation '{operationName}' is already registered");
            }
        }

        /// <inheritdoc />
        public bool UnregisterCircuitBreaker(FixedString64Bytes operationName)
        {
            if (operationName.IsEmpty)
                return false;

            var removed = _circuitBreakers.TryRemove(operationName, out var circuitBreaker);

            if (removed)
            {
                _statistics.TryRemove(operationName, out _);
                circuitBreaker?.Dispose();

                _logger.LogDebug("Unregistered circuit breaker for {Operation}", operationName);
            }

            return removed;
        }

        /// <inheritdoc />
        public CircuitBreakerStatistics GetCircuitBreakerStatistics(FixedString64Bytes operationName)
        {
            return _statistics.TryGetValue(operationName, out var stats)
                ? stats
                : new CircuitBreakerStatistics();
        }

        /// <inheritdoc />
        public Dictionary<FixedString64Bytes, CircuitBreakerStatistics> GetAllCircuitBreakerStatistics()
        {
            return new Dictionary<FixedString64Bytes, CircuitBreakerStatistics>(_statistics);
        }

        /// <inheritdoc />
        public void ResetCircuitBreakerStatistics(FixedString64Bytes operationName)
        {
            if (!operationName.IsEmpty && _statistics.ContainsKey(operationName))
            {
                InitializeStatistics(operationName);
                _logger.LogDebug("Reset statistics for circuit breaker {Operation}", operationName);
            }
        }

        /// <inheritdoc />
        public void ResetAllCircuitBreakerStatistics()
        {
            var operationNames = _statistics.Keys.AsValueEnumerable().ToArray();

            foreach (var operationName in operationNames)
            {
                InitializeStatistics(operationName);
            }

            _logger.LogInfo("Reset statistics for all {Count} circuit breakers", operationNames.Length);
        }

        /// <inheritdoc />
        public bool UpdateCircuitBreakerConfiguration(
            FixedString64Bytes operationName,
            int failureThreshold,
            TimeSpan timeout,
            int successThreshold)
        {
            if (operationName.IsEmpty)
                return false;

            if (_circuitBreakers.TryGetValue(operationName, out var circuitBreaker))
            {
                // For this simplified implementation, we'll need to recreate the circuit breaker
                // In a full implementation, the circuit breaker would support configuration updates
                UnregisterCircuitBreaker(operationName);
                RegisterCircuitBreaker(operationName, failureThreshold, timeout, successThreshold);

                _logger.LogInfo("Updated configuration for circuit breaker {Operation}", operationName);
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public int GetCircuitBreakerCount()
        {
            return _circuitBreakers.Count;
        }

        /// <inheritdoc />
        public int GetOpenCircuitBreakerCount()
        {
            return _circuitBreakers.Values
                .AsValueEnumerable()
                .Count(cb => cb.State == CircuitBreakerState.Open);
        }

        private void InitializeStatistics(FixedString64Bytes operationName)
        {
            var stats = new CircuitBreakerStatistics
            {
                OperationName = operationName.ToString(),
                TotalRequests = 0,
                SuccessfulRequests = 0,
                FailedRequests = 0,
                CircuitOpenCount = 0,
                LastStateChange = DateTime.UtcNow,
                AverageExecutionTime = TimeSpan.Zero,
                CreatedAt = DateTime.UtcNow
            };

            _statistics.AddOrUpdate(operationName, stats, (key, existing) => stats);
        }

        private void UpdateStatistics(FixedString64Bytes operationName, bool success, TimeSpan executionTime)
        {
            lock (_statsLock)
            {
                if (_statistics.TryGetValue(operationName, out var stats))
                {
                    stats.TotalRequests++;

                    if (success)
                        stats.SuccessfulRequests++;
                    else
                        stats.FailedRequests++;

                    // Update average execution time
                    var totalTime = stats.AverageExecutionTime.TotalMilliseconds * (stats.TotalRequests - 1) + executionTime.TotalMilliseconds;
                    stats.AverageExecutionTime = TimeSpan.FromMilliseconds(totalTime / stats.TotalRequests);

                    _statistics.TryUpdate(operationName, stats, stats);
                }
            }
        }

        private void OnCircuitBreakerStateChanged(
            FixedString64Bytes operationName,
            CircuitBreakerState oldState,
            CircuitBreakerState newState,
            string reason)
        {
            // Update statistics
            if (newState == CircuitBreakerState.Open && _statistics.TryGetValue(operationName, out var stats))
            {
                stats.CircuitOpenCount++;
                stats.LastStateChange = DateTime.UtcNow;
                _statistics.TryUpdate(operationName, stats, stats);
            }

            // Raise event
            try
            {
                CircuitBreakerStateChanged?.Invoke(this,
                    new CircuitBreakerStateChangedEventArgs(operationName, oldState, newState, reason));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error raising CircuitBreakerStateChanged event for {Operation}", operationName);
            }
        }
    }
}