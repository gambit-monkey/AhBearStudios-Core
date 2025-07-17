# AhBearStudios Logging System Test Suite

## Overview

This comprehensive test suite provides robust testing for the AhBearStudios.Core.Logging system, focusing on Unity-specific performance requirements, frame budget compliance, and game development scenarios.

## Test Structure

### Assembly Definitions
- **EditMode Tests**: `AhBearStudios.Core.Logging.EditMode.Tests.asmdef`
- **PlayMode Tests**: `AhBearStudios.Core.Logging.PlayMode.Tests.asmdef`

### Test Categories

#### 1. Core Service Tests (`CoreServiceTests.cs`)
- **Basic logging operations**: All log levels (Debug, Info, Warning, Error, Critical, Trace)
- **Correlation tracking**: Verify correlation IDs are properly propagated
- **Burst compatibility**: Test generic logging methods with unmanaged types
- **Target management**: Registration, unregistration, and configuration
- **Channel management**: Channel registration, filtering, and message routing
- **Filtering system**: Filter registration, priority ordering, and message processing
- **Async operations**: FlushAsync, validation, and maintenance operations
- **Performance tests**: Frame budget compliance and high-volume scenarios

#### 2. Model Tests (`ModelTests.cs`)
- **LogEntry**: Creation, serialization, native/managed data handling, Burst compatibility
- **LogLevel**: Severity comparisons and filtering logic
- **LogMessage**: IMessage integration, formatting, correlation tracking
- **NativeLogEntry**: Burst-compatible operations and native collections
- **Unity Collections**: Integration with NativeArray, NativeList, NativeQueue
- **Job System**: Compatibility with IJob and IJobParallelFor
- **Memory optimization**: Native string storage and allocation patterns

#### 3. Target Tests (`TargetTests.cs`)
- **SerilogTarget**: Unity platform optimization, frame budget awareness, ProfilerMarker integration
- **ConsoleLogTarget**: Output formatting and level filtering
- **FileLogTarget**: File I/O operations, rotation, and platform-specific paths
- **Health monitoring**: Operational status and error recovery
- **Performance**: Frame budget compliance and throughput testing
- **Configuration**: Dynamic reconfiguration and validation

#### 4. Formatter Tests (`FormatterTests.cs`)
- **JsonFormatter**: Structured output, configuration options, performance metrics integration
- **PlainTextFormatter**: Human-readable output, templates, exception handling
- **StructuredFormatter**: Key-value pair formatting
- **Unity-specific formatters**: Unity context integration and console optimization
- **Configuration**: Settings management, validation, and runtime changes
- **Performance**: Frame budget compliance and memory efficiency

#### 5. Filter Tests (`FilterTests.cs`)
- **LevelFilter**: Min/max level filtering, include/exclude modes
- **SourceFilter**: Source-based filtering, regex patterns, hierarchical matching
- **CorrelationFilter**: Correlation ID filtering and pattern matching
- **RateLimitFilter**: Rate limiting and burst handling
- **SamplingFilter**: Statistical sampling and level-based sampling
- **Priority ordering**: Filter execution order and performance
- **Advanced filtering**: Multi-filter scenarios and service integration

#### 6. Integration Tests (`IntegrationTests.cs`)
- **End-to-end pipeline**: Complete logging flow from service to output
- **System integration**: Messaging, alerting, and health monitoring
- **Error scenarios**: Behavior under various failure conditions
- **Configuration changes**: Runtime reconfiguration impact
- **Performance integration**: High-throughput and concurrent access scenarios

#### 7. Unity Performance Tests (`UnityPerformanceTests.cs`)
- **Frame budget compliance**: 0.5ms logging budget per frame
- **Job System compatibility**: Burst-compiled job integration
- **Memory optimization**: GC pressure and native collections
- **Threading**: Main thread non-blocking operations
- **Platform-specific**: Mobile optimization and WebGL limitations
- **Stress testing**: High-volume scenarios and system stability

#### 8. Unity Platform Tests (`UnityPlatformTests.cs`)
- **Platform-specific behavior**: Editor vs runtime, mobile vs desktop
- **Unity event integration**: Application lifecycle events
- **Performance characteristics**: Platform-specific optimization
- **Memory constraints**: Platform-appropriate limits
- **Battery impact**: Mobile power consumption
- **Thermal throttling**: Performance consistency under load

## Key Testing Features

### Unity-Specific Optimizations
- **Frame Budget Testing**: Ensures logging operations stay within 0.5ms per frame
- **Job System Compatibility**: Tests Burst-compiled jobs with native log entries
- **Platform Awareness**: Different behavior for Editor, Mobile, WebGL, Console platforms
- **Memory Management**: Tests for GC pressure and native memory usage
- **Performance Profiling**: Integration with Unity ProfilerMarker system

