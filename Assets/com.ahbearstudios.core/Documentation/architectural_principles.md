# Architectural Principles

## 🎯 Core Design Philosophy

### Functional Organization Over Technical Layers
We organize our codebase by **business capabilities** rather than technical concerns. Instead of having separate "Controllers", "Services", and "Data" folders, we group all related functionality within domain-specific modules.

**Benefits:**
- 🔍 **Easier Navigation**: Find all logging-related code in one place
- 🔧 **Simplified Maintenance**: Changes to a feature are localized
- 🚀 **Team Scalability**: Different teams can own different functional areas
- 📦 **Better Encapsulation**: Internal implementation details stay hidden

**Example Structure:**
```
❌ Traditional Layer-Based
├── Controllers/
│   ├── LoggingController.cs
│   └── MessagingController.cs
├── Services/
│   ├── LoggingService.cs
│   └── MessagingService.cs
└── Data/
    ├── LogRepository.cs
    └── MessageRepository.cs

✅ Function-Based Organization
├── Logging/
│   ├── ILoggingService.cs
│   ├── LoggingService.cs
│   ├── LogRepository.cs
│   └── LoggingController.cs
└── Messaging/
    ├── IMessagingService.cs
    ├── MessagingService.cs
    ├── MessageRepository.cs
    └── MessagingController.cs
```

### Builder → Config → Factory → Service Pattern

Every system follows a consistent creation and configuration flow that promotes flexibility and testability.

#### 🏗️ Builder Pattern
- **Purpose**: Construct complex configuration objects with validation
- **Benefits**: Fluent API, compile-time safety, defaults handling

```csharp
var config = new LogConfigBuilder()
    .WithMinimumLevel(LogLevel.Debug)
    .WithTarget(new FileLogTarget("game.log"))
    .WithTarget(new ConsoleLogTarget())
    .WithAsyncBatching(batchSize: 100)
    .Build();
```

#### ⚙️ Configuration Objects
- **Purpose**: Immutable data containers for system settings
- **Benefits**: Serializable, validatable, version-safe

```csharp
public class LoggingConfig
{
    public LogLevel MinimumLevel { get; init; }
    public IReadOnlyList<ILogTarget> Targets { get; init; }
    public BatchingConfig Batching { get; init; }
    public bool EnableStructuredLogging { get; init; }
}
```

#### 🏭 Factory Pattern
- **Purpose**: Create instances based on configuration
- **Benefits**: Dependency resolution, lifecycle management, testing

```csharp
public class LoggingFactory : ILoggingFactory
{
    public ILoggingService CreateLoggingService(LoggingConfig config)
    {
        var targets = config.Targets.Select(CreateTarget);
        return new LoggingService(targets, config);
    }
}
```

#### 🔧 Service Implementation
- **Purpose**: Actual business logic implementation
- **Benefits**: Clean separation, interface-based, testable

```csharp
public class LoggingService : ILoggingService
{
    private readonly IReadOnlyList<ILogTarget> _targets;
    private readonly LoggingConfig _config;

    public LoggingService(IReadOnlyList<ILogTarget> targets, LoggingConfig config)
    {
        _targets = targets;
        _config = config;
    }
}
```

### Compositional Architecture

We favor **composition over inheritance** to build flexible, reusable systems.

#### ✅ Composition Benefits
- **Runtime Flexibility**: Swap implementations at runtime
- **Easier Testing**: Mock individual components
- **Reduced Coupling**: Components don't inherit complex hierarchies
- **Better Reusability**: Mix and match components as needed

#### Example: Audio System Composition
```csharp
public class AudioService : IAudioService
{
    private readonly IAudioEngine _engine;          // Core audio processing
    private readonly IAudioCache _cache;            // Asset caching
    private readonly IAudioMixer _mixer;            // Volume/effects mixing
    private readonly IAudioSpatializer _spatial;    // 3D audio positioning

    public AudioService(
        IAudioEngine engine,
        IAudioCache cache,
        IAudioMixer mixer,
        IAudioSpatializer spatial)
    {
        _engine = engine;
        _cache = cache;
        _mixer = mixer;
        _spatial = spatial;
    }
}
```

