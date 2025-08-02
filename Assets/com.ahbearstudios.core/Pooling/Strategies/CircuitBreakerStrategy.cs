using System;
using System.Collections.Generic;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Pooling.Strategies.Models;

namespace AhBearStudios.Core.Pooling.Strategies
{
    /// <summary>
    /// Circuit breaker pooling strategy that provides automatic degradation and recovery.
    /// Wraps another strategy and provides resilience against cascading failures.
    /// Essential for production environments where system stability is critical.
    /// </summary>
    public class CircuitBreakerStrategy : IPoolingStrategy
    {
        private readonly IPoolingStrategy _innerStrategy;
        private readonly PoolingStrategyConfig _configuration;
        private readonly PerformanceBudget _performanceBudget;
        
        // Circuit breaker state
        private CircuitBreakerState _state = CircuitBreakerState.Closed;
        private DateTime _lastFailureTime = DateTime.MinValue;
        private DateTime _stateTransitionTime = DateTime.UtcNow;
        private int _consecutiveFailures;
        private int _successfulOperations;
        
        // Performance monitoring
        private readonly List<TimeSpan> _recentOperationTimes = new();
        private readonly List<Exception> _recentErrors = new();
        private readonly object _metricsLock = new object();
        private int _totalOperations;
        private int _totalErrors;
        private int _circuitBreakerActivations;

        /// <summary>
        /// Initializes a new instance of the CircuitBreakerStrategy.
        /// </summary>
        /// <param name="innerStrategy">The strategy to wrap with circuit breaker functionality</param>
        /// <param name="configuration">Circuit breaker configuration (optional)</param>
        public CircuitBreakerStrategy(IPoolingStrategy innerStrategy, PoolingStrategyConfig configuration = null)
        {
            _innerStrategy = innerStrategy ?? throw new ArgumentNullException(nameof(innerStrategy));
            _configuration = configuration ?? CreateCircuitBreakerConfig();
            _performanceBudget = _configuration.PerformanceBudget ?? PerformanceBudget.For60FPS();
        }

        /// <summary>
        /// Gets the name of this strategy.
        /// </summary>
        public string Name => $"CircuitBreaker({_innerStrategy.Name})";

        /// <summary>
        /// Gets the current state of the circuit breaker.
        /// </summary>
        public CircuitBreakerState State => _state;

        /// <summary>
        /// Calculates the target size with circuit breaker protection.
        /// </summary>
        /// <param name="statistics">Current pool statistics</param>
        /// <returns>Target pool size</returns>
        public int CalculateTargetSize(PoolStatistics statistics)
        {
            return ExecuteWithCircuitBreaker(
                () => _innerStrategy.CalculateTargetSize(statistics),
                statistics?.TotalCount ?? 0); // Fallback to current size
        }

        /// <summary>
        /// Determines if the pool should expand with circuit breaker protection.
        /// </summary>
        /// <param name="statistics">Current pool statistics</param>
        /// <returns>True if the pool should expand</returns>
        public bool ShouldExpand(PoolStatistics statistics)
        {
            return ExecuteWithCircuitBreaker(
                () => _innerStrategy.ShouldExpand(statistics),
                false); // Conservative fallback
        }

        /// <summary>
        /// Determines if the pool should contract with circuit breaker protection.
        /// </summary>
        /// <param name="statistics">Current pool statistics</param>
        /// <returns>True if the pool should contract</returns>
        public bool ShouldContract(PoolStatistics statistics)
        {
            return ExecuteWithCircuitBreaker(
                () => _innerStrategy.ShouldContract(statistics),
                false); // Conservative fallback
        }

        /// <summary>
        /// Determines if a new object should be created with circuit breaker protection.
        /// </summary>
        /// <param name="statistics">Current pool statistics</param>
        /// <returns>True if a new object should be created</returns>
        public bool ShouldCreateNew(PoolStatistics statistics)
        {
            return ExecuteWithCircuitBreaker(
                () => _innerStrategy.ShouldCreateNew(statistics),
                statistics?.AvailableCount == 0); // Fallback based on availability
        }

        /// <summary>
        /// Determines if objects should be destroyed with circuit breaker protection.
        /// </summary>
        /// <param name="statistics">Current pool statistics</param>
        /// <returns>True if objects should be destroyed</returns>
        public bool ShouldDestroy(PoolStatistics statistics)
        {
            return ExecuteWithCircuitBreaker(
                () => _innerStrategy.ShouldDestroy(statistics),
                false); // Conservative fallback
        }

