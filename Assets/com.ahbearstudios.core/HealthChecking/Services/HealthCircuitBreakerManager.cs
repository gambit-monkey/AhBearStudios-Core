using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

using Unity.Collections;
using Unity.Profiling;
using ZLinq;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging;

namespace AhBearStudios.Core.HealthChecking.Services
{
    /// <summary>
    /// Production implementation of circuit breaker management for health checks.
    /// Provides automatic failure protection, state transitions, and recovery coordination.
    /// </summary>
    public sealed class HealthCircuitBreakerManager : IHealthCircuitBreakerManager
    {
        private readonly ILoggingService _logger;
        private readonly IMessageBusService _messageBus;
        private readonly Guid _managerId;
        private readonly DateTime _startTime;
        private readonly object _stateLock = new();

        // Profiler markers
        private readonly ProfilerMarker _recordResultMarker = new("HealthCircuitBreakerManager.RecordResult");
        private readonly ProfilerMarker _stateTransitionMarker = new("HealthCircuitBreakerManager.StateTransition");
        private readonly ProfilerMarker _recoveryCheckMarker = new("HealthCircuitBreakerManager.RecoveryCheck");

        // Circuit breaker storage
        private readonly ConcurrentDictionary<FixedString64Bytes, CircuitBreakerData> _circuitBreakers;
        private readonly ConcurrentDictionary<FixedString64Bytes, CircuitBreakerStatistics> _statistics;

        // System state
        private bool _isActive = true;
        private bool _disposed;

        // System statistics
        private long _totalTrips;
        private long _totalRecoveries;
        private long _failedRecoveries;

        /// <summary>
        /// Event triggered when a circuit breaker state changes.
        /// </summary>
        public event EventHandler<CircuitBreakerStateChangedEventArgs> StateChanged;

        /// <summary>
        /// Event triggered when a circuit breaker trip occurs.
        /// </summary>
        public event EventHandler<CircuitBreakerTripEventArgs> CircuitBreakerTripped;

        /// <summary>
        /// Event triggered when a circuit breaker recovery attempt is made.
        /// </summary>
        public event EventHandler<CircuitBreakerRecoveryEventArgs> RecoveryAttempted;

        /// <summary>
        /// Gets whether the circuit breaker manager is active.
        /// </summary>
        public bool IsActive
        {
            get
            {
                lock (_stateLock)
                {
                    return _isActive;
                }
            }
        }

        /// <summary>
        /// Gets the total number of registered circuit breakers.
        /// </summary>
        public int RegisteredCircuitBreakers => _circuitBreakers.Count;