### Dependency Injection via Reflex

We use **constructor injection** as our primary dependency resolution pattern.

#### 🎯 Injection Principles
- **Constructor Injection**: Primary pattern for required dependencies
- **Property Injection**: Only for optional dependencies
- **Method Injection**: For per-operation dependencies
- **No Service Locator**: Avoid anti-patterns

#### Example Registration
```csharp
public class CoreSystemsInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        // Register as singleton
        Container.Bind<ILoggingService>()
            .To<LoggingService>()
            .AsSingle();

        // Register with configuration
        Container.Bind<LoggingConfig>()
            .FromMethod(ctx => CreateLoggingConfig())
            .AsSingle();

        // Register factory
        Container.Bind<ILoggingFactory>()
            .To<LoggingFactory>()
            .AsSingle();
    }
}
```

### Unity/Core Separation

We maintain a clear boundary between **pure business logic** and **Unity-specific code**.

#### 📦 AhBearStudios.Core.*
- **Pure C#**: No Unity dependencies
- **Business Logic**: Core algorithms and data processing
- **Cross-Platform**: Can run on server, mobile, desktop
- **Highly Testable**: No MonoBehaviour dependencies

#### 🎮 AhBearStudios.Unity.*
- **Unity Integration**: MonoBehaviour wrappers
- **Unity-Specific Features**: Coroutines, GameObjects, etc.
- **Platform Bridges**: Unity services integration
- **Editor Tools**: Custom inspectors and tools

#### Example Separation
```csharp
// Core - Pure business logic
namespace AhBearStudios.Core.Audio
{
    public class AudioService : IAudioService
    {
        public void PlaySound(SoundId id, AudioParameters parameters)
        {
            // Pure audio processing logic
        }
    }
}

// Unity - Integration layer
namespace AhBearStudios.Unity.Audio
{
    public class UnityAudioBehaviour : MonoBehaviour
    {
        [Inject] private IAudioService _audioService;

        public void PlaySound(AudioClip clip)
        {
            var soundId = _audioService.RegisterClip(clip);
            _audioService.PlaySound(soundId, AudioParameters.Default);
        }
    }
}
```

## 🔗 Integration Philosophy

### Interface-First Design

All system boundaries are defined by **explicit contracts** rather than concrete implementations.

#### 📋 Interface Design Principles
- **Focused Responsibility**: Each interface has a single, clear purpose
- **Minimal Surface Area**: Expose only what consumers need
- **Stable Contracts**: Interfaces change less frequently than implementations
- **Version Compatibility**: Support backward compatibility through interface versioning

#### Example Interface Design
```csharp
public interface ILoggingService
{
    // Core functionality - stable contract
    void LogDebug(string message);
    void LogInfo(string message);
    void LogWarning(string message);
    void LogError(string message);
    void LogCritical(string message);
    
    // Advanced functionality - optional
    void LogException(Exception exception, string context = null);
    
    // Management functionality - separate interface
    // Moved to ILoggingManager to keep core interface stable
}

public interface ILoggingManager
{
    void RegisterTarget(ILogTarget target);
    void UnregisterTarget(string targetName);
    IReadOnlyList<ILogTarget> GetRegisteredTargets();
    LoggingStatistics GetStatistics();
}
```

### Minimal Cross-System Dependencies

Systems communicate through the **message bus** when possible to minimize direct coupling.

#### 🔄 Communication Patterns

**Direct Dependency** (when immediate response needed):
```csharp
public class SaveService : ISaveService
{
    private readonly ISerializationService _serializer;
    
    public async Task SaveGameAsync(GameState state)
    {
        var data = _serializer.Serialize(state); // Direct call for immediate result
        await WriteToFileAsync(data);
    }
}
```

