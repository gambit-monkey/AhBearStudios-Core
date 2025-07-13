using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Pooling;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
    /// Internal data structure for managing health check history
    /// </summary>
    internal sealed class HealthCheckHistoryEntry : IDisposable
    {
        private readonly FixedString64Bytes _checkName;
        private readonly int _maxSize;
        private readonly IPoolingService _poolingService;
        private readonly ILoggingService _logger;
        private readonly List<HealthCheckResult> _results;
        private readonly object _lock = new object();
        private bool _disposed;

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _results.Count;
                }
            }
        }

        public HealthCheckHistoryEntry(
            FixedString64Bytes checkName,
            int maxSize,
            IPoolingService poolingService,
            ILoggingService logger)
        {
            _checkName = checkName;
            _maxSize = maxSize;
            _poolingService = poolingService;
            _logger = logger;
            _results = new List<HealthCheckResult>();
        }

        public void AddResult(HealthCheckResult result)
        {
            lock (_lock)
            {
                _results.Add(result);

                if (_results.Count > _maxSize)
                {
                    TrimToSize(_maxSize);
                }
            }
        }

        public IEnumerable<HealthCheckResult> GetResultsInTimeRange(DateTime startTime, DateTime endTime)
        {
            lock (_lock)
            {
                return _results.Where(r => r.Timestamp >= startTime && r.Timestamp <= endTime).ToList();
            }
        }

        public bool RemoveResultsOlderThan(DateTime cutoffTime)
        {
            lock (_lock)
            {
                var countBefore = _results.Count;
                _results.RemoveAll(r => r.Timestamp < cutoffTime);
                return _results.Count < countBefore;
            }
        }

        public void TrimToSize(int maxSize)
        {
            lock (_lock)
            {
                if (_results.Count > maxSize)
                {
                    var toRemove = _results.Count - maxSize;
                    _results.RemoveRange(0, toRemove);
                }
            }
        }

        public bool CompactData()
        {
            // Implementation could compress older data points
            return false;
        }

        public void Dispose()
        {
            if (_disposed) return;

            lock (_lock)
            {
                _results.Clear();
            }

            _disposed = true;
        }
    }