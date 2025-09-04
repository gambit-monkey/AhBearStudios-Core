using System;
using AhBearStudios.Core.Pooling.Pools;
using Unity.Collections;
using Cysharp.Threading.Tasks;

namespace AhBearStudios.Core.Pooling.Services
{
    /// <summary>
    /// Service interface for publishing pool-related messages.
    /// Centralizes message creation and publishing with proper ID generation and correlation tracking.
    /// Follows CLAUDE.md message patterns for consistent pool event communication.
    /// </summary>
    public interface IPoolMessagePublisher : IDisposable
    {
        #region Object Lifecycle Messages

        /// <summary>
        /// Publishes a message when an object is retrieved from a pool.
        /// </summary>
        /// <typeparam name="T">Type of pooled object</typeparam>
        /// <param name="item">Retrieved object</param>
        /// <param name="pool">Source pool</param>
        /// <param name="correlationId">Optional correlation ID for message tracing</param>
        UniTask PublishObjectRetrievedAsync<T>(T item, IObjectPool<T> pool, Guid correlationId = default) 
            where T : class, IPooledObject;

        /// <summary>
        /// Publishes a message when an object is returned to a pool.
        /// </summary>
        /// <typeparam name="T">Type of pooled object</typeparam>
        /// <param name="item">Returned object</param>
        /// <param name="pool">Destination pool</param>
        /// <param name="wasValid">Whether the object was valid when returned</param>
        /// <param name="correlationId">Optional correlation ID for message tracing</param>
        UniTask PublishObjectReturnedAsync<T>(T item, IObjectPool<T> pool, bool wasValid, Guid correlationId = default) 
            where T : class, IPooledObject;

        #endregion

        #region Pool Status Messages

        /// <summary>
        /// Publishes a message when a pool reaches its capacity limits.
        /// </summary>
        /// <param name="poolName">Name of the pool</param>
        /// <param name="poolType">Type of objects in the pool</param>
        /// <param name="currentCapacity">Current pool capacity</param>
        /// <param name="maxCapacity">Maximum pool capacity</param>
        /// <param name="correlationId">Optional correlation ID for message tracing</param>
        UniTask PublishCapacityReachedAsync(
            string poolName, 
            string poolType, 
            int currentCapacity, 
            int maxCapacity, 
            Guid correlationId = default);

        /// <summary>
        /// Publishes a message when pool validation detects issues.
        /// </summary>
        /// <param name="poolName">Name of the pool with issues</param>
        /// <param name="poolType">Type of objects in the pool</param>
        /// <param name="issueCount">Number of issues found</param>
        /// <param name="objectsValidated">Total objects validated</param>
        /// <param name="correlationId">Optional correlation ID for message tracing</param>
        UniTask PublishValidationIssuesAsync(
            string poolName, 
            string poolType, 
            int issueCount, 
            int objectsValidated, 
            Guid correlationId = default);

        #endregion

        #region Pool Operations Messages

        /// <summary>
        /// Publishes a message when a pool operation starts.
        /// </summary>
        /// <param name="operationType">Type of operation (get, return, validate, etc.)</param>
        /// <param name="poolName">Name of the pool</param>
        /// <param name="poolType">Type of objects in the pool</param>
        /// <param name="operationId">Unique operation identifier</param>
        /// <param name="correlationId">Optional correlation ID for message tracing</param>
        UniTask PublishOperationStartedAsync(
            string operationType, 
            string poolName, 
            string poolType, 
            Guid operationId, 
            Guid correlationId = default);

        /// <summary>
        /// Publishes a message when a pool operation completes successfully.
        /// </summary>
        /// <param name="operationType">Type of operation that completed</param>
        /// <param name="poolName">Name of the pool</param>
        /// <param name="poolType">Type of objects in the pool</param>
        /// <param name="operationId">Unique operation identifier</param>
        /// <param name="duration">Operation duration</param>
        /// <param name="correlationId">Optional correlation ID for message tracing</param>
        UniTask PublishOperationCompletedAsync(
            string operationType, 
            string poolName, 
            string poolType, 
            Guid operationId, 
            TimeSpan duration, 
            Guid correlationId = default);

        /// <summary>
        /// Publishes a message when a pool operation fails.
        /// </summary>
        /// <param name="operationType">Type of operation that failed</param>
        /// <param name="poolName">Name of the pool</param>
        /// <param name="poolType">Type of objects in the pool</param>
        /// <param name="operationId">Unique operation identifier</param>
        /// <param name="exception">Exception that caused the failure</param>
        /// <param name="correlationId">Optional correlation ID for message tracing</param>
        UniTask PublishOperationFailedAsync(
            string operationType, 
            string poolName, 
            string poolType, 
            Guid operationId, 
            Exception exception, 
            Guid correlationId = default);

        #endregion

        #region Pool Scaling Messages

        /// <summary>
        /// Publishes a message when a pool expands its size.
        /// </summary>
        /// <param name="poolName">Name of the expanding pool</param>
        /// <param name="poolType">Type of objects in the pool</param>
        /// <param name="oldSize">Previous pool size</param>
        /// <param name="newSize">New pool size</param>
        /// <param name="reason">Reason for expansion</param>
        /// <param name="correlationId">Optional correlation ID for message tracing</param>
        UniTask PublishPoolExpansionAsync(
            string poolName, 
            string poolType, 
            int oldSize, 
            int newSize, 
            string reason, 
            Guid correlationId = default);

        /// <summary>
        /// Publishes a message when a pool contracts its size.
        /// </summary>
        /// <param name="poolName">Name of the contracting pool</param>
        /// <param name="poolType">Type of objects in the pool</param>
        /// <param name="oldSize">Previous pool size</param>
        /// <param name="newSize">New pool size</param>
        /// <param name="reason">Reason for contraction</param>
        /// <param name="correlationId">Optional correlation ID for message tracing</param>
        UniTask PublishPoolContractionAsync(
            string poolName, 
            string poolType, 
            int oldSize, 
            int newSize, 
            string reason, 
            Guid correlationId = default);

        /// <summary>
        /// Publishes a message when buffer exhaustion is detected.
        /// </summary>
        /// <param name="poolName">Name of the exhausted pool</param>
        /// <param name="poolType">Type of objects in the pool</param>
        /// <param name="requestedCount">Number of objects requested</param>
        /// <param name="availableCount">Number of objects available</param>
        /// <param name="correlationId">Optional correlation ID for message tracing</param>
        UniTask PublishBufferExhaustionAsync(
            string poolName, 
            string poolType, 
            int requestedCount, 
            int availableCount, 
            Guid correlationId = default);

        #endregion

        #region Health and Performance Messages

        /// <summary>
        /// Publishes a message reporting pool strategy health status.
        /// </summary>
        /// <param name="strategyName">Name of the pool strategy</param>
        /// <param name="poolName">Name of the pool</param>
        /// <param name="healthStatus">Current health status</param>
        /// <param name="performanceMetrics">Performance metrics</param>
        /// <param name="correlationId">Optional correlation ID for message tracing</param>
        UniTask PublishStrategyHealthStatusAsync(
            string strategyName, 
            string poolName, 
            string healthStatus, 
            string performanceMetrics, 
            Guid correlationId = default);

        #endregion
    }
}