# AhBearStudios Core - Unity Game Development Testing Guidelines

This document provides comprehensive testing guidelines for the AhBearStudios Core project, ensuring consistent, production-ready test suites that follow Unity game development best practices.

## Testing Philosophy

AhBearStudios Core testing prioritizes **game development requirements** over enterprise testing patterns:

- **Frame Budget Compliance**: All operations must complete within 16.67ms (60 FPS)
- **Zero-Allocation Patterns**: Validate Unity.Collections usage and memory efficiency
- **Production Readiness**: Test real-world game scenarios, not just happy paths
- **Performance Critical**: Benchmark operations under load with realistic data volumes
- **Cross-Platform Compatibility**: Ensure tests work across Unity's target platforms

## Test Organization Structure

### Test Assembly Architecture

Tests are organized into two main assemblies matching the core package structure:

```
Assets/com.ahbearstudios.core.tests/
├── AhBearStudios.Core.Tests.asmdef        // Test assembly definition
├── Shared/                                 // Shared test infrastructure
│   ├── Mocks/                             // Mock service implementations
│   ├── Base/                              // Base test classes
│   ├── Utilities/                         // Test utilities and helpers
│   └── Builders/                          // Test data builders
└── {SystemName}/                          // Per-system test suites
    ├── Unit/                              // Unit tests for individual components
    └── Integration/                       // Integration tests for complete workflows
```

### Test Assembly Definition

```json
{
    "name": "AhBearStudios.Core.Tests",
    "rootNamespace": "AhBearStudios.Core.Tests",
    "references": [
        "GUID:0acc523941302664db1f4e527237feb3",  // NUnit
        "GUID:6055be8ebefd69e48b49212b09b47b2f",  // Test Runner
        "GUID:27619889b8ba8c24980f49ee34dbb44a",  // Core assembly
        "GUID:f06555f75b070af458a003d92f9efb00",  // Unity Collections
        "GUID:593a5b492d29ac6448b1ebf7f035ef33",  // ZLinq
        "GUID:6546d7765b4165b40850b3667f981c26"   // UniTask
    ],
    "includePlatforms": ["Editor"],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": ["nunit.framework.dll"],
    "autoReferenced": false,
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "versionDefines": [],
    "noEngineReferences": false
}
```

## Shared Test Infrastructure

### **ALWAYS Use Shared Mock Services**

**Never create system-specific mocks** - use the shared infrastructure in `Assets/com.ahbearstudios.core.tests/Shared/Mocks/`:

```csharp
// ✅ CORRECT: Use shared mock services
public class MySystemTests : BaseServiceTest
{
    [SetUp]
    public override void Setup()
    {
        base.Setup(); // Initializes all shared mocks

        // MockLogging, MockMessageBus, MockSerialization, etc. are ready
        var myService = new MyService(MockLogging, MockMessageBus);
    }
}

// ❌ AVOID: Creating system-specific mocks
public class MySystemTests
{
    private IMockLoggingService _customMockLogging; // Don't do this
}
```

### Available Shared Mock Services

- **`MockLoggingService`** - Captures log calls with verification helpers
- **`MockMessageBusService`** - Records published messages, simulates subscriptions
- **`MockSerializationService`** - Configurable serialization for testing
- **`MockPoolingService`** - Simulates object pooling without actual pooling
- **`MockProfilerService`** - Records profiling calls for performance testing
- **`MockHealthCheckService`** - Simulates health check operations

### Base Test Classes Hierarchy

**Choose the appropriate base class** for your test type:

```csharp
// Unit tests for individual services/components
public class MyServiceTests : BaseServiceTest
{
    // Provides: All mock services, correlation helpers, assertion helpers
}

// Integration tests testing multiple services together
public class MyIntegrationTests : BaseIntegrationTest
{
    // Provides: Service container, performance helpers, async utilities
}

// Performance tests with frame budget validation
public class MyPerformanceTests : BasePerformanceTest
{
    // Provides: Performance measurement, allocation tracking, stress testing
}
```

## Test Naming and Organization

