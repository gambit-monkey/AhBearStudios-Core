using System;
using Unity.Collections;
using AhBearStudios.Core.Pooling;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Common.Utilities;

namespace AhBearStudios.Core.Pooling.Models
{
    /// <summary>
    /// Pooled network buffer for FishNet serialization operations.
    /// Implements IPooledObject for proper lifecycle management with production-ready features.
    /// </summary>
    public class PooledNetworkBuffer : IPooledObject, IDisposable
    {
        #region Private Fields
        
        private byte[] _buffer;
        private int _capacity;
        private int _length;
        private bool _disposed;
        private DateTime _activeStartTime;
        private bool _isActive;
        
        // Circuit breaker thresholds
        private const int MaxConsecutiveFailures = 5;
        private const int MaxValidationErrors = 10;
        private const double CorruptionThresholdPercentage = 0.25;
        
        #endregion
        
        #region Public Properties

        /// <summary>
        /// Gets the underlying byte array buffer.
        /// </summary>
        public byte[] Buffer => _buffer;

        /// <summary>
        /// Gets the current length of data in the buffer.
        /// </summary>
        public int Length => _length;

        /// <summary>
        /// Gets the total capacity of the buffer.
        /// </summary>
        public int Capacity => _capacity;

        #endregion
        
        #region IPooledObject Core Properties

        /// <summary>
        /// Gets or sets the pool name for tracking purposes.
        /// </summary>
        public string PoolName { get; set; } = "NetworkBuffer";
        
        /// <summary>
        /// Gets or sets the unique identifier for this pooled object instance.
        /// </summary>
        public Guid PoolId { get; set; }

        /// <summary>
        /// Gets or sets the last used timestamp.
        /// </summary>
        public DateTime LastUsed { get; set; }
        
        /// <summary>
        /// Gets or sets the timestamp when this object was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// Gets or sets the number of times this object has been used.
        /// </summary>
        public long UseCount { get; set; }
        
        /// <summary>
        /// Gets or sets the total time this object has been active (in use).
        /// </summary>
        public TimeSpan TotalActiveTime { get; set; }
        
        /// <summary>
        /// Gets or sets the timestamp of the last validation check.
        /// </summary>
        public DateTime LastValidationTime { get; set; }
        
        /// <summary>
        /// Gets or sets the priority for pool eviction decisions.
        /// Higher values indicate higher priority to keep in pool.
        /// </summary>
        public int Priority { get; set; }
        
        /// <summary>
        /// Gets or sets the number of validation errors encountered.
        /// </summary>
        public int ValidationErrorCount { get; set; }
        
        /// <summary>
        /// Gets or sets whether corruption has been detected in this object.
        /// </summary>
        public bool CorruptionDetected { get; set; }
        
        /// <summary>
        /// Gets or sets the number of consecutive failures for this object.
        /// </summary>
        public int ConsecutiveFailures { get; set; }

        #endregion

        #region Constructor
        
        /// <summary>
        /// Initializes a new PooledNetworkBuffer with the specified capacity.
        /// </summary>
        /// <param name="capacity">Initial buffer capacity</param>
        public PooledNetworkBuffer(int capacity = 4096)
        {
            _capacity = capacity;
            _buffer = new byte[capacity];
            _length = 0;
            
            var now = DateTime.UtcNow;
            PoolId = DeterministicIdGenerator.GeneratePooledObjectId("PooledNetworkBuffer", "NetworkBuffer", _capacity);
            CreatedAt = now;
            LastUsed = now;
            LastValidationTime = now;
            
            // Set default priority based on buffer size
            Priority = capacity switch
            {
                <= 1024 => 1,      // Small buffers - low priority
                <= 16384 => 2,     // Medium buffers - medium priority
                _ => 3             // Large buffers - high priority
            };
        }
        
        #endregion
        
        #region Public Methods

        /// <summary>
        /// Sets the data in the buffer.
        /// </summary>
        /// <param name="data">Data to set</param>
        /// <param name="offset">Offset in source data</param>
        /// <param name="count">Number of bytes to copy</param>
        public void SetData(byte[] data, int offset = 0, int count = -1)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (count == -1)
                count = data.Length - offset;

            EnsureCapacity(count);
            Array.Copy(data, offset, _buffer, 0, count);
            _length = count;
        }

        /// <summary>
        /// Sets the data in the buffer from a ReadOnlySpan.
        /// </summary>
        /// <param name="data">Data to set</param>
        public void SetData(ReadOnlySpan<byte> data)
        {
            EnsureCapacity(data.Length);
            data.CopyTo(_buffer.AsSpan());
            _length = data.Length;
        }

