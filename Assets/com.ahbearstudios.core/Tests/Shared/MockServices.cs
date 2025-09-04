using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Profiling.Models;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.HealthChecking;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Messaging.Messages;

namespace AhBearStudios.Core.Tests.Shared
{
    /// <summary>
    /// Mock profiler service for testing logging performance integration.
    /// </summary>
    public class MockProfilerService : IProfilerService
    {
        private readonly Dictionary<string, double> _metrics = new();
        private readonly Dictionary<string, long> _counters = new();
        private readonly List<(ProfilerTag tag, double value, string unit)> _thresholdEvents = new();
        private readonly List<MetricSnapshot> _allMetrics = new();
        private readonly Dictionary<string, IReadOnlyCollection<MetricSnapshot>> _metricsByTag = new();
        private int _activeScopeCount;
        private long _totalScopeCount;
        private Exception _lastError;
        
        public bool IsEnabled { get; private set; } = true;
        public bool IsRecording { get; private set; } = true;
        public float SamplingRate { get; private set; } = 1.0f;
        public int ActiveScopeCount => _activeScopeCount;
        public long TotalScopeCount => _totalScopeCount;

        public event Action<ProfilerTag, double, string> ThresholdExceeded;
        public event Action<ProfilerTag, double> DataRecorded;
        public event Action<Exception> ErrorOccurred;

        public IDisposable BeginScope(ProfilerTag tag)
        {
            _activeScopeCount++;
            _totalScopeCount++;
            return new MockProfilerScope(this, tag);
        }

        public IDisposable BeginScope(string tagName)
        {
            var tag = new ProfilerTag { Name = tagName };
            return BeginScope(tag);
        }

        public IDisposable BeginScope(ProfilerTag tag, IReadOnlyDictionary<string, object> metadata)
        {
            return BeginScope(tag);
        }

        public void RecordSample(ProfilerTag tag, float value, string unit = "ms")
        {
            if (!IsEnabled || !IsRecording) return;
            
            var snapshot = new MetricSnapshot(DateTime.UtcNow, tag.Name, value, unit, null);
            _allMetrics.Add(snapshot);
            
            if (!_metricsByTag.TryGetValue(tag.Name, out var metrics))
            {
                metrics = new List<MetricSnapshot>();
                _metricsByTag[tag.Name] = metrics;
            }
            ((List<MetricSnapshot>)metrics).Add(snapshot);
            
            DataRecorded?.Invoke(tag, value);
        }

        public void RecordMetric(string metricName, double value, string unit = null, IReadOnlyDictionary<string, string> tags = null)
        {
            if (!IsEnabled || !IsRecording) return;
            
            _metrics[metricName] = value;
            var snapshot = new MetricSnapshot(DateTime.UtcNow, metricName, value, unit, tags);
            _allMetrics.Add(snapshot);
            
            DataRecorded?.Invoke(new ProfilerTag { Name = metricName }, value);
        }

        public void IncrementCounter(string counterName, long increment = 1, IReadOnlyDictionary<string, string> tags = null)
        {
            if (!IsEnabled || !IsRecording) return;
            
            _counters[counterName] = _counters.GetValueOrDefault(counterName, 0) + increment;
            var snapshot = new MetricSnapshot(DateTime.UtcNow, counterName, _counters[counterName], "count", tags);
            _allMetrics.Add(snapshot);
        }

        public void DecrementCounter(string counterName, long decrement = 1, IReadOnlyDictionary<string, string> tags = null)
        {
            if (!IsEnabled || !IsRecording) return;
            
            _counters[counterName] = _counters.GetValueOrDefault(counterName, 0) - decrement;
            var snapshot = new MetricSnapshot(DateTime.UtcNow, counterName, _counters[counterName], "count", tags);
            _allMetrics.Add(snapshot);
        }

        public IReadOnlyCollection<MetricSnapshot> GetMetrics(ProfilerTag tag)
        {
            return _metricsByTag.TryGetValue(tag.Name, out var metrics) ? metrics : new List<MetricSnapshot>();
        }

