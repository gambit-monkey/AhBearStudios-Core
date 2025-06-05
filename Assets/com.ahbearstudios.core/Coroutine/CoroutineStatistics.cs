using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using AhBearStudios.Core.Coroutine.Interfaces;

namespace AhBearStudios.Core.Coroutine
{
    /// <summary>
    /// Thread-safe implementation of coroutine statistics tracking.
    /// </summary>
    internal sealed class CoroutineStatistics : ICoroutineStatistics, IDisposable
    {
        private readonly object _lockObject = new object();
        private readonly ConcurrentDictionary<string, int> _tagCounts = new ConcurrentDictionary<string, int>();
        
        private long _totalStarted;
        private long _totalCompleted;
        private long _totalCancelled;
        private int _currentActive;
        private int _peakActive;
        private long _totalDurationTicks;
        private bool _isDisposed;

        /// <inheritdoc />
        public long TotalCoroutinesStarted => _totalStarted;

        /// <inheritdoc />
        public long TotalCoroutinesCompleted => _totalCompleted;

        /// <inheritdoc />
        public long TotalCoroutinesCancelled => _totalCancelled;

        /// <inheritdoc />
        public int CurrentActiveCoroutines => _currentActive;

        /// <inheritdoc />
        public int PeakActiveCoroutines => _peakActive;

        /// <inheritdoc />
        public TimeSpan AverageCoroutineDuration
        {
            get
            {
                long completed = _totalCompleted;
                if (completed == 0)
                    return TimeSpan.Zero;

                return new TimeSpan(_totalDurationTicks / completed);
            }
        }

        /// <summary>
        /// Records the start of a coroutine.
        /// </summary>
        /// <param name="tag">Optional tag for the coroutine.</param>
        internal void RecordStart(string tag = null)
        {
            if (_isDisposed)
                return;

            lock (_lockObject)
            {
                _totalStarted++;
                _currentActive++;
                
                if (_currentActive > _peakActive)
                    _peakActive = _currentActive;
            }

            if (!string.IsNullOrEmpty(tag))
            {
                _tagCounts.AddOrUpdate(tag, 1, (key, value) => value + 1);
            }
        }

        /// <summary>
        /// Records the completion of a coroutine.
        /// </summary>
        /// <param name="duration">How long the coroutine ran.</param>
        /// <param name="tag">Optional tag for the coroutine.</param>
        internal void RecordCompletion(TimeSpan duration, string tag = null)
        {
            if (_isDisposed)
                return;

            lock (_lockObject)
            {
                _totalCompleted++;
                _currentActive--;
                _totalDurationTicks += duration.Ticks;
            }

            if (!string.IsNullOrEmpty(tag))
            {
                _tagCounts.AddOrUpdate(tag, 0, (key, value) => Math.Max(0, value - 1));
            }
        }

        /// <summary>
        /// Records the cancellation of a coroutine.
        /// </summary>
        /// <param name="tag">Optional tag for the coroutine.</param>
        internal void RecordCancellation(string tag = null)
        {
            if (_isDisposed)
                return;

            lock (_lockObject)
            {
                _totalCancelled++;
                _currentActive--;
            }

            if (!string.IsNullOrEmpty(tag))
            {
                _tagCounts.AddOrUpdate(tag, 0, (key, value) => Math.Max(0, value - 1));
            }
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, int> GetCoroutineCountsByTag()
        {
            return new Dictionary<string, int>(_tagCounts.Where(kvp => kvp.Value > 0));
        }

        /// <inheritdoc />
        public void Reset()
        {
            if (_isDisposed)
                return;

            lock (_lockObject)
            {
                _totalStarted = 0;
                _totalCompleted = 0;
                _totalCancelled = 0;
                _currentActive = 0;
                _peakActive = 0;
                _totalDurationTicks = 0;
            }

            _tagCounts.Clear();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _tagCounts.Clear();
            _isDisposed = true;
        }
    }
}