### Test Method Naming Convention

**ALWAYS follow the pattern**: `MethodName_StateUnderTest_ExpectedBehavior`

```csharp
[Test]
public void RaiseAlert_WithValidMessage_RaisesAlertSuccessfully()
{
    // Arrange
    var message = "Test alert message";
    var severity = AlertSeverity.Warning;

    // Act
    _alertService.RaiseAlert(message, severity, "TestSource");

    // Assert
    var activeAlerts = _alertService.GetActiveAlerts();
    Assert.That(activeAlerts, Is.Not.Empty);
}

[Test]
public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
{
    // Arrange, Act & Assert
    Assert.Throws<ArgumentNullException>(() => new MyService(null));
}
```

### Test Categories and Organization

Organize tests into logical categories within each test class:

```csharp
[TestFixture]
public class AlertServiceTests : BaseServiceTest
{
    #region Service Lifecycle Tests
    [Test] public void Constructor_WithValidConfiguration_InitializesCorrectly() { }
    [Test] public void StartAsync_WhenNotStarted_StartsServiceSuccessfully() { }
    [Test] public void Dispose_WhenCalled_DisposesServiceCleanly() { }
    #endregion

    #region Core Functionality Tests
    [Test] public void RaiseAlert_WithValidAlert_RaisesSuccessfully() { }
    [Test] public void RaiseAlert_WhenDisabled_DoesNotRaiseAlert() { }
    #endregion

    #region Error Handling Tests
    [Test] public void RaiseAlert_WithNullAlert_DoesNotThrow() { }
    [Test] public void RegisterChannel_WithNullChannel_DoesNotThrow() { }
    #endregion
}
```

## Performance Testing Requirements

### Frame Budget Compliance

**ALWAYS validate operations complete within Unity's 16.67ms frame budget**:

```csharp
[Test]
public async Task BulkOperation_WithManyItems_CompletesWithinFrameBudget()
{
    // Arrange
    var items = CreateTestData(1000);

    // Act & Assert
    var result = await ExecuteWithPerformanceMeasurementAsync(
        () => _service.ProcessBulkAsync(items),
        "BulkProcessing",
        TestConstants.FrameBudget); // 16.67ms limit

    // Verify performance
    Assert.That(result.Duration, Is.LessThan(TestConstants.FrameBudget));
    LogPerformanceMetrics(result);
}
```

### Zero-Allocation Pattern Validation

**Validate Unity.Collections usage** and memory efficiency:

```csharp
[Test]
public void FastPathOperation_WithUnityCollections_ProducesZeroAllocations()
{
    // Arrange
    var data = new FixedString512Bytes("Test data");

    // Act & Assert
    var allocResult = MeasureAllocations(() =>
    {
        _service.ProcessWithFixedString(data);
    }, "FastPathOperation");

    AssertZeroAllocations(allocResult);
}

[Test]
public void SlowPathOperation_WithManagedTypes_HasAcceptableAllocations()
{
    // Arrange
    var data = "Test data";

    // Act & Assert
    var allocResult = MeasureAllocations(() =>
    {
        _service.ProcessWithString(data);
    }, "SlowPathOperation");

    AssertAcceptableAllocations(allocResult, maxBytes: 1024); // 1KB limit
}
```

### Stress Testing Requirements

**Test system behavior under realistic game load**:

```csharp
[Test]
public async Task SystemUnderLoad_With1000Operations_MaintainsPerformance()
{
    // Arrange
    var operationCount = 1000;

    // Act
    var stressResult = await PerformStressTestAsync(
        () => _service.ProcessSingleOperationAsync(),
        "StressTest",
        iterations: operationCount,
        totalTimeLimit: TimeSpan.FromSeconds(2));

    // Assert
    Assert.That(stressResult.FailureCount, Is.EqualTo(0));
    Assert.That(stressResult.Statistics.AverageDuration, Is.LessThan(TimeSpan.FromMilliseconds(2)));

    // Verify system remains healthy
    Assert.That(_service.IsHealthy, Is.True);
}
```

