using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using NUnit.Framework;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Logging.Tests.Utilities;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Pooling.Pools;

namespace AhBearStudios.Core.Logging.Tests
{
    /// <summary>
    /// Comprehensive tests for logging model classes including LogEntry, LogLevel, 
    /// LogMessage, and related data structures.
    /// Tests creation, serialization, Burst compatibility, Unity Job System integration, and memory management.
    /// </summary>
    [TestFixture]
    public class LoggingModelTests
    {
        [SetUp]
        public void SetUp()
        {
            // Clean up any previous test artifacts
            TestUtilities.CleanupTempFiles();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up test artifacts
            TestUtilities.CleanupTempFiles();
        }

        #region LogLevel Tests

        [Test]
        public void LogLevel_EnumValues_HaveCorrectOrder()
        {
            // Assert
            Assert.That((int)LogLevel.Debug, Is.EqualTo(0));
            Assert.That((int)LogLevel.Info, Is.EqualTo(1));
            Assert.That((int)LogLevel.Warning, Is.EqualTo(2));
            Assert.That((int)LogLevel.Error, Is.EqualTo(3));
            Assert.That((int)LogLevel.Critical, Is.EqualTo(4));
            Assert.That((int)LogLevel.Trace, Is.EqualTo(5));
        }

        [Test]
        public void LogLevel_Comparison_WorksCorrectly()
        {
            // Assert
            Assert.That(LogLevel.Debug < LogLevel.Info, Is.True);
            Assert.That(LogLevel.Info < LogLevel.Warning, Is.True);
            Assert.That(LogLevel.Warning < LogLevel.Error, Is.True);
            Assert.That(LogLevel.Error < LogLevel.Critical, Is.True);
            Assert.That(LogLevel.Critical < LogLevel.Trace, Is.True);
        }

        [Test]
        public void LogLevel_Equality_WorksCorrectly()
        {
            // Assert
            Assert.That(LogLevel.Info == LogLevel.Info, Is.True);
            Assert.That(LogLevel.Debug == LogLevel.Info, Is.False);
            Assert.That(LogLevel.Error != LogLevel.Warning, Is.True);
        }

        [Test]
        public void LogLevel_AllValues_CanBeUsedInSwitch()
        {
            // Arrange
            var allLevels = TestDataFactory.GetAllLogLevels();
            var processedLevels = new List<LogLevel>();

            // Act
            foreach (var level in allLevels)
            {
                var processed = level switch
                {
                    LogLevel.Debug => LogLevel.Debug,
                    LogLevel.Info => LogLevel.Info,
                    LogLevel.Warning => LogLevel.Warning,
                    LogLevel.Error => LogLevel.Error,
                    LogLevel.Critical => LogLevel.Critical,
                    LogLevel.Trace => LogLevel.Trace,
                    _ => throw new ArgumentException($"Unhandled log level: {level}")
                };
                processedLevels.Add(processed);
            }

            // Assert
            Assert.That(processedLevels.Count, Is.EqualTo(allLevels.Length));
            CollectionAssert.AreEquivalent(allLevels, processedLevels);
        }

        #endregion

        #region LogEntry Tests