        public IReadOnlyDictionary<string, IReadOnlyCollection<MetricSnapshot>> GetAllMetrics()
        {
            return _metricsByTag;
        }

        public IReadOnlyDictionary<string, object> GetStatistics()
        {
            return new Dictionary<string, object>
            {
                ["ActiveScopeCount"] = _activeScopeCount,
                ["TotalScopeCount"] = _totalScopeCount,
                ["MetricCount"] = _allMetrics.Count,
                ["IsEnabled"] = IsEnabled,
                ["IsRecording"] = IsRecording,
                ["SamplingRate"] = SamplingRate
            };
        }

        public void Enable(float samplingRate = 1.0f)
        {
            IsEnabled = true;
            SamplingRate = samplingRate;
        }

        public void Disable()
        {
            IsEnabled = false;
        }

        public void StartRecording()
        {
            IsRecording = true;
        }

        public void StopRecording()
        {
            IsRecording = false;
        }

        public void ClearData()
        {
            _metrics.Clear();
            _counters.Clear();
            _thresholdEvents.Clear();
            _allMetrics.Clear();
            _metricsByTag.Clear();
        }

        public void Flush()
        {
            // Mock implementation - no buffering to flush
        }

        public bool PerformHealthCheck()
        {
            return IsEnabled && _lastError == null;
        }

        public Exception GetLastError()
        {
            return _lastError;
        }

        public void TriggerThresholdExceeded(ProfilerTag tag, double value, string unit)
        {
            _thresholdEvents.Add((tag, value, unit));
            ThresholdExceeded?.Invoke(tag, value, unit);
        }

        public void SetLastError(Exception error)
        {
            _lastError = error;
            ErrorOccurred?.Invoke(error);
        }

        public void DecrementActiveScopeCount()
        {
            if (_activeScopeCount > 0)
                _activeScopeCount--;
        }

        public Dictionary<string, double> GetBasicMetrics() => new(_metrics);
        public List<(ProfilerTag tag, double value, string unit)> GetThresholdEvents() => new(_thresholdEvents);

        public void Dispose() { }
    }

    /// <summary>
    /// Mock profiler scope for testing profiler integration.
    /// </summary>
    public class MockProfilerScope : IDisposable
    {
        private readonly MockProfilerService _profiler;
        private readonly ProfilerTag _tag;
        private readonly DateTime _startTime;

        public MockProfilerScope(MockProfilerService profiler, ProfilerTag tag)
        {
            _profiler = profiler;
            _tag = tag;
            _startTime = DateTime.UtcNow;
        }

        public void Dispose()
        {
            var elapsed = DateTime.UtcNow - _startTime;
            _profiler.RecordMetric(_tag.Name, elapsed.TotalMilliseconds, "ms");
            _profiler.DecrementActiveScopeCount();
        }
    }

    /// <summary>
    /// Mock alert service for testing logging alert integration.
    /// </summary>
    public class MockAlertService : IAlertService
    {
        private readonly List<AlertMessage> _alerts = new();

        public IReadOnlyList<AlertMessage> Alerts => _alerts;

        public void RaiseAlert(string message, AlertSeverity severity, string source, string tag)
        {
            _alerts.Add(new AlertMessage
            {
                Message = message,
                Severity = severity,
                Source = source,
                Tag = tag,
                Timestamp = DateTime.UtcNow
            });
        }

        public void RaiseAlert(FixedString128Bytes message, AlertSeverity severity, FixedString64Bytes source, FixedString64Bytes tag)
        {
            _alerts.Add(new AlertMessage
            {
                Message = message.ToString(),
                Severity = severity,
                Source = source.ToString(),
                Tag = tag.ToString(),
                Timestamp = DateTime.UtcNow
            });
        }

        public void ClearAlerts() => _alerts.Clear();

        public void Dispose() { }
    }

