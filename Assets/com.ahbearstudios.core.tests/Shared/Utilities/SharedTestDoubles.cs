using System;
using AhBearStudios.Core.Tests.Shared.TestDoubles.Stubs;
using AhBearStudios.Core.Tests.Shared.TestDoubles.Spies;
using AhBearStudios.Core.Tests.Shared.TestDoubles.Fakes;

namespace AhBearStudios.Core.Tests.Shared.Utilities
{
    /// <summary>
    /// Container for shared test doubles following CLAUDETESTS.md guidelines.
    /// Provides centralized access to TDD-compliant test doubles for consistent testing.
    /// Eliminates the need for test classes to create their own test doubles.
    /// </summary>
    public sealed class SharedTestDoubles : IDisposable
    {
        public StubLoggingService StubLogging { get; }
        public SpyMessageBusService SpyMessageBus { get; }
        public FakeSerializationService FakeSerialization { get; }
        public FakePoolingService FakePooling { get; }
        public NullProfilerService NullProfiler { get; }
        public StubHealthCheckService StubHealthCheck { get; }
        public TestCorrelationHelper CorrelationHelper { get; }
        public PerformanceTestHelper PerformanceHelper { get; }
        public AllocationTracker AllocationTracker { get; }

        public SharedTestDoubles()
        {
            StubLogging = new StubLoggingService();
            SpyMessageBus = new SpyMessageBusService();
            FakeSerialization = new FakeSerializationService();
            FakePooling = new FakePoolingService(SpyMessageBus);
            NullProfiler = NullProfilerService.Instance;
            StubHealthCheck = new StubHealthCheckService();
            CorrelationHelper = new TestCorrelationHelper();
            PerformanceHelper = new PerformanceTestHelper();
            AllocationTracker = new AllocationTracker();
        }

        public void ClearAll()
        {
            StubLogging?.ClearLogs();
            SpyMessageBus?.ClearRecordedInteractions();
            FakeSerialization?.ClearData();
            FakePooling?.ClearRecordedInteractions();
            StubHealthCheck?.ClearConfiguration();
            CorrelationHelper?.Clear();
            PerformanceHelper?.Clear();
            AllocationTracker?.Clear();
        }

        public void Dispose()
        {
            StubLogging?.Dispose();
            SpyMessageBus?.Dispose();
            FakeSerialization?.Dispose();
            FakePooling?.Dispose();
            StubHealthCheck?.Dispose();
            PerformanceHelper?.Dispose();
            AllocationTracker?.Dispose();
        }
    }
}