        [Test]
        public void LogEntry_Create_WithValidParameters_CreatesCorrectEntry()
        {
            // Arrange
            var level = LogLevel.Info;
            var channel = "TestChannel";
            var message = "Test message";
            var correlationId = "test-correlation-id";
            var sourceContext = "TestClass";
            var exception = TestDataFactory.CreateTestException();
            var properties = TestDataFactory.CreateTestProperties();

            // Act
            var entry = LogEntry.Create(level, channel, message, correlationId, sourceContext, 
                exception: exception, properties: properties);

            // Assert
            Assert.That(entry.Level, Is.EqualTo(level));
            Assert.That(entry.Channel.ToString(), Is.EqualTo(channel));
            Assert.That(entry.Message.ToString(), Is.EqualTo(message));
            Assert.That(entry.CorrelationId.ToString(), Is.EqualTo(correlationId));
            Assert.That(entry.SourceContext.ToString(), Is.EqualTo(sourceContext));
            Assert.That(entry.HasException, Is.True);
            Assert.That(entry.HasProperties, Is.True);
            Assert.That(entry.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(entry.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
        }

        [Test]
        public void LogEntry_Create_WithMinimalParameters_UsesDefaults()
        {
            // Arrange
            var level = LogLevel.Warning;
            var channel = "MinimalChannel";
            var message = "Minimal message";

            // Act
            var entry = LogEntry.Create(level, channel, message);

            // Assert
            Assert.That(entry.Level, Is.EqualTo(level));
            Assert.That(entry.Channel.ToString(), Is.EqualTo(channel));
            Assert.That(entry.Message.ToString(), Is.EqualTo(message));
            Assert.That(entry.Source.ToString(), Is.EqualTo("LoggingSystem"));
            Assert.That(entry.Priority, Is.EqualTo(MessagePriority.Normal));
            Assert.That(entry.HasException, Is.False);
            Assert.That(entry.HasProperties, Is.False);
        }

        [Test]
        public void LogEntry_CreateNative_WithValidParameters_CreatesCorrectEntry()
        {
            // Arrange
            var level = LogLevel.Error;
            var channel = new FixedString64Bytes("NativeChannel");
            var message = new FixedString512Bytes("Native message");
            var correlationId = new FixedString128Bytes("native-correlation-id");
            var sourceContext = new FixedString128Bytes("NativeTestClass");

            // Act
            var entry = LogEntry.CreateNative(level, channel, message, correlationId, sourceContext);

            // Assert
            Assert.That(entry.Level, Is.EqualTo(level));
            Assert.That(entry.Channel.ToString(), Is.EqualTo(channel.ToString()));
            Assert.That(entry.Message.ToString(), Is.EqualTo(message.ToString()));
            Assert.That(entry.CorrelationId.ToString(), Is.EqualTo(correlationId.ToString()));
            Assert.That(entry.SourceContext.ToString(), Is.EqualTo(sourceContext.ToString()));
            Assert.That(entry.HasException, Is.False);
            Assert.That(entry.HasProperties, Is.False);
        }

        [Test]
        public void LogEntry_ShouldProcess_WithDifferentLevels_ReturnsCorrectResults()
        {
            // Arrange
            var debugEntry = TestDataFactory.CreateLogEntry(LogLevel.Debug);
            var infoEntry = TestDataFactory.CreateLogEntry(LogLevel.Info);
            var warningEntry = TestDataFactory.CreateLogEntry(LogLevel.Warning);
            var errorEntry = TestDataFactory.CreateLogEntry(LogLevel.Error);

            // Act & Assert
            Assert.That(debugEntry.ShouldProcess(LogLevel.Debug), Is.True);
            Assert.That(debugEntry.ShouldProcess(LogLevel.Info), Is.False);
            Assert.That(infoEntry.ShouldProcess(LogLevel.Debug), Is.True);
            Assert.That(infoEntry.ShouldProcess(LogLevel.Warning), Is.False);
            Assert.That(warningEntry.ShouldProcess(LogLevel.Info), Is.True);
            Assert.That(errorEntry.ShouldProcess(LogLevel.Critical), Is.False);
        }

        [Test]
        public void LogEntry_GetNativeSize_ReturnsReasonableSize()
        {
            // Arrange
            var entry = TestDataFactory.CreateLogEntry();

            // Act
            var size = entry.GetNativeSize();

            // Assert
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size, Is.LessThan(2048)); // Should be less than 2KB for most entries
        }

        [Test]
        public void LogEntry_ToLogMessage_ConvertsCorrectly()
        {
            // Arrange
            var entry = TestDataFactory.CreateLogEntry(LogLevel.Warning, "TestChannel", "Test message");

            // Act
            var logMessage = entry.ToLogMessage();

            // Assert
            Assert.That(logMessage.Id, Is.EqualTo(entry.Id));
            Assert.That(logMessage.Level, Is.EqualTo(entry.Level));
            Assert.That(logMessage.Channel.ToString(), Is.EqualTo(entry.Channel.ToString()));
            Assert.That(logMessage.Message.ToString(), Is.EqualTo(entry.Message.ToString()));
            Assert.That(logMessage.CorrelationId.ToString(), Is.EqualTo(entry.CorrelationId.ToString()));
        }

        [Test]
        public void LogEntry_ToManagedStrings_ConvertsCorrectly()
        {
            // Arrange
            var entry = TestDataFactory.CreateLogEntry(LogLevel.Info, "TestChannel", "Test message", 
                "test-correlation", "TestClass");

            // Act
            var (channel, message, correlationId, sourceContext, source, machineName, instanceId) = 
                entry.ToManagedStrings();

            // Assert
            Assert.That(channel, Is.EqualTo("TestChannel"));
            Assert.That(message, Is.EqualTo("Test message"));
            Assert.That(correlationId, Is.EqualTo("test-correlation"));
            Assert.That(sourceContext, Is.EqualTo("TestClass"));
            Assert.That(source, Is.Not.Null.And.Not.Empty);
            Assert.That(machineName, Is.Not.Null.And.Not.Empty);
            Assert.That(instanceId, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void LogEntry_ToDictionary_ContainsAllExpectedKeys()
        {
            // Arrange
            var exception = TestDataFactory.CreateTestException();
            var properties = TestDataFactory.CreateTestProperties();
            var entry = TestDataFactory.CreateLogEntry(LogLevel.Error, "TestChannel", "Test message",
                "test-correlation", "TestClass", exception, properties);

            // Act
            var dictionary = entry.ToDictionary();

            // Assert
            Assert.That(dictionary.ContainsKey("Id"), Is.True);
            Assert.That(dictionary.ContainsKey("Timestamp"), Is.True);
            Assert.That(dictionary.ContainsKey("Level"), Is.True);
            Assert.That(dictionary.ContainsKey("Channel"), Is.True);
            Assert.That(dictionary.ContainsKey("Message"), Is.True);
            Assert.That(dictionary.ContainsKey("CorrelationId"), Is.True);
            Assert.That(dictionary.ContainsKey("SourceContext"), Is.True);
            Assert.That(dictionary.ContainsKey("Exception"), Is.True);
            Assert.That(dictionary.ContainsKey("ExceptionType"), Is.True);
            
            // Check properties are included
            Assert.That(dictionary.Keys.Any(k => k.StartsWith("Properties.")), Is.True);
        }

        [Test]
        public void LogEntry_ToString_FormatsCorrectly()
        {
            // Arrange
            var entry = TestDataFactory.CreateLogEntry(LogLevel.Warning, "TestChannel", "Test message");

            // Act
            var result = entry.ToString();

            // Assert
            Assert.That(result, Contains.Substring("Warning"));
            Assert.That(result, Contains.Substring("TestChannel"));
            Assert.That(result, Contains.Substring("Test message"));
            Assert.That(result, Contains.Substring(entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff")));
        }

        [Test]
        public void LogEntry_Equals_ComparesById()
        {
            // Arrange
            var id = Guid.NewGuid();
            var entry1 = TestDataFactory.CreateLogEntry(LogLevel.Info, "Channel1", "Message1");
            var entry2 = TestDataFactory.CreateLogEntry(LogLevel.Error, "Channel2", "Message2");

            // Use reflection to set the same ID for testing
            var entry1WithId = new LogEntry(id, DateTime.UtcNow, LogLevel.Info, 
                new FixedString64Bytes("Channel1"), new FixedString512Bytes("Message1"));
            var entry2WithId = new LogEntry(id, DateTime.UtcNow, LogLevel.Error, 
                new FixedString64Bytes("Channel2"), new FixedString512Bytes("Message2"));

            // Act & Assert
            Assert.That(entry1WithId.Equals(entry2WithId), Is.True);
            Assert.That(entry1.Equals(entry2), Is.False);
        }

        [Test]
        public void LogEntry_GetHashCode_BasedOnId()
        {
            // Arrange
            var entry1 = TestDataFactory.CreateLogEntry();
            var entry2 = TestDataFactory.CreateLogEntry();

            // Act
            var hash1 = entry1.GetHashCode();
            var hash2 = entry2.GetHashCode();

            // Assert
            Assert.That(hash1, Is.Not.EqualTo(hash2)); // Different IDs should have different hashes
            Assert.That(hash1, Is.EqualTo(entry1.Id.GetHashCode()));
        }

        [Test]
        public void LogEntry_Dispose_DoesNotThrow()
        {
            // Arrange
            var entry = TestDataFactory.CreateLogEntry();

            // Act & Assert
            Assert.DoesNotThrow(() => entry.Dispose());
        }

        #endregion

        #region LogMessage Tests

        [Test]
        public void LogMessage_Create_WithValidParameters_CreatesCorrectMessage()
        {
            // Arrange
            var level = LogLevel.Warning;
            var channel = "MessageChannel";
            var message = "Test log message";
            var correlationId = "msg-correlation-id";
            var sourceContext = "MessageClass";
            var exception = TestDataFactory.CreateTestException();
            var properties = TestDataFactory.CreateTestProperties();

            // Act
            var logMessage = LogMessage.Create(level, channel, message, exception, correlationId, 
                properties, sourceContext);

            // Assert
            Assert.That(logMessage.Level, Is.EqualTo(level));
            Assert.That(logMessage.Channel.ToString(), Is.EqualTo(channel));
            Assert.That(logMessage.Message.ToString(), Is.EqualTo(message));
            Assert.That(logMessage.CorrelationId.ToString(), Is.EqualTo(correlationId));
            Assert.That(logMessage.SourceContext.ToString(), Is.EqualTo(sourceContext));
            Assert.That(logMessage.HasException, Is.True);
            Assert.That(logMessage.HasProperties, Is.True);
            Assert.That(logMessage.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(logMessage.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
        }

        [Test]
        public void LogMessage_Create_WithMinimalParameters_UsesDefaults()
        {
            // Arrange
            var level = LogLevel.Info;
            var channel = "MinimalChannel";
            var message = "Minimal log message";

            // Act
            var logMessage = LogMessage.Create(level, channel, message);

            // Assert
            Assert.That(logMessage.Level, Is.EqualTo(level));
            Assert.That(logMessage.Channel.ToString(), Is.EqualTo(channel));
            Assert.That(logMessage.Message.ToString(), Is.EqualTo(message));
            Assert.That(logMessage.HasException, Is.False);
            Assert.That(logMessage.HasProperties, Is.False);
            Assert.That(logMessage.Priority, Is.EqualTo(MessagePriority.Normal));
        }

        [Test]
        public void LogMessage_CreateNative_WithValidParameters_CreatesCorrectMessage()
        {
            // Arrange
            var level = LogLevel.Info;
            var channel = new FixedString64Bytes("NativeChannel");
            var message = new FixedString512Bytes("Native log message");
            var correlationId = new FixedString128Bytes("native-correlation");
            var sourceContext = new FixedString128Bytes("NativeClass");
            var threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;

            // Act
            var logMessage = LogMessage.CreateNative(level, channel, message, correlationId, 
                sourceContext, threadId: threadId);

            // Assert
            Assert.That(logMessage.Level, Is.EqualTo(level));
            Assert.That(logMessage.Channel.ToString(), Is.EqualTo(channel.ToString()));
            Assert.That(logMessage.Message.ToString(), Is.EqualTo(message.ToString()));
            Assert.That(logMessage.CorrelationId.ToString(), Is.EqualTo(correlationId.ToString()));
            Assert.That(logMessage.SourceContext.ToString(), Is.EqualTo(sourceContext.ToString()));
            Assert.That(logMessage.ThreadId, Is.EqualTo(threadId));
            Assert.That(logMessage.HasException, Is.False);
            Assert.That(logMessage.HasProperties, Is.False);
        }

        [Test]
        public void LogMessage_Format_WithDefaultFormat_FormatsCorrectly()
        {
            // Arrange
            var logMessage = TestDataFactory.CreateLogMessage(LogLevel.Warning, "TestChannel", "Test message");

            // Act
            var result = logMessage.Format();

            // Assert
            Assert.That(result, Contains.Substring("Warning"));
            Assert.That(result, Contains.Substring("TestChannel"));
            Assert.That(result, Contains.Substring("Test message"));
            Assert.That(result, Contains.Substring(logMessage.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff")));
        }

        [Test]
        public void LogMessage_FormatNative_ReturnsCorrectFormat()
        {
            // Arrange
            var logMessage = LogMessage.CreateNative(LogLevel.Error, 
                new FixedString64Bytes("ErrorChannel"), 
                new FixedString512Bytes("Error message"));

            // Act
            var result = logMessage.FormatNative();

            // Assert
            var resultString = result.ToString();
            Assert.That(resultString, Contains.Substring("Error"));
            Assert.That(resultString, Contains.Substring("ErrorChannel"));
            Assert.That(resultString, Contains.Substring("Error message"));
        }

        [Test]
        public void LogMessage_ToManagedStrings_ConvertsCorrectly()
        {
            // Arrange
            var logMessage = LogMessage.CreateNative(LogLevel.Info, 
                new FixedString64Bytes("TestChannel"), 
                new FixedString512Bytes("Test message"),
                new FixedString128Bytes("test-correlation"),
                new FixedString128Bytes("TestClass"));

            // Act
            var (channel, message, correlationId, sourceContext, source) = logMessage.ToManagedStrings();

            // Assert
            Assert.That(channel, Is.EqualTo("TestChannel"));
            Assert.That(message, Is.EqualTo("Test message"));
            Assert.That(correlationId, Is.EqualTo("test-correlation"));
            Assert.That(sourceContext, Is.EqualTo("TestClass"));
            Assert.That(source, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void LogMessage_ShouldProcess_WithDifferentLevels_ReturnsCorrectResults()
        {
            // Arrange
            var debugMessage = TestDataFactory.CreateLogMessage(LogLevel.Debug);
            var infoMessage = TestDataFactory.CreateLogMessage(LogLevel.Info);
            var warningMessage = TestDataFactory.CreateLogMessage(LogLevel.Warning);

            // Act & Assert
            Assert.That(debugMessage.ShouldProcess(LogLevel.Debug), Is.True);
            Assert.That(debugMessage.ShouldProcess(LogLevel.Info), Is.False);
            Assert.That(infoMessage.ShouldProcess(LogLevel.Debug), Is.True);
            Assert.That(warningMessage.ShouldProcess(LogLevel.Error), Is.False);
        }

        [Test]
        public void LogMessage_GetNativeSize_ReturnsReasonableSize()
        {
            // Arrange
            var logMessage = TestDataFactory.CreateLogMessage();

            // Act
            var size = logMessage.GetNativeSize();

            // Assert
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size, Is.LessThan(2048)); // Should be less than 2KB for most messages
        }

        [Test]
        public void LogMessage_CreateWithGuidCorrelation_CreatesCorrectly()
        {
            // Arrange
            var correlationId = Guid.NewGuid();
            var level = LogLevel.Critical;
            var channel = "GuidChannel";
            var message = "Guid correlation message";

            // Act
            var logMessage = LogMessage.CreateWithGuidCorrelation(level, channel, message, correlationId);

            // Assert
            Assert.That(logMessage.Level, Is.EqualTo(level));
            Assert.That(logMessage.Channel.ToString(), Is.EqualTo(channel));
            Assert.That(logMessage.Message.ToString(), Is.EqualTo(message));
            Assert.That(logMessage.CorrelationId.ToString(), Is.EqualTo(correlationId.ToString()));
        }

        #endregion

        #region Burst Compatibility Tests

        [Test]
        public void LogEntry_BurstCompatibility_IsUnmanaged()
        {
            // Arrange
            var entry = LogEntry.CreateNative(LogLevel.Info, 
                new FixedString64Bytes("TestChannel"), 
                new FixedString512Bytes("Test message"));

            // Act & Assert
            TestUtilities.AssertBurstCompatibility(entry);
        }

        [Test]
        public void LogMessage_BurstCompatibility_IsUnmanaged()
        {
            // Arrange
            var message = LogMessage.CreateNative(LogLevel.Info, 
                new FixedString64Bytes("TestChannel"), 
                new FixedString512Bytes("Test message"));

            // Act & Assert
            TestUtilities.AssertBurstCompatibility(message);
        }

        [Test]
        public void LogLevel_BurstCompatibility_IsUnmanaged()
        {
            // Arrange
            var level = LogLevel.Info;

            // Act & Assert
            TestUtilities.AssertBurstCompatibility(level);
        }

        [Test]
        public void FixedString_BurstCompatibility_IsUnmanaged()
        {
            // Arrange
            var fixedString64 = new FixedString64Bytes("Test");
            var fixedString128 = new FixedString128Bytes("Test");
            var fixedString512 = new FixedString512Bytes("Test");

            // Act & Assert
            TestUtilities.AssertBurstCompatibility(fixedString64);
            TestUtilities.AssertBurstCompatibility(fixedString128);
            TestUtilities.AssertBurstCompatibility(fixedString512);
        }

        #endregion

        #region Unity Collections Tests

        [Test]
        public void LogEntry_InNativeArray_WorksCorrectly()
        {
            // Arrange
            var count = 10;
            
            // Act
            using (var array = new NativeArray<LogEntry>(count, Allocator.Temp))
            {
                for (int i = 0; i < count; i++)
                {
                    var entry = LogEntry.CreateNative(LogLevel.Info, 
                        new FixedString64Bytes($"Native{i}"), 
                        new FixedString512Bytes($"Native test message {i}"));
                    array[i] = entry;
                }

                // Assert
                Assert.That(array.Length, Is.EqualTo(count));
                for (int i = 0; i < count; i++)
                {
                    Assert.That(array[i].Level, Is.EqualTo(LogLevel.Info));
                    Assert.That(array[i].Channel.ToString(), Is.EqualTo($"Native{i}"));
                    Assert.That(array[i].Message.ToString(), Is.EqualTo($"Native test message {i}"));
                }
            }
        }

        [Test]
        public void LogEntry_InNativeList_WorksCorrectly()
        {
            // Arrange
            var count = 5;
            
            // Act
            using (var list = new NativeList<LogEntry>(count, Allocator.Temp))
            {
                for (int i = 0; i < count; i++)
                {
                    var entry = LogEntry.CreateNative(LogLevel.Warning, 
                        new FixedString64Bytes($"List{i}"), 
                        new FixedString512Bytes($"List message {i}"));
                    list.Add(entry);
                }

                // Assert
                Assert.That(list.Length, Is.EqualTo(count));
                for (int i = 0; i < count; i++)
                {
                    Assert.That(list[i].Level, Is.EqualTo(LogLevel.Warning));
                    Assert.That(list[i].Channel.ToString(), Is.EqualTo($"List{i}"));
                }
            }
        }

        [Test]
        public void LogEntry_InNativeQueue_WorksCorrectly()
        {
            // Arrange
            var count = 3;
            
            // Act
            using (var queue = new NativeQueue<LogEntry>(Allocator.Temp))
            {
                for (int i = 0; i < count; i++)
                {
                    var entry = LogEntry.CreateNative(LogLevel.Debug, 
                        new FixedString64Bytes($"Queue{i}"), 
                        new FixedString512Bytes($"Queue message {i}"));
                    queue.Enqueue(entry);
                }

                // Assert
                Assert.That(queue.Count, Is.EqualTo(count));
                
                for (int i = 0; i < count; i++)
                {
                    var entry = queue.Dequeue();
                    Assert.That(entry.Level, Is.EqualTo(LogLevel.Debug));
                    Assert.That(entry.Channel.ToString(), Is.EqualTo($"Queue{i}"));
                }
            }
        }

        [Test]
        public void LogMessage_InNativeArray_WorksCorrectly()
        {
            // Arrange
            var count = 5;
            
            // Act
            using (var array = new NativeArray<LogMessage>(count, Allocator.Temp))
            {
                for (int i = 0; i < count; i++)
                {
                    var message = LogMessage.CreateNative(LogLevel.Warning, 
                        new FixedString64Bytes($"MsgChannel{i}"), 
                        new FixedString512Bytes($"Message {i}"));
                    array[i] = message;
                }

                // Assert
                Assert.That(array.Length, Is.EqualTo(count));
                for (int i = 0; i < count; i++)
                {
                    Assert.That(array[i].Level, Is.EqualTo(LogLevel.Warning));
                    Assert.That(array[i].Channel.ToString(), Is.EqualTo($"MsgChannel{i}"));
                    Assert.That(array[i].Message.ToString(), Is.EqualTo($"Message {i}"));
                }
            }
        }

        #endregion

        #region Performance Tests

        [Test]
        public void LogEntry_Creation_PerformanceTest()
        {
            // Arrange
            var count = 1000;
            var level = LogLevel.Info;
            var channel = "PerfChannel";
            var message = "Performance test message";

            // Act & Assert
            TestUtilities.AssertExecutionTime(() =>
            {
                for (int i = 0; i < count; i++)
                {
                    var entry = TestDataFactory.CreateLogEntry(level, channel, $"{message} {i}");
                    Assert.That(entry.Level, Is.EqualTo(level));
                }
            }, TimeSpan.FromSeconds(1));
        }

        [Test]
        public void LogEntry_CreateNative_PerformanceTest()
        {
            // Arrange
            var count = 1000;
            var level = LogLevel.Info;
            var channel = new FixedString64Bytes("PerfChannel");
            var message = "Performance test message";

            // Act & Assert
            TestUtilities.AssertExecutionTime(() =>
            {
                for (int i = 0; i < count; i++)
                {
                    var entry = LogEntry.CreateNative(level, channel, 
                        new FixedString512Bytes($"{message} {i}"));
                    Assert.That(entry.Level, Is.EqualTo(level));
                }
            }, TimeSpan.FromSeconds(1));
        }

        [Test]
        public void LogEntry_ToDictionary_PerformanceTest()
        {
            // Arrange
            var count = 100;
            var entries = TestDataFactory.CreateLogEntryBatch(count);

            // Act & Assert
            TestUtilities.AssertExecutionTime(() =>
            {
                foreach (var entry in entries)
                {
                    var dictionary = entry.ToDictionary();
                    Assert.That(dictionary.Count, Is.GreaterThan(0));
                }
            }, TimeSpan.FromSeconds(1));
        }

        [Test]
        public void LogEntry_NativeArray_PerformanceTest()
        {
            // Arrange
            var count = 10000;

            // Act & Assert
            TestUtilities.AssertExecutionTime(() =>
            {
                using (var array = new NativeArray<LogEntry>(count, Allocator.Temp))
                {
                    for (int i = 0; i < count; i++)
                    {
                        var entry = LogEntry.CreateNative(LogLevel.Info, 
                            new FixedString64Bytes($"Perf{i}"), 
                            new FixedString512Bytes($"Performance test {i}"));
                        array[i] = entry;
                    }

                    for (int i = 0; i < array.Length; i++)
                    {
                        Assert.That(array[i].Level, Is.EqualTo(LogLevel.Info));
                    }
                }
            }, TimeSpan.FromSeconds(1));
        }

        #endregion

        #region Memory Tests

        [Test]
        public void LogEntry_Creation_MemoryTest()
        {
            // Arrange
            var count = 100;

            // Act & Assert
            TestUtilities.AssertMemoryUsage(() =>
            {
                var entries = TestDataFactory.CreateLogEntryBatch(count);
                Assert.That(entries.Count, Is.EqualTo(count));
            }, 1024 * 1024); // 1MB limit
        }

        [Test]
        public void LogEntry_CreateNative_MemoryTest()
        {
            // Arrange
            var count = 100;

            // Act & Assert
            TestUtilities.AssertMemoryUsage(() =>
            {
                var entries = new List<LogEntry>();
                for (int i = 0; i < count; i++)
                {
                    entries.Add(LogEntry.CreateNative(LogLevel.Info, 
                        new FixedString64Bytes($"Memory{i}"), 
                        new FixedString512Bytes($"Memory test {i}")));
                }
                Assert.That(entries.Count, Is.EqualTo(count));
            }, 512 * 1024); // 512KB limit - should be less than managed entries
        }

        [Test]
        public void LogEntry_NativeArray_MemoryTest()
        {
            // Arrange
            var count = 1000;

            // Act & Assert
            TestUtilities.AssertMemoryUsage(() =>
            {
                using (var array = new NativeArray<LogEntry>(count, Allocator.Temp))
                {
                    for (int i = 0; i < count; i++)
                    {
                        var entry = LogEntry.CreateNative(LogLevel.Info, 
                            new FixedString64Bytes($"Mem{i}"), 
                            new FixedString512Bytes($"Memory test {i}"));
                        array[i] = entry;
                    }
                    Assert.That(array.Length, Is.EqualTo(count));
                }
            }, 256 * 1024); // 256KB limit
        }

        #endregion

        #region Job System Tests

        [Test]
        public void LogEntry_InJob_WorksCorrectly()
        {
            // Arrange
            var count = 100;
            using (var inputArray = new NativeArray<LogEntry>(count, Allocator.TempJob))
            using (var outputArray = new NativeArray<bool>(count, Allocator.TempJob))
            {
                // Fill input array with test data
                for (int i = 0; i < count; i++)
                {
                    var entry = LogEntry.CreateNative(LogLevel.Info, 
                        new FixedString64Bytes($"Job{i}"), 
                        new FixedString512Bytes($"Job test message {i}"));
                    inputArray[i] = entry;
                }

                var job = new TestLogEntryJob
                {
                    InputEntries = inputArray,
                    OutputResults = outputArray,
                    MinimumLevel = LogLevel.Info
                };

                // Act
                var handle = job.Schedule();
                handle.Complete();

                // Assert
                for (int i = 0; i < count; i++)
                {
                    Assert.That(outputArray[i], Is.True); // All entries should pass the minimum level check
                }
            }
        }

        [Test]
        public void LogMessage_InJob_WorksCorrectly()
        {
            // Arrange
            var count = 50;
            using (var inputArray = new NativeArray<LogMessage>(count, Allocator.TempJob))
            using (var outputArray = new NativeArray<int>(count, Allocator.TempJob))
            {
                // Fill input array with test data
                for (int i = 0; i < count; i++)
                {
                    var message = LogMessage.CreateNative(LogLevel.Warning, 
                        new FixedString64Bytes($"MsgJob{i}"), 
                        new FixedString512Bytes($"Message job test {i}"));
                    inputArray[i] = message;
                }

                var job = new TestLogMessageJob
                {
                    InputMessages = inputArray,
                    OutputSizes = outputArray
                };

                // Act
                var handle = job.Schedule();
                handle.Complete();

                // Assert
                for (int i = 0; i < count; i++)
                {
                    Assert.That(outputArray[i], Is.GreaterThan(0)); // All messages should have a positive size
                }
            }
        }

        [BurstCompile]
        private struct TestLogEntryJob : IJob
        {
            [ReadOnly] public NativeArray<LogEntry> InputEntries;
            [WriteOnly] public NativeArray<bool> OutputResults;
            [ReadOnly] public LogLevel MinimumLevel;

            public void Execute()
            {
                for (int i = 0; i < InputEntries.Length; i++)
                {
                    OutputResults[i] = InputEntries[i].ShouldProcess(MinimumLevel);
                }
            }
        }

        [BurstCompile]
        private struct TestLogMessageJob : IJob
        {
            [ReadOnly] public NativeArray<LogMessage> InputMessages;
            [WriteOnly] public NativeArray<int> OutputSizes;

            public void Execute()
            {
                for (int i = 0; i < InputMessages.Length; i++)
                {
                    OutputSizes[i] = InputMessages[i].GetNativeSize();
                }
            }
        }

        #endregion

        #region New Features Tests

        [Test]
        public void LogEntry_FromLogMessage_ConvertsCorrectly()
        {
            // Arrange
            var logMessage = TestDataFactory.CreateLogMessage(LogLevel.Error, "TestChannel", "Test message");

            // Act
            var logEntry = LogEntry.FromLogMessage(logMessage);

            // Assert
            Assert.That(logEntry.Id, Is.EqualTo(logMessage.Id));
            Assert.That(logEntry.Level, Is.EqualTo(logMessage.Level));
            Assert.That(logEntry.Channel.ToString(), Is.EqualTo(logMessage.Channel.ToString()));
            Assert.That(logEntry.Message.ToString(), Is.EqualTo(logMessage.Message.ToString()));
            Assert.That(logEntry.CorrelationId.ToString(), Is.EqualTo(logMessage.CorrelationId.ToString()));
            Assert.That(logEntry.SourceContext.ToString(), Is.EqualTo(logMessage.SourceContext.ToString()));
        }

        [Test]
        public void LogEntry_WithManagedData_CreatesCorrectly()
        {
            // Arrange
            var baseEntry = LogEntry.CreateNative(LogLevel.Warning, 
                new FixedString64Bytes("TestChannel"), 
                new FixedString512Bytes("Test message"));
            var exception = TestDataFactory.CreateTestException();
            var properties = TestDataFactory.CreateTestProperties();

            // Act
            var entryWithData = LogEntry.WithManagedData(baseEntry, null, exception, properties);

            // Assert
            Assert.That(entryWithData.Id, Is.EqualTo(baseEntry.Id));
            Assert.That(entryWithData.Level, Is.EqualTo(baseEntry.Level));
            Assert.That(entryWithData.Channel.ToString(), Is.EqualTo(baseEntry.Channel.ToString()));
            Assert.That(entryWithData.Message.ToString(), Is.EqualTo(baseEntry.Message.ToString()));
            Assert.That(entryWithData.HasException, Is.True);
            Assert.That(entryWithData.HasProperties, Is.True);
        }

        [Test]
        public void LogMessage_WithManagedData_CreatesCorrectly()
        {
            // Arrange
            var baseMessage = LogMessage.CreateNative(LogLevel.Error, 
                new FixedString64Bytes("TestChannel"), 
                new FixedString512Bytes("Test message"));
            var exception = TestDataFactory.CreateTestException();
            var properties = TestDataFactory.CreateTestProperties();

            // Act
            var messageWithData = LogMessage.WithManagedData(baseMessage, null, exception, properties);

            // Assert
            Assert.That(messageWithData.Id, Is.EqualTo(baseMessage.Id));
            Assert.That(messageWithData.Level, Is.EqualTo(baseMessage.Level));
            Assert.That(messageWithData.Channel.ToString(), Is.EqualTo(baseMessage.Channel.ToString()));
            Assert.That(messageWithData.Message.ToString(), Is.EqualTo(baseMessage.Message.ToString()));
            Assert.That(messageWithData.HasException, Is.True);
            Assert.That(messageWithData.HasProperties, Is.True);
        }

        [Test]
        public void LogMessage_IMessage_PropertiesWork()
        {
            // Arrange
            var correlationId = Guid.NewGuid();
            var logMessage = LogMessage.CreateWithGuidCorrelation(LogLevel.Info, "TestChannel", 
                "Test message", correlationId);

            // Act
            IMessage message = logMessage;

            // Assert
            Assert.That(message.Id, Is.EqualTo(logMessage.Id));
            Assert.That(message.Source.ToString(), Is.EqualTo(logMessage.Source.ToString()));
            Assert.That(message.Priority, Is.EqualTo(logMessage.Priority));
            Assert.That(message.TypeCode, Is.EqualTo(MessageTypeCodes.LogMessage));
            Assert.That(message.TimestampTicks, Is.EqualTo(logMessage.Timestamp.Ticks));
        }

        [Test]
        public void LogEntry_Equals_AndHashCode_WorkCorrectly()
        {
            // Arrange
            var id = Guid.NewGuid();
            var entry1 = new LogEntry(id, DateTime.UtcNow, LogLevel.Info, 
                new FixedString64Bytes("Channel1"), new FixedString512Bytes("Message1"));
            var entry2 = new LogEntry(id, DateTime.UtcNow.AddMinutes(1), LogLevel.Error, 
                new FixedString64Bytes("Channel2"), new FixedString512Bytes("Message2"));
            var entry3 = LogEntry.CreateNative(LogLevel.Info, 
                new FixedString64Bytes("Channel1"), new FixedString512Bytes("Message1"));

            // Act & Assert
            Assert.That(entry1.Equals(entry2), Is.True); // Same ID should be equal
            Assert.That(entry1.Equals(entry3), Is.False); // Different IDs should not be equal
            Assert.That(entry1.GetHashCode(), Is.EqualTo(entry2.GetHashCode())); // Same ID should have same hash
            Assert.That(entry1.GetHashCode(), Is.Not.EqualTo(entry3.GetHashCode())); // Different IDs should have different hash
        }

        [Test]
        public void LogMessage_Equals_AndHashCode_WorkCorrectly()
        {
            // Arrange
            var id = Guid.NewGuid();
            var message1 = new LogMessage(id, DateTime.UtcNow, LogLevel.Info, 
                new FixedString64Bytes("Channel1"), new FixedString512Bytes("Message1"));
            var message2 = new LogMessage(id, DateTime.UtcNow.AddMinutes(1), LogLevel.Error, 
                new FixedString64Bytes("Channel2"), new FixedString512Bytes("Message2"));
            var message3 = LogMessage.CreateNative(LogLevel.Info, 
                new FixedString64Bytes("Channel1"), new FixedString512Bytes("Message1"));

            // Act & Assert
            Assert.That(message1.Equals(message2), Is.True); // Same ID should be equal
            Assert.That(message1.Equals(message3), Is.False); // Different IDs should not be equal
            Assert.That(message1.GetHashCode(), Is.EqualTo(message2.GetHashCode())); // Same ID should have same hash
            Assert.That(message1.GetHashCode(), Is.Not.EqualTo(message3.GetHashCode())); // Different IDs should have different hash
        }

        #endregion
    }
}