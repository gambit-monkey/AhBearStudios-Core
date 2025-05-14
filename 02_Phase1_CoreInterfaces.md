# Phase 1: Core System Interfaces

## Objective

Define foundational contracts for all systems to ensure full swappability, testability, and clean separation of concerns.

---

## Design Principles

* **Interfaces First**: Concrete systems only rely on interfaces
* **Plug-and-Play Architecture**: Swap implementations without modifying consumers
* **Composition over Inheritance**
* **Burst and Job-compatible where possible**
* **Managed/unmanaged type support**

---

## Interfaces Per System

### Dependency Injection

* `IServiceRegistrar`
* `IDependencyResolver`

### Logging

* `ILogger`
* `ILogFormatter`
* `ILogSink`

### Messaging

* `IMessageBus`
* `ISubscriber`
* `IPublisher`

### Metrics & Performance

* `IMetricsRecorder`
* `IPerformanceMonitor`
* `IProfilerTag`

### Database

* `IDatabase`
* `IDatabaseCollection<T>`
* `IDatabaseQueryHandler<TQuery, TResult>`

### Pooling

* `IPool<T>`
* `IObjectPool<T>`
* `IPoolMetrics`

### Networking

* `INetworkAdapter`
* `INetworkMessageRouter`
* `INetworkEntityRegistry`

---

## Interface Constraints

* Use generic constraints to support struct/class-specific cases
* Ensure thread safety for interfaces that interact with Jobs
* Prefer dependency injection (via interface) over ServiceLocator

---

## Implementation Examples (Stubbed)

```csharp
public interface ILogger {
    void Log(LogLevel level, string message, object context = null);
}

public interface IDatabase {
    Task InsertAsync<T>(string collection, T data);
    Task<T> FindAsync<T>(string collection, string id);
}

public interface IMessageBus {
    void Publish<T>(T message);
    void Subscribe<T>(Action<T> handler);
    void Unsubscribe<T>(Action<T> handler);
}
```

---

## Deliverables

* Interfaces for all core systems
* Documentation and XML summaries
* Mock implementations for testing

---

**Next:** [Phase 2 - System Implementations](03_Phase2_SystemImplementations.md)
