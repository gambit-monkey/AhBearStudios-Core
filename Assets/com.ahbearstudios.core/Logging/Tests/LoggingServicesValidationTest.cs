using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Logging.Services;
using AhBearStudios.Core.Logging.Filters;
using AhBearStudios.Core.Pooling.Pools;

namespace AhBearStudios.Core.Logging.Tests
{
    /// <summary>
    /// Comprehensive test class to validate all logging services after fixing compatibility issues.
    /// Tests the hybrid logging approach with Unity.Collections v2 compatibility.
    /// </summary>
    public static class LoggingServicesValidationTest
    {
        /// <summary>
        /// Runs all logging services validation tests.
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== Starting Logging Services Validation Tests ===");
            
            try
            {
                TestLogContextService();
                TestLogBatchingService();
                TestLogBufferService();
                TestLogCorrelationService();
                TestLogFormattingService();
                TestLogFilterService();
                TestHybridIntegration();
                
                Console.WriteLine("=== All Tests Completed Successfully! ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== Test Failed: {ex.Message} ===");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Tests LogContextService functionality.
        /// </summary>
        public static void TestLogContextService()
        {
            Console.WriteLine("Testing LogContextService...");
            
            var service = new LogContextService();
            var pool = new ManagedLogDataPool(null, null);
            
            try
            {
                // Test basic context operations
                var properties = new Dictionary<string, object>
                {
                    ["TestProperty"] = "TestValue"
                };
                
                using (var scope = service.PushContext("TestContext", properties))
                {
                    var currentContext = service.CurrentContext;
                    Assert(!currentContext.Equals(LogContext.Empty), "Context should not be empty");
                    
                    // Test log entry enrichment
                    var logEntry = LogEntry.Create(
                        LogLevel.Info,
                        "TestChannel",
                        "Test message",
                        managedDataPool: pool);
                    
                    var enrichedEntry = service.EnrichLogEntry(logEntry, pool);
                    Assert(enrichedEntry.Properties.ContainsKey("TestProperty"), "Enriched entry should contain context properties");
                }
                
                // Test global properties
                service.SetGlobalProperty("GlobalKey", "GlobalValue");
                var globalProps = service.GlobalProperties;
                Assert(globalProps.ContainsKey("GlobalKey"), "Global properties should be accessible");
                
                Console.WriteLine("✓ LogContextService tests passed");
            }
            finally
            {
                service.Dispose();
                pool.Dispose();
            }
        }

        /// <summary>
        /// Tests LogBatchingService functionality.
        /// </summary>
        public static void TestLogBatchingService()
        {
            Console.WriteLine("Testing LogBatchingService...");
            
            var targets = new List<MockLogTarget> { new MockLogTarget() };
            var service = new LogBatchingService(targets);
            
            try
            {
                // Test Unity.Collections v2 compatibility
                var message = LogMessage.Create(
                    LogLevel.Info,
                    "TestChannel",
                    "Test message");
                
                Assert(service.EnqueueMessage(message), "Should enqueue message successfully");
                
                // Test native array enqueueing
                using var nativeMessages = new NativeArray<LogMessage>(1, Allocator.Temp);
                nativeMessages[0] = message;
                
                int enqueued = service.EnqueueMessages(nativeMessages);
                Assert(enqueued == 1, "Should enqueue one message from native array");
                
                // Test native log message support
                var nativeMessage = NativeLogMessage.Create(
                    LogLevel.Info,
                    "TestChannel",
                    "Native test message");
                
                Assert(service.EnqueueNativeMessage(nativeMessage), "Should enqueue native message successfully");
                
                // Test metrics
                var metrics = service.GetMetrics();
                Assert(metrics.EnqueuedMessages > 0, "Should have enqueued messages in metrics");
                
                Console.WriteLine("✓ LogBatchingService tests passed");
            }
            finally
            {
                service.Dispose();
            }
        }

        /// <summary>
        /// Tests LogBufferService functionality.
        /// </summary>
        public static void TestLogBufferService()
        {
            Console.WriteLine("Testing LogBufferService...");
            
            var service = new LogBufferService(maxBufferSize: 10);
            var pool = new ManagedLogDataPool(null, null);
            
            try
            {
                // Test basic buffering
                var logEntry = LogEntry.Create(
                    LogLevel.Info,
                    "TestChannel",
                    "Test message",
                    managedDataPool: pool);
                
                Assert(service.BufferEntry(logEntry), "Should buffer entry successfully");
                Assert(service.BufferedCount == 1, "Should have one buffered entry");
                
                // Test native array buffering
                using var nativeEntries = new NativeArray<LogEntry>(1, Allocator.Temp);
                nativeEntries[0] = logEntry;
                
                int buffered = service.BufferEntries(nativeEntries);
                Assert(buffered == 1, "Should buffer one entry from native array");
                
                // Test flush
                var flushed = service.FlushBuffer();
                Assert(flushed.Count == 2, "Should flush two entries");
                Assert(service.BufferedCount == 0, "Buffer should be empty after flush");
                
                Console.WriteLine("✓ LogBufferService tests passed");
            }
            finally
            {
                service.Dispose();
                pool.Dispose();
            }
        }

        /// <summary>
        /// Tests LogCorrelationService functionality.
        /// </summary>
        public static void TestLogCorrelationService()
        {
            Console.WriteLine("Testing LogCorrelationService...");
            
            var service = new LogCorrelationService();
            var pool = new ManagedLogDataPool(null, null);
            
            try
            {
                // Test correlation scoping
                using (var scope = service.StartCorrelation("TestOperation"))
                {
                    Assert(!service.CurrentCorrelationId.IsEmpty, "Should have current correlation ID");
                    
                    // Test log entry enrichment
                    var logEntry = LogEntry.Create(
                        LogLevel.Info,
                        "TestChannel",
                        "Test message",
                        managedDataPool: pool);
                    
                    var enrichedEntry = service.EnrichLogEntry(logEntry);
                    Assert(!string.IsNullOrEmpty(enrichedEntry.CorrelationId), "Enriched entry should have correlation ID");
                    
                    // Test with managed data pool
                    var enrichedEntry2 = service.EnrichLogEntry(logEntry, pool);
                    Assert(!string.IsNullOrEmpty(enrichedEntry2.CorrelationId), "Enriched entry should have correlation ID");
                }
                
                // Test metrics
                var metrics = service.GetMetrics();
                Assert(metrics.StartedCorrelations > 0, "Should have started correlations in metrics");
                
                Console.WriteLine("✓ LogCorrelationService tests passed");
            }
            finally
            {
                service.Dispose();
                pool.Dispose();
            }
        }

        /// <summary>
        /// Tests LogFormattingService functionality.
        /// </summary>
        public static void TestLogFormattingService()
        {
            Console.WriteLine("Testing LogFormattingService...");
            
            var service = new LogFormattingService();
            
            try
            {
                // Test FixedString compatibility
                var message = LogMessage.Create(
                    LogLevel.Info,
                    "TestChannel",
                    "Test message",
                    correlationId: "test-correlation-id");
                
                var formatted = service.FormatMessage(message);
                Assert(!string.IsNullOrEmpty(formatted), "Should format message successfully");
                Assert(formatted.Contains("Test message"), "Formatted message should contain original message");
                
                // Test structured formatting
                var structured = service.FormatStructured(message);
                Assert(structured.ContainsKey("@level"), "Structured format should contain level");
                Assert(structured.ContainsKey("@channel"), "Structured format should contain channel");
                Assert(structured.ContainsKey("@message"), "Structured format should contain message");
                
                // Test custom formatter
                service.RegisterFormatter("TestFormatter", new TestFormatter());
                var formatters = service.GetRegisteredFormatters();
                Assert(formatters.ContainsKey("TestFormatter"), "Should register custom formatter");
                
                Console.WriteLine("✓ LogFormattingService tests passed");
            }
            finally
            {
                // Service doesn't implement IDisposable, so no cleanup needed
            }
        }

        /// <summary>
        /// Tests LogFilterService functionality.
        /// </summary>
        public static void TestLogFilterService()
        {
            Console.WriteLine("Testing LogFilterService...");
            
            var service = new LogFilterService();
            var pool = new ManagedLogDataPool(null, null);
            
            try
            {
                // Test filter functionality
                var logEntry = LogEntry.Create(
                    LogLevel.Info,
                    "TestChannel",
                    "Test message",
                    managedDataPool: pool);
                
                // Should allow without filters
                Assert(service.ShouldProcess(logEntry), "Should process without filters");
                
                // Add a filter
                var filter = new TestFilter();
                service.AddFilter(filter);
                
                Assert(service.FilterCount == 1, "Should have one filter");
                
                // Test statistics
                var stats = service.GetFilterStatistics();
                Assert(stats != null, "Should return filter statistics");
                
                Console.WriteLine("✓ LogFilterService tests passed");
            }
            finally
            {
                service.Dispose();
                pool.Dispose();
            }
        }

        /// <summary>
        /// Tests hybrid logging integration across all services.
        /// </summary>
        public static void TestHybridIntegration()
        {
            Console.WriteLine("Testing Hybrid Integration...");
            
            var pool = new ManagedLogDataPool(null, null);
            var contextService = new LogContextService();
            var correlationService = new LogCorrelationService();
            var formattingService = new LogFormattingService();
            
            try
            {
                // Test end-to-end integration
                var exception = new Exception("Test exception");
                var properties = new Dictionary<string, object>
                {
                    ["IntegrationTest"] = true,
                    ["TestValue"] = 42
                };
                
                using (var correlationScope = correlationService.StartCorrelation("IntegrationTest"))
                using (var contextScope = contextService.PushContext("IntegrationContext", properties))
                {
                    // Create hybrid log message
                    var logMessage = LogMessage.Create(
                        LogLevel.Error,
                        "IntegrationChannel",
                        "Integration test message",
                        exception: exception,
                        properties: properties,
                        managedDataPool: pool);
                    
                    // Test Unity.Collections v2 compatibility
                    using var messageQueue = new NativeQueue<LogMessage>(Allocator.Temp);
                    messageQueue.Enqueue(logMessage);
                    
                    using var messageArray = new NativeArray<LogMessage>(1, Allocator.Temp);
                    messageArray[0] = logMessage;
                    
                    // Test Burst job compatibility
                    var job = new TestLogProcessingJob { Messages = messageArray };
                    var handle = job.Schedule();
                    handle.Complete();
                    
                    // Test service integration
                    var logEntry = LogEntry.Create(
                        LogLevel.Error,
                        "IntegrationChannel",
                        "Integration test entry",
                        exception: exception,
                        properties: properties,
                        managedDataPool: pool);
                    
                    var enrichedEntry = contextService.EnrichLogEntry(logEntry, pool);
                    enrichedEntry = correlationService.EnrichLogEntry(enrichedEntry, pool);
                    
                    // Test formatting
                    var enrichedMessage = LogMessage.Create(
                        enrichedEntry.Level,
                        enrichedEntry.Channel,
                        enrichedEntry.Message,
                        exception: enrichedEntry.Exception,
                        correlationId: enrichedEntry.CorrelationId,
                        properties: enrichedEntry.Properties,
                        managedDataPool: pool);
                    
                    var formatted = formattingService.FormatMessage(enrichedMessage);
                    Assert(!string.IsNullOrEmpty(formatted), "Should format integrated message");
                    
                    // Verify managed data is preserved
                    Assert(enrichedEntry.HasException, "Should preserve exception");
                    Assert(enrichedEntry.HasProperties, "Should preserve properties");
                    Assert(enrichedEntry.ManagedDataId != Guid.Empty, "Should have managed data ID");
                }
                
                Console.WriteLine("✓ Hybrid Integration tests passed");
            }
            finally
            {
                contextService.Dispose();
                correlationService.Dispose();
                pool.Dispose();
            }
        }

        /// <summary>
        /// Simple assertion helper.
        /// </summary>
        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception($"Assertion failed: {message}");
            }
        }

