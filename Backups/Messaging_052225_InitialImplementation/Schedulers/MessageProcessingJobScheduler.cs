using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using AhBearStudios.Core.Messaging.Interfaces;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Profiling.Interfaces;

namespace AhBearStudios.Core.Messaging.Jobs
{
    /// <summary>
    /// Utility class for creating and scheduling message processing jobs.
    /// Provides convenience methods for job scheduling with appropriate safety checks.
    /// </summary>
    public static class MessageProcessingJobScheduler
    {
        /// <summary>
        /// Creates and schedules a message processing job with the specified parameters.
        /// </summary>
        /// <typeparam name="T">The type of message to process.</typeparam>
        /// <param name="sourceQueue">The source message queue to read messages from.</param>
        /// <param name="processFunction">The function pointer for message processing.</param>
        /// <param name="dependsOn">The JobHandle to depend on.</param>
        /// <param name="destinationQueue">The destination message queue to write processed messages to (optional).</param>
        /// <param name="processAllMessages">Whether to process all available messages.</param>
        /// <param name="maxMessagesToProcess">The maximum number of messages to process.</param>
        /// <param name="processedCount">Counter for the number of messages processed (optional).</param>
        /// <param name="failedCount">Counter for the number of messages that failed processing (optional).</param>
        /// <param name="logger">Optional logger for job operations.</param>
        /// <param name="profiler">Optional profiler for performance monitoring.</param>
        /// <returns>A JobHandle for the scheduled job.</returns>
        public static JobHandle Schedule<T>(
            NativeMessageQueue<T> sourceQueue,
            FunctionPointer<MessageProcessingJob<T>.ProcessMessageDelegate> processFunction,
            JobHandle dependsOn = default,
            NativeMessageQueue<T> destinationQueue = default,
            bool processAllMessages = true,
            int maxMessagesToProcess = 100,
            NativeReference<int> processedCount = default,
            NativeReference<int> failedCount = default,
            IBurstLogger logger = null,
            IProfiler profiler = null)
            where T : unmanaged, IMessage
        {
            using (profiler?.BeginSample("MessageProcessingJobScheduler.Schedule"))
            {
                // Validate parameters
                if (!sourceQueue.IsCreated)
                {
                    logger?.Error("Cannot schedule MessageProcessingJob: source queue is not created");
                    throw new ArgumentException("Source queue must be created", nameof(sourceQueue));
                }
                
                if (!processFunction.IsCreated)
                {
                    logger?.Error("Cannot schedule MessageProcessingJob: process function is not created");
                    throw new ArgumentException("Process function must be created", nameof(processFunction));
                }
                
                if (maxMessagesToProcess <= 0 && !processAllMessages)
                {
                    logger?.Warning("MessageProcessingJob scheduled with maxMessagesToProcess <= 0 and processAllMessages=false, job will not process any messages");
                }

                // Create the job
                var job = new MessageProcessingJob<T>
                {
                    SourceQueue = sourceQueue,
                    DestinationQueue = destinationQueue,
                    ProcessFunction = processFunction,
                    ProcessAllMessages = processAllMessages,
                    MaxMessagesToProcess = maxMessagesToProcess,
                    ProcessedCount = processedCount,
                    FailedCount = failedCount
                };
                
                // Schedule the job
                JobHandle jobHandle = job.Schedule(dependsOn);
                
                logger?.Debug($"Scheduled MessageProcessingJob for message type {typeof(T).Name}, maxMessages={maxMessagesToProcess}, processAll={processAllMessages}");
                
                return jobHandle;
            }
        }

        /// <summary>
        /// Creates and schedules a message processing job that executes a simple pass-through function.
        /// Useful for transferring messages between queues without modifying them.
        /// </summary>
        /// <typeparam name="T">The type of message to process.</typeparam>
        /// <param name="sourceQueue">The source message queue to read messages from.</param>
        /// <param name="destinationQueue">The destination message queue to write messages to.</param>
        /// <param name="dependsOn">The JobHandle to depend on.</param>
        /// <param name="maxMessagesToProcess">The maximum number of messages to process.</param>
        /// <param name="logger">Optional logger for job operations.</param>
        /// <param name="profiler">Optional profiler for performance monitoring.</param>
        /// <returns>A JobHandle for the scheduled job.</returns>
        public static JobHandle ScheduleTransfer<T>(
            NativeMessageQueue<T> sourceQueue,
            NativeMessageQueue<T> destinationQueue,
            JobHandle dependsOn = default,
            int maxMessagesToProcess = 100,
            IBurstLogger logger = null,
            IProfiler profiler = null)
            where T : unmanaged, IMessage
        {
            using (profiler?.BeginSample("MessageProcessingJobScheduler.ScheduleTransfer"))
            {
                // Validate parameters
                if (!sourceQueue.IsCreated)
                {
                    logger?.Error("Cannot schedule transfer job: source queue is not created");
                    throw new ArgumentException("Source queue must be created", nameof(sourceQueue));
                }
                
                if (!destinationQueue.IsCreated)
                {
                    logger?.Error("Cannot schedule transfer job: destination queue is not created");
                    throw new ArgumentException("Destination queue must be created", nameof(destinationQueue));
                }

                // Create a pass-through function
                var passThroughFn = new MessageProcessingJob<T>.ProcessMessageDelegate(message => message);
                var fnPtr = BurstCompiler.CompileFunctionPointer(passThroughFn);
                
                // Create counters to track transfer statistics
                var processedCount = new NativeReference<int>(0, Allocator.TempJob);
                var failedCount = new NativeReference<int>(0, Allocator.TempJob);
                
                // Schedule the job
                var job = new MessageProcessingJob<T>
                {
                    SourceQueue = sourceQueue,
                    DestinationQueue = destinationQueue,
                    ProcessFunction = fnPtr,
                    ProcessAllMessages = true,
                    MaxMessagesToProcess = maxMessagesToProcess,
                    ProcessedCount = processedCount,
                    FailedCount = failedCount
                };
                
                JobHandle jobHandle = job.Schedule(dependsOn);
                
                logger?.Debug($"Scheduled transfer job for message type {typeof(T).Name}, maxMessages={maxMessagesToProcess}");
                
                // Create a cleanup job to dispose the counters
                var cleanupJob = new CleanupCountersJob
                {
                    ProcessedCount = processedCount,
                    FailedCount = failedCount
                };
                
                return cleanupJob.Schedule(jobHandle);
            }
        }

        /// <summary>
        /// Job to clean up temporary counters after processing is complete.
        /// </summary>
        [BurstCompile]
        private struct CleanupCountersJob : IJob
        {
            [DeallocateOnJobCompletion]
            public NativeReference<int> ProcessedCount;
            
            [DeallocateOnJobCompletion]
            public NativeReference<int> FailedCount;
            
            public void Execute()
            {
                // Resources will be automatically deallocated due to the DeallocateOnJobCompletion attribute
            }
        }
    }
}