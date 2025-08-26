# AhBearStudios Core Pooling System - Comprehensive Testing Plan

## Test Categories Overview

This testing plan covers all aspects of the enhanced pooling system to ensure production readiness and compliance with performance requirements.

## 1. Unit Testing

### 1.1 Core Functionality Tests

#### Strategy Selection Tests
```csharp
[TestFixture]
public class PoolingStrategySelectionTests
{
    [Test]
    public void SelectStrategy_HighPerformanceConfig_ReturnsHighPerformanceStrategy()
    {
        var config = new PoolConfiguration 
        { 
            StrategyType = PoolingStrategyType.HighPerformance,
            PerformanceBudget = PerformanceBudget.For60FPS()
        };
        
        var strategy = _strategySelector.SelectStrategy(config);
        Assert.IsInstanceOf<HighPerformanceStrategy>(strategy);
    }
    
    [Test]
    public void SelectStrategy_NetworkConfig_ReturnsAdaptiveNetworkStrategy()
    {
        var config = new PoolConfiguration 
        { 
            StrategyType = PoolingStrategyType.AdaptiveNetwork,
            MaxCapacity = 1000
        };
        
        var strategy = _strategySelector.SelectStrategy(config);
        Assert.IsInstanceOf<AdaptiveNetworkStrategy>(strategy);
    }
}
```

#### Pool Type Selection Tests
```csharp
[TestFixture]
public class PoolTypeSelectionTests
{
    [Test]
    public void SelectPoolType_NetworkBuffer_ReturnsCorrectBufferType()
    {
        var config = new PoolConfiguration();
        var poolType = _poolTypeSelector.SelectPoolType<PooledNetworkBuffer>(config);
        
        Assert.That(poolType, Is.OneOf(PoolType.SmallBuffer, PoolType.MediumBuffer, PoolType.LargeBuffer));
    }
    
    [Test]
    public void SelectPoolType_LogData_ReturnsManagedLogDataType()
    {
        var config = new PoolConfiguration();
        var poolType = _poolTypeSelector.SelectPoolType<ManagedLogData>(config);
        
        Assert.AreEqual(PoolType.ManagedLogData, poolType);
    }
}
```

### 1.2 Async Operations Tests

#### Cancellation Support Tests
```csharp
[TestFixture]
public class AsyncOperationTests
{
    [Test]
    public async Task GetAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));
        
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _poolingService.GetAsync<TestObject>(cts.Token));
    }
    
    [Test]
    public async Task GetAsync_WithTimeout_ThrowsTimeoutException()
    {
        var timeout = TimeSpan.FromMilliseconds(50);
        
        await Assert.ThrowsAsync<TimeoutException>(() =>
            _poolingService.GetAsync<TestObject>(timeout, CancellationToken.None));
    }
    
    [Test]
    public async Task GetMultipleAsync_ValidCount_ReturnsCorrectNumberOfObjects()
    {
        _poolingService.RegisterPool<TestObject>();
        
        var objects = await _poolingService.GetMultipleAsync<TestObject>(
            count: 5, 
            timeout: TimeSpan.FromSeconds(1), 
            CancellationToken.None);
        
        Assert.AreEqual(5, objects.Count);
        
        await _poolingService.ReturnMultipleAsync(objects, CancellationToken.None);
    }
}
```

### 1.3 Circuit Breaker Tests

```csharp
[TestFixture]
public class CircuitBreakerTests
{
    [Test]
    public async Task CircuitBreaker_ConsecutiveFailures_OpensCircuit()
    {
        // Simulate failures to trigger circuit breaker
        for (int i = 0; i < 5; i++)
        {
            try
            {
                await _poolingService.GetAsync<FailingTestObject>();
            }
            catch
            {
                // Expected failures
            }
        }
        
        var stats = _poolingService.GetCircuitBreakerStatistics();
        Assert.IsTrue(stats.ContainsKey("FailingTestObject"));
    }
    
    [Test]
    public void CircuitBreaker_StateChange_PublishesMessage()
    {
        var messageReceived = false;
        _messageBus.Subscribe<PoolCircuitBreakerStateChangedMessage>(msg => messageReceived = true);
        
        // Trigger circuit breaker state change
        var circuitBreaker = _circuitBreakerFactory.CreateCircuitBreaker("TestPool");
        circuitBreaker.Open("Test");
        
        Assert.IsTrue(messageReceived);
    }
}
```

