using System;
using Unity.Collections;
using Unity.Profiling;
using AhBearStudios.Core.Pooling;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.HealthChecking.Models;
using HealthStatus = AhBearStudios.Core.HealthChecking.Models.HealthStatus;

namespace AhBearStudios.Core.Alerting.Models
{
    /// <summary>
    /// Pooled container for Alert instances that implements IPooledObject.
    /// Manages the lifecycle of Alert records while keeping Alert itself simple and immutable.
    /// Designed for high-frequency alert creation with zero-allocation patterns.
    /// </summary>
    public sealed class PooledAlertContainer : IPooledObject, IDisposable
    {
        private static readonly ProfilerMarker _createAlertMarker = new("PooledAlertContainer.CreateAlert");
        private static readonly ProfilerMarker _resetMarker = new("PooledAlertContainer.Reset");

        private Alert _currentAlert;
        private bool _isInUse;
        private DateTime _usageStartTime;

        #region IPooledObject Implementation

        public string PoolName { get; set; } = "AlertContainer";
        public Guid PoolId { get; set; }
        public DateTime LastUsed { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public long UseCount { get; set; }
        public TimeSpan TotalActiveTime { get; set; }
        public DateTime LastValidationTime { get; set; }
        public int Priority { get; set; }
        public int ValidationErrorCount { get; set; }
        public bool CorruptionDetected { get; set; }
        public int ConsecutiveFailures { get; set; }

        public void OnGet()
        {
            LastUsed = DateTime.UtcNow;
            _usageStartTime = LastUsed;
            UseCount++;
            _isInUse = true;
        }

        public void OnReturn()
        {
            if (_isInUse)
            {
                var activeTime = DateTime.UtcNow - _usageStartTime;
                TotalActiveTime = TotalActiveTime.Add(activeTime);
                _isInUse = false;
            }
        }

        public void Reset()
        {
            using (_resetMarker.Auto())
            {
                _currentAlert?.Dispose();
                _currentAlert = null;
                _isInUse = false;
                _usageStartTime = default;
                ValidationErrorCount = 0;
                ConsecutiveFailures = 0;
                CorruptionDetected = false;
            }
        }

        public bool IsValid()
        {
            return !CorruptionDetected && 
                   CreatedAt != default &&
                   PoolId != Guid.Empty;
        }

        public long GetEstimatedMemoryUsage()
        {
            long baseSize = 0;
            baseSize += 16; // PoolId Guid
            baseSize += 8 * 4; // DateTime fields (4 x 8 bytes)
            baseSize += 8; // UseCount long
            baseSize += 8; // TotalActiveTime TimeSpan
            baseSize += 4 * 3; // int fields (3 x 4 bytes)
            baseSize += 1; // bool fields
            baseSize += _currentAlert?.GetEstimatedMemoryUsage() ?? 0;
            return baseSize;
        }

        public AhBearStudios.Core.HealthChecking.Models.HealthStatus GetHealthStatus()
        {
            if (CorruptionDetected)
                return HealthStatus.Degraded;
            if (ValidationErrorCount > 5)
                return HealthStatus.Degraded;
            if (ConsecutiveFailures > 3)
                return HealthStatus.Degraded;
            return HealthStatus.Healthy;
        }

        public bool CanBePooled()
        {
            return !CorruptionDetected && 
                   ValidationErrorCount < 10 &&
                   ConsecutiveFailures < 5;
        }

        public bool ShouldCircuitBreak()
        {
            return ConsecutiveFailures > 3 || CorruptionDetected;
        }

        public bool HasCriticalIssue()
        {
            return CorruptionDetected || 
                   (_currentAlert?.Severity == AlertSeverity.Critical && 
                    _currentAlert?.State == AlertState.Active);
        }

        public FixedString512Bytes? GetAlertMessage()
        {
            if (CorruptionDetected)
                return new FixedString512Bytes("Alert container corruption detected");
            if (_currentAlert?.HasCriticalIssue() == true)
                return _currentAlert?.Message;
            return null;
        }

        public PooledObjectDiagnostics GetDiagnosticInfo()
        {
            return new PooledObjectDiagnostics
            {
                PoolName = new FixedString64Bytes(PoolName ?? "Unknown"),
                HealthStatus = GetHealthStatus(),
                UseCount = UseCount,
                TotalActiveTimeMs = (long)TotalActiveTime.TotalMilliseconds,
                ValidationErrors = ValidationErrorCount,
                ConsecutiveFailures = ConsecutiveFailures,
                MemoryUsageBytes = GetEstimatedMemoryUsage(),
                CreatedAtTicks = CreatedAt.Ticks,
                LastUsedTicks = LastUsed.Ticks,
                DiagnosticMessage = _currentAlert != null 
                    ? new FixedString512Bytes($"Alert:{_currentAlert.Severity}-{_currentAlert.Source}")
                    : new FixedString512Bytes("Empty"),
                IsPoolable = CanBePooled(),
                CorruptionDetected = CorruptionDetected
            };
        }

        #endregion

        #region Alert Creation Methods

        /// <summary>
        /// Creates a new alert with the specified parameters using Unity Collections.
        /// </summary>
        /// <param name="message">Alert message</param>
        /// <param name="severity">Alert severity</param>
        /// <param name="source">Source system</param>
        /// <param name="tag">Optional tag</param>
        /// <param name="correlationId">Correlation ID</param>
        /// <param name="operationId">Operation ID</param>
        /// <param name="context">Optional context</param>
        /// <returns>New alert instance</returns>
        public Alert CreateAlert(
            FixedString512Bytes message,
            AlertSeverity severity,
            FixedString64Bytes source,
            FixedString32Bytes tag = default,
            Guid correlationId = default,
            Guid operationId = default,
            AlertContext context = default)
        {
            using (_createAlertMarker.Auto())
            {
                try
                {
                    _currentAlert = new Alert
                    {
                        Id = Guid.NewGuid(),
                        Message = message,
                        Severity = severity,
                        Source = source,
                        Tag = tag,
                        TimestampTicks = DateTime.UtcNow.Ticks,
                        CorrelationId = correlationId == default ? Guid.NewGuid() : correlationId,
                        OperationId = operationId,
                        Context = context,
                        State = AlertState.Active
                    };

                    return _currentAlert;
                }
                catch (Exception)
                {
                    ConsecutiveFailures++;
                    throw;
                }
            }
        }

        /// <summary>
        /// Creates a new alert with string message (converted to FixedString).
        /// </summary>
        /// <param name="message">Alert message</param>
        /// <param name="severity">Alert severity</param>
        /// <param name="source">Source system</param>
        /// <param name="tag">Optional tag</param>
        /// <param name="correlationId">Correlation ID</param>
        /// <param name="operationId">Operation ID</param>
        /// <param name="context">Optional context</param>
        /// <returns>New alert instance</returns>
        public Alert CreateAlert(
            string message,
            AlertSeverity severity,
            FixedString64Bytes source,
            FixedString32Bytes tag = default,
            Guid correlationId = default,
            Guid operationId = default,
            AlertContext context = default)
        {
            var fixedMessage = message.Length <= 512 ? 
                new FixedString512Bytes(message) : 
                new FixedString512Bytes(message[..512]);

            return CreateAlert(fixedMessage, severity, source, tag, correlationId, operationId, context);
        }

        /// <summary>
        /// Creates an acknowledged alert copy from the current alert.
        /// </summary>
        /// <param name="acknowledgedBy">User or system acknowledging the alert</param>
        /// <returns>Acknowledged alert copy</returns>
        public Alert AcknowledgeCurrentAlert(FixedString64Bytes acknowledgedBy)
        {
            if (_currentAlert == null)
                throw new InvalidOperationException("No current alert to acknowledge");

            _currentAlert = _currentAlert.Acknowledge(acknowledgedBy);
            return _currentAlert;
        }

        /// <summary>
        /// Creates a resolved alert copy from the current alert.
        /// </summary>
        /// <param name="resolvedBy">User or system resolving the alert</param>
        /// <returns>Resolved alert copy</returns>
        public Alert ResolveCurrentAlert(FixedString64Bytes resolvedBy)
        {
            if (_currentAlert == null)
                throw new InvalidOperationException("No current alert to resolve");

            _currentAlert = _currentAlert.Resolve(resolvedBy);
            return _currentAlert;
        }

        /// <summary>
        /// Gets the current alert instance.
        /// </summary>
        public Alert CurrentAlert => _currentAlert;

        /// <summary>
        /// Checks if the container has an active alert.
        /// </summary>
        public bool HasAlert => _currentAlert != null;

        #endregion

        /// <summary>
        /// Disposes the container and any contained alert.
        /// </summary>
        public void Dispose()
        {
            _currentAlert?.Dispose();
            _currentAlert = null;
        }
    }
}