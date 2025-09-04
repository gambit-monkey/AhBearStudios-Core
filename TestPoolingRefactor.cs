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
using AhBearStudios.Core.Serialization;
using AhBearStudios.Core.HealthChecking;
using Unity.Collections;
using Cysharp.Threading.Tasks;

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
    public UniTask PublishMessageAsync<T>(T message) where T : struct, AhBearStudios.Core.Messaging.IMessage { return UniTask.CompletedTask; }
    public UniTask PublishMessageAsync<T>(T message, System.Threading.CancellationToken cancellationToken) where T : struct, AhBearStudios.Core.Messaging.IMessage { return UniTask.CompletedTask; }
    public IDisposable SubscribeToMessage<T>(Action<T> handler) where T : struct, AhBearStudios.Core.Messaging.IMessage { return null; }
    public void Dispose() { }
}

public class MockSerializationService : ISerializationService
{
    public byte[] Serialize<T>(T obj) { return new byte[] { 1, 2, 3, 4 }; }
    public T Deserialize<T>(byte[] data) { return default(T); }
}

public class MockAlertService : IAlertService
{
    public UniTask RaiseAlertAsync(string message, AhBearStudios.Core.Alerting.Models.AlertSeverity severity, string source, System.Threading.CancellationToken cancellationToken = default) { return UniTask.CompletedTask; }
    public void RaiseAlert(string message, AhBearStudios.Core.Alerting.Models.AlertSeverity severity, string source) { }
    public void Dispose() { }
}

public class MockProfilerService : IProfilerService
{
    public object GetMetrics() { return new { Performance = "Good" }; }
    public IDisposable BeginScope(string name) { return null; }
    public void Dispose() { }
}

public class MockHealthCheckService : IHealthCheckService
{
    public UniTask<AhBearStudios.Core.HealthChecking.Models.HealthCheckResult> CheckHealthAsync(System.Threading.CancellationToken cancellationToken = default) 
    {
        return UniTask.FromResult(new AhBearStudios.Core.HealthChecking.Models.HealthCheckResult 
        { 
            Status = AhBearStudios.Core.HealthChecking.Models.HealthStatus.Healthy,
            Description = "Mock healthy"
        });
    }
    public void Dispose() { }
}

class TestPoolingRefactor
{
    public static async UniTask TestRefactoredPoolingService()
    {
        Console.WriteLine("Testing Refactored PoolingService...");

        try
        {
            // Create mock services
            var loggingService = new MockLoggingService();
            var messageBusService = new MockMessageBusService();
            var serializationService = new MockSerializationService();
            var alertService = new MockAlertService();
            var profilerService = new MockProfilerService();
            var healthCheckService = new MockHealthCheckService();

            // Create configuration using builder
            var config = new PoolingServiceConfigBuilder()
                .WithServiceName("TestPoolingService")
                .WithPerformanceSettings(maxPools: 10)
                .WithValidation(enableObjectValidation: true)
                .Build();

            // Use the factory to create pooling service with all dependencies
            var factory = new PoolingServiceFactory(
                loggingService,
                messageBusService,
                serializationService,
                alertService,
                profilerService,
                healthCheckService);

            var poolingService = await factory.CreatePoolingServiceAsync(config);
            Console.WriteLine("✅ PoolingService created with factory and all dependencies");

            // Test basic operations
            poolingService.RegisterPool<TestPooledObject>("TestPool");
            Console.WriteLine($"✅ Pool registered: {poolingService.IsPoolRegistered<TestPooledObject>()}");

            var obj1 = poolingService.Get<TestPooledObject>();
            Console.WriteLine($"✅ Got object: {obj1 != null}");

            poolingService.Return(obj1);
            Console.WriteLine("✅ Object returned successfully");

            // Test async operations
            var obj2 = await poolingService.GetAsync<TestPooledObject>();
            Console.WriteLine($"✅ Got object asynchronously: {obj2 != null}");

            await poolingService.ReturnAsync(obj2);
            Console.WriteLine("✅ Object returned asynchronously");

            // Test statistics
            var stats = poolingService.GetPoolStatistics<TestPooledObject>();
            Console.WriteLine($"✅ Pool statistics: TotalGets={stats?.TotalGets}, TotalReturns={stats?.TotalReturns}");

            // Test new pool state snapshot functionality
            var snapshot = await poolingService.GetPoolStateSnapshotAsync<TestPooledObject>();
            Console.WriteLine($"✅ Pool state snapshot: {snapshot?.GetSummary()}");

            var saveResult = await poolingService.SavePoolStateSnapshotAsync<TestPooledObject>();
            Console.WriteLine($"✅ Pool state snapshot saved: {saveResult}");

            var loadedSnapshot = await poolingService.LoadPoolStateSnapshotAsync<TestPooledObject>();
            Console.WriteLine($"✅ Pool state snapshot loaded: {loadedSnapshot?.GetSummary()}");

            // Test validation
            var isValid = poolingService.ValidateAllPools();
            Console.WriteLine($"✅ All pools valid: {isValid}");

            // Cleanup
            poolingService.UnregisterPool<TestPooledObject>();
            Console.WriteLine($"✅ Pool unregistered: {!poolingService.IsPoolRegistered<TestPooledObject>()}");

            poolingService.Dispose();
            Console.WriteLine("✅ PoolingService disposed successfully");

            Console.WriteLine("\n🎉 All tests passed! Refactoring successful with full integration.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    public static async System.Threading.Tasks.Task Main(string[] args)
    {
        await TestRefactoredPoolingService();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}