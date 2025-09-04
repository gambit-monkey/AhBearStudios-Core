using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Pooling.Services;

namespace AhBearStudios.Core.Pooling.Services
{
    /// <summary>
    /// Service interface for coordinating pool operations with proper async handling, cancellation, and timeout support.
    /// Extracts the complex Get/Return operation logic from PoolingService for better separation of concerns.
    /// Handles operation monitoring, performance budgets, error recovery, and correlation tracking.
    /// </summary>
    public interface IPoolOperationCoordinator : IDisposable
    {
        #region Synchronous Operations

        /// <summary>
        /// Coordinates getting an object from the specified pool type synchronously.
        /// Handles validation, monitoring, and error recovery.
        /// </summary>
        /// <typeparam name="T">Type that implements IPooledObject</typeparam>
        /// <param name="correlationId">Optional correlation ID for operation tracing</param>
        /// <returns>Object from the pool</returns>
        T CoordinateGet<T>(Guid correlationId = default) where T : class, IPooledObject, new();

        /// <summary>
        /// Coordinates returning an object to its pool synchronously.
        /// Handles validation, cleanup, monitoring, and error recovery.
        /// </summary>
        /// <typeparam name="T">Type that implements IPooledObject</typeparam>
        /// <param name="item">Object to return to the pool</param>
        /// <param name="correlationId">Optional correlation ID for operation tracing</param>
        void CoordinateReturn<T>(T item, Guid correlationId = default) where T : class, IPooledObject, new();

        #endregion

        #region Asynchronous Operations

        /// <summary>
        /// Coordinates getting an object from the specified pool type asynchronously.
        /// Handles validation, monitoring, timeout, cancellation, and error recovery.
        /// </summary>
        /// <typeparam name="T">Type that implements IPooledObject</typeparam>
        /// <param name="correlationId">Optional correlation ID for operation tracing</param>
        /// <returns>Object from the pool</returns>
        UniTask<T> CoordinateGetAsync<T>(Guid correlationId = default) where T : class, IPooledObject, new();

        /// <summary>
        /// Coordinates getting an object from the specified pool type asynchronously with cancellation support.
        /// </summary>
        /// <typeparam name="T">Type that implements IPooledObject</typeparam>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <param name="correlationId">Optional correlation ID for operation tracing</param>
        /// <returns>Object from the pool</returns>
        UniTask<T> CoordinateGetAsync<T>(CancellationToken cancellationToken, Guid correlationId = default) where T : class, IPooledObject, new();

        /// <summary>
        /// Coordinates getting an object from the specified pool type asynchronously with timeout and cancellation support.
        /// </summary>
        /// <typeparam name="T">Type that implements IPooledObject</typeparam>
        /// <param name="timeout">Maximum time to wait for an object</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <param name="correlationId">Optional correlation ID for operation tracing</param>
        /// <returns>Object from the pool</returns>
        UniTask<T> CoordinateGetAsync<T>(TimeSpan timeout, CancellationToken cancellationToken, Guid correlationId = default) where T : class, IPooledObject, new();

        /// <summary>
        /// Coordinates returning an object to its pool asynchronously.
        /// Handles validation, cleanup, monitoring, cancellation, and error recovery.
        /// </summary>
        /// <typeparam name="T">Type that implements IPooledObject</typeparam>
        /// <param name="item">Object to return to the pool</param>
        /// <param name="correlationId">Optional correlation ID for operation tracing</param>
        UniTask CoordinateReturnAsync<T>(T item, Guid correlationId = default) where T : class, IPooledObject, new();

        /// <summary>
        /// Coordinates returning an object to its pool asynchronously with cancellation support.
        /// </summary>
        /// <typeparam name="T">Type that implements IPooledObject</typeparam>
        /// <param name="item">Object to return to the pool</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <param name="correlationId">Optional correlation ID for operation tracing</param>
        UniTask CoordinateReturnAsync<T>(T item, CancellationToken cancellationToken, Guid correlationId = default) where T : class, IPooledObject, new();

        #endregion

        #region Batch Operations