## Builder → Config → Factory → Service Testing

### Builder Testing Pattern

**Test the complete builder workflow** with fluent API validation:

```csharp
[TestFixture]
public class AlertBuilderTests : BaseServiceTest
{
    [Test]
    public void FluentChain_WithMultipleOperations_BuildsCorrectConfiguration()
    {
        // Act - Test complete fluent chain
        var config = new AlertConfigBuilder(MockPooling)
            .ForProduction()
            .WithMinimumSeverity(AlertSeverity.Warning)
            .WithHistorySize(500)
            .AddSeverityFilter("ErrorFilter", AlertSeverity.Error)
            .AddRateLimitFilter("RateLimit", 50)
            .AddConsoleChannel("Console")
            .Build();

        // Assert - Verify all configuration is applied correctly
        Assert.That(config.MinimumSeverity, Is.EqualTo(AlertSeverity.Warning));
        Assert.That(config.MaxHistorySize, Is.EqualTo(500));
        Assert.That(config.FilterConfigurations.Count, Is.EqualTo(2));
    }

    [Test]
    public void Build_CalledMultipleTimes_ReturnsConsistentConfiguration()
    {
        // Arrange
        _builder.ForProduction().WithMinimumSeverity(AlertSeverity.Warning);

        // Act
        var config1 = _builder.Build();
        var config2 = _builder.Build();

        // Assert
        Assert.That(config1.MinimumSeverity, Is.EqualTo(config2.MinimumSeverity));
    }
}
```

### Factory Testing Pattern

**Test simple creation without lifecycle management**:

```csharp
[TestFixture]
public class AlertServiceFactoryTests : BaseServiceTest
{
    [Test]
    public async Task CreateAlertServiceAsync_WithValidConfig_CreatesService()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var factory = new AlertServiceFactory();

        // Act
        var service = await factory.CreateAlertServiceAsync(config);

        // Assert
        Assert.That(service, Is.Not.Null);
        Assert.That(service.Configuration, Is.EqualTo(config));

        // Verify factory doesn't track created objects
        service.Dispose(); // Factory should not be affected
    }

    [Test]
    public void Factory_DoesNotImplementIDisposable()
    {
        // Act & Assert
        var factory = new AlertServiceFactory();
        Assert.That(factory, Is.Not.InstanceOf<IDisposable>());
    }
}
```

### Service Testing Pattern

**Test complete service functionality including lifecycle**:

```csharp
[TestFixture]
public class AlertServiceTests : BaseServiceTest
{
    [Test]
    public async Task StartAsync_WhenNotStarted_StartsServiceSuccessfully()
    {
        // Arrange
        var correlationId = CreateTestCorrelationId();

        // Act
        await _alertService.StartAsync(correlationId);

        // Assert
        Assert.That(_alertService.IsEnabled, Is.True);
        AssertLogContains("Alert service started");
    }

    [Test]
    public void Dispose_WhenCalled_DisposesServiceCleanly()
    {
        // Act
        _alertService.Dispose();

        // Assert
        Assert.That(_alertService.IsEnabled, Is.False);
        AssertLogContains("Alert service disposed");
    }
}
```

## Message Testing Requirements

### IMessage Interface Compliance

**ALWAYS validate complete IMessage implementation**:

```csharp
[TestFixture]
public class MyMessageTests : BaseServiceTest
{
    [Test]
    public void MyMessage_Create_WithValidParameters_CreatesMessageCorrectly()
    {
        // Arrange
        var data = "Test data";
        var source = "TestSource";
        var correlationId = CreateTestCorrelationId();

        // Act
        var message = MyMessage.Create(data, source, correlationId);

        // Assert - Validate all IMessage properties
        Assert.That(message.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(message.TypeCode, Is.EqualTo(MessageTypeCodes.MyMessage));
        Assert.That(message.Source.ToString(), Is.EqualTo(source));
        Assert.That(message.CorrelationId, Is.EqualTo(correlationId));
        Assert.That(message.Priority, Is.EqualTo(MessagePriority.Normal));
        Assert.That(message.Timestamp.Kind, Is.EqualTo(DateTimeKind.Utc));
    }

    [Test]
    public void MyMessage_Create_WithNullData_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            MyMessage.Create(null, "TestSource"));
    }

    [Test]
    public void MyMessage_TypeCode_IsInCorrectSystemRange()
    {
        // Arrange & Act
        var typeCode = MessageTypeCodes.MyMessage;

        // Assert - Verify type code is in correct system range
        Assert.That(typeCode, Is.GreaterThanOrEqualTo(1400)); // Alerting system range
        Assert.That(typeCode, Is.LessThanOrEqualTo(1499));
    }
}
```

