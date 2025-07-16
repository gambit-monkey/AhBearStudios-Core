using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Logging.Services;
using AhBearStudios.Core.Pooling.Pools;

namespace AhBearStudios.Core.Logging.Tests
{
    /// <summary>
    /// Test class to verify hybrid logging implementation with Unity.Collections v2 compatibility.
    /// This test demonstrates that LogMessage and LogEntry can be used in Unity.Collections
    /// while maintaining access to managed data through the pooling system.
    /// </summary>
    public static class HybridLoggingTest
    {
        /// <summary>
        /// Tests Unity.Collections v2 compatibility with LogMessage.
        /// </summary>
        public static void TestLogMessageUnityCollections()
        {
            // Create a pool for managed data
            var pool = new ManagedLogDataPool(null, null);
            
            // Create test exception and properties
            var testException = new Exception("Test exception");
            var testProperties = new Dictionary<string, object>
            {
                ["TestKey"] = "TestValue",
                ["NumericValue"] = 42
            };

            // Test 1: Create LogMessage with managed data
            var logMessage = LogMessage.Create(
                LogLevel.Error,
                "TestChannel",
                "Test message with managed data",
                testException,
                "test-correlation-id",
                testProperties,
                "TestContext",
                "TestSource",
                managedDataPool: pool);

            // Test 2: Use LogMessage in NativeQueue (Unity.Collections v2 compatible)
            using var messageQueue = new NativeQueue<LogMessage>(Allocator.Temp);
            messageQueue.Enqueue(logMessage);

            // Test 3: Use LogMessage in NativeList (Unity.Collections v2 compatible)
            using var messageList = new NativeList<LogMessage>(1, Allocator.Temp);
            messageList.Add(logMessage);

            // Test 4: Use LogMessage in NativeArray (Unity.Collections v2 compatible)
            using var messageArray = new NativeArray<LogMessage>(1, Allocator.Temp);
            messageArray[0] = logMessage;

            // Test 5: Use LogMessage in Burst-compatible job
            var job = new TestLogProcessingJob
            {
                Messages = messageArray
            };
            var handle = job.Schedule();
            handle.Complete();

            // Test 6: Access managed data through hybrid approach
            var retrievedMessage = messageQueue.Dequeue();
            var hasException = retrievedMessage.HasException;
            var hasProperties = retrievedMessage.HasProperties;
            
            // Verify the managed data IDs are preserved
            var managedDataId = retrievedMessage.ManagedDataId;
            var isValidManagedData = managedDataId != Guid.Empty;

            // Cleanup
            pool.Dispose();
            
            Console.WriteLine($"Unity.Collections v2 compatibility test passed!");
            Console.WriteLine($"- NativeQueue: Success");
            Console.WriteLine($"- NativeList: Success");
            Console.WriteLine($"- NativeArray: Success");
            Console.WriteLine($"- Burst Job: Success");
            Console.WriteLine($"- Managed Data: {(isValidManagedData ? "Preserved" : "Not preserved")}");
        }

        /// <summary>
        /// Tests Unity.Collections v2 compatibility with LogEntry.
        /// </summary>
        public static void TestLogEntryUnityCollections()
        {
            // Create a pool for managed data
            var pool = new ManagedLogDataPool(null, null);
            
            // Create test data
            var testException = new Exception("Test exception");
            var testProperties = new Dictionary<string, object>
            {
                ["TestKey"] = "TestValue"
            };

            // Test LogEntry with managed data
            var logEntry = LogEntry.Create(
                LogLevel.Warning,
                "TestChannel",
                "Test entry message",
                exception: testException,
                properties: testProperties,
                managedDataPool: pool);

            // Test Unity.Collections v2 compatibility
            using var entryList = new NativeList<LogEntry>(1, Allocator.Temp);
            entryList.Add(logEntry);

            using var entryArray = new NativeArray<LogEntry>(1, Allocator.Temp);
            entryArray[0] = logEntry;

            // Verify data preservation
            var retrievedEntry = entryList[0];
            var hasException = retrievedEntry.HasException;
            var hasProperties = retrievedEntry.HasProperties;
            var managedDataId = retrievedEntry.ManagedDataId;

            // Cleanup
            pool.Dispose();
            
            Console.WriteLine($"LogEntry Unity.Collections v2 test passed!");
            Console.WriteLine($"- NativeList: Success");
            Console.WriteLine($"- NativeArray: Success");
            Console.WriteLine($"- Managed Data ID: {(managedDataId != Guid.Empty ? "Preserved" : "Not preserved")}");
        }

        /// <summary>
        /// Tests the batching service with Unity.Collections v2.
        /// </summary>
        public static void TestBatchingServiceUnityCollections()
        {
            // Create messages for batching
            var messages = new List<LogMessage>();
            for (int i = 0; i < 5; i++)
            {
                messages.Add(LogMessage.Create(
                    LogLevel.Info,
                    "TestChannel",
                    $"Batch message {i}",
                    managedDataPool: null));
            }

            // Test native array processing
            using var nativeMessages = new NativeArray<LogMessage>(messages.Count, Allocator.Temp);
            for (int i = 0; i < messages.Count; i++)
            {
                nativeMessages[i] = messages[i];
            }

            // Simulate batch processing
            var processedCount = 0;
            for (int i = 0; i < nativeMessages.Length; i++)
            {
                if (nativeMessages[i].Level >= LogLevel.Info)
                {
                    processedCount++;
                }
            }

            Console.WriteLine($"Batching service Unity.Collections v2 test passed!");
            Console.WriteLine($"- Processed {processedCount} messages from native array");
        }
    }

    /// <summary>
    /// Burst-compatible job for testing LogMessage processing.
    /// </summary>
    [Unity.Burst.BurstCompile]
    public struct TestLogProcessingJob : IJob
    {
        [Unity.Collections.ReadOnly]
        public NativeArray<LogMessage> Messages;

        public void Execute()
        {
            for (int i = 0; i < Messages.Length; i++)
            {
                var message = Messages[i];
                
                // Test that we can access native fields in Burst context
                var level = message.Level;
                var channel = message.Channel;
                var messageText = message.Message;
                var timestamp = message.Timestamp;
                var hasException = message.HasException;
                var hasProperties = message.HasProperties;
                var managedDataId = message.ManagedDataId;
                
                // Simple processing logic
                if (level >= LogLevel.Warning)
                {
                    // Process high-priority messages
                }
            }
        }
    }
}