        /// <summary>
        /// Initializes a new circuit breaker manager.
        /// </summary>
        /// <param name="logger">Logging service</param>
        /// <param name="messageBus">Message bus for events</param>
        /// <exception cref="ArgumentNullException">Thrown when required dependencies are null</exception>
        public HealthCircuitBreakerManager(ILoggingService logger, IMessageBusService messageBus)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));

            _managerId = DeterministicIdGenerator.GenerateHealthCheckId("CircuitBreakerManager", Environment.MachineName);
            _startTime = DateTime.UtcNow;
            _circuitBreakers = new ConcurrentDictionary<FixedString64Bytes, CircuitBreakerData>();
            _statistics = new ConcurrentDictionary<FixedString64Bytes, CircuitBreakerStatistics>();

            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("CircuitBreakerManagerInit", _managerId.ToString());
            _logger.LogInfo("HealthCircuitBreakerManager initialized", correlationId);
        }

        /// <summary>
        /// Registers a circuit breaker for a health check.
        /// </summary>
        public bool RegisterCircuitBreaker(FixedString64Bytes healthCheckName, int failureThreshold, TimeSpan recoveryTimeout, int halfOpenTestCount = 1)
        {
            ThrowIfDisposed();

            if (healthCheckName.IsEmpty)
                throw new ArgumentException("Health check name cannot be empty", nameof(healthCheckName));
            if (failureThreshold <= 0)
                throw new ArgumentOutOfRangeException(nameof(failureThreshold), "Failure threshold must be greater than zero");
            if (recoveryTimeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(recoveryTimeout), "Recovery timeout must be greater than zero");
            if (halfOpenTestCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(halfOpenTestCount), "Half-open test count must be greater than zero");

            var circuitBreakerData = new CircuitBreakerData
            {
                HealthCheckName = healthCheckName,
                FailureThreshold = failureThreshold,
                RecoveryTimeout = recoveryTimeout,
                HalfOpenTestCount = halfOpenTestCount,
                State = CircuitBreakerState.Closed,
                ConsecutiveFailures = 0,
                LastFailureTime = DateTime.MinValue,
                LastSuccessTime = DateTime.UtcNow,
                HalfOpenTests = 0,
                HalfOpenSuccesses = 0,
                CreatedAt = DateTime.UtcNow
            };

            var statistics = new CircuitBreakerStatistics
            {
                HealthCheckName = healthCheckName,
                State = CircuitBreakerState.Closed,
                FailureThreshold = failureThreshold,
                RecoveryTimeout = recoveryTimeout,
                HalfOpenTestCount = halfOpenTestCount,
                TotalExecutions = 0,
                SuccessfulExecutions = 0,
                FailedExecutions = 0,
                TotalTrips = 0,
                TotalRecoveries = 0,
                FailedRecoveries = 0,
                AverageRecoveryTime = TimeSpan.Zero,
                LastTrip = DateTime.MinValue,
                LastRecovery = DateTime.MinValue,
                TimeInCurrentState = TimeSpan.Zero,
                CreatedAt = DateTime.UtcNow
            };

            var added = _circuitBreakers.TryAdd(healthCheckName, circuitBreakerData);
            if (added)
            {
                _statistics.TryAdd(healthCheckName, statistics);
                
                var correlationId = DeterministicIdGenerator.GenerateCorrelationId("CircuitBreakerRegistered", healthCheckName.ToString());
                _logger.LogInfo($"Circuit breaker registered for {healthCheckName} with threshold {failureThreshold}", correlationId);
            }

            return added;
        }

        /// <summary>
        /// Unregisters a circuit breaker.
        /// </summary>
        public bool UnregisterCircuitBreaker(FixedString64Bytes healthCheckName)
        {
            ThrowIfDisposed();

            var removed = _circuitBreakers.TryRemove(healthCheckName, out var circuitBreakerData);
            if (removed)
            {
                _statistics.TryRemove(healthCheckName, out _);
                
                var correlationId = DeterministicIdGenerator.GenerateCorrelationId("CircuitBreakerUnregistered", healthCheckName.ToString());
                _logger.LogInfo($"Circuit breaker unregistered for {healthCheckName}", correlationId);
            }

            return removed;
        }

        /// <summary>
        /// Records a health check result and updates circuit breaker state.
        /// </summary>
        public bool RecordHealthCheckResult(HealthCheckResult result)
        {
            ThrowIfDisposed();

            if (!_isActive)
                return true; // Allow all executions when disabled

            if (!_circuitBreakers.TryGetValue(result.Name, out var circuitBreakerData))
                return true; // No circuit breaker registered

            using (_recordResultMarker.Auto())
            {
                lock (circuitBreakerData)
                {
                    return ProcessHealthCheckResult(circuitBreakerData, result);
                }
            }
        }

        /// <summary>
        /// Checks if a health check execution is allowed by the circuit breaker.
        /// </summary>
        public bool IsExecutionAllowed(FixedString64Bytes healthCheckName)
        {
            ThrowIfDisposed();

            if (!_isActive)
                return true;

            if (!_circuitBreakers.TryGetValue(healthCheckName, out var circuitBreakerData))
                return true;

            lock (circuitBreakerData)
            {
                return GetExecutionAllowed(circuitBreakerData);
            }
        }

        /// <summary>
        /// Gets the current state of a circuit breaker.
        /// </summary>
        public CircuitBreakerState? GetCircuitBreakerState(FixedString64Bytes healthCheckName)
        {
            ThrowIfDisposed();

            if (!_circuitBreakers.TryGetValue(healthCheckName, out var circuitBreakerData))
                return null;

            lock (circuitBreakerData)
            {
                return circuitBreakerData.State;
            }
        }

        /// <summary>
        /// Manually sets the state of a circuit breaker.
        /// </summary>
        public bool SetCircuitBreakerState(FixedString64Bytes healthCheckName, CircuitBreakerState newState, string reason)
        {
            ThrowIfDisposed();

            if (!_circuitBreakers.TryGetValue(healthCheckName, out var circuitBreakerData))
                return false;

            lock (circuitBreakerData)
            {
                if (circuitBreakerData.State == newState)
                    return false;

                var oldState = circuitBreakerData.State;
                TransitionToState(circuitBreakerData, newState, reason ?? "Manual state change", false);
                
                var correlationId = DeterministicIdGenerator.GenerateCorrelationId("ManualStateChange", healthCheckName.ToString());
                _logger.LogInfo($"Circuit breaker {healthCheckName} manually changed from {oldState} to {newState}: {reason}", correlationId);
                
                return true;
            }
        }

        /// <summary>
        /// Forces all circuit breakers to reset to closed state.
        /// </summary>
        public void ResetAllCircuitBreakers(string reason = null)
        {
            ThrowIfDisposed();

            var resetReason = reason ?? "System reset";
            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("CircuitBreakerReset", _managerId.ToString());
            
            var resetCount = 0;
            foreach (var kvp in _circuitBreakers)
            {
                lock (kvp.Value)
                {
                    if (kvp.Value.State != CircuitBreakerState.Closed)
                    {
                        TransitionToState(kvp.Value, CircuitBreakerState.Closed, resetReason, false);
                        kvp.Value.ConsecutiveFailures = 0;
                        kvp.Value.HalfOpenTests = 0;
                        kvp.Value.HalfOpenSuccesses = 0;
                        resetCount++;
                    }
                }
            }

            _logger.LogInfo($"Reset {resetCount} circuit breakers: {resetReason}", correlationId);
        }

        /// <summary>
        /// Gets statistics for a specific circuit breaker.
        /// </summary>
        public CircuitBreakerStatistics GetCircuitBreakerStatistics(FixedString64Bytes healthCheckName)
        {
            ThrowIfDisposed();

            if (!_statistics.TryGetValue(healthCheckName, out var stats))
                return null;

            if (!_circuitBreakers.TryGetValue(healthCheckName, out var circuitBreakerData))
                return stats;

            lock (circuitBreakerData)
            {
                return stats with
                {
                    State = circuitBreakerData.State,
                    TimeInCurrentState = DateTime.UtcNow - circuitBreakerData.StateChangedAt,
                    ConsecutiveFailures = circuitBreakerData.ConsecutiveFailures,
                    LastFailureMessage = circuitBreakerData.LastFailureMessage
                };
            }
        }

        /// <summary>
        /// Gets statistics for all registered circuit breakers.
        /// </summary>
        public IReadOnlyDictionary<FixedString64Bytes, CircuitBreakerStatistics> GetAllCircuitBreakerStatistics()
        {
            ThrowIfDisposed();

            var result = new Dictionary<FixedString64Bytes, CircuitBreakerStatistics>();
            
            foreach (var kvp in _statistics)
            {
                var stats = GetCircuitBreakerStatistics(kvp.Key);
                if (stats != null)
                {
                    result[kvp.Key] = stats;
                }
            }

            return result.AsReadOnly();
        }

        /// <summary>
        /// Gets circuit breakers in a specific state.
        /// </summary>
        public IEnumerable<FixedString64Bytes> GetCircuitBreakersInState(CircuitBreakerState state)
        {
            ThrowIfDisposed();

            return _circuitBreakers.AsValueEnumerable()
                .Where(kvp => 
                {
                    lock (kvp.Value)
                    {
                        return kvp.Value.State == state;
                    }
                })
                .Select(kvp => kvp.Key)
                .ToList();
        }

        /// <summary>
        /// Updates configuration for an existing circuit breaker.
        /// </summary>
        public bool UpdateCircuitBreakerConfiguration(FixedString64Bytes healthCheckName, int failureThreshold, TimeSpan recoveryTimeout, int halfOpenTestCount)
        {
            ThrowIfDisposed();

            if (!_circuitBreakers.TryGetValue(healthCheckName, out var circuitBreakerData))
                return false;

            if (failureThreshold <= 0 || recoveryTimeout <= TimeSpan.Zero || halfOpenTestCount <= 0)
                throw new ArgumentException("All configuration values must be positive");

            lock (circuitBreakerData)
            {
                circuitBreakerData.FailureThreshold = failureThreshold;
                circuitBreakerData.RecoveryTimeout = recoveryTimeout;
                circuitBreakerData.HalfOpenTestCount = halfOpenTestCount;
            }

            // Update statistics
            if (_statistics.TryGetValue(healthCheckName, out var stats))
            {
                _statistics.TryUpdate(healthCheckName, stats with
                {
                    FailureThreshold = failureThreshold,
                    RecoveryTimeout = recoveryTimeout,
                    HalfOpenTestCount = halfOpenTestCount
                }, stats);
            }

            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("CircuitBreakerConfigUpdate", healthCheckName.ToString());
            _logger.LogInfo($"Updated circuit breaker configuration for {healthCheckName}", correlationId);

            return true;
        }

        /// <summary>
        /// Enables or disables the circuit breaker manager.
        /// </summary>
        public void SetEnabled(bool enabled, string reason = null)
        {
            ThrowIfDisposed();

            lock (_stateLock)
            {
                if (_isActive == enabled)
                    return;

                _isActive = enabled;
            }

            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("CircuitBreakerManagerToggle", _managerId.ToString());
            var status = enabled ? "enabled" : "disabled";
            var changeReason = reason ?? $"Circuit breaker manager {status}";
            
            _logger.LogInfo($"Circuit breaker manager {status}: {changeReason}", correlationId);
        }

        /// <summary>
        /// Gets comprehensive circuit breaker system statistics.
        /// </summary>
        public CircuitBreakerSystemStatistics GetSystemStatistics()
        {
            ThrowIfDisposed();

            var closedCount = 0;
            var openCount = 0;
            var halfOpenCount = 0;
            var mostTrippedName = new FixedString64Bytes();
            var longestRecoveryName = new FixedString64Bytes();
            var maxTrips = 0L;
            var maxRecoveryTime = TimeSpan.Zero;

            foreach (var kvp in _circuitBreakers)
            {
                lock (kvp.Value)
                {
                    switch (kvp.Value.State)
                    {
                        case CircuitBreakerState.Closed:
                            closedCount++;
                            break;
                        case CircuitBreakerState.Open:
                            openCount++;
                            break;
                        case CircuitBreakerState.HalfOpen:
                            halfOpenCount++;
                            break;
                    }
                }

                if (_statistics.TryGetValue(kvp.Key, out var stats))
                {
                    if (stats.TotalTrips > maxTrips)
                    {
                        maxTrips = stats.TotalTrips;
                        mostTrippedName = kvp.Key;
                    }

                    if (stats.AverageRecoveryTime > maxRecoveryTime)
                    {
                        maxRecoveryTime = stats.AverageRecoveryTime;
                        longestRecoveryName = kvp.Key;
                    }
                }
            }

            return new CircuitBreakerSystemStatistics
            {
                TotalCircuitBreakers = _circuitBreakers.Count,
                ClosedCircuitBreakers = closedCount,
                OpenCircuitBreakers = openCount,
                HalfOpenCircuitBreakers = halfOpenCount,
                TotalTrips = _totalTrips,
                TotalRecoveries = _totalRecoveries,
                FailedRecoveries = _failedRecoveries,
                IsEnabled = _isActive,
                StartTime = _startTime,
                Uptime = DateTime.UtcNow - _startTime,
                MostTrippedCircuitBreaker = mostTrippedName,
                LongestRecoveryCircuitBreaker = longestRecoveryName
            };
        }

        private bool ProcessHealthCheckResult(CircuitBreakerData circuitBreakerData, HealthCheckResult result)
        {
            using (_recordResultMarker.Auto())
            {
                UpdateStatistics(circuitBreakerData.HealthCheckName, result);

                switch (circuitBreakerData.State)
                {
                    case CircuitBreakerState.Closed:
                        return ProcessClosedState(circuitBreakerData, result);

                    case CircuitBreakerState.Open:
                        return ProcessOpenState(circuitBreakerData, result);

                    case CircuitBreakerState.HalfOpen:
                        return ProcessHalfOpenState(circuitBreakerData, result);

                    default:
                        return true;
                }
            }
        }

        private bool ProcessClosedState(CircuitBreakerData circuitBreakerData, HealthCheckResult result)
        {
            if (result.Status == HealthStatus.Healthy)
            {
                circuitBreakerData.ConsecutiveFailures = 0;
                circuitBreakerData.LastSuccessTime = DateTime.UtcNow;
                return true;
            }

            circuitBreakerData.ConsecutiveFailures++;
            circuitBreakerData.LastFailureTime = DateTime.UtcNow;
            circuitBreakerData.LastFailureMessage = result.Description;

            if (circuitBreakerData.ConsecutiveFailures >= circuitBreakerData.FailureThreshold)
            {
                TripCircuitBreaker(circuitBreakerData, result);
                return false;
            }

            return true;
        }

        private bool ProcessOpenState(CircuitBreakerData circuitBreakerData, HealthCheckResult result)
        {
            var now = DateTime.UtcNow;
            var timeSinceLastFailure = now - circuitBreakerData.LastFailureTime;

            if (timeSinceLastFailure >= circuitBreakerData.RecoveryTimeout)
            {
                using (_recoveryCheckMarker.Auto())
                {
                    AttemptRecovery(circuitBreakerData, "Recovery timeout reached");
                    return GetExecutionAllowed(circuitBreakerData);
                }
            }

            return false; // Still in open state, reject execution
        }

        private bool ProcessHalfOpenState(CircuitBreakerData circuitBreakerData, HealthCheckResult result)
        {
            circuitBreakerData.HalfOpenTests++;

            if (result.Status == HealthStatus.Healthy)
            {
                circuitBreakerData.HalfOpenSuccesses++;
                circuitBreakerData.LastSuccessTime = DateTime.UtcNow;
                circuitBreakerData.ConsecutiveFailures = 0;

                if (circuitBreakerData.HalfOpenSuccesses >= circuitBreakerData.HalfOpenTestCount)
                {
                    CompleteRecovery(circuitBreakerData, "All half-open tests passed");
                }
            }
            else
            {
                circuitBreakerData.LastFailureTime = DateTime.UtcNow;
                circuitBreakerData.LastFailureMessage = result.Description;
                FailRecovery(circuitBreakerData, "Half-open test failed");
                return false;
            }

            return true;
        }

        private void TripCircuitBreaker(CircuitBreakerData circuitBreakerData, HealthCheckResult result)
        {
            using (_stateTransitionMarker.Auto())
            {
                var correlationId = DeterministicIdGenerator.GenerateCorrelationId("CircuitBreakerTrip", circuitBreakerData.HealthCheckName.ToString());
                
                TransitionToState(circuitBreakerData, CircuitBreakerState.Open, "Failure threshold exceeded", true);
                System.Threading.Interlocked.Increment(ref _totalTrips);

                UpdateTripStatistics(circuitBreakerData.HealthCheckName);

                var tripEventArgs = new CircuitBreakerTripEventArgs
                {
                    HealthCheckName = circuitBreakerData.HealthCheckName,
                    FailureThreshold = circuitBreakerData.FailureThreshold,
                    ConsecutiveFailures = circuitBreakerData.ConsecutiveFailures,
                    LastFailureMessage = result.Description,
                    CorrelationId = correlationId,
                    RecoveryTimeout = circuitBreakerData.RecoveryTimeout
                };

                CircuitBreakerTripped?.Invoke(this, tripEventArgs);
                _logger.LogWarning($"Circuit breaker tripped for {circuitBreakerData.HealthCheckName} after {circuitBreakerData.ConsecutiveFailures} failures", correlationId);
            }
        }

        private void AttemptRecovery(CircuitBreakerData circuitBreakerData, string reason)
        {
            TransitionToState(circuitBreakerData, CircuitBreakerState.HalfOpen, reason, true);
            circuitBreakerData.HalfOpenTests = 0;
            circuitBreakerData.HalfOpenSuccesses = 0;
            
            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("RecoveryAttempt", circuitBreakerData.HealthCheckName.ToString());
            _logger.LogInfo($"Circuit breaker {circuitBreakerData.HealthCheckName} attempting recovery: {reason}", correlationId);
        }

        private void CompleteRecovery(CircuitBreakerData circuitBreakerData, string reason)
        {
            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("RecoveryComplete", circuitBreakerData.HealthCheckName.ToString());
            
            TransitionToState(circuitBreakerData, CircuitBreakerState.Closed, reason, true);
            System.Threading.Interlocked.Increment(ref _totalRecoveries);

            UpdateRecoveryStatistics(circuitBreakerData.HealthCheckName, true);

            var recoveryEventArgs = new CircuitBreakerRecoveryEventArgs
            {
                HealthCheckName = circuitBreakerData.HealthCheckName,
                IsSuccessful = true,
                NewState = CircuitBreakerState.Closed,
                TestsPerformed = circuitBreakerData.HalfOpenTests,
                SuccessfulTests = circuitBreakerData.HalfOpenSuccesses,
                CorrelationId = correlationId,
                Context = reason
            };

            RecoveryAttempted?.Invoke(this, recoveryEventArgs);
            _logger.LogInfo($"Circuit breaker {circuitBreakerData.HealthCheckName} successfully recovered: {reason}", correlationId);
        }

        private void FailRecovery(CircuitBreakerData circuitBreakerData, string reason)
        {
            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("RecoveryFailed", circuitBreakerData.HealthCheckName.ToString());
            
            TransitionToState(circuitBreakerData, CircuitBreakerState.Open, reason, true);
            System.Threading.Interlocked.Increment(ref _failedRecoveries);

            UpdateRecoveryStatistics(circuitBreakerData.HealthCheckName, false);

            var recoveryEventArgs = new CircuitBreakerRecoveryEventArgs
            {
                HealthCheckName = circuitBreakerData.HealthCheckName,
                IsSuccessful = false,
                NewState = CircuitBreakerState.Open,
                TestsPerformed = circuitBreakerData.HalfOpenTests,
                SuccessfulTests = circuitBreakerData.HalfOpenSuccesses,
                CorrelationId = correlationId,
                Context = reason
            };

            RecoveryAttempted?.Invoke(this, recoveryEventArgs);
            _logger.LogWarning($"Circuit breaker {circuitBreakerData.HealthCheckName} recovery failed: {reason}", correlationId);
        }

        private void TransitionToState(CircuitBreakerData circuitBreakerData, CircuitBreakerState newState, string reason, bool isAutomatic)
        {
            var oldState = circuitBreakerData.State;
            if (oldState == newState)
                return;

            circuitBreakerData.State = newState;
            circuitBreakerData.StateChangedAt = DateTime.UtcNow;

            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("StateTransition", circuitBreakerData.HealthCheckName.ToString());

            var stateChangeEventArgs = new CircuitBreakerStateChangedEventArgs
            {
                HealthCheckName = circuitBreakerData.HealthCheckName,
                OldState = oldState,
                NewState = newState,
                Reason = reason,
                CorrelationId = correlationId,
                FailureCount = circuitBreakerData.ConsecutiveFailures,
                IsAutomatic = isAutomatic
            };

            StateChanged?.Invoke(this, stateChangeEventArgs);
        }

        private bool GetExecutionAllowed(CircuitBreakerData circuitBreakerData)
        {
            return circuitBreakerData.State switch
            {
                CircuitBreakerState.Closed => true,
                CircuitBreakerState.Open => false,
                CircuitBreakerState.HalfOpen => true,
                _ => true
            };
        }

        private void UpdateStatistics(FixedString64Bytes healthCheckName, HealthCheckResult result)
        {
            if (!_statistics.TryGetValue(healthCheckName, out var currentStats))
                return;

            var updatedStats = currentStats with
            {
                TotalExecutions = currentStats.TotalExecutions + 1,
                SuccessfulExecutions = result.Status == HealthStatus.Healthy 
                    ? currentStats.SuccessfulExecutions + 1 
                    : currentStats.SuccessfulExecutions,
                FailedExecutions = result.Status != HealthStatus.Healthy
                    ? currentStats.FailedExecutions + 1
                    : currentStats.FailedExecutions,
                LastExecution = DateTime.UtcNow,
                LastFailureMessage = result.Status != HealthStatus.Healthy ? result.Description : currentStats.LastFailureMessage
            };

            _statistics.TryUpdate(healthCheckName, updatedStats, currentStats);
        }

        private void UpdateTripStatistics(FixedString64Bytes healthCheckName)
        {
            if (!_statistics.TryGetValue(healthCheckName, out var currentStats))
                return;

            var updatedStats = currentStats with
            {
                TotalTrips = currentStats.TotalTrips + 1,
                LastTrip = DateTime.UtcNow
            };

            _statistics.TryUpdate(healthCheckName, updatedStats, currentStats);
        }

        private void UpdateRecoveryStatistics(FixedString64Bytes healthCheckName, bool successful)
        {
            if (!_statistics.TryGetValue(healthCheckName, out var currentStats))
                return;

            var recoveryTime = DateTime.UtcNow - currentStats.LastTrip;
            var newAverageRecovery = currentStats.TotalRecoveries > 0
                ? TimeSpan.FromTicks((currentStats.AverageRecoveryTime.Ticks * currentStats.TotalRecoveries + recoveryTime.Ticks) / (currentStats.TotalRecoveries + 1))
                : recoveryTime;

            var updatedStats = currentStats with
            {
                TotalRecoveries = successful ? currentStats.TotalRecoveries + 1 : currentStats.TotalRecoveries,
                FailedRecoveries = !successful ? currentStats.FailedRecoveries + 1 : currentStats.FailedRecoveries,
                AverageRecoveryTime = successful ? newAverageRecovery : currentStats.AverageRecoveryTime,
                LastRecovery = successful ? DateTime.UtcNow : currentStats.LastRecovery
            };

            _statistics.TryUpdate(healthCheckName, updatedStats, currentStats);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(HealthCircuitBreakerManager));
        }

        /// <summary>
        /// Disposes the circuit breaker manager.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("CircuitBreakerManagerDispose", _managerId.ToString());
            _logger.LogInfo($"HealthCircuitBreakerManager disposed with {_circuitBreakers.Count} registered circuit breakers", correlationId);

            _circuitBreakers.Clear();
            _statistics.Clear();
        }

        /// <summary>
        /// Internal circuit breaker data structure.
        /// </summary>
        private sealed class CircuitBreakerData
        {
            public FixedString64Bytes HealthCheckName { get; set; }
            public int FailureThreshold { get; set; }
            public TimeSpan RecoveryTimeout { get; set; }
            public int HalfOpenTestCount { get; set; }
            public CircuitBreakerState State { get; set; }
            public int ConsecutiveFailures { get; set; }
            public DateTime LastFailureTime { get; set; }
            public DateTime LastSuccessTime { get; set; }
            public DateTime StateChangedAt { get; set; }
            public string LastFailureMessage { get; set; }
            public int HalfOpenTests { get; set; }
            public int HalfOpenSuccesses { get; set; }
            public DateTime CreatedAt { get; set; }
        }
    }
}