        /// <summary>
        /// Gets the data as a ReadOnlySpan.
        /// </summary>
        /// <returns>ReadOnlySpan containing the buffer data</returns>
        public ReadOnlySpan<byte> GetData()
        {
            return _buffer.AsSpan(0, _length);
        }

        /// <summary>
        /// Gets the data as a byte array (creates a copy).
        /// </summary>
        /// <returns>Copy of the buffer data</returns>
        public byte[] ToArray()
        {
            var result = new byte[_length];
            Array.Copy(_buffer, 0, result, 0, _length);
            return result;
        }

        /// <summary>
        /// Ensures the buffer has at least the specified capacity.
        /// </summary>
        /// <param name="requiredCapacity">Required capacity</param>
        public void EnsureCapacity(int requiredCapacity)
        {
            if (_capacity >= requiredCapacity)
                return;

            // Grow buffer by doubling until we meet the requirement
            var newCapacity = Math.Max(_capacity * 2, requiredCapacity);
            var newBuffer = new byte[newCapacity];
            
            if (_length > 0)
            {
                Array.Copy(_buffer, 0, newBuffer, 0, _length);
            }

            _buffer = newBuffer;
            _capacity = newCapacity;
        }

        #endregion
        
        #region IPooledObject Implementation

        /// <summary>
        /// Called when the object is retrieved from the pool.
        /// </summary>
        public void OnGet()
        {
            var now = DateTime.UtcNow;
            LastUsed = now;
            _activeStartTime = now;
            _isActive = true;
            UseCount++;
            _length = 0; // Reset length but keep buffer allocated
            
            // Reset failure counter on successful get
            ConsecutiveFailures = 0;
        }

        /// <summary>
        /// Called when the object is returned to the pool.
        /// </summary>
        public void OnReturn()
        {
            var now = DateTime.UtcNow;
            LastUsed = now;
            
            // Calculate and update active time
            if (_isActive)
            {
                var sessionTime = now - _activeStartTime;
                TotalActiveTime = TotalActiveTime.Add(sessionTime);
                _isActive = false;
            }
            
            // Don't clear the buffer to avoid allocations, just reset length
            _length = 0;
            
            // Validate the object state on return
            if (!IsValid())
            {
                ValidationErrorCount++;
                ConsecutiveFailures++;
            }
        }

        /// <summary>
        /// Resets the buffer state for reuse.
        /// </summary>
        public void Reset()
        {
            _length = 0;
            ConsecutiveFailures = 0;
            _isActive = false;
            // Keep buffer allocated to avoid GC pressure
        }