        /// <summary>
        /// Mock log target for testing.
        /// </summary>
        private class MockLogTarget : ILogTarget
        {
            public string Name => "MockTarget";
            public bool IsEnabled { get; set; } = true;
            public LogLevel MinimumLevel { get; set; } = LogLevel.Debug;

            public void Write(LogMessage logMessage) { }
            public void Write(LogEntry logEntry) { }
            public void WriteBatch(IReadOnlyList<LogMessage> logMessages) { }
            public void WriteBatch(IReadOnlyList<LogEntry> logEntries) { }
            public void Flush() { }
            public bool ShouldProcessMessage(LogMessage logMessage) => true;
            public bool ShouldProcessEntry(LogEntry logEntry) => true;
            public bool PerformHealthCheck() => true;
            public void Dispose() { }
        }

        /// <summary>
        /// Test formatter for testing.
        /// </summary>
        private class TestFormatter : ILogFormatter
        {
            public string Format(in LogMessage logMessage)
            {
                return $"[TEST] {logMessage.Message}";
            }
        }

        /// <summary>
        /// Test filter for testing.
        /// </summary>
        private class TestFilter : ILogFilter
        {
            public FixedString64Bytes Name => "TestFilter";
            public bool IsEnabled { get; set; } = true;
            public int Priority => 100;

            public bool ShouldProcess(LogEntry logEntry, FixedString64Bytes correlationId = default)
            {
                return true;
            }

            public ValidationResult Validate(FixedString64Bytes correlationId = default)
            {
                return ValidationResult.Success("TestFilter");
            }

            public void Reset(FixedString64Bytes correlationId = default) { }
            public FilterStatistics GetStatistics() => new FilterStatistics();
        }

        /// <summary>
        /// Burst-compatible job for testing Unity.Collections v2 integration.
        /// </summary>
        [Unity.Burst.BurstCompile]
        public struct TestLogProcessingJob : IJob
        {
            [ReadOnly] public NativeArray<LogMessage> Messages;

            public void Execute()
            {
                for (int i = 0; i < Messages.Length; i++)
                {
                    var message = Messages[i];
                    // Test that we can access all native fields
                    var _ = message.Level;
                    var __ = message.Channel;
                    var ___ = message.Message;
                    var ____ = message.Timestamp;
                    var _____ = message.HasException;
                    var ______ = message.HasProperties;
                    var _______ = message.ManagedDataId;
                }
            }
        }
    }
}