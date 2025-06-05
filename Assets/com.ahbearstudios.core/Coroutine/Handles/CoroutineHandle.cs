using System;
using AhBearStudios.Core.Coroutine.Interfaces;

namespace AhBearStudios.Core.Coroutine.Handles
{
    /// <summary>
    /// Implementation of ICoroutineHandle that tracks coroutine state and provides control.
    /// </summary>
    internal sealed class CoroutineHandle : ICoroutineHandle
    {
        private readonly int _id;
        private readonly string _tag;
        private readonly DateTime _startTime;
        private readonly Action<CoroutineHandle> _onDispose;
        
        private Coroutine _coroutine;
        private bool _isCompleted;
        private bool _isCancelled;
        private bool _isDisposed;

        /// <summary>
        /// Event fired when the coroutine completes or is cancelled.
        /// </summary>
        public event Action<ICoroutineHandle> OnCompleted;

        /// <inheritdoc />
        public int Id => _id;

        /// <inheritdoc />
        public string Tag => _tag;

        /// <inheritdoc />
        public bool IsRunning => !_isCompleted && !_isCancelled && _coroutine != null;

        /// <inheritdoc />
        public bool IsCompleted => _isCompleted;

        /// <inheritdoc />
        public bool IsCancelled => _isCancelled;

        /// <inheritdoc />
        public DateTime StartTime => _startTime;

        /// <inheritdoc />
        public TimeSpan Duration => DateTime.UtcNow - _startTime;

        /// <summary>
        /// Initializes a new coroutine handle.
        /// </summary>
        /// <param name="id">Unique identifier for the coroutine.</param>
        /// <param name="tag">Optional tag for categorizing the coroutine.</param>
        /// <param name="coroutine">The Unity coroutine instance.</param>
        /// <param name="onDispose">Callback to invoke when this handle is disposed.</param>
        internal CoroutineHandle(int id, string tag, Coroutine coroutine, Action<CoroutineHandle> onDispose)
        {
            _id = id;
            _tag = tag;
            _coroutine = coroutine ?? throw new ArgumentNullException(nameof(coroutine));
            _onDispose = onDispose;
            _startTime = DateTime.UtcNow;
        }

        /// <inheritdoc />
        public bool Stop()
        {
            if (_isCompleted || _isCancelled || _coroutine == null)
                return false;

            _isCancelled = true;
            return true;
        }

        /// <summary>
        /// Marks the coroutine as completed.
        /// </summary>
        internal void MarkCompleted()
        {
            if (_isCompleted || _isCancelled)
                return;

            _isCompleted = true;
            OnCompleted?.Invoke(this);
        }

        /// <summary>
        /// Marks the coroutine as cancelled.
        /// </summary>
        internal void MarkCancelled()
        {
            if (_isCompleted || _isCancelled)
                return;

            _isCancelled = true;
            OnCompleted?.Invoke(this);
        }

        /// <summary>
        /// Sets the Unity coroutine instance.
        /// </summary>
        /// <param name="coroutine">The Unity coroutine instance.</param>
        internal void SetCoroutine(Coroutine coroutine)
        {
            _coroutine = coroutine;
        }

        /// <summary>
        /// Gets the Unity coroutine instance.
        /// </summary>
        internal Coroutine GetCoroutine()
        {
            return _coroutine;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_isDisposed)
                return;

            Stop();
            _onDispose?.Invoke(this);
            _coroutine = null;
            _isDisposed = true;
        }
    }
}