        /// <summary>
        /// Validates that the buffer is in a valid state.
        /// </summary>
        /// <returns>True if valid</returns>
        public bool IsValid()
        {
            LastValidationTime = DateTime.UtcNow;
            
            // Basic structural validation
            if (_disposed || _buffer == null || _capacity <= 0 || _length < 0 || _length > _capacity)
            {
                return false;
            }
            
            // Check for corruption based on validation error rate
            if (UseCount > 0)
            {
                var errorRate = (double)ValidationErrorCount / UseCount;
                if (errorRate > CorruptionThresholdPercentage)
                {
                    CorruptionDetected = true;
                    return false;
                }
            }
            
            // Validate buffer array length matches capacity
            if (_buffer.Length != _capacity)
            {
                CorruptionDetected = true;
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Gets the estimated memory usage of this object in bytes.
        /// </summary>
        /// <returns>Estimated memory footprint in bytes</returns>
        public long GetEstimatedMemoryUsage()
        {
            // Base object overhead (approximately)
            long memoryUsage = 64; // Base object size
            
            // Buffer array
            if (_buffer != null)
            {
                memoryUsage += _buffer.Length + 24; // Array overhead
            }
            
            // Properties and fields
            memoryUsage += 16; // Guid
            memoryUsage += 8 * 5; // DateTimes (5 instances)
            memoryUsage += 8; // TimeSpan
            memoryUsage += 8; // long UseCount
            memoryUsage += 4 * 5; // ints (capacity, length, priority, validation errors, failures)
            memoryUsage += 2 * 3; // bools (disposed, active, corruption)
            
            return memoryUsage;
        }
        
        /// <summary>
        /// Gets the health status of this pooled object.
        /// </summary>
        /// <returns>Health status information</returns>
        public HealthStatus GetHealthStatus()
        {
            if (_disposed || CorruptionDetected)
                return HealthStatus.Critical;
                
            if (!IsValid())
                return HealthStatus.Unhealthy;
                
            if (ConsecutiveFailures >= MaxConsecutiveFailures)
                return HealthStatus.Critical;
                
            if (ConsecutiveFailures > 0)
                return HealthStatus.Degraded;
                
            if (ValidationErrorCount >= MaxValidationErrors)
                return HealthStatus.Warning;
                
            return HealthStatus.Healthy;
        }
        
        /// <summary>
        /// Determines if this object can currently be pooled.
        /// </summary>
        /// <returns>True if the object can be returned to the pool</returns>
        public bool CanBePooled()
        {
            // Cannot pool disposed objects
            if (_disposed)
                return false;
                
            // Cannot pool corrupted objects
            if (CorruptionDetected)
                return false;
                
            // Cannot pool if circuit breaker is triggered
            if (ShouldCircuitBreak())
                return false;
                
            // Check basic validity
            return _buffer != null && _capacity > 0;
        }
        
        /// <summary>
        /// Determines if this object should trigger a circuit breaker.
        /// </summary>
        /// <returns>True if circuit breaker should be triggered</returns>
        public bool ShouldCircuitBreak()
        {
            return ConsecutiveFailures >= MaxConsecutiveFailures ||
                   ValidationErrorCount >= MaxValidationErrors ||
                   CorruptionDetected;
        }
        
        /// <summary>
        /// Checks if this object has a critical issue that requires alerting.
        /// </summary>
        /// <returns>True if a critical issue exists</returns>
        public bool HasCriticalIssue()
        {
            return CorruptionDetected ||
                   ConsecutiveFailures >= MaxConsecutiveFailures ||
                   GetHealthStatus() == HealthStatus.Critical;
        }
        
        /// <summary>
        /// Gets an alert message describing any critical issues with this object.
        /// </summary>
        /// <returns>Alert message or null if no issues</returns>
        public FixedString512Bytes? GetAlertMessage()
        {
            if (!HasCriticalIssue())
                return null;
                
            var message = new FixedString512Bytes();
            
            if (CorruptionDetected)
            {
                message.Append($"PooledNetworkBuffer {PoolId}: Corruption detected. ");
            }
            
            if (ConsecutiveFailures >= MaxConsecutiveFailures)
            {
                message.Append($"Max consecutive failures ({ConsecutiveFailures}) reached. ");
            }
            
            if (ValidationErrorCount >= MaxValidationErrors)
            {
                message.Append($"Max validation errors ({ValidationErrorCount}) reached. ");
            }
            
            message.Append($"Health: {GetHealthStatus()}");
            
            return message;
        }
        
        /// <summary>
        /// Gets comprehensive diagnostic information about this object.
        /// </summary>
        /// <returns>Diagnostic data for troubleshooting and monitoring</returns>
        public PooledObjectDiagnostics GetDiagnosticInfo()
        {
            var diagnosticMessage = new FixedString512Bytes();
            diagnosticMessage.Append($"Buffer[{_capacity}b] Uses:{UseCount} ");
            diagnosticMessage.Append($"Active:{TotalActiveTime.TotalSeconds:F1}s ");
            diagnosticMessage.Append($"Errors:{ValidationErrorCount} Failures:{ConsecutiveFailures}");
            
            return new PooledObjectDiagnostics
            {
                Id = PoolId,
                PoolName = new FixedString64Bytes(PoolName),
                HealthStatus = GetHealthStatus(),
                UseCount = UseCount,
                TotalActiveTimeMs = (long)TotalActiveTime.TotalMilliseconds,
                ValidationErrors = ValidationErrorCount,
                ConsecutiveFailures = ConsecutiveFailures,
                MemoryUsageBytes = GetEstimatedMemoryUsage(),
                CreatedAtTicks = CreatedAt.Ticks,
                LastUsedTicks = LastUsed.Ticks,
                DiagnosticMessage = diagnosticMessage,
                IsPoolable = CanBePooled(),
                CorruptionDetected = CorruptionDetected
            };
        }
        
        #endregion

        #region IDisposable Implementation
        
        /// <summary>
        /// Disposes the buffer and releases memory.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                // Mark as disposed first to prevent usage during cleanup
                _disposed = true;
                
                // Update final statistics
                if (_isActive)
                {
                    var sessionTime = DateTime.UtcNow - _activeStartTime;
                    TotalActiveTime = TotalActiveTime.Add(sessionTime);
                    _isActive = false;
                }
                
                // Clear the buffer reference
                _buffer = null;
                _capacity = 0;
                _length = 0;
                
                // Mark as corrupted to prevent reuse
                CorruptionDetected = true;
            }
        }
        
        #endregion
    }
}