### 1.4 Performance Budget Tests

```csharp
[TestFixture]
public class PerformanceBudgetTests
{
    [Test]
    public void PerformanceBudget_ViolationDetection_LogsWarning()
    {
        var budget = PerformanceBudget.For60FPS();
        var logMessages = new List<string>();
        
        _loggingService.OnLogMessage += (level, message) => logMessages.Add(message);
        
        // Simulate slow operation
        _poolingService.RegisterPool<SlowTestObject>(new PoolConfiguration 
        { 
            PerformanceBudget = budget 
        });
        
        var obj = _poolingService.Get<SlowTestObject>(); // This should violate budget
        
        Assert.IsTrue(logMessages.Any(msg => msg.Contains("Performance budget violated")));
    }
    
    [Test]
    public void IsPerformanceAcceptable_HighViolationRate_ReturnsFalse()
    {
        // Simulate high violation rate
        for (int i = 0; i < 20; i++)
        {
            _poolingService.Get<SlowTestObject>();
        }
        
        Assert.IsFalse(_poolingService.IsPerformanceAcceptable());
    }
}
```

### 1.5 Auto-Scaling Tests

```csharp
[TestFixture]
public class AutoScalingTests
{
    [Test]
    public void StartAutoScaling_ValidInterval_StartsSuccessfully()
    {
        Assert.DoesNotThrow(() => 
            _poolingService.StartAutoScaling(TimeSpan.FromSeconds(1)));
        
        _poolingService.StopAutoScaling();
    }
    
    [Test]
    public async Task AutoScaling_HighUtilization_TriggersScaleUp()
    {
        _poolingService.RegisterPool<TestObject>(new PoolConfiguration 
        { 
            InitialCapacity = 10,
            MaxCapacity = 100
        });
        
        _poolingService.StartAutoScaling(TimeSpan.FromMilliseconds(100));
        
        // Create high utilization
        var objects = new List<TestObject>();
        for (int i = 0; i < 9; i++) // 90% utilization
        {
            objects.Add(_poolingService.Get<TestObject>());
        }
        
        await UniTask.Delay(500); // Wait for scaling check
        
        var stats = _poolingService.GetAutoScalingStatistics();
        Assert.IsTrue(stats.ContainsKey("TestObject"));
        
        _poolingService.StopAutoScaling();
    }
}
```

### 1.6 Error Recovery Tests

```csharp
[TestFixture]
public class ErrorRecoveryTests
{
    [Test]
    public async Task ForcePoolRecovery_ValidPool_CompletesSuccessfully()
    {
        _poolingService.RegisterPool<TestObject>();
        
        await Assert.DoesNotThrowAsync(() => 
            _poolingService.ForcePoolRecovery("TestObject"));
    }
    
    [Test]
    public void IsRecoverySystemHealthy_NormalOperation_ReturnsTrue()
    {
        _poolingService.RegisterPool<TestObject>();
        
        Assert.IsTrue(_poolingService.IsRecoverySystemHealthy());
    }
    
    [Test]
    public void GetErrorRecoveryStatistics_AfterRecovery_ShowsCorrectMetrics()
    {
        // Force some recoveries
        _poolingService.ForcePoolRecovery("TestObject").Forget();
        
        var stats = _poolingService.GetErrorRecoveryStatistics();
        Assert.IsNotEmpty(stats);
    }
}
```

## 2. Integration Testing

### 2.1 Service Integration Tests

#### Health Check Integration
```csharp
[TestFixture]
public class HealthCheckIntegrationTests
{
    [Test]
    public void RegisterHealthChecks_WithHealthService_RegistersSuccessfully()
    {
        var healthService = new Mock<IHealthCheckService>();
        var poolingService = new PoolingService(
            _messageBus, _logging, _profiler, _alertService, 
            healthService.Object);
        
        poolingService.RegisterHealthChecks();
        
        healthService.Verify(hs => hs.RegisterHealthCheck(It.IsAny<IHealthCheck>()), Times.AtLeastOnce);
    }
}
```