        /// <summary>
        /// Gets the validation interval from the inner strategy.
        /// </summary>
        /// <returns>Validation interval</returns>
        public TimeSpan GetValidationInterval()
        {
            return ExecuteWithCircuitBreaker(
                () => _innerStrategy.GetValidationInterval(),
                TimeSpan.FromMinutes(1)); // Fallback interval
        }

        /// <summary>
        /// Validates configuration with circuit breaker protection.
        /// </summary>
        /// <param name="config">Pool configuration to validate</param>
        /// <returns>True if configuration is valid</returns>
        public bool ValidateConfiguration(PoolConfiguration config)
        {
            return ExecuteWithCircuitBreaker(
                () => _innerStrategy.ValidateConfiguration(config),
                config != null); // Basic fallback validation
        }

        /// <summary>
        /// Determines if the circuit breaker should be triggered (always checks inner strategy first).
        /// </summary>
        /// <param name="statistics">Current pool statistics</param>
        /// <returns>True if circuit breaker should be triggered</returns>
        public bool ShouldTriggerCircuitBreaker(PoolStatistics statistics)
        {
            // Check our own circuit breaker state first
            if (_state == CircuitBreakerState.Open)
                return true;

            // Check inner strategy's circuit breaker
            var innerShouldTrigger = ExecuteWithCircuitBreaker(
                () => _innerStrategy.ShouldTriggerCircuitBreaker(statistics),
                false);

            // Check our own conditions
            var errorRate = _totalOperations > 0 ? (double)_totalErrors / _totalOperations : 0.0;
            var shouldTrigger = innerShouldTrigger || 
                               _consecutiveFailures >= _configuration.CircuitBreakerFailureThreshold ||
                               errorRate > 0.5; // 50% error rate

            return shouldTrigger;
        }

        /// <summary>
        /// Gets the performance budget (prefers inner strategy, falls back to own).
        /// </summary>
        /// <returns>Performance budget configuration</returns>
        public PerformanceBudget GetPerformanceBudget()
        {
            return ExecuteWithCircuitBreaker(
                () => _innerStrategy.GetPerformanceBudget(),
                _performanceBudget);
        }

