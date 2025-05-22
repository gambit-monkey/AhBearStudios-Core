using System;
using System.Collections.Generic;
using System.Linq;

namespace AhBearStudios.Core.Messaging.Data
{
    /// <summary>
    /// Result of a batch message delivery operation.
    /// </summary>
    public sealed class BatchDeliveryResult
    {
        /// <summary>
        /// Gets whether all messages in the batch were delivered successfully.
        /// </summary>
        public bool IsSuccess { get; }
        
        /// <summary>
        /// Gets the total number of messages in the batch.
        /// </summary>
        public int TotalMessages { get; }
        
        /// <summary>
        /// Gets the number of messages that were delivered successfully.
        /// </summary>
        public int SuccessfulDeliveries { get; }
        
        /// <summary>
        /// Gets the number of messages that failed to deliver.
        /// </summary>
        public int FailedDeliveries { get; }
        
        /// <summary>
        /// Gets the individual delivery results.
        /// </summary>
        public IReadOnlyList<DeliveryResult> Results { get; }
        
        /// <summary>
        /// Gets the time when the batch operation completed.
        /// </summary>
        public DateTime CompletionTime { get; }
        
        /// <summary>
        /// Gets the duration of the batch operation.
        /// </summary>
        public TimeSpan Duration { get; }
        
        /// <summary>
        /// Initializes a new instance of the BatchDeliveryResult class.
        /// </summary>
        /// <param name="results">The individual delivery results.</param>
        /// <param name="completionTime">The time when the batch operation completed.</param>
        /// <param name="duration">The duration of the batch operation.</param>
        public BatchDeliveryResult(IReadOnlyList<DeliveryResult> results, DateTime completionTime, TimeSpan duration)
        {
            Results = results ?? throw new ArgumentNullException(nameof(results));
            CompletionTime = completionTime;
            Duration = duration;
            
            TotalMessages = results.Count;
            SuccessfulDeliveries = results.Count(r => r.IsSuccess);
            FailedDeliveries = results.Count(r => !r.IsSuccess);
            IsSuccess = FailedDeliveries == 0;
        }
    }
}