#### Message Bus Integration
```csharp
[TestFixture]
public class MessageBusIntegrationTests
{
    [Test]
    public async Task PoolOperations_PublishesCorrectMessages()
    {
        var messages = new List<IMessage>();
        _messageBus.Subscribe<PoolObjectRetrievedMessage>(msg => messages.Add(msg));
        _messageBus.Subscribe<PoolObjectReturnedMessage>(msg => messages.Add(msg));
        
        _poolingService.RegisterPool<TestObject>();
        
        var obj = await _poolingService.GetAsync<TestObject>();
        await _poolingService.ReturnAsync(obj);
        
        Assert.AreEqual(2, messages.Count);
        Assert.IsInstanceOf<PoolObjectRetrievedMessage>(messages[0]);
        Assert.IsInstanceOf<PoolObjectReturnedMessage>(messages[1]);
    }
}
```

### 2.2 End-to-End Workflow Tests

```csharp
[TestFixture]
public class EndToEndWorkflowTests
{
    [Test]
    public async Task CompletePoolWorkflow_AllFeatures_WorksTogether()
    {
        // Setup comprehensive configuration
        var config = new PoolConfiguration
        {
            InitialCapacity = 20,
            MaxCapacity = 200,
            MinCapacity = 10,
            StrategyType = PoolingStrategyType.HighPerformance,
            PerformanceBudget = PerformanceBudget.For60FPS()
        };
        
        _poolingService.RegisterPool<TestObject>(config);
        _poolingService.StartAutoScaling(TimeSpan.FromSeconds(1));
        _poolingService.RegisterHealthChecks();
        
        // Perform operations
        var objects = await _poolingService.GetMultipleAsync<TestObject>(
            count: 15, 
            timeout: TimeSpan.FromSeconds(2), 
            CancellationToken.None);
        
        Assert.AreEqual(15, objects.Count);
        
        await _poolingService.ReturnMultipleAsync(objects, CancellationToken.None);
        
        // Verify all systems are healthy
        Assert.IsTrue(_poolingService.IsHealthy());
        Assert.IsTrue(_poolingService.IsPerformanceAcceptable());
        Assert.IsTrue(_poolingService.IsRecoverySystemHealthy());
        
        _poolingService.StopAutoScaling();
    }
}
```

## 3. Performance Testing

### 3.1 Frame Rate Impact Testing

```csharp
[TestFixture]
public class PerformanceImpactTests
{
    [Test]
    public void PoolOperations_Under60FPSBudget_MeetsFrameTimeRequirements()
    {
        _poolingService.RegisterPool<TestObject>(new PoolConfiguration 
        { 
            PerformanceBudget = PerformanceBudget.For60FPS() 
        });
        
        var stopwatch = Stopwatch.StartNew();
        
        // Perform 1000 operations
        for (int i = 0; i < 1000; i++)
        {
            var obj = _poolingService.Get<TestObject>();
            _poolingService.Return(obj);
        }
        
        stopwatch.Stop();
        
        // Average operation time should be well under frame budget
        var avgOperationTime = stopwatch.Elapsed.TotalMilliseconds / 1000;
        Assert.Less(avgOperationTime, 0.1); // 0.1ms per operation
    }
    
    [Test]
    public async Task ConcurrentOperations_HighLoad_MaintainsPerformance()
    {
        _poolingService.RegisterPool<TestObject>(new PoolConfiguration 
        { 
            InitialCapacity = 100,
            MaxCapacity = 1000 
        });
        
        var tasks = new List<UniTask>();
        
        // Create concurrent load
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(UniTask.Run(async () =>
            {
                for (int j = 0; j < 10; j++)
                {
                    var obj = await _poolingService.GetAsync<TestObject>();
                    await _poolingService.ReturnAsync(obj);
                }
            }));
        }
        
        var stopwatch = Stopwatch.StartNew();
        await UniTask.WhenAll(tasks);
        stopwatch.Stop();
        
        // 1000 operations across 100 concurrent tasks should complete quickly
        Assert.Less(stopwatch.Elapsed.TotalSeconds, 5);
    }
}
```