    /// <summary>
    /// Mock health check service for testing logging health integration.
    /// </summary>
    public class MockHealthCheckService : IHealthCheckService
    {
        private readonly Dictionary<FixedString64Bytes, IHealthCheck> _healthChecks = new();
        private readonly Dictionary<FixedString64Bytes, HealthCheckConfiguration> _configurations = new();
        private readonly Dictionary<FixedString64Bytes, List<HealthCheckResult>> _history = new();
        private readonly Dictionary<FixedString64Bytes, CircuitBreakerState> _circuitBreakers = new();
        private readonly Dictionary<FixedString64Bytes, bool> _enabledChecks = new();
        private readonly Dictionary<FixedString64Bytes, Dictionary<string, object>> _metadata = new();
        private bool _automaticChecksRunning = false;
        private DegradationLevel _degradationLevel = DegradationLevel.None;

        public event EventHandler<HealthStatusChangedEventArgs> HealthStatusChanged;
        public event EventHandler<CircuitBreakerStateChangedEventArgs> CircuitBreakerStateChanged;
        public event EventHandler<DegradationStatusChangedEventArgs> DegradationStatusChanged;

        public void RegisterHealthCheck(IHealthCheck healthCheck, HealthCheckConfiguration config = null)
        {
            if (healthCheck == null) throw new ArgumentNullException(nameof(healthCheck));
            
            var name = new FixedString64Bytes(healthCheck.Name);
            if (_healthChecks.ContainsKey(name))
                throw new InvalidOperationException($"Health check '{healthCheck.Name}' already exists");

            _healthChecks[name] = healthCheck;
            _configurations[name] = config ?? new HealthCheckConfiguration();
            _history[name] = new List<HealthCheckResult>();
            _enabledChecks[name] = true;
            _metadata[name] = new Dictionary<string, object>
            {
                ["RegisteredAt"] = DateTime.UtcNow,
                ["Category"] = healthCheck.Category,
                ["Tags"] = healthCheck.Tags
            };
        }

        public void RegisterHealthChecks(Dictionary<IHealthCheck, HealthCheckConfiguration> healthChecks)
        {
            if (healthChecks == null) throw new ArgumentNullException(nameof(healthChecks));
            
            foreach (var kvp in healthChecks)
            {
                RegisterHealthCheck(kvp.Key, kvp.Value);
            }
        }

        public bool UnregisterHealthCheck(FixedString64Bytes name)
        {
            if (!_healthChecks.ContainsKey(name)) return false;
            
            _healthChecks.Remove(name);
            _configurations.Remove(name);
            _history.Remove(name);
            _enabledChecks.Remove(name);
            _metadata.Remove(name);
            return true;
        }

        public async Task<HealthCheckResult> ExecuteHealthCheckAsync(FixedString64Bytes name, CancellationToken cancellationToken = default)
        {
            if (!_healthChecks.TryGetValue(name, out var healthCheck))
                throw new ArgumentException($"Health check '{name}' not found");

            if (!_enabledChecks.GetValueOrDefault(name, true))
                return HealthCheckResult.Unknown(name.ToString(), "Health check is disabled");

            var startTime = DateTime.UtcNow;
            HealthCheckResult result;

            try
            {
                var context = new HealthCheckContext { Name = name.ToString() };
                result = await healthCheck.CheckHealthAsync(context, cancellationToken);
            }
            catch (Exception ex)
            {
                result = HealthCheckResult.Unhealthy(name.ToString(), ex.Message, 
                    DateTime.UtcNow - startTime, exception: ex);
            }

            _history[name].Add(result);
            if (_history[name].Count > 100)
                _history[name].RemoveAt(0);

            return result;
        }

        public async Task<HealthReport> ExecuteAllHealthChecksAsync(CancellationToken cancellationToken = default)
        {
            var results = new Dictionary<string, HealthCheckResult>();
            var overallStatus = HealthStatus.Healthy;

            foreach (var kvp in _healthChecks)
            {
                var result = await ExecuteHealthCheckAsync(kvp.Key, cancellationToken);
                results[kvp.Key.ToString()] = result;

                if (result.Status == HealthStatus.Unhealthy)
                    overallStatus = HealthStatus.Unhealthy;
                else if (result.Status == HealthStatus.Degraded && overallStatus == HealthStatus.Healthy)
                    overallStatus = HealthStatus.Degraded;
            }

            return new HealthReport(results, overallStatus, TimeSpan.FromMilliseconds(100));
        }

