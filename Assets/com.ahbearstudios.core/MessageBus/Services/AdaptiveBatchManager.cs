using System;
using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Logging.Interfaces;
using AhBearStudios.Core.MessageBus.Data;

namespace AhBearStudios.Core.MessageBus.Services
{
    /// <summary>
    /// Manager for adaptive batch sizing based on throughput metrics.
    /// </summary>
    internal sealed class AdaptiveBatchManager
    {
        private readonly ILoggingService _logger;
        private readonly int _targetThroughput;
        private readonly Queue<ThroughputSample> _samples = new Queue<ThroughputSample>();
        private readonly object _samplesLock = new object();
        
        private int _currentBatchSize;
        private DateTime _lastAdjustment = DateTime.UtcNow;
        private readonly TimeSpan _adjustmentInterval = TimeSpan.FromSeconds(10);
        
        /// <summary>
        /// Gets the current optimal batch size.
        /// </summary>
        public int CurrentBatchSize => _currentBatchSize;
        
        /// <summary>
        /// Initializes a new instance of the AdaptiveBatchManager class.
        /// </summary>
        /// <param name="initialBatchSize">The initial batch size.</param>
        /// <param name="targetThroughput">The target throughput in messages per second.</param>
        /// <param name="logger">The logger to use for logging.</param>
        public AdaptiveBatchManager(int initialBatchSize, int targetThroughput, ILoggingService logger)
        {
            _currentBatchSize = initialBatchSize;
            _targetThroughput = targetThroughput;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Records a throughput sample for adaptive batch sizing.
        /// </summary>
        /// <param name="messageCount">The number of messages processed.</param>
        /// <param name="processingTime">The time taken to process the messages.</param>
        public void RecordSample(int messageCount, TimeSpan processingTime)
        {
            if (messageCount <= 0 || processingTime <= TimeSpan.Zero)
                return;
            
            var throughput = messageCount / processingTime.TotalSeconds;
            var sample = new ThroughputSample(DateTime.UtcNow, throughput, _currentBatchSize);
            
            lock (_samplesLock)
            {
                _samples.Enqueue(sample);
                
                // Keep only the last 100 samples
                while (_samples.Count > 100)
                {
                    _samples.Dequeue();
                }
            }
            
            // Check if it's time to adjust batch size
            if (DateTime.UtcNow - _lastAdjustment >= _adjustmentInterval)
            {
                AdjustBatchSize();
                _lastAdjustment = DateTime.UtcNow;
            }
        }
        
        private void AdjustBatchSize()
        {
            lock (_samplesLock)
            {
                if (_samples.Count < 5) // Need at least 5 samples
                    return;
                
                var recentSamples = _samples.TakeLast(10).ToList();
                var averageThroughput = recentSamples.Average(s => s.Throughput);
                
                var previousBatchSize = _currentBatchSize;
                
                if (averageThroughput < _targetThroughput * 0.8) // Below 80% of target
                {
                    // Increase batch size to improve throughput
                    _currentBatchSize = Math.Min(_currentBatchSize + 10, 500);
                }
                else if (averageThroughput > _targetThroughput * 1.2) // Above 120% of target
                {
                    // Decrease batch size to avoid over-processing
                    _currentBatchSize = Math.Max(_currentBatchSize - 5, 10);
                }
                
                if (_currentBatchSize != previousBatchSize)
                {
                    _logger.Log(LogLevel.Debug,
                        $"Adjusted batch size from {previousBatchSize} to {_currentBatchSize} " +
                        $"(avg throughput: {averageThroughput:F1}/s, target: {_targetThroughput}/s)",
                        "AdaptiveBatchManager");
                }
            }
        }
    }
}