### 3.2 Memory Allocation Testing

```csharp
[TestFixture]
public class MemoryAllocationTests
{
    [Test]
    public void PoolOperations_ZeroAllocation_NoGCPressure()
    {
        _poolingService.RegisterPool<TestObject>();
        
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var initialMemory = GC.GetTotalMemory(false);
        
        // Perform operations
        for (int i = 0; i < 1000; i++)
        {
            var obj = _poolingService.Get<TestObject>();
            _poolingService.Return(obj);
        }
        
        var finalMemory = GC.GetTotalMemory(false);
        var allocatedMemory = finalMemory - initialMemory;
        
        // Should have minimal allocation beyond pool objects themselves
        Assert.Less(allocatedMemory, 1024 * 100); // Less than 100KB
    }
}
```

### 3.3 Scaling Performance Testing

```csharp
[TestFixture]
public class ScalingPerformanceTests
{
    [Test]
    public async Task AutoScaling_ScaleUpOperation_CompletesQuickly()
    {
        _poolingService.RegisterPool<TestObject>(new PoolConfiguration 
        { 
            InitialCapacity = 10,
            MaxCapacity = 100
        });
        
        _poolingService.StartAutoScaling(TimeSpan.FromMilliseconds(100));
        
        var stopwatch = Stopwatch.StartNew();
        
        // Force scaling by creating high utilization
        var objects = new List<TestObject>();
        for (int i = 0; i < 9; i++)
        {
            objects.Add(_poolingService.Get<TestObject>());
        }
        
        // Wait for scaling to complete
        await UniTask.Delay(1000);
        
        stopwatch.Stop();
        
        // Scaling should complete quickly
        Assert.Less(stopwatch.Elapsed.TotalSeconds, 2);
        
        _poolingService.StopAutoScaling();
    }
}
```

## 4. Stress Testing

### 4.1 High Load Testing

```csharp
[TestFixture]
public class StressTests
{
    [Test]
    public async Task HighConcurrency_1000Threads_SystemRemainsStable()
    {
        _poolingService.RegisterPool<TestObject>(new PoolConfiguration 
        { 
            InitialCapacity = 100,
            MaxCapacity = 10000
        });
        
        var tasks = new List<UniTask>();
        var successCount = 0;
        
        // Create 1000 concurrent tasks
        for (int i = 0; i < 1000; i++)
        {
            tasks.Add(UniTask.Run(async () =>
            {
                try
                {
                    var obj = await _poolingService.GetAsync<TestObject>();
                    await UniTask.Delay(Random.Shared.Next(1, 10));
                    await _poolingService.ReturnAsync(obj);
                    Interlocked.Increment(ref successCount);
                }
                catch
                {
                    // Some failures expected under extreme load
                }
            }));
        }
        
        await UniTask.WhenAll(tasks);
        
        // At least 90% should succeed
        Assert.Greater(successCount, 900);
        Assert.IsTrue(_poolingService.IsHealthy());
    }
    
    [Test]
    public async Task LongRunning_24HourTest_SystemRemainsHealthy()
    {
        // Simulate 24-hour operation (scaled down for testing)
        var testDuration = TimeSpan.FromMinutes(5); // 5 minutes represents 24 hours
        var endTime = DateTime.UtcNow + testDuration;
        
        _poolingService.RegisterPool<TestObject>();
        _poolingService.StartAutoScaling(TimeSpan.FromSeconds(10));
        
        while (DateTime.UtcNow < endTime)
        {
            // Simulate varying load
            var loadFactor = Random.Shared.NextDouble();
            var operationCount = (int)(loadFactor * 100);
            
            var tasks = new List<UniTask>();
            for (int i = 0; i < operationCount; i++)
            {
                tasks.Add(UniTask.Run(async () =>
                {
                    var obj = await _poolingService.GetAsync<TestObject>();
                    await UniTask.Delay(Random.Shared.Next(1, 5));
                    await _poolingService.ReturnAsync(obj);
                }));
            }
            
            await UniTask.WhenAll(tasks);
            await UniTask.Delay(1000); // 1 second intervals
        }
        
        // System should still be healthy after long running test
        Assert.IsTrue(_poolingService.IsHealthy());
        Assert.IsTrue(_poolingService.IsRecoverySystemHealthy());
        
        _poolingService.StopAutoScaling();
    }
}
```

