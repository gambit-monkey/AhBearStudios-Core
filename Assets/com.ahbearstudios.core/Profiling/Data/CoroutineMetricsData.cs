using System;
using Unity.Collections;
using Unity.Mathematics;

namespace AhBearStudios.Core.Profiling.Data
{
    /// <summary>
    /// Burst-compatible structure containing coroutine metrics data
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    public struct CoroutineMetricsData : IEquatable<CoroutineMetricsData>
    {
        // Runner identification
        public FixedString128Bytes RunnerName;
        public FixedString64Bytes RunnerId;
        public FixedString64Bytes RunnerType;
        
        // Coroutine counts and usage statistics
        public int ActiveCoroutines;
        public int PeakActiveCoroutines;
        public long TotalCoroutinesStarted;
        public long TotalCoroutinesCompleted;
        public long TotalCoroutinesCancelled;
        public long TotalCoroutinesFailed;
        
        // Performance metrics
        public float AverageExecutionTimeMs;
        public float MaxExecutionTimeMs;
        public float MinExecutionTimeMs;
        public float LastExecutionTimeMs;
        public float TotalExecutionTimeMs;
        public int ExecutionSampleCount;
        
        public float AverageStartupTimeMs;
        public float MaxStartupTimeMs;
        public float MinStartupTimeMs;
        public float TotalStartupTimeMs;
        public int StartupSampleCount;
        
        public float AverageCleanupTimeMs;
        public float MaxCleanupTimeMs;
        public float MinCleanupTimeMs;
        public float TotalCleanupTimeMs;
        public int CleanupSampleCount;
        
        // Tag-based metrics
        public int TaggedCoroutines;
        public int UntaggedCoroutines;
        public int UniqueTagCount;
        
        // Memory and allocation tracking
        public long TotalMemoryBytes;
        public long PeakMemoryBytes;
        public int EstimatedCoroutineOverheadBytes;
        public long TotalGCAllocations;
        
        // Error tracking
        public int ExceptionCount;
        public int TimeoutCount;
        public int CancellationCount;
        
        // Time tracking
        public float LastOperationTime;
        public float LastResetTime;
        public float CreationTime;
        public float UpTimeSeconds;
        
        // Throughput metrics
        public float CoroutinesPerSecond;
        public float CompletionRate;
        public float FailureRate;
        
        /// <summary>
        /// Creates a new CoroutineMetricsData with default values
        /// </summary>
        /// <param name="runnerId">Runner identifier</param>
        /// <param name="runnerName">Runner name</param>
        public CoroutineMetricsData(FixedString64Bytes runnerId, FixedString128Bytes runnerName)
        {
            RunnerId = runnerId;
            RunnerName = runnerName;
            RunnerType = default;
            
            ActiveCoroutines = 0;
            PeakActiveCoroutines = 0;
            TotalCoroutinesStarted = 0;
            TotalCoroutinesCompleted = 0;
            TotalCoroutinesCancelled = 0;
            TotalCoroutinesFailed = 0;
            
            AverageExecutionTimeMs = 0;
            MaxExecutionTimeMs = 0;
            MinExecutionTimeMs = float.MaxValue;
            LastExecutionTimeMs = 0;
            TotalExecutionTimeMs = 0;
            ExecutionSampleCount = 0;
            
            AverageStartupTimeMs = 0;
            MaxStartupTimeMs = 0;
            MinStartupTimeMs = float.MaxValue;
            TotalStartupTimeMs = 0;
            StartupSampleCount = 0;
            
            AverageCleanupTimeMs = 0;
            MaxCleanupTimeMs = 0;
            MinCleanupTimeMs = float.MaxValue;
            TotalCleanupTimeMs = 0;
            CleanupSampleCount = 0;
            
            TaggedCoroutines = 0;
            UntaggedCoroutines = 0;
            UniqueTagCount = 0;
            
            TotalMemoryBytes = 0;
            PeakMemoryBytes = 0;
            EstimatedCoroutineOverheadBytes = 0;
            TotalGCAllocations = 0;
            
            ExceptionCount = 0;
            TimeoutCount = 0;
            CancellationCount = 0;
            
            LastOperationTime = 0;
            LastResetTime = 0;
            CreationTime = 0;
            UpTimeSeconds = 0;
            
            CoroutinesPerSecond = 0;
            CompletionRate = 0;
            FailureRate = 0;
        }
        
        /// <summary>
        /// Gets the total number of coroutines that have finished (completed + cancelled + failed)
        /// </summary>
        public readonly long TotalFinishedCoroutines => TotalCoroutinesCompleted + TotalCoroutinesCancelled + TotalCoroutinesFailed;
        
        /// <summary>
        /// Gets the success rate of coroutines (0-1)
        /// </summary>
        public readonly float SuccessRate => TotalFinishedCoroutines > 0 
            ? (float)TotalCoroutinesCompleted / TotalFinishedCoroutines 
            : 0f;
        
        /// <summary>
        /// Gets the average execution time including startup and cleanup
        /// </summary>
        public readonly float AverageTotalTimeMs => 
            AverageStartupTimeMs + AverageExecutionTimeMs + AverageCleanupTimeMs;
        
        /// <summary>
        /// Gets the efficiency rating of the runner (0-1)
        /// Higher values indicate better resource utilization
        /// </summary>
        public readonly float RunnerEfficiency
        {
            get
            {
                if (TotalCoroutinesStarted == 0) return 0;
                
                float successWeight = SuccessRate * 0.4f;
                float utilizationWeight = ActiveCoroutines > 0 ? 0.3f : 0.1f;
                float performanceWeight = AverageExecutionTimeMs > 0 ? 
                    math.clamp(1.0f - (AverageExecutionTimeMs / 1000.0f), 0.1f, 0.3f) : 0.3f;
                
                return successWeight + utilizationWeight + performanceWeight;
            }
        }
        
        /// <summary>
        /// Records a coroutine start and returns updated metrics
        /// </summary>
        public readonly CoroutineMetricsData RecordStart(float startupTimeMs, bool hasTag, float currentTime)
        {
            var result = this;
            
            // Update counts
            result.ActiveCoroutines++;
            result.PeakActiveCoroutines = math.max(result.PeakActiveCoroutines, result.ActiveCoroutines);
            result.TotalCoroutinesStarted++;
            result.LastOperationTime = currentTime;
            
            // Track creation time
            if (result.CreationTime == 0)
                result.CreationTime = currentTime;
                
            result.UpTimeSeconds = currentTime - result.CreationTime;
            
            // Update startup time metrics
            result.LastExecutionTimeMs = startupTimeMs;
            result.TotalStartupTimeMs += startupTimeMs;
            result.StartupSampleCount++;
            
            result.MaxStartupTimeMs = math.max(result.MaxStartupTimeMs, startupTimeMs);
            result.MinStartupTimeMs = math.min(result.MinStartupTimeMs, startupTimeMs);
            result.AverageStartupTimeMs = result.TotalStartupTimeMs / result.StartupSampleCount;
            
            // Update tag tracking
            if (hasTag)
                result.TaggedCoroutines++;
            else
                result.UntaggedCoroutines++;
            
            // Calculate throughput
            if (result.UpTimeSeconds > 0)
                result.CoroutinesPerSecond = result.TotalCoroutinesStarted / result.UpTimeSeconds;
            
            return result;
        }
        
        /// <summary>
        /// Records a coroutine completion and returns updated metrics
        /// </summary>
        public readonly CoroutineMetricsData RecordCompletion(
            float executionTimeMs, 
            float cleanupTimeMs, 
            bool hasTag, 
            float currentTime)
        {
            var result = this;
            
            // Update state
            result.ActiveCoroutines = math.max(0, result.ActiveCoroutines - 1);
            result.TotalCoroutinesCompleted++;
            result.LastOperationTime = currentTime;
            
            // Track time
            if (result.CreationTime == 0)
                result.CreationTime = currentTime;
                
            result.UpTimeSeconds = currentTime - result.CreationTime;
            
            // Update execution time metrics
            result.LastExecutionTimeMs = executionTimeMs;
            result.TotalExecutionTimeMs += executionTimeMs;
            result.ExecutionSampleCount++;
            
            result.MaxExecutionTimeMs = math.max(result.MaxExecutionTimeMs, executionTimeMs);
            result.MinExecutionTimeMs = math.min(result.MinExecutionTimeMs, executionTimeMs);
            result.AverageExecutionTimeMs = result.TotalExecutionTimeMs / result.ExecutionSampleCount;
            
            // Update cleanup time metrics
            if (cleanupTimeMs > 0)
            {
                result.TotalCleanupTimeMs += cleanupTimeMs;
                result.CleanupSampleCount++;
                
                result.MaxCleanupTimeMs = math.max(result.MaxCleanupTimeMs, cleanupTimeMs);
                result.MinCleanupTimeMs = math.min(result.MinCleanupTimeMs, cleanupTimeMs);
                result.AverageCleanupTimeMs = result.TotalCleanupTimeMs / result.CleanupSampleCount;
            }
            
            // Update tag tracking
            if (hasTag)
                result.TaggedCoroutines = math.max(0, result.TaggedCoroutines - 1);
            else
                result.UntaggedCoroutines = math.max(0, result.UntaggedCoroutines - 1);
            
            // Calculate rates
            long totalFinished = result.TotalFinishedCoroutines;
            if (totalFinished > 0)
            {
                result.CompletionRate = (float)result.TotalCoroutinesCompleted / totalFinished;
                result.FailureRate = (float)(result.TotalCoroutinesCancelled + result.TotalCoroutinesFailed) / totalFinished;
            }
            
            return result;
        }
        
        /// <summary>
        /// Records a coroutine cancellation and returns updated metrics
        /// </summary>
        public readonly CoroutineMetricsData RecordCancellation(bool hasTag, float currentTime)
        {
            var result = this;
            
            // Update state
            result.ActiveCoroutines = math.max(0, result.ActiveCoroutines - 1);
            result.TotalCoroutinesCancelled++;
            result.CancellationCount++;
            result.LastOperationTime = currentTime;
            
            // Track time
            if (result.CreationTime == 0)
                result.CreationTime = currentTime;
                
            result.UpTimeSeconds = currentTime - result.CreationTime;
            
            // Update tag tracking
            if (hasTag)
                result.TaggedCoroutines = math.max(0, result.TaggedCoroutines - 1);
            else
                result.UntaggedCoroutines = math.max(0, result.UntaggedCoroutines - 1);
            
            // Calculate rates
            long totalFinished = result.TotalFinishedCoroutines;
            if (totalFinished > 0)
            {
                result.CompletionRate = (float)result.TotalCoroutinesCompleted / totalFinished;
                result.FailureRate = (float)(result.TotalCoroutinesCancelled + result.TotalCoroutinesFailed) / totalFinished;
            }
            
            return result;
        }
        
        /// <summary>
        /// Records a coroutine failure and returns updated metrics
        /// </summary>
        public readonly CoroutineMetricsData RecordFailure(bool hasTag, bool isTimeout, float currentTime)
        {
            var result = this;
            
            // Update state
            result.ActiveCoroutines = math.max(0, result.ActiveCoroutines - 1);
            result.TotalCoroutinesFailed++;
            result.ExceptionCount++;
            result.LastOperationTime = currentTime;
            
            if (isTimeout)
                result.TimeoutCount++;
            
            // Track time
            if (result.CreationTime == 0)
                result.CreationTime = currentTime;
                
            result.UpTimeSeconds = currentTime - result.CreationTime;
            
            // Update tag tracking
            if (hasTag)
                result.TaggedCoroutines = math.max(0, result.TaggedCoroutines - 1);
            else
                result.UntaggedCoroutines = math.max(0, result.UntaggedCoroutines - 1);
            
            // Calculate rates
            long totalFinished = result.TotalFinishedCoroutines;
            if (totalFinished > 0)
            {
                result.CompletionRate = (float)result.TotalCoroutinesCompleted / totalFinished;
                result.FailureRate = (float)(result.TotalCoroutinesCancelled + result.TotalCoroutinesFailed) / totalFinished;
            }
            
            return result;
        }
        
        /// <summary>
        /// Returns a new CoroutineMetricsData with reset statistics
        /// </summary>
        public readonly CoroutineMetricsData Reset(float currentTime)
        {
            return new CoroutineMetricsData(RunnerId, RunnerName)
            {
                RunnerType = RunnerType,
                EstimatedCoroutineOverheadBytes = EstimatedCoroutineOverheadBytes,
                LastResetTime = currentTime,
                CreationTime = CreationTime
            };
        }
        
        /// <summary>
        /// Determines whether this instance is equal to another
        /// </summary>
        public bool Equals(CoroutineMetricsData other)
        {
            return RunnerId.Equals(other.RunnerId) &&
                   RunnerName.Equals(other.RunnerName) &&
                   ActiveCoroutines == other.ActiveCoroutines &&
                   TotalCoroutinesStarted == other.TotalCoroutinesStarted &&
                   TotalCoroutinesCompleted == other.TotalCoroutinesCompleted;
        }
        
        /// <summary>
        /// Gets the hash code for this instance
        /// </summary>
        public override int GetHashCode()
        {
            return RunnerId.GetHashCode();
        }
    }
    
    /// <summary>
    /// Represents a key-value pair for coroutine metrics in native collections
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    public struct CoroutineMetricsKeyValuePair
    {
        public FixedString64Bytes RunnerId;
        public CoroutineMetricsData Metrics;
    }
}