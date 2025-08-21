using System;
using System.Diagnostics;
using System.Threading;
using MessagePipe;
using Unity.Profiling;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Profiling;

namespace AhBearStudios.Core.Messaging.Filters;

/// <summary>
/// MessagePipe filter that provides comprehensive performance monitoring and Unity Profiler integration.
/// Essential for Unity game development to track message processing performance,
/// memory allocations, and maintain frame budget compliance.
/// </summary>
/// <typeparam name="TMessage">The message type implementing IMessage</typeparam>
public sealed class MetricsFilter<TMessage> : MessageHandlerFilter<TMessage>
    where TMessage : IMessage
{
    private readonly ILoggingService _logger;
    private readonly IProfilerService _profilerService;
    private readonly ProfilerMarker _filterMarker;
    private readonly ProfilerMarker _processingMarker;
    private readonly ProfilerMarker _messageCountMarker;
    private readonly ProfilerMarker _processingTimeMarker;
    private readonly bool _enableDetailedLogging;
    
    private static readonly ProfilerMarker _staticFilterMarker = new("MetricsFilter.Handle");
    
    // Counters for metrics tracking
    private long _messageCount;
    private long _totalProcessingTimeNs;

    /// <summary>
    /// Gets the total number of messages processed by this filter.
    /// </summary>
    public long MessageCount => Interlocked.Read(ref _messageCount);

    /// <summary>
    /// Gets the total processing time in nanoseconds for all messages.
    /// </summary>
    public long TotalProcessingTimeNanoseconds => Interlocked.Read(ref _totalProcessingTimeNs);

    /// <summary>
    /// Gets the average processing time in microseconds per message.
    /// </summary>
    public double AverageProcessingTimeMicroseconds
    {
        get
        {
            var count = MessageCount;
            return count > 0 ? (double)TotalProcessingTimeNanoseconds / (count * 1000.0) : 0.0;
        }
    }

    /// <summary>
    /// Initializes a new MetricsFilter with Unity Profiler integration and performance monitoring.
    /// </summary>
    /// <param name="logger">Optional logging service for metrics output</param>
    /// <param name="profilerService">Optional profiler service for advanced metrics</param>
    /// <param name="enableDetailedLogging">Whether to enable detailed performance logging (default: false)</param>
    public MetricsFilter(
        ILoggingService logger = null, 
        IProfilerService profilerService = null,
        bool enableDetailedLogging = false)
    {
        _logger = logger;
        _profilerService = profilerService;
        _enableDetailedLogging = enableDetailedLogging;
        
        var messageTypeName = typeof(TMessage).Name;
        _filterMarker = new ProfilerMarker($"MetricsFilter<{messageTypeName}>.Handle");
        _processingMarker = new ProfilerMarker($"MessageProcessing<{messageTypeName}>");
        _messageCountMarker = new ProfilerMarker($"Messages<{messageTypeName}>");
        _processingTimeMarker = new ProfilerMarker($"ProcessingTime<{messageTypeName}>");
    }

    /// <summary>
    /// Handles message processing with comprehensive performance monitoring.
    /// Tracks processing time, memory allocations, and Unity Profiler integration.
    /// </summary>
    /// <param name="message">The message to process</param>
    /// <param name="next">The next handler in the filter chain</param>
    public override void Handle(TMessage message, Action<TMessage> next)
    {
        using (_staticFilterMarker.Auto())
        using (_filterMarker.Auto())
        {
            if (message == null)
            {
                _logger?.LogWarning($"MetricsFilter<{typeof(TMessage).Name}>: Received null message");
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var startMemory = GC.GetTotalMemory(false);
            
            // Start profiler scope if available
            using var profilerScope = _profilerService?.BeginScope($"Message-{typeof(TMessage).Name}-{message.Id:N}");
            
            try
            {
                using (_processingMarker.Auto())
                {
                    // Process the message
                    next(message);
                }
                
                stopwatch.Stop();
                
                // Record successful processing metrics
                RecordSuccessMetrics(message, stopwatch.Elapsed, startMemory);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                // Record failure metrics
                RecordFailureMetrics(message, stopwatch.Elapsed, startMemory, ex);
                
                // Re-throw to maintain filter chain behavior
                throw;
            }
        }
    }

    /// <summary>
    /// Records metrics for successful message processing.
    /// </summary>
    private void RecordSuccessMetrics(TMessage message, TimeSpan processingTime, long startMemory)
    {
        var endMemory = GC.GetTotalMemory(false);
        var memoryDelta = endMemory - startMemory;
        
        // Update internal counters and Unity Profiler markers
        Interlocked.Increment(ref _messageCount);
        Interlocked.Add(ref _totalProcessingTimeNs, (long)processingTime.TotalNanoseconds);
        
        // Use ProfilerMarker.Begin/End for Unity Profiler integration
        _messageCountMarker.Begin();
        _messageCountMarker.End();
        
        _processingTimeMarker.Begin((int)processingTime.TotalMicroseconds);
        _processingTimeMarker.End();
        
        // Record in profiler service if available
        _profilerService?.RecordMetric("MessageProcessing", typeof(TMessage).Name, processingTime.TotalMilliseconds);
        
        if (memoryDelta > 0)
        {
            _profilerService?.RecordMetric("MessageMemoryAllocation", typeof(TMessage).Name, memoryDelta);
        }
        
        // Detailed logging if enabled
        if (_enableDetailedLogging)
        {
            _logger?.LogDebug($"MetricsFilter<{typeof(TMessage).Name}>: Processed message {message.Id} " +
                            $"in {processingTime.TotalMicroseconds:F2}μs, " +
                            $"memory delta: {memoryDelta} bytes");
        }
        
        // Warning for slow processing (potential frame budget impact)
        if (processingTime.TotalMilliseconds > 1.0) // 1ms threshold
        {
            _logger?.LogWarning($"MetricsFilter<{typeof(TMessage).Name}>: Slow message processing detected - " +
                              $"message {message.Id} took {processingTime.TotalMilliseconds:F2}ms " +
                              $"(may impact 60 FPS target)");
        }
        
        // Warning for memory allocations
        if (memoryDelta > 1024) // 1KB threshold
        {
            _logger?.LogWarning($"MetricsFilter<{typeof(TMessage).Name}>: High memory allocation detected - " +
                              $"message {message.Id} allocated {memoryDelta} bytes");
        }
    }

    /// <summary>
    /// Records metrics for failed message processing.
    /// </summary>
    private void RecordFailureMetrics(TMessage message, TimeSpan processingTime, long startMemory, Exception exception)
    {
        var endMemory = GC.GetTotalMemory(false);
        var memoryDelta = endMemory - startMemory;
        
        // Record failure in profiler service if available
        _profilerService?.RecordMetric("MessageProcessingFailure", typeof(TMessage).Name, 1);
        _profilerService?.RecordMetric("MessageProcessingFailureTime", typeof(TMessage).Name, processingTime.TotalMilliseconds);
        
        // Log failure with detailed information
        _logger?.LogException($"MetricsFilter<{typeof(TMessage).Name}>: Failed to process message {message.Id} " +
                            $"after {processingTime.TotalMicroseconds:F2}μs, " +
                            $"memory delta: {memoryDelta} bytes", exception);
    }
}