## 5. Failure Testing

### 5.1 Circuit Breaker Failure Testing

```csharp
[TestFixture]
public class FailureTests
{
    [Test]
    public async Task CircuitBreaker_UnderFailureLoad_ProtectsSystem()
    {
        _poolingService.RegisterPool<FailingTestObject>();
        
        // Generate failures to trip circuit breaker
        var failureCount = 0;
        for (int i = 0; i < 10; i++)
        {
            try
            {
                await _poolingService.GetAsync<FailingTestObject>();
            }
            catch
            {
                failureCount++;
            }
        }
        
        // Circuit breaker should be protecting the system
        var stats = _poolingService.GetCircuitBreakerStatistics();
        Assert.IsTrue(stats.ContainsKey("FailingTestObject"));
        Assert.Greater(failureCount, 0);
    }
}
```

### 5.2 Recovery Testing

```csharp
[TestFixture]
public class RecoveryTests
{
    [Test]
    public async Task EmergencyRecovery_CorruptedPool_RecreatesPool()
    {
        _poolingService.RegisterPool<TestObject>();
        
        // Simulate pool corruption by forcing multiple failures
        for (int i = 0; i < 10; i++)
        {
            try
            {
                await _poolingService.GetAsync<CorruptedTestObject>();
            }
            catch
            {
                // Expected failures
            }
        }
        
        // Force emergency recovery
        await _poolingService.ForcePoolRecovery("CorruptedTestObject");
        
        // Pool should be functional again
        Assert.DoesNotThrow(() => _poolingService.Get<TestObject>());
    }
}
```

## 6. Test Execution Plan

### 6.1 Continuous Integration Tests
- **Unit Tests**: Run on every commit
- **Integration Tests**: Run on pull requests
- **Performance Tests**: Run nightly
- **Stress Tests**: Run weekly

### 6.2 Pre-Production Testing
- **Full Test Suite**: Run before any production deployment
- **Load Testing**: Simulate production traffic patterns
- **Failover Testing**: Test all failure scenarios
- **Performance Validation**: Confirm frame rate requirements

### 6.3 Production Monitoring
- **Health Check Monitoring**: Continuous health monitoring
- **Performance Metrics**: Real-time performance tracking
- **Alert Verification**: Validate alert thresholds
- **Recovery Testing**: Periodic recovery drills

## Test Data and Mock Objects

### Test Object Definitions
```csharp
public class TestObject : IPooledObject
{
    public void Reset() { }
    public bool IsValid() => true;
    public bool CanBePooled() => true;
    public long GetEstimatedMemoryUsage() => 64;
    public HealthStatus GetHealthStatus() => HealthStatus.Healthy;
}

public class SlowTestObject : IPooledObject
{
    public void Reset() 
    { 
        // Simulate slow operation
        Thread.Sleep(10); 
    }
    // ... other implementations
}

public class FailingTestObject : IPooledObject
{
    public void Reset() => throw new InvalidOperationException("Simulated failure");
    // ... other implementations
}
```

## Expected Test Results

### Performance Benchmarks
- **Get/Return Operations**: < 0.1ms average
- **Concurrent Operations**: Support 1000+ concurrent operations
- **Memory Allocation**: < 100KB for 1000 operations
- **Frame Rate Impact**: No detectable impact on 60 FPS

### Reliability Metrics
- **Circuit Breaker Protection**: 99.9% uptime under failure conditions
- **Auto-scaling**: Sub-second response to utilization changes
- **Recovery Success**: > 95% automatic recovery success rate
- **Error Handling**: Graceful degradation under all failure scenarios

This comprehensive testing plan ensures the enhanced pooling system meets all production requirements and maintains Unity's performance standards.