using System;
using Unity.Collections;
using AhBearStudios.Core.Pooling;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.Tests.Shared.TestDoubles
{
    /// <summary>
    /// Test implementation of IPooledObject for verification tests.
    /// Provides complete interface implementation following TDD patterns.
    /// </summary>
    public class TestPooledObject : IPooledObject
    {
        #region Core Lifecycle

        public void OnGet()
        {
            LastUsed = DateTime.UtcNow;
            UseCount++;
        }

        public void OnReturn()
        {
            // Update active time calculation
            if (LastUsed != default)
            {
                TotalActiveTime += DateTime.UtcNow - LastUsed;
            }
        }

        public void Reset()
        {
            // Reset state but preserve pool tracking information
            ConsecutiveFailures = 0;
            CorruptionDetected = false;
            ValidationErrorCount = 0;
        }

        public bool IsValid()
        {
            return !CorruptionDetected && ValidationErrorCount < 5;
        }

        #endregion

        #region Pool Information

        public string PoolName { get; set; } = "TestPool";
        public Guid PoolId { get; set; } = Guid.NewGuid();
        public DateTime LastUsed { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        #endregion

        #region Performance Monitoring

        public long UseCount { get; set; } = 0;
        public TimeSpan TotalActiveTime { get; set; } = TimeSpan.Zero;
        public DateTime LastValidationTime { get; set; } = DateTime.UtcNow;
        public int Priority { get; set; } = 1;

        public long GetEstimatedMemoryUsage()
        {
            // Rough estimate for test object
            return 1024; // 1KB
        }

        #endregion

        #region Health and Validation

        public int ValidationErrorCount { get; set; } = 0;
        public bool CorruptionDetected { get; set; } = false;

        public HealthStatus GetHealthStatus()
        {
            if (CorruptionDetected) return HealthStatus.Critical;
            if (ValidationErrorCount > 3) return HealthStatus.Degraded;
            if (ValidationErrorCount > 0) return HealthStatus.Warning;
            return HealthStatus.Healthy;
        }

        public bool CanBePooled()
        {
            return !CorruptionDetected && IsValid();
        }

        #endregion

        #region Circuit Breaker Support

        public int ConsecutiveFailures { get; set; } = 0;

        public bool ShouldCircuitBreak()
        {
            return ConsecutiveFailures >= 5;
        }

        #endregion

        #region Alert Integration

        public bool HasCriticalIssue()
        {
            return CorruptionDetected || ValidationErrorCount > 10;
        }

        public FixedString512Bytes? GetAlertMessage()
        {
            if (CorruptionDetected)
                return new FixedString512Bytes("Test object corruption detected");
            if (ValidationErrorCount > 10)
                return new FixedString512Bytes($"High validation error count: {ValidationErrorCount}");
            return null;
        }

        #endregion

        #region Diagnostics

        public PooledObjectDiagnostics GetDiagnosticInfo()
        {
            return new PooledObjectDiagnostics
            {
                Id = PoolId,
                PoolName = new FixedString64Bytes(PoolName),
                CreatedAtTicks = CreatedAt.Ticks,
                LastUsedTicks = LastUsed.Ticks,
                UseCount = UseCount,
                TotalActiveTimeMs = (long)TotalActiveTime.TotalMilliseconds,
                HealthStatus = GetHealthStatus(),
                ValidationErrors = ValidationErrorCount,
                CorruptionDetected = CorruptionDetected,
                ConsecutiveFailures = ConsecutiveFailures,
                MemoryUsageBytes = GetEstimatedMemoryUsage(),
                IsPoolable = CanBePooled(),
                DiagnosticMessage = new FixedString512Bytes("Test pooled object diagnostics")
            };
        }

        #endregion
    }
}