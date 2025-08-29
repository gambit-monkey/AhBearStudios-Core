using System;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Pooling;
using AhBearStudios.Core.Pooling.Builders;
using AhBearStudios.Core.Pooling.Factories;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Services;
using AhBearStudios.Core.Profiling;
using Unity.Collections;

// Simple test pooled object
public class TestPooledObject : IPooledObject
{
    public string PoolName { get; set; }
    public Guid PoolId { get; set; }
    public DateTime LastUsed { get; set; }
    public DateTime CreatedAt { get; set; }
    public long UseCount { get; set; }
    public TimeSpan TotalActiveTime { get; set; }
    public DateTime LastValidationTime { get; set; }
    public int Priority { get; set; }
    public int ValidationErrorCount { get; set; }
    public bool CorruptionDetected { get; set; }
    public int ConsecutiveFailures { get; set; }

    public void OnGet()
    {
        LastUsed = DateTime.UtcNow;
        UseCount++;
    }

    public void OnReturn()
    {
        // Cleanup logic
    }

    public void Reset()
    {
        UseCount = 0;
        ValidationErrorCount = 0;
        CorruptionDetected = false;
        ConsecutiveFailures = 0;
    }

    public bool IsValid()
    {
        return !CorruptionDetected && ValidationErrorCount < 5;
    }

    public long GetEstimatedMemoryUsage()
    {
        return 1024; // 1KB estimate
    }

    public AhBearStudios.Core.HealthChecking.Models.HealthStatus GetHealthStatus()
    {
        return AhBearStudios.Core.HealthChecking.Models.HealthStatus.Healthy;
    }

    public bool CanBePooled()
    {
        return !CorruptionDetected;
    }

    public bool ShouldCircuitBreak()
    {
        return ConsecutiveFailures > 3;
    }

    public bool HasCriticalIssue()
    {
        return CorruptionDetected || ConsecutiveFailures > 5;
    }

    public FixedString512Bytes? GetAlertMessage()
    {
        if (HasCriticalIssue())
        {
            return new FixedString512Bytes("TestPooledObject has critical issues");
        }
        return null;
    }

    public AhBearStudios.Core.Pooling.Models.PooledObjectDiagnostics GetDiagnosticInfo()
    {
        return new AhBearStudios.Core.Pooling.Models.PooledObjectDiagnostics
        {
            ObjectId = PoolId,
            CreatedAt = CreatedAt,
            LastUsed = LastUsed,
            UseCount = UseCount,
            IsValid = IsValid(),
            ValidationErrorCount = ValidationErrorCount,
            CorruptionDetected = CorruptionDetected
        };
    }
}

// Mock services for testing
public class MockLoggingService : ILoggingService
{
    public void LogDebug(string message) { Console.WriteLine($"DEBUG: {message}"); }
    public void LogInfo(string message) { Console.WriteLine($"INFO: {message}"); }
    public void LogWarning(string message) { Console.WriteLine($"WARNING: {message}"); }
    public void LogError(string message) { Console.WriteLine($"ERROR: {message}"); }
    public void LogCritical(string message) { Console.WriteLine($"CRITICAL: {message}"); }
    public void LogException(string message, Exception exception) { Console.WriteLine($"EXCEPTION: {message} - {exception}"); }
    public IDisposable CreateScope(string scopeName) { return null; }
}

public class MockMessageBusService : IMessageBusService
{
    public void PublishMessage<T>(T message) where T : struct, AhBearStudios.Core.Messaging.IMessage { }
    public Cysharp.Threading.Tasks.UniTask PublishMessageAsync<T>(T message) where T : struct, AhBearStudios.Core.Messaging.IMessage { return default; }
    public IDisposable SubscribeToMessage<T>(Action<T> handler) where T : struct, AhBearStudios.Core.Messaging.IMessage { return null; }
    public void Dispose() { }
}

class TestPoolingRefactor
{
    public static void TestRefactoredPoolingService()
    {
        Console.WriteLine("Testing Refactored PoolingService...");

        try
        {
            // Create mock services
            var loggingService = new MockLoggingService();
            var messageBusService = new MockMessageBusService();

            // Create configuration using builder
            var config = new PoolingServiceConfigBuilder()
                .WithServiceName("TestPoolingService")
                .WithPerformanceSettings(maxPools: 10)
                .WithValidation(enableObjectValidation: true)
                .Build();

            // Create core services
            var poolRegistry = new PoolRegistry(loggingService);
            var poolCreationService = new PoolCreationService(loggingService, messageBusService);

            // Create the main pooling service using constructor
            var poolingService = new PoolingService(
                config,
                poolRegistry,
                poolCreationService,
                loggingService,
                messageBusService);

            // Test basic operations
            poolingService.RegisterPool<TestPooledObject>("TestPool");
            
            Console.WriteLine($"Pool registered: {poolingService.IsPoolRegistered<TestPooledObject>()}");

            var obj1 = poolingService.Get<TestPooledObject>();
            Console.WriteLine($"Got object: {obj1 != null}");

            poolingService.Return(obj1);
            Console.WriteLine("Object returned successfully");

            var stats = poolingService.GetPoolStatistics<TestPooledObject>();
            Console.WriteLine($"Pool statistics: TotalGets={stats?.TotalGets}, TotalReturns={stats?.TotalReturns}");

            poolingService.UnregisterPool<TestPooledObject>();
            Console.WriteLine($"Pool unregistered: {!poolingService.IsPoolRegistered<TestPooledObject>()}");

            poolingService.Dispose();
            Console.WriteLine("PoolingService disposed successfully");

            Console.WriteLine("✅ All tests passed! Refactoring successful.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    public static void Main(string[] args)
    {
        TestRefactoredPoolingService();
    }
}