        public async Task<HealthStatus> GetOverallHealthStatusAsync(CancellationToken cancellationToken = default)
        {
            var report = await ExecuteAllHealthChecksAsync(cancellationToken);
            return report.Status;
        }

        public DegradationLevel GetCurrentDegradationLevel()
        {
            return _degradationLevel;
        }

        public CircuitBreakerState GetCircuitBreakerState(FixedString64Bytes operationName)
        {
            return _circuitBreakers.GetValueOrDefault(operationName, CircuitBreakerState.Closed);
        }

        public Dictionary<FixedString64Bytes, CircuitBreakerState> GetAllCircuitBreakerStates()
        {
            return new Dictionary<FixedString64Bytes, CircuitBreakerState>(_circuitBreakers);
        }

        public List<HealthCheckResult> GetHealthCheckHistory(FixedString64Bytes name, int maxResults = 100)
        {
            if (!_history.TryGetValue(name, out var history))
                return new List<HealthCheckResult>();

            return history.TakeLast(maxResults).ToList();
        }

        public List<FixedString64Bytes> GetRegisteredHealthCheckNames()
        {
            return _healthChecks.Keys.ToList();
        }

        public Dictionary<string, object> GetHealthCheckMetadata(FixedString64Bytes name)
        {
            return _metadata.GetValueOrDefault(name, new Dictionary<string, object>());
        }

        public void StartAutomaticChecks()
        {
            _automaticChecksRunning = true;
        }

        public void StopAutomaticChecks()
        {
            _automaticChecksRunning = false;
        }

        public bool IsAutomaticChecksRunning()
        {
            return _automaticChecksRunning;
        }

        public void ForceCircuitBreakerOpen(FixedString64Bytes operationName, string reason)
        {
            var oldState = _circuitBreakers.GetValueOrDefault(operationName, CircuitBreakerState.Closed);
            _circuitBreakers[operationName] = CircuitBreakerState.Open;
            
            CircuitBreakerStateChanged?.Invoke(this, new CircuitBreakerStateChangedEventArgs
            {
                OperationName = operationName,
                OldState = oldState,
                NewState = CircuitBreakerState.Open,
                Reason = reason
            });
        }

        public void ForceCircuitBreakerClosed(FixedString64Bytes operationName, string reason)
        {
            var oldState = _circuitBreakers.GetValueOrDefault(operationName, CircuitBreakerState.Open);
            _circuitBreakers[operationName] = CircuitBreakerState.Closed;
            
            CircuitBreakerStateChanged?.Invoke(this, new CircuitBreakerStateChangedEventArgs
            {
                OperationName = operationName,
                OldState = oldState,
                NewState = CircuitBreakerState.Closed,
                Reason = reason
            });
        }

        public void SetDegradationLevel(DegradationLevel level, string reason)
        {
            var oldLevel = _degradationLevel;
            _degradationLevel = level;
            
            DegradationStatusChanged?.Invoke(this, new DegradationStatusChangedEventArgs
            {
                OldLevel = oldLevel,
                NewLevel = level,
                Reason = reason
            });
        }

        public HealthStatistics GetHealthStatistics()
        {
            return new HealthStatistics
            {
                TotalHealthChecks = _healthChecks.Count,
                EnabledHealthChecks = _enabledChecks.Count(kvp => kvp.Value),
                DisabledHealthChecks = _enabledChecks.Count(kvp => !kvp.Value),
                AutomaticChecksRunning = _automaticChecksRunning,
                CurrentDegradationLevel = _degradationLevel,
                CircuitBreakerStates = _circuitBreakers.Count
            };
        }

        public bool IsHealthCheckEnabled(FixedString64Bytes name)
        {
            return _enabledChecks.GetValueOrDefault(name, true);
        }

        public void SetHealthCheckEnabled(FixedString64Bytes name, bool enabled)
        {
            if (_healthChecks.ContainsKey(name))
            {
                _enabledChecks[name] = enabled;
            }
        }