### DeterministicIdGenerator Validation

**Verify consistent ID generation**:

```csharp
[Test]
public void DeterministicIdGenerator_IsUsedConsistently_AcrossAllMessages()
{
    // Arrange
    var data = "Test data";
    var source = "TestSource";
    var correlationId = CreateTestCorrelationId();

    // Act - Create same message twice with same parameters
    var message1 = MyMessage.Create(data, source, correlationId);
    var message2 = MyMessage.Create(data, source, correlationId);

    // Assert - IDs should be deterministic
    Assert.That(message1.Id, Is.Not.EqualTo(Guid.Empty));
    Assert.That(message2.Id, Is.Not.EqualTo(Guid.Empty));
    // Note: Specific deterministic behavior depends on implementation
}
```

## Integration Testing Patterns

### End-to-End Workflow Testing

**Test complete system workflows with correlation tracking**:

```csharp
[TestFixture]
public class MyIntegrationTests : BaseIntegrationTest
{
    [Test]
    public async Task EndToEndWorkflow_WithCompleteFlow_WorksCorrectly()
    {
        // Arrange
        await _service.StartAsync(CreateTestCorrelationId());
        var correlationId = CreateTestCorrelationId();

        // Act - Execute complete workflow
        var data = CreateTestData();
        _service.ProcessData(data, correlationId);

        // Wait for async processing
        await UniTask.Delay(100);

        // Assert - Verify complete flow
        var results = _service.GetResults();
        Assert.That(results, Is.Not.Empty);

        // Verify message was published
        AssertMessagePublished<MyProcessedMessage>();
        var message = GetLastMessage<MyProcessedMessage>();
        Assert.That(message.CorrelationId, Is.EqualTo(correlationId));

        // Verify logging with correlation
        AssertLogContains("Data processed");
        Assert.That(MockLogging.LogEntries.Any(log => log.CorrelationId == correlationId), Is.True);
    }
}
```

### Service Integration Testing

**Test service interactions and failover scenarios**:

```csharp
[Test]
public async Task ServiceIntegration_WithFailoverScenarios_HandlesGracefully()
{
    // Arrange
    MockMessageBus.ShouldThrowOnPublish = true;
    var data = CreateTestData();

    // Act & Assert - Should handle message bus failure gracefully
    await AssertGracefulFailureHandlingAsync(async () =>
    {
        _service.ProcessData(data);
        await UniTask.Delay(100);
    });

    // Verify operation completed despite failure
    var results = _service.GetResults();
    Assert.That(results, Is.Not.Empty);

    // Reset mock
    MockMessageBus.ShouldThrowOnPublish = false;
}
```

## Health Monitoring and Diagnostics Testing

### Health Check Validation

**Test comprehensive health reporting**:

```csharp
[Test]
public async Task HealthMonitoring_WithCompleteSystem_ReportsCorrectHealth()
{
    // Arrange
    await _service.StartAsync(CreateTestCorrelationId());
    var correlationId = CreateTestCorrelationId();

    // Act
    var healthReport = await _service.PerformHealthCheckAsync(correlationId);

    // Assert
    Assert.That(healthReport.OverallHealth, Is.True);
    Assert.That(healthReport.ServiceEnabled, Is.True);
    Assert.That(healthReport.Timestamp, Is.LessThanOrEqualTo(DateTime.UtcNow));

    // Verify all subsystems are healthy
    AssertAllServicesHealthy();
}
```