**Message Bus** (for notifications and events):
```csharp
public class AudioService : IAudioService
{
    private readonly IMessageBus _messageBus;
    
    public void PlaySound(SoundId id)
    {
        // Play sound logic...
        
        // Notify other systems without direct coupling
        _messageBus.Publish(new SoundPlayedEvent(id));
    }
}
```

### Fail-Fast with Graceful Degradation

We detect errors **early** but isolate failures to prevent cascading system failures.

#### ⚡ Fail-Fast Principles
- **Early Validation**: Validate inputs at system boundaries
- **Clear Error Messages**: Provide actionable error information
- **Exception Safety**: Use strong exception safety guarantees
- **Resource Cleanup**: Always clean up resources on failure

#### 🛡️ Graceful Degradation
- **Isolation**: One system failure doesn't crash others
- **Fallback Modes**: Reduced functionality rather than total failure
- **Recovery**: Automatic retry with exponential backoff
- **Health Monitoring**: Continuous health checks and alerts

#### Example: Graceful Audio Degradation
```csharp
public class AudioService : IAudioService
{
    public void PlaySound(SoundId id)
    {
        try
        {
            // Try hardware-accelerated audio
            _hardwareAudio.PlaySound(id);
        }
        catch (AudioHardwareException)
        {
            try
            {
                // Fallback to software audio
                _softwareAudio.PlaySound(id);
            }
            catch (AudioException)
            {
                // Graceful degradation - continue without audio
                _logger.LogWarning($"Audio playback failed for {id}, continuing silently");
                _messageBus.Publish(new AudioDegradedEvent());
            }
        }
    }
}
```

### Observable System Health

Every system provides **comprehensive monitoring** to enable proactive maintenance.

#### 📊 Health Check Categories
- **Liveness**: Is the system running?
- **Readiness**: Can the system serve requests?
- **Performance**: How well is the system performing?
- **Resources**: What resources is the system using?

#### Example Health Check Implementation
```csharp
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly IDatabaseService _database;
    
    public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            await _database.TestConnectionAsync(cancellationToken);
            stopwatch.Stop();
            
            var responseTime = stopwatch.ElapsedMilliseconds;
            
            if (responseTime < 100)
                return HealthCheckResult.Healthy($"Database responding in {responseTime}ms");
            else if (responseTime < 1000)
                return HealthCheckResult.Degraded($"Database slow: {responseTime}ms");
            else
                return HealthCheckResult.Unhealthy($"Database timeout: {responseTime}ms");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Database connection failed: {ex.Message}");
        }
    }
}
```

#### Alerting Integration
```csharp
public class AlertingHealthCheckService : IHealthCheckService
{
    private readonly IAlertService _alerts;
    
    protected override void OnHealthCheckFailed(IHealthCheck check, HealthCheckResult result)
    {
        _alerts.RaiseAlert(
            $"Health check failed: {check.Name}", 
            AlertSeverity.Critical,
            "HealthCheck",
            check.Name);
    }
}
```

## 🎯 Benefits Summary

| Principle | Primary Benefit | Secondary Benefits |
|-----------|-----------------|-------------------|
| **Functional Organization** | Easier navigation and maintenance | Better team scalability, clearer ownership |
| **Builder → Config → Factory → Service** | Consistent, testable creation patterns | Easy configuration, dependency injection friendly |
| **Compositional Architecture** | Runtime flexibility and easier testing | Better reusability, reduced coupling |
| **Dependency Injection** | Loose coupling and testability | Easy mocking, configuration flexibility |
| **Unity/Core Separation** | Cross-platform business logic | Better testing, cleaner architecture |
| **Interface-First Design** | Stable contracts and substitutability | Easier mocking, versioning support |
| **Message Bus Communication** | Reduced coupling between systems | Event-driven architecture, easier debugging |
| **Fail-Fast + Graceful Degradation** | Early error detection with system resilience | Better user experience, easier debugging |
| **Observable Health** | Proactive issue detection | Better monitoring, operational excellence |

These principles work together to create a maintainable, scalable, and robust game development architecture that can evolve with changing requirements while maintaining high code quality standards.