        /// <summary>
        /// Gets the current health status including circuit breaker state.
        /// </summary>
        /// <returns>Strategy health status</returns>
        public StrategyHealthStatus GetHealthStatus()
        {
            var warnings = new List<string>();
            var errors = new List<string>();

            // Check circuit breaker state
            switch (_state)
            {
                case CircuitBreakerState.Open:
                    errors.Add($"Circuit breaker is OPEN - activated {_circuitBreakerActivations} times");
                    break;
                case CircuitBreakerState.HalfOpen:
                    warnings.Add("Circuit breaker is HALF-OPEN - testing recovery");
                    break;
            }

            // Check failure rate
            if (_consecutiveFailures > 0)
            {
                warnings.Add($"Consecutive failures: {_consecutiveFailures}");
            }

            // Check error rate
            var errorRate = _totalOperations > 0 ? (double)_totalErrors / _totalOperations : 0.0;
            if (errorRate > 0.1)
            {
                warnings.Add($"High error rate: {errorRate:P1}");
            }

            // Get inner strategy health if possible
            StrategyHealthStatus innerHealth = null;
            try
            {
                if (_state != CircuitBreakerState.Open)
                {
                    innerHealth = _innerStrategy.GetHealthStatus();
                    if (innerHealth.HasIssues)
                    {
                        warnings.AddRange(innerHealth.Warnings);
                        errors.AddRange(innerHealth.Errors);
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Inner strategy health check failed: {ex.Message}");
            }

            var status = _state == CircuitBreakerState.Open ? StrategyHealth.CircuitBreakerOpen :
                        errors.Count > 0 ? StrategyHealth.Unhealthy :
                        warnings.Count > 0 ? StrategyHealth.Degraded :
                        StrategyHealth.Healthy;

            return new StrategyHealthStatus
            {
                Status = status,
                Description = GetHealthDescription(),
                Timestamp = DateTime.UtcNow,
                Warnings = warnings,
                Errors = errors,
                CircuitBreakerOpen = _state == CircuitBreakerState.Open,
                OperationCount = _totalOperations,
                ErrorCount = _totalErrors,
                AverageOperationTime = GetAverageOperationTime(),
                MaxOperationTime = GetMaxOperationTime(),
                Metrics = new Dictionary<string, object>
                {
                    ["CircuitBreakerState"] = _state.ToString(),
                    ["ConsecutiveFailures"] = _consecutiveFailures,
                    ["SuccessfulOperations"] = _successfulOperations,
                    ["CircuitBreakerActivations"] = _circuitBreakerActivations,
                    ["ErrorRate"] = errorRate,
                    ["StateTransitionTime"] = _stateTransitionTime,
                    ["InnerStrategy"] = _innerStrategy.Name
                }
            };
        }

        /// <summary>
        /// Called when a pool operation starts.
        /// </summary>
        public void OnPoolOperationStart()
        {
            _totalOperations++;
            
            // Forward to inner strategy if circuit is not open
            if (_state != CircuitBreakerState.Open)
            {
                try
                {
                    _innerStrategy.OnPoolOperationStart();
                }
                catch (Exception ex)
                {
                    RecordFailure(ex);
                }
            }
        }

        /// <summary>
        /// Called when a pool operation completes.
        /// </summary>
        /// <param name="duration">Duration of the operation</param>
        public void OnPoolOperationComplete(TimeSpan duration)
        {
            if (_configuration.EnableDetailedMetrics)
            {
                lock (_metricsLock)
                {
                    _recentOperationTimes.Add(duration);
                    if (_recentOperationTimes.Count > _configuration.MaxMetricsSamples)
                    {
                        _recentOperationTimes.RemoveAt(0);
                    }
                }
            }

            // Forward to inner strategy if circuit is not open
            if (_state != CircuitBreakerState.Open)
            {
                try
                {
                    _innerStrategy.OnPoolOperationComplete(duration);
                    RecordSuccess();
                }
                catch (Exception ex)
                {
                    RecordFailure(ex);
                }
            }
            else
            {
                // Circuit is open, consider this a successful operation at our level
                RecordSuccess();
            }
        }

        /// <summary>
        /// Called when a pool operation encounters an error.
        /// </summary>
        /// <param name="error">The error that occurred</param>
        public void OnPoolError(Exception error)
        {
            RecordFailure(error);
            
            // Forward to inner strategy if circuit is not open
            if (_state != CircuitBreakerState.Open)
            {
                try
                {
                    _innerStrategy.OnPoolError(error);
                }
                catch (Exception innerEx)
                {
                    // Inner strategy itself failed, record additional failure
                    RecordFailure(innerEx);
                }
            }
        }

        /// <summary>
        /// Gets network-specific metrics from the inner strategy.
        /// </summary>
        /// <returns>Network pooling metrics, or null if not supported</returns>
        public NetworkPoolingMetrics GetNetworkMetrics()
        {
            return ExecuteWithCircuitBreaker(
                () => _innerStrategy.GetNetworkMetrics(),
                null); // No fallback for network metrics
        }

        /// <summary>
        /// Gets the strategy configuration.
        /// </summary>
        /// <returns>Strategy configuration</returns>
        public PoolingStrategyConfig GetConfiguration()
        {
            return _configuration;
        }

        /// <summary>
        /// Gets the inner strategy being protected.
        /// </summary>
        /// <returns>The inner strategy</returns>
        public IPoolingStrategy GetInnerStrategy()
        {
            return _innerStrategy;
        }

        /// <summary>
        /// Manually opens the circuit breaker.
        /// </summary>
        public void OpenCircuit()
        {
            TransitionTo(CircuitBreakerState.Open);
        }

        /// <summary>
        /// Manually closes the circuit breaker.
        /// </summary>
        public void CloseCircuit()
        {
            _consecutiveFailures = 0;
            TransitionTo(CircuitBreakerState.Closed);
        }

        /// <summary>
        /// Resets circuit breaker statistics.
        /// </summary>
        public void ResetStatistics()
        {
            lock (_metricsLock)
            {
                _totalOperations = 0;
                _totalErrors = 0;
                _consecutiveFailures = 0;
                _successfulOperations = 0;
                _circuitBreakerActivations = 0;
                _recentOperationTimes.Clear();
                _recentErrors.Clear();
            }
        }

        #region Private Methods

        private T ExecuteWithCircuitBreaker<T>(Func<T> operation, T fallback)
        {
            // Update circuit breaker state
            UpdateCircuitBreakerState();

            if (_state == CircuitBreakerState.Open)
            {
                return fallback;
            }

            try
            {
                var result = operation();
                RecordSuccess();
                return result;
            }
            catch (Exception ex)
            {
                RecordFailure(ex);
                return fallback;
            }
        }

        private void RecordSuccess()
        {
            _successfulOperations++;
            _consecutiveFailures = 0;
            
            // Transition from half-open to closed after successful operation
            if (_state == CircuitBreakerState.HalfOpen)
            {
                TransitionTo(CircuitBreakerState.Closed);
            }
        }

        private void RecordFailure(Exception ex)
        {
            _totalErrors++;
            _consecutiveFailures++;
            _lastFailureTime = DateTime.UtcNow;

            if (_configuration.EnableDetailedMetrics)
            {
                lock (_metricsLock)
                {
                    _recentErrors.Add(ex);
                    if (_recentErrors.Count > _configuration.MaxMetricsSamples)
                    {
                        _recentErrors.RemoveAt(0);
                    }
                }
            }

            // Check if we should open the circuit
            if (_consecutiveFailures >= _configuration.CircuitBreakerFailureThreshold && _state == CircuitBreakerState.Closed)
            {
                TransitionTo(CircuitBreakerState.Open);
            }
            else if (_state == CircuitBreakerState.HalfOpen)
            {
                // Failure in half-open state, go back to open
                TransitionTo(CircuitBreakerState.Open);
            }
        }

        private void UpdateCircuitBreakerState()
        {
            if (_state == CircuitBreakerState.Open)
            {
                var timeSinceOpen = DateTime.UtcNow - _stateTransitionTime;
                if (timeSinceOpen >= _configuration.CircuitBreakerRecoveryTime)
                {
                    TransitionTo(CircuitBreakerState.HalfOpen);
                }
            }
        }

        private void TransitionTo(CircuitBreakerState newState)
        {
            if (_state != newState)
            {
                var oldState = _state;
                _state = newState;
                _stateTransitionTime = DateTime.UtcNow;

                if (newState == CircuitBreakerState.Open)
                {
                    _circuitBreakerActivations++;
                }

                // Could log state transition here if logging service was available
            }
        }

        private TimeSpan GetAverageOperationTime()
        {
            lock (_metricsLock)
            {
                if (_recentOperationTimes.Count == 0)
                    return TimeSpan.Zero;

                var total = TimeSpan.Zero;
                foreach (var time in _recentOperationTimes)
                    total = total.Add(time);
                return TimeSpan.FromTicks(total.Ticks / _recentOperationTimes.Count);
            }
        }

        private TimeSpan GetMaxOperationTime()
        {
            lock (_metricsLock)
            {
                if (_recentOperationTimes.Count == 0)
                    return TimeSpan.Zero;

                var max = TimeSpan.Zero;
                foreach (var time in _recentOperationTimes)
                {
                    if (time > max)
                        max = time;
                }
                return max;
            }
        }

        private string GetHealthDescription()
        {
            return _state switch
            {
                CircuitBreakerState.Closed => $"Circuit breaker protecting {_innerStrategy.Name} - operating normally",
                CircuitBreakerState.Open => $"Circuit breaker OPEN - protecting {_innerStrategy.Name} from failures",
                CircuitBreakerState.HalfOpen => $"Circuit breaker testing recovery for {_innerStrategy.Name}",
                _ => $"Circuit breaker status unknown for {_innerStrategy.Name}"
            };
        }

        private static PoolingStrategyConfig CreateCircuitBreakerConfig()
        {
            return new PoolingStrategyConfig
            {
                Name = "CircuitBreaker",
                PerformanceBudget = PerformanceBudget.For60FPS(),
                EnableCircuitBreaker = true,
                CircuitBreakerFailureThreshold = 5,
                CircuitBreakerRecoveryTime = TimeSpan.FromSeconds(30),
                EnableHealthMonitoring = true,
                HealthCheckInterval = TimeSpan.FromSeconds(15),
                EnableDetailedMetrics = true,
                MaxMetricsSamples = 100,
                EnableDebugLogging = false,
                Tags = new HashSet<string> { "circuit-breaker", "resilience", "production" }
            };
        }

        #endregion
    }

    /// <summary>
    /// Represents the state of a circuit breaker.
    /// </summary>
    public enum CircuitBreakerState
    {
        /// <summary>
        /// Circuit is closed - operations are allowed through.
        /// </summary>
        Closed,

        /// <summary>
        /// Circuit is open - operations are blocked and fallback is used.
        /// </summary>
        Open,

        /// <summary>
        /// Circuit is half-open - testing if the service has recovered.
        /// </summary>
        HalfOpen
    }
}