### Diagnostics Validation

**Test diagnostic information completeness**:

```csharp
[Test]
public void DiagnosticsReporting_WithActiveSystem_ProvidesCompleteDiagnostics()
{
    // Arrange
    _service.ProcessSomeData();

    // Act
    var diagnostics = _service.GetDiagnostics(CreateTestCorrelationId());

    // Assert
    Assert.That(diagnostics.ServiceVersion, Is.Not.Null);
    Assert.That(diagnostics.IsEnabled, Is.True);
    Assert.That(diagnostics.IsHealthy, Is.True);
    Assert.That(diagnostics.SubsystemStatuses, Is.Not.Null);
    Assert.That(diagnostics.SubsystemStatuses.Count, Is.GreaterThan(0));
}
```

## Correlation Tracking Testing

### **ALWAYS Use TestCorrelationHelper**

**Use shared correlation tracking** for consistent test correlation IDs:

```csharp
[Test]
public void CorrelationTracking_AcrossEntireLifecycle_MaintainsCorrelation()
{
    // Arrange
    var correlationId = CreateTestCorrelationId(); // Uses TestCorrelationHelper

    // Act - Perform operations with correlation ID
    _service.StartOperation(correlationId);
    _service.ProcessStep1(correlationId);
    _service.CompleteOperation(correlationId);

    // Assert - All messages should have the same correlation ID
    var messages = MockMessageBus.PublishedMessages;
    Assert.That(messages.All(m => m.CorrelationId == correlationId), Is.True);

    // Verify correlation in log entries
    Assert.That(MockLogging.LogEntries.Any(log => log.CorrelationId == correlationId), Is.True);
}

[Test]
public void CorrelationTracking_WithRelatedOperations_MaintainsRelationships()
{
    // Arrange
    var relatedIds = CorrelationHelper.CreateRelatedCorrelationIds("MyOperation", 3);

    // Act
    foreach (var id in relatedIds)
    {
        _service.ProcessWithCorrelation(id);
    }

    // Assert - All related messages should be trackable
    var messages = MockMessageBus.PublishedMessages;
    foreach (var relatedId in relatedIds)
    {
        Assert.That(messages.Any(m => m.CorrelationId == relatedId), Is.True);
    }
}
```

## Error Handling and Edge Cases

### Graceful Failure Testing

**Test system behavior under failure conditions**:

```csharp
[Test]
public void ErrorHandling_WithInvalidInput_HandlesGracefully()
{
    // Arrange
    var invalidData = CreateInvalidTestData();

    // Act & Assert - Should not throw
    Assert.DoesNotThrow(() => _service.ProcessData(invalidData));

    // Verify appropriate error logging
    AssertLogContains("Invalid data");
    Assert.That(MockLogging.HasErrorLogs(), Is.True);
}

[Test]
public void NullHandling_WithNullInputs_DoesNotThrow()
{
    // Act & Assert - All public methods should handle null gracefully
    Assert.DoesNotThrow(() => _service.ProcessData(null));
    Assert.DoesNotThrow(() => _service.RegisterItem(null));
    Assert.DoesNotThrow(() => _service.UpdateConfiguration(null));
}
```

## Performance Testing Anti-Patterns

### ❌ **Avoid These Performance Testing Mistakes**

```csharp
// ❌ AVOID: Testing without frame budget validation
[Test]
public void SlowOperation_ShouldComplete() // No performance validation
{
    var result = _service.ExpensiveOperation();
    Assert.That(result, Is.Not.Null);
}

// ❌ AVOID: Ignoring memory allocations
[Test]
public void ProcessData_ShouldWork() // No allocation checking
{
    _service.ProcessLargeDataSet(CreateLargeData());
}

// ❌ AVOID: Testing with unrealistic data sizes
[Test]
public void BulkOperation_WithTenItems() // Too small for realistic testing
{
    var items = CreateTestData(10); // Should test with 100s or 1000s
}

// ✅ CORRECT: Comprehensive performance testing
[Test]
public async Task BulkOperation_With1000Items_CompletesWithinFrameBudget()
{
    var items = CreateTestData(1000);

    var result = await ExecuteWithPerformanceMeasurementAsync(
        () => _service.ProcessBulkAsync(items),
        "BulkProcessing",
        TestConstants.FrameBudget);

    var allocResult = await MeasureAllocationsAsync(
        () => _service.ProcessBulkAsync(items),
        "BulkProcessing");

    AssertAcceptableAllocations(allocResult, maxBytes: 4096);
    LogPerformanceMetrics(result);
}
```