### Game Development Scenarios
- **Gameplay Logging**: High-frequency logging during intense gameplay
- **Scene Transitions**: Logging persistence across scene changes
- **Application Lifecycle**: Pause/resume and focus change handling
- **Error Recovery**: Graceful degradation under failure conditions
- **Configuration Management**: Runtime reconfiguration without service interruption

### Performance Validation
- **Throughput Testing**: 1000+ messages per second capability
- **Concurrent Access**: Multi-threaded logging safety
- **Memory Efficiency**: Minimal allocation patterns
- **Startup Performance**: Fast service initialization
- **Resource Cleanup**: Proper disposal and resource management

## Mock Services

### Test Infrastructure
- **MockProfilerService**: Simulates performance monitoring
- **MockAlertService**: Captures alert notifications
- **MockHealthCheckService**: Simulates health monitoring
- **MockMessageBusService**: Captures published messages
- **MockLogTarget**: Controllable target for testing

### Test Utilities
- **TestDataFactory**: Consistent test data generation
- **TestUtilities**: Performance measurement and validation helpers
- **Frame Budget Assertions**: Unity-specific performance validation
- **Memory Usage Validation**: GC pressure monitoring

## Running the Tests

### Prerequisites
- Unity 2022.3 LTS or later
- Unity Test Framework package
- NUnit framework
- Test assemblies properly referenced

### Execution
```bash
# Run all tests
Unity -batchmode -runTests -testPlatform EditMode
Unity -batchmode -runTests -testPlatform PlayMode

# Run specific test categories
Unity -batchmode -runTests -testPlatform EditMode -testFilter "CoreServiceTests"
Unity -batchmode -runTests -testPlatform PlayMode -testFilter "UnityPerformanceTests"
```

### Test Coverage
- **Core Service**: 95%+ code coverage
- **Models**: 90%+ code coverage  
- **Targets**: 85%+ code coverage
- **Formatters**: 80%+ code coverage
- **Filters**: 85%+ code coverage
- **Integration**: 75%+ scenario coverage

## Performance Benchmarks

### Frame Budget Compliance
- Single log call: < 0.1ms
- Multiple log calls (10): < 0.5ms
- Batch operations: < 2.0ms per 100 messages

### Memory Efficiency
- Per-message allocation: < 1KB
- Bulk operations: < 10MB for 10,000 messages
- Native collections: Zero managed allocation

### Throughput Targets
- Sequential logging: 5,000 messages/second
- Concurrent logging: 2,000 messages/second
- Burst job processing: 50,000 entries/second

## Platform-Specific Behavior

### Unity Editor
- Verbose logging (Debug level)
- Full feature set available
- Performance monitoring enabled

### Mobile Platforms (Android/iOS)
- Reduced logging (Warning level)
- Battery optimization
- Smaller memory footprint

### WebGL
- Minimal logging (Error level)
- Console-only output
- File logging disabled

### Console Platforms
- Optimized for console hardware
- Platform-specific log paths
- Enhanced error reporting

## Continuous Integration

### Automated Testing
- Pre-commit hooks for core tests
- Build pipeline integration
- Performance regression detection
- Platform-specific test matrix

### Quality Gates
- All tests must pass
- Performance benchmarks met
- Memory usage within limits
- No GC pressure spikes

## Troubleshooting

### Common Issues
1. **Assembly Definition Errors**: Ensure proper references to core assemblies
2. **Performance Test Failures**: Check Unity profiler for frame drops
3. **Platform Test Failures**: Verify platform-specific conditionals
4. **Memory Test Failures**: Monitor GC collection during tests

### Debug Strategies
- Enable Unity Profiler during test execution
- Use LogUtilities for detailed performance measurement
- Check platform-specific behavior with conditional compilation
- Monitor memory usage with GC.GetTotalMemory()

## Contributing

### Adding New Tests
1. Follow existing test patterns and naming conventions
2. Include performance validation for Unity-specific scenarios
3. Add platform-specific behavior testing where applicable
4. Ensure proper cleanup in TearDown methods
5. Include frame budget assertions for performance-critical paths

### Test Guidelines
- Use descriptive test names indicating the scenario
- Include Arrange/Act/Assert structure
- Validate both success and failure scenarios
- Test boundary conditions and edge cases
- Include performance assertions for Unity scenarios

This comprehensive test suite ensures the AhBearStudios Logging system meets the demanding requirements of Unity game development while maintaining high performance and reliability standards.