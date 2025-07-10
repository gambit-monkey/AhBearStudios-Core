using System;
using AhBearStudios.Core.Coroutine.Interfaces;
using UnityEngine;

namespace AhBearStudios.Core.Coroutine.Handles
{
    /// <summary>
    /// Implementation of ICoroutineHandle for Unity coroutines.
    /// Provides state tracking and control for individual coroutines.
    /// </summary>
    public sealed class CoroutineHandle : ICoroutineHandle
    {
        #region Private Fields

        private readonly int _id;
        private readonly string _tag;
        private readonly ICoroutineRunner _runner;
        private readonly DateTime _startTime;
        
        private UnityEngine.Coroutine _unityCoroutine;
        private bool _isCompleted;
        private bool _isCancelled;
        private bool _isDisposed;

        #endregion

        #region Events

        /// <inheritdoc />
        public event Action<ICoroutineHandle> OnCompleted;

        #endregion

        #region Properties

        /// <inheritdoc />
        public int Id => _id;

        /// <inheritdoc />
        public string Tag => _tag;

        /// <inheritdoc />
        public bool IsRunning => !_isCompleted && !_isCancelled && !_isDisposed && _unityCoroutine != null;

        /// <inheritdoc />
        public bool IsCompleted => _isCompleted;

        /// <inheritdoc />
        public bool IsCancelled => _isCancelled;

        /// <inheritdoc />
        public DateTime StartTime => _startTime;

        /// <inheritdoc />
        public TimeSpan Duration => DateTime.UtcNow - _startTime;

        /// <summary>
        /// Gets the underlying Unity coroutine object.
        /// </summary>
        internal UnityEngine.Coroutine UnityCoroutine => _unityCoroutine;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new coroutine handle.
        /// </summary>
        /// <param name="id">Unique identifier for the coroutine.</param>
        /// <param name="runner">The runner that owns this coroutine.</param>
        /// <param name="tag">Optional tag for the coroutine.</param>
        public CoroutineHandle(int id, ICoroutineRunner runner, string tag = null)
        {
            _id = id;
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _tag = tag;
            _startTime = DateTime.UtcNow;
            _isCompleted = false;
            _isCancelled = false;
            _isDisposed = false;
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public bool Stop()
        {
            if (_isDisposed || _isCompleted || _isCancelled)
                return false;

            return _runner.StopCoroutine(this);
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Sets the Unity coroutine object for this handle.
        /// </summary>
        /// <param name="unityCoroutine">The Unity coroutine to associate with this handle.</param>
        internal void SetUnityCoroutine(UnityEngine.Coroutine unityCoroutine)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(CoroutineHandle));

            _unityCoroutine = unityCoroutine;
        }

        /// <summary>
        /// Marks the coroutine as cancelled.
        /// </summary>
        internal void Cancel()
        {
            if (_isDisposed || _isCompleted || _isCancelled)
                return;

            _isCancelled = true;
            NotifyCompleted();
        }

        /// <summary>
        /// Marks the coroutine as completed.
        /// </summary>
        internal void Complete()
        {
            if (_isDisposed || _isCompleted || _isCancelled)
                return;

            _isCompleted = true;
            NotifyCompleted();
        }

        /// <summary>
        /// Notifies subscribers that the coroutine has completed or been cancelled.
        /// </summary>
        private void NotifyCompleted()
        {
            try
            {
                OnCompleted?.Invoke(this);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception in CoroutineHandle.OnCompleted event: {ex}");
            }
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Disposes of the coroutine handle and stops the coroutine if still running.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            // Stop the coroutine if it's still running
            if (IsRunning)
            {
                Cancel();
            }

            // Clear event subscribers to prevent memory leaks
            OnCompleted = null;
            _unityCoroutine = null;
        }

        #endregion

        #region Object Overrides

        /// <summary>
        /// Returns a string representation of the coroutine handle.
        /// </summary>
        public override string ToString()
        {
            var status = _isCompleted ? "Completed" :
                        _isCancelled ? "Cancelled" :
                        _isDisposed ? "Disposed" :
                        IsRunning ? "Running" : "Stopped";

            var tagInfo = string.IsNullOrEmpty(_tag) ? "" : $" [{_tag}]";
            return $"CoroutineHandle({_id}){tagInfo} - {status} - Duration: {Duration.TotalMilliseconds:F2}ms";
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is CoroutineHandle other && _id == other._id;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override int GetHashCode()
        {
            return _id.GetHashCode();
        }

        #endregion
    }
}