        /// <summary>
        /// Coordinates getting multiple objects from the specified pool type.
        /// More efficient than multiple individual get operations.
        /// </summary>
        /// <typeparam name="T">Type that implements IPooledObject</typeparam>
        /// <param name="count">Number of objects to get</param>
        /// <param name="correlationId">Optional correlation ID for operation tracing</param>
        /// <returns>Array of objects from the pool</returns>
        T[] CoordinateGetBatch<T>(int count, Guid correlationId = default) where T : class, IPooledObject, new();

        /// <summary>
        /// Coordinates returning multiple objects to their pool.
        /// More efficient than multiple individual return operations.
        /// </summary>
        /// <typeparam name="T">Type that implements IPooledObject</typeparam>
        /// <param name="items">Objects to return to the pool</param>
        /// <param name="correlationId">Optional correlation ID for operation tracing</param>
        void CoordinateReturnBatch<T>(T[] items, Guid correlationId = default) where T : class, IPooledObject, new();

        /// <summary>
        /// Coordinates getting multiple objects from the specified pool type asynchronously.
        /// </summary>
        /// <typeparam name="T">Type that implements IPooledObject</typeparam>
        /// <param name="count">Number of objects to get</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <param name="correlationId">Optional correlation ID for operation tracing</param>
        /// <returns>Array of objects from the pool</returns>
        UniTask<T[]> CoordinateGetBatchAsync<T>(int count, CancellationToken cancellationToken = default, Guid correlationId = default) where T : class, IPooledObject, new();

        /// <summary>
        /// Coordinates returning multiple objects to their pool asynchronously.
        /// </summary>
        /// <typeparam name="T">Type that implements IPooledObject</typeparam>
        /// <param name="items">Objects to return to the pool</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <param name="correlationId">Optional correlation ID for operation tracing</param>
        UniTask CoordinateReturnBatchAsync<T>(T[] items, CancellationToken cancellationToken = default, Guid correlationId = default) where T : class, IPooledObject, new();

        #endregion

        #region Operation Validation

        /// <summary>
        /// Validates that a pool operation can be performed.
        /// Checks circuit breaker state, pool health, and operation constraints.
        /// </summary>
        /// <typeparam name="T">Type that implements IPooledObject</typeparam>
        /// <param name="operationType">Type of operation (get, return, etc.)</param>
        /// <param name="correlationId">Optional correlation ID for operation tracing</param>
        /// <returns>True if the operation should be allowed</returns>
        bool ValidateOperation<T>(string operationType, Guid correlationId = default) where T : class, IPooledObject, new();

        /// <summary>
        /// Validates that a pooled object is suitable for return to pool.
        /// Checks object state, corruption, and validity.
        /// </summary>
        /// <typeparam name="T">Type that implements IPooledObject</typeparam>
        /// <param name="item">Object to validate</param>
        /// <param name="correlationId">Optional correlation ID for operation tracing</param>
        /// <returns>True if the object can be safely returned to pool</returns>
        bool ValidateObjectForReturn<T>(T item, Guid correlationId = default) where T : class, IPooledObject, new();

        #endregion

        #region Performance Monitoring

        /// <summary>
        /// Gets performance metrics for pool operations.
        /// </summary>
        /// <returns>Performance metrics data</returns>
        object GetPerformanceMetrics();

        /// <summary>
        /// Resets performance counters and metrics.
        /// </summary>
        void ResetPerformanceMetrics();

        #endregion

        #region Configuration

        /// <summary>
        /// Updates the default timeout for async operations.
        /// </summary>
        /// <param name="timeout">New default timeout</param>
        void UpdateDefaultTimeout(TimeSpan timeout);

        /// <summary>
        /// Updates the maximum retry attempts for failed operations.
        /// </summary>
        /// <param name="maxRetries">New maximum retry count</param>
        void UpdateMaxRetryAttempts(int maxRetries);

        /// <summary>
        /// Enables or disables operation validation.
        /// </summary>
        /// <param name="enableValidation">Whether to enable validation</param>
        void SetValidationEnabled(bool enableValidation);

        #endregion
    }
}