        public void SetHealthStatus(string name, bool isHealthy)
        {
            var fixedName = new FixedString64Bytes(name);
            var result = isHealthy 
                ? HealthCheckResult.Healthy(name, "Mock health check passed")
                : HealthCheckResult.Unhealthy(name, "Mock health check failed");
            
            if (!_history.TryGetValue(fixedName, out var history))
            {
                history = new List<HealthCheckResult>();
                _history[fixedName] = history;
            }
            
            history.Add(result);
            if (history.Count > 100)
                history.RemoveAt(0);
        }

        public void ClearHealthStatuses()
        {
            _history.Clear();
            _circuitBreakers.Clear();
            _degradationLevel = DegradationLevel.None;
        }

        public void Dispose() { }
    }

    /// <summary>
    /// Mock types for health check system testing
    /// </summary>

    public interface IHealthCheck
    {
        string Name { get; }
        HealthCheckCategory Category { get; }
        HashSet<FixedString64Bytes> Tags { get; }
        Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default);
    }

    public class HealthCheckContext
    {
        public string Name { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    public class HealthCheckConfiguration
    {
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
        public bool Enabled { get; set; } = true;
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    public class HealthReport
    {
        public Dictionary<string, HealthCheckResult> Results { get; }
        public HealthStatus Status { get; }
        public TimeSpan Duration { get; }

        public HealthReport(Dictionary<string, HealthCheckResult> results, HealthStatus status, TimeSpan duration)
        {
            Results = results;
            Status = status;
            Duration = duration;
        }
    }

    public class HealthStatusChangedEventArgs : EventArgs
    {
        public HealthStatus OldStatus { get; set; }
        public HealthStatus NewStatus { get; set; }
        public string Reason { get; set; }
    }

    public class CircuitBreakerStateChangedEventArgs : EventArgs
    {
        public FixedString64Bytes OperationName { get; set; }
        public CircuitBreakerState OldState { get; set; }
        public CircuitBreakerState NewState { get; set; }
        public string Reason { get; set; }
    }

    public class DegradationStatusChangedEventArgs : EventArgs
    {
        public DegradationLevel OldLevel { get; set; }
        public DegradationLevel NewLevel { get; set; }
        public string Reason { get; set; }
    }

    public class HealthStatistics
    {
        public int TotalHealthChecks { get; set; }
        public int EnabledHealthChecks { get; set; }
        public int DisabledHealthChecks { get; set; }
        public bool AutomaticChecksRunning { get; set; }
        public DegradationLevel CurrentDegradationLevel { get; set; }
        public int CircuitBreakerStates { get; set; }
    }

    public enum HealthCheckCategory
    {
        Unknown = 0,
        Database = 1,
        ExternalService = 2,
        FileSystem = 3,
        Network = 4,
        Memory = 5,
        Performance = 6,
        Security = 7,
        Custom = 8
    }

    /// <summary>
    /// Mock message bus service for testing logging message integration.
    /// </summary>
    public class MockMessageBusService : IMessageBusService
    {
        private readonly List<IMessage> _publishedMessages = new();
        private readonly Dictionary<Type, List<object>> _subscribers = new();
        private readonly Dictionary<Type, List<object>> _asyncSubscribers = new();
        private readonly Dictionary<Type, CircuitBreakerState> _circuitBreakers = new();
        private readonly Dictionary<Type, MockMessagePublisher> _publishers = new();
        private readonly Dictionary<Type, MockMessageSubscriber> _subscribers2 = new();
        private HealthStatus _healthStatus = HealthStatus.Healthy;
        private bool _disposed = false;

        public IReadOnlyList<IMessage> PublishedMessages => _publishedMessages;

        public event EventHandler<HealthStatusChangedEventArgs> HealthStatusChanged;
        public event EventHandler<MessageProcessingFailedEventArgs> MessageProcessingFailed;
        public event EventHandler<CircuitBreakerStateChangedEventArgs> CircuitBreakerStateChanged;

        public void PublishMessage<TMessage>(TMessage message) where TMessage : IMessage
        {
            if (_disposed) throw new InvalidOperationException("Service is disposed");
            if (message == null) throw new ArgumentNullException(nameof(message));

            _publishedMessages.Add(message);
            
            if (_subscribers.TryGetValue(typeof(TMessage), out var handlers))
            {
                foreach (var handler in handlers)
                {
                    if (handler is Action<TMessage> action)
                        action(message);
                }
            }
        }

        public async Task PublishMessageAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : IMessage
        {
            if (_disposed) throw new InvalidOperationException("Service is disposed");
            if (message == null) throw new ArgumentNullException(nameof(message));

            _publishedMessages.Add(message);
            
            if (_asyncSubscribers.TryGetValue(typeof(TMessage), out var handlers))
            {
                foreach (var handler in handlers)
                {
                    if (handler is Func<TMessage, Task> asyncHandler)
                        await asyncHandler(message);
                }
            }
        }

        public void PublishBatch<TMessage>(TMessage[] messages) where TMessage : IMessage
        {
            if (_disposed) throw new InvalidOperationException("Service is disposed");
            if (messages == null) throw new ArgumentNullException(nameof(messages));

            foreach (var message in messages)
            {
                PublishMessage(message);
            }
        }

        public async Task PublishBatchAsync<TMessage>(TMessage[] messages, CancellationToken cancellationToken = default) where TMessage : IMessage
        {
            if (_disposed) throw new InvalidOperationException("Service is disposed");
            if (messages == null) throw new ArgumentNullException(nameof(messages));

            foreach (var message in messages)
            {
                await PublishMessageAsync(message, cancellationToken);
            }
        }

        public IDisposable SubscribeToMessage<TMessage>(Action<TMessage> handler) where TMessage : IMessage
        {
            if (_disposed) throw new InvalidOperationException("Service is disposed");
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            if (!_subscribers.TryGetValue(typeof(TMessage), out var handlers))
            {
                handlers = new List<object>();
                _subscribers[typeof(TMessage)] = handlers;
            }
            handlers.Add(handler);
            
            return new MockSubscription(() => handlers.Remove(handler));
        }

        public IDisposable SubscribeToMessageAsync<TMessage>(Func<TMessage, Task> handler) where TMessage : IMessage
        {
            if (_disposed) throw new InvalidOperationException("Service is disposed");
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            if (!_asyncSubscribers.TryGetValue(typeof(TMessage), out var handlers))
            {
                handlers = new List<object>();
                _asyncSubscribers[typeof(TMessage)] = handlers;
            }
            handlers.Add(handler);
            
            return new MockSubscription(() => handlers.Remove(handler));
        }

        public IMessagePublisher<TMessage> GetPublisher<TMessage>() where TMessage : IMessage
        {
            if (_disposed) throw new InvalidOperationException("Service is disposed");
            
            if (!_publishers.TryGetValue(typeof(TMessage), out var publisher))
            {
                publisher = new MockMessagePublisher<TMessage>(this);
                _publishers[typeof(TMessage)] = publisher;
            }
            return (IMessagePublisher<TMessage>)publisher;
        }

        public IMessageSubscriber<TMessage> GetSubscriber<TMessage>() where TMessage : IMessage
        {
            if (_disposed) throw new InvalidOperationException("Service is disposed");
            
            if (!_subscribers2.TryGetValue(typeof(TMessage), out var subscriber))
            {
                subscriber = new MockMessageSubscriber<TMessage>(this);
                _subscribers2[typeof(TMessage)] = subscriber;
            }
            return (IMessageSubscriber<TMessage>)subscriber;
        }

        public IDisposable SubscribeWithFilter<TMessage>(Func<TMessage, bool> filter, Action<TMessage> handler) where TMessage : IMessage
        {
            if (_disposed) throw new InvalidOperationException("Service is disposed");
            if (filter == null) throw new ArgumentNullException(nameof(filter));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            return SubscribeToMessage<TMessage>(message =>
            {
                if (filter(message))
                    handler(message);
            });
        }

        public IDisposable SubscribeWithFilterAsync<TMessage>(Func<TMessage, bool> filter, Func<TMessage, Task> handler) where TMessage : IMessage
        {
            if (_disposed) throw new InvalidOperationException("Service is disposed");
            if (filter == null) throw new ArgumentNullException(nameof(filter));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            return SubscribeToMessageAsync<TMessage>(async message =>
            {
                if (filter(message))
                    await handler(message);
            });
        }

        public IDisposable SubscribeWithPriority<TMessage>(Action<TMessage> handler, MessagePriority minPriority) where TMessage : IMessage
        {
            if (_disposed) throw new InvalidOperationException("Service is disposed");
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            return SubscribeToMessage<TMessage>(message =>
            {
                if (message.Priority >= minPriority)
                    handler(message);
            });
        }

        public IMessageScope CreateScope()
        {
            if (_disposed) throw new InvalidOperationException("Service is disposed");
            return new MockMessageScope();
        }

        public MessageBusStatistics GetStatistics()
        {
            return new MessageBusStatistics
            {
                TotalMessagesPublished = _publishedMessages.Count,
                TotalSubscribers = _subscribers.Values.Sum(h => h.Count),
                ActiveCircuitBreakers = _circuitBreakers.Count,
                HealthStatus = _healthStatus
            };
        }

        public void ClearMessageHistory()
        {
            _publishedMessages.Clear();
        }

        public HealthStatus GetHealthStatus()
        {
            return _healthStatus;
        }

        public async Task<HealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(1, cancellationToken);
            return _healthStatus;
        }

        public CircuitBreakerState GetCircuitBreakerState<TMessage>() where TMessage : IMessage
        {
            return _circuitBreakers.GetValueOrDefault(typeof(TMessage), CircuitBreakerState.Closed);
        }

        public void ResetCircuitBreaker<TMessage>() where TMessage : IMessage
        {
            var oldState = _circuitBreakers.GetValueOrDefault(typeof(TMessage), CircuitBreakerState.Open);
            _circuitBreakers[typeof(TMessage)] = CircuitBreakerState.Closed;
            
            CircuitBreakerStateChanged?.Invoke(this, new CircuitBreakerStateChangedEventArgs
            {
                OperationName = new FixedString64Bytes(typeof(TMessage).Name),
                OldState = oldState,
                NewState = CircuitBreakerState.Closed,
                Reason = "Manual reset"
            });
        }

        public void SetHealthStatus(HealthStatus status)
        {
            var oldStatus = _healthStatus;
            _healthStatus = status;
            
            HealthStatusChanged?.Invoke(this, new HealthStatusChangedEventArgs
            {
                OldStatus = oldStatus,
                NewStatus = status,
                Reason = "Manual status change"
            });
        }

        public void TriggerCircuitBreakerOpen<TMessage>() where TMessage : IMessage
        {
            var oldState = _circuitBreakers.GetValueOrDefault(typeof(TMessage), CircuitBreakerState.Closed);
            _circuitBreakers[typeof(TMessage)] = CircuitBreakerState.Open;
            
            CircuitBreakerStateChanged?.Invoke(this, new CircuitBreakerStateChangedEventArgs
            {
                OperationName = new FixedString64Bytes(typeof(TMessage).Name),
                OldState = oldState,
                NewState = CircuitBreakerState.Open,
                Reason = "Circuit breaker tripped"
            });
        }

        public void Subscribe<T>(Action<T> handler) where T : IMessage
        {
            SubscribeToMessage(handler);
        }

        public void Unsubscribe<T>(Action<T> handler) where T : IMessage
        {
            if (_subscribers.TryGetValue(typeof(T), out var handlers))
            {
                handlers.Remove(handler);
            }
        }

        public void Publish<T>(T message) where T : IMessage
        {
            PublishMessage(message);
        }

        public void ClearMessages() => _publishedMessages.Clear();

        public void Dispose()
        {
            if (!_disposed)
            {
                _publishedMessages.Clear();
                _subscribers.Clear();
                _asyncSubscribers.Clear();
                _circuitBreakers.Clear();
                _publishers.Clear();
                _subscribers2.Clear();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Alert message model for testing.
    /// </summary>
    public class AlertMessage
    {
        public string Message { get; set; }
        public AlertSeverity Severity { get; set; }
        public string Source { get; set; }
        public string Tag { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Supporting mock types for message bus testing
    /// </summary>
    public enum MessagePriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3
    }

    public class MockSubscription : IDisposable
    {
        private readonly Action _unsubscribe;
        private bool _disposed = false;

        public MockSubscription(Action unsubscribe)
        {
            _unsubscribe = unsubscribe;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _unsubscribe?.Invoke();
                _disposed = true;
            }
        }
    }

    public class MockMessagePublisher<TMessage> : IMessagePublisher<TMessage> where TMessage : IMessage
    {
        private readonly MockMessageBusService _messageBus;

        public MockMessagePublisher(MockMessageBusService messageBus)
        {
            _messageBus = messageBus;
        }

        public void PublishMessage(TMessage message)
        {
            _messageBus.PublishMessage(message);
        }

        public Task PublishMessageAsync(TMessage message, CancellationToken cancellationToken = default)
        {
            return _messageBus.PublishMessageAsync(message, cancellationToken);
        }

        public void PublishBatch(TMessage[] messages)
        {
            _messageBus.PublishBatch(messages);
        }

        public Task PublishBatchAsync(TMessage[] messages, CancellationToken cancellationToken = default)
        {
            return _messageBus.PublishBatchAsync(messages, cancellationToken);
        }
    }

    public class MockMessageSubscriber<TMessage> : IMessageSubscriber<TMessage> where TMessage : IMessage
    {
        private readonly MockMessageBusService _messageBus;

        public MockMessageSubscriber(MockMessageBusService messageBus)
        {
            _messageBus = messageBus;
        }

        public IDisposable Subscribe(Action<TMessage> handler)
        {
            return _messageBus.SubscribeToMessage(handler);
        }

        public IDisposable SubscribeAsync(Func<TMessage, Task> handler)
        {
            return _messageBus.SubscribeToMessageAsync(handler);
        }

        public IDisposable SubscribeWithFilter(Func<TMessage, bool> filter, Action<TMessage> handler)
        {
            return _messageBus.SubscribeWithFilter(filter, handler);
        }

        public IDisposable SubscribeWithFilterAsync(Func<TMessage, bool> filter, Func<TMessage, Task> handler)
        {
            return _messageBus.SubscribeWithFilterAsync(filter, handler);
        }
    }

    public class MockMessageScope : IMessageScope
    {
        private readonly List<IDisposable> _subscriptions = new();
        private bool _disposed = false;

        public IDisposable Subscribe<TMessage>(Action<TMessage> handler) where TMessage : IMessage
        {
            var subscription = new MockSubscription(() => { });
            _subscriptions.Add(subscription);
            return subscription;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                foreach (var subscription in _subscriptions)
                {
                    subscription?.Dispose();
                }
                _subscriptions.Clear();
                _disposed = true;
            }
        }
    }

    public class MessageBusStatistics
    {
        public int TotalMessagesPublished { get; set; }
        public int TotalSubscribers { get; set; }
        public int ActiveCircuitBreakers { get; set; }
        public HealthStatus HealthStatus { get; set; }
    }

    public class MessageProcessingFailedEventArgs : EventArgs
    {
        public Type MessageType { get; set; }
        public Exception Exception { get; set; }
        public string HandlerName { get; set; }
    }

    public interface IMessagePublisher<TMessage> where TMessage : IMessage
    {
        void PublishMessage(TMessage message);
        Task PublishMessageAsync(TMessage message, CancellationToken cancellationToken = default);
        void PublishBatch(TMessage[] messages);
        Task PublishBatchAsync(TMessage[] messages, CancellationToken cancellationToken = default);
    }

    public interface IMessageSubscriber<TMessage> where TMessage : IMessage
    {
        IDisposable Subscribe(Action<TMessage> handler);
        IDisposable SubscribeAsync(Func<TMessage, Task> handler);
        IDisposable SubscribeWithFilter(Func<TMessage, bool> filter, Action<TMessage> handler);
        IDisposable SubscribeWithFilterAsync(Func<TMessage, bool> filter, Func<TMessage, Task> handler);
    }

    public interface IMessageScope : IDisposable
    {
        IDisposable Subscribe<TMessage>(Action<TMessage> handler) where TMessage : IMessage;
    }
}