## Test Data Management

### Test Constants Usage

**Use TestConstants for consistent test values**:

```csharp
// ✅ CORRECT: Use shared test constants
[Test]
public void ProcessMessage_WithStandardMessage_ProcessesCorrectly()
{
    // Arrange
    var message = TestConstants.SampleAlertMessage;
    var source = TestConstants.TestSource;
    var tag = TestConstants.SampleTag;

    // Act & Assert
    var result = _service.ProcessMessage(message, source, tag);
    Assert.That(result.IsValid, Is.True);
}

// ✅ CORRECT: Validate Unity Collections constraints
[Test]
public void FixedStringHandling_WithLongString_TruncatesCorrectly()
{
    // Arrange
    var longString = new string('A', 1000);

    // Act
    var truncated = TestConstants.TruncateForFixedString(longString, TestConstants.MaxFixedString64Length);

    // Assert
    Assert.That(truncated.Length, Is.EqualTo(TestConstants.MaxFixedString64Length));
    Assert.That(TestConstants.IsValidFixedString(truncated, TestConstants.MaxFixedString64Length), Is.True);
}
```

### Test Data Builders

**Create fluent test data builders for complex objects**:

```csharp
public class TestAlertBuilder
{
    private string _message = TestConstants.SampleAlertMessage;
    private AlertSeverity _severity = AlertSeverity.Info;
    private string _source = TestConstants.TestSource;
    private Guid _correlationId = Guid.Empty;

    public TestAlertBuilder WithMessage(string message)
    {
        _message = message;
        return this;
    }

    public TestAlertBuilder WithSeverity(AlertSeverity severity)
    {
        _severity = severity;
        return this;
    }

    public TestAlertBuilder WithCorrelation(Guid correlationId)
    {
        _correlationId = correlationId;
        return this;
    }

    public Alert Build()
    {
        return Alert.Create(_message, _severity, _source, correlationId: _correlationId);
    }
}

// Usage in tests
[Test]
public void ProcessAlert_WithCriticalSeverity_PrioritizesCorrectly()
{
    // Arrange
    var alert = new TestAlertBuilder()
        .WithSeverity(AlertSeverity.Critical)
        .WithCorrelation(CreateTestCorrelationId())
        .Build();

    // Act & Assert
    _service.ProcessAlert(alert);
    Assert.That(_service.GetHighPriorityQueue(), Contains.Item(alert));
}
```

## Unity-Specific Testing Considerations

### Platform Compatibility Testing

**Consider Unity's platform diversity**:

```csharp
[Test]
public void SerializationHandling_CrossPlatform_WorksCorrectly()
{
    // Arrange
    var data = CreateComplexTestData();

    // Act - Test serialization/deserialization
    var serialized = MockSerialization.Serialize(data);
    var deserialized = MockSerialization.Deserialize<TestData>(serialized);

    // Assert - Should work on all Unity target platforms
    Assert.That(deserialized.Id, Is.EqualTo(data.Id));
    Assert.That(deserialized.Name, Is.EqualTo(data.Name));
}

[Test]
public void FixedStringOperations_WithUnicodeContent_HandlesCorrectly()
{
    // Arrange
    var unicodeContent = "Test 🎮 Content"; // Unicode emojis for edge case testing

    // Act
    var fixedString = new FixedString64Bytes(unicodeContent);

    // Assert
    Assert.That(fixedString.ToString(), Does.Contain("Test"));
    // Note: Emoji handling may vary by platform
}
```

### IL2CPP Compatibility

**Ensure tests work with Unity's IL2CPP compilation**:

```csharp
[Test]
public void GenericOperations_WithIL2CPP_CompileCorrectly()
{
    // Arrange - Test generic operations that might have IL2CPP issues
    var genericService = new GenericService<TestData>();
    var data = CreateTestData();

    // Act & Assert - Should work with IL2CPP AOT compilation
    var result = genericService.Process(data);
    Assert.That(result, Is.Not.Null);
}
```

## Test Performance and Scalability

### Test Execution Performance

**Ensure tests themselves are performant**:

```csharp
// ✅ CORRECT: Efficient test setup and teardown
[SetUp]
public override void Setup()
{
    base.Setup(); // Shared setup is cached and reused

    // Minimal per-test setup
    _testService = CreateMinimalTestService();
}

// ✅ CORRECT: Parallel-friendly tests
[Test]
public void IndependentOperation_Test1_WorksCorrectly()
{
    // Each test should be independent and not rely on test execution order
    var service = CreateIsolatedService();
    var result = service.DoOperation();
    Assert.That(result.IsSuccess, Is.True);
}
```

## Documentation and Comments

### Test Documentation Standards

**Document complex test scenarios and business logic**:

```csharp
/// <summary>
/// Tests the complete alert lifecycle under realistic game conditions.
/// Validates that the system can handle 1000+ alerts per second while maintaining
/// 60 FPS performance and proper correlation tracking across all subsystems.
///
/// This test simulates a real game scenario where multiple systems (AI, Physics,
/// Networking) generate alerts simultaneously during peak gameplay.
/// </summary>
[Test]
public async Task AlertLifecycle_Under1000AlertsPerSecond_MaintainsGamePerformance()
{
    // Test implementation with clear step documentation
}

/// <summary>
/// Validates Builder → Config → Factory → Service pattern compliance.
/// Ensures the factory creates services with proper configuration and lifecycle
/// management while maintaining the separation of concerns principle.
/// </summary>
[Test]
public void BuilderFactoryServicePattern_WithCompleteWorkflow_FollowsArchitecturePattern()
{
    // Test implementation
}
```

## Remember: Game Development First

**Every test decision should prioritize**:

1. **Player Experience** - Does this ensure smooth gameplay?
2. **Frame Rate Performance** - Will this maintain 60+ FPS?
3. **Memory Efficiency** - Does this validate zero-allocation patterns?
4. **Platform Compatibility** - Will this work on mobile and console?
5. **Production Readiness** - Does this test real-world scenarios?

When in doubt, choose the test approach that ensures the game runs smoothly at 60 FPS over the one that follows pure testing theory.

## Quick Reference

### Essential Test Imports

```csharp
using System;
using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using ZLinq;                                    // ✅ Use ZLinq not System.Linq
using Cysharp.Threading.Tasks;                  // ✅ Use UniTask not Task
using AhBearStudios.Core.Tests.Shared.Base;     // ✅ Use shared base classes
using AhBearStudios.Core.Tests.Shared.Utilities; // ✅ Use shared utilities
```

### Common Test Patterns

```csharp
// ✅ CORRECT: Complete test method structure
[Test]
public async Task ServiceMethod_WithSpecificCondition_ProducesExpectedResult()
{
    // Arrange
    var correlationId = CreateTestCorrelationId();
    var testData = CreateValidTestData();

    // Act
    var result = await ExecuteWithPerformanceMeasurementAsync(
        () => _service.ProcessDataAsync(testData, correlationId),
        "ProcessData",
        TestConstants.FrameBudget);

    // Assert
    Assert.That(result.IsSuccess, Is.True);
    AssertMessagePublished<DataProcessedMessage>();
    AssertLogContains("Data processed successfully");
    AssertNoErrors();
}
```

This testing approach ensures your tests are production-ready, performant, and aligned with Unity game development requirements while maintaining consistency across the entire AhBearStudios Core framework.