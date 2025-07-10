# **C\# Full Development Guidelines**

## **Core Architecture Pattern**

* **Always follow Builder → Config → Factory → Service design flow**  
* Use compositional architecture over inheritance-based designs  
* Implement dependency injection via constructors using Reflex DI  
* Design for testability with clear separation of concerns and minimal coupling

## **Code Organization and Architecture Structure**

### **Functional Organization Principle**

* **Organize by functional domain, not architectural layer** \- Group related functionality together rather than separating by technical concerns  
* **Each functional system is self-contained** \- All related components (interfaces, implementations, builders, factories, configs) reside in the same functional namespace  
* **Interfaces stay with implementations** \- Place interfaces in the same namespace as their concrete implementations, not in separate `.Interfaces` namespaces  
* **Enums, Structs, and Records** \- Place data objects in the `.Models` namespaces.

### **Core System Organization Pattern**

Every functional system follows this consistent structure:

AhBearStudios.Core.{FunctionalSystem}/

├── I{System}Service.cs              // Primary interface at root level

├── {System}Service.cs               // Primary implementation at root level

├── Configs/                         // Configuration objects (Builder step)

│   ├── {System}Config.cs

│   └── {Specific}Config.cs

├── Builders/                        // Builder pattern implementations

│   ├── I{System}ConfigBuilder.cs

│   ├── {System}ConfigBuilder.cs

│   └── I{Specific}Builder.cs

├── Factories/                       // Factory pattern implementations

│   ├── I{System}ServiceFactory.cs

│   ├── {System}ServiceFactory.cs

│   └── I{Specific}Factory.cs

├── Services/                        // Additional service implementations

│   ├── {Additional}Service.cs

│   └── {Support}Service.cs

├── {DomainSpecific}/               // System-specific functionality

│   ├── I{Domain}Component.cs

│   └── {Domain}Component.cs

└── HealthChecks/                    // Health monitoring

    ├── {System}ServiceHealthCheck.cs

    └── {Component}HealthCheck.cs

### **Namespace Organization Standards**

#### **Functional System Namespaces**

// Core Infrastructure (True infrastructure only)

AhBearStudios.Core.Infrastructure.DependencyInjection

AhBearStudios.Core.Infrastructure.Configuration  

AhBearStudios.Core.Infrastructure.Bootstrap

// Functional Systems (Domain-organized)

AhBearStudios.Core.Logging

AhBearStudios.Core.Messaging

AhBearStudios.Core.Pooling

AhBearStudios.Core.Serialization

AhBearStudios.Core.Profiling

AhBearStudios.Core.Alerts

AhBearStudios.Core.HealthCheck

// Application Layer

AhBearStudios.Application.Services

AhBearStudios.Application.Domain

AhBearStudios.Application.Builders

AhBearStudios.Application.Factories

// Common Utilities

AhBearStudios.Core.Common.Extensions

AhBearStudios.Core.Common.Utilities

AhBearStudios.Core.Common.Exceptions

AhBearStudios.Core.Common.Constants

#### **Pattern-Based Sub-Namespaces**

// Core interfaces and implementations at functional root

AhBearStudios.Core.Logging.ILoggingService

AhBearStudios.Core.Logging.LoggingService

// Builder → Config → Factory → Service pattern organization

AhBearStudios.Core.Logging.Configs.LoggingConfig

AhBearStudios.Core.Logging.Builders.ILogConfigBuilder

AhBearStudios.Core.Logging.Factories.ILoggingServiceFactory

AhBearStudios.Core.Logging.Services.LogBatchingService

// Domain-specific functionality within functional systems

AhBearStudios.Core.Logging.Targets.ILogTarget

AhBearStudios.Core.Logging.Formatters.ILogFormatter

AhBearStudios.Core.Logging.HealthChecks.LoggingServiceHealthCheck

### **File Organization Rules**

#### **Primary Service Location**

* **Main interface and implementation at root** \- `I{System}Service.cs` and `{System}Service.cs` files live directly in the functional namespace root  
* **No nested service folders for primary services** \- Avoid `AhBearStudios.Core.Logging.Services.ILoggingService`

#### **Pattern Alignment**

* **Configs/ folder** \- All configuration classes and related builders  
* **Builders/ folder** \- All builder pattern implementations for the functional system  
* **Factories/ folder** \- All factory pattern implementations for the functional system  
* **Services/ folder** \- Additional supporting services (not the primary service)  
* **{Domain}/ folders** \- System-specific functionality (Targets/, Formatters/, Collectors/, etc.)  
* **HealthChecks/ folder** \- All health check implementations for the functional system

#### **Interface Placement Rules**

// ✅ CORRECT: Interfaces with implementations

namespace AhBearStudios.Core.Logging

{

    public interface ILoggingService { }

    public class LoggingService : ILoggingService { }

}

namespace AhBearStudios.Core.Logging.Targets

{

    public interface ILogTarget { }

    public class UnityConsoleLogTarget : ILogTarget { }

}

// ❌ INCORRECT: Separate interface namespaces

namespace AhBearStudios.Core.Logging.Interfaces

{

    public interface ILoggingService { }

}

namespace AhBearStudios.Core.Logging.Services  

{

    public class LoggingService : ILoggingService { }

}

### **Benefits of This Organization**

#### **Developer Experience**

* **Intuitive navigation** \- Developers immediately know where to find logging, messaging, or pooling code  
* **Reduced cognitive load** \- No need to remember which "layer" contains what functionality  
* **Unity ecosystem alignment** \- Follows Unity's own functional organization patterns

#### **Maintainability**

* **Cohesive functionality** \- All related components are colocated and can be modified together  
* **Clear boundaries** \- Functional systems have well-defined responsibilities and interfaces  
* **Consistent growth** \- New functionality naturally fits into established functional patterns

#### **Team Collaboration**

* **Functional ownership** \- Different developers can own different functional systems  
* **Reduced conflicts** \- Teams working on different functional areas rarely conflict  
* **Clear integration points** \- Cross-system dependencies are explicit and well-defined

### **Migration Guidelines**

#### **For New Development**

* **Always follow functional organization** \- New systems must use the established functional pattern  
* **Place interfaces with implementations** \- Never create separate `.Interfaces` namespaces  
* **Follow Builder → Config → Factory → Service structure** \- Organize folders according to this pattern

#### **For Existing Code**

* **Refactor gradually** \- Move existing code to functional organization during major updates  
* **Maintain backward compatibility** \- Use `using` aliases during transition periods  
* **Update documentation** \- Ensure all references point to new functional locations

### **Anti-Patterns to Avoid**

#### **Layered Organization**

// ❌ AVOID: Technical layer organization

AhBearStudios.Core.Infrastructure.Services.LoggingService

AhBearStudios.Core.Foundation.Services.MessagingService

AhBearStudios.Core.Observability.Services.ProfilingService

#### **Type-Based Grouping**

// ❌ AVOID: Grouping by type rather than function

AhBearStudios.Core.Interfaces.ILoggingService

AhBearStudios.Core.Services.LoggingService

AhBearStudios.Core.Builders.LogConfigBuilder

AhBearStudios.Core.Factories.LogTargetFactory

#### **Deep Nesting**

// ❌ AVOID: Excessive namespace depth

AhBearStudios.Core.Logging.Services.Implementations.Production.LoggingService

### **Validation Checklist**

When organizing code, verify:

* \[ \] Primary interfaces and implementations are at functional root level  
* \[ \] Builder → Config → Factory → Service folders exist where needed  
* \[ \] No separate `.Interfaces`, `.Services`, or `.Implementations` namespaces  
* \[ \] Domain-specific functionality is grouped in descriptive folders  
* \[ \] Health checks are included for all functional systems  
* \[ \] Namespace depth is reasonable (typically 3-4 levels maximum)  
* \[ \] All related functionality is colocated within the functional system

## **File Organization**

* **One class/struct/interface/enum per file** \- Each type must reside in its own individual file and code block  
* **Use descriptive file names** that match the type name exactly  
* **Follow functional organization** \- Place files in appropriate functional system folders  
* **Respect Builder → Config → Factory → Service pattern** \- Organize files according to established folder structure  
* **Interfaces with implementations** \- Keep interface and implementation files in the same functional namespace

## **C\# Language Standards**

* **Use C\# 10+ patterns and features**  
* Use `record struct` for structs where appropriate  
* Use `record class` for classes where appropriate  
* Leverage pattern matching, switch expressions, and other modern C\# features

## **Implementation Requirements**

* **No incomplete code** \- Every class must be fully implemented with no stubs, TODOs, or placeholder methods  
* **Preserve existing functionality** unless changes are required for performance or compliance  
* **Maintain architectural compatibility** \- Do not introduce ECS-style patterns  
* **Follow functional organization** \- All new implementations must use established functional patterns

## **Core Systems Integration**

### **Reflex Dependency Injection Integration**

* **Use Reflex Container** as the primary DI container throughout the system  
* **Register services** using `Container.Bind<TInterface>().To<TImplementation>()` with appropriate lifetimes  
* **Resolve dependencies** using `Container.Resolve<T>()` or constructor injection  
* **Validate registrations** using `Container.HasBinding<T>()` before resolution  
* **Use ContainerBuilder** for registration during bootstrap and configuration  
* **Leverage Reflex attributes** like `[Inject]` for property/field injection when needed  
* **Create child containers** using Reflex scoping for isolated subsystems  
* **Organize installers functionally** \- Place DI registration logic in functional system folders

// Example Reflex registration following functional organization

namespace AhBearStudios.Core.Logging

{

    public class LoggingInstaller : MonoInstaller  

    {  

        public override void InstallBindings(ContainerBuilder builder)  

        {  

            builder.Bind\<ILoggingService\>().To\<LoggingService\>().AsSingle();

            builder.Bind\<ILogTarget\>().To\<UnityConsoleLogTarget\>().AsSingle().WithId("unity");

            builder.Bind\<ILogTarget\>().To\<SerilogLogTarget\>().AsSingle().WithId("serilog");

              

            // Factory registration in functional context

            builder.Bind\<ILogTargetFactory\>().To\<LogTargetFactory\>().AsSingle();

              

            // Configuration in functional context

            builder.Bind\<LoggingConfig\>().FromInstance(loggingConfig);

        }  

    }

}

// Example service with Reflex injection in functional namespace

namespace AhBearStudios.Core.Logging

{

    public class LoggingService : ILoggingService

    {  

        private readonly IEnumerable\<ILogTarget\> \_targets;  

        private readonly IMessageBusService \_messageBus;  

          

        public LoggingService(IEnumerable\<ILogTarget\> targets, IMessageBusService messageBus)  

        {  

            \_targets \= targets ?? throw new ArgumentNullException(nameof(targets));  

            \_messageBus \= messageBus ?? throw new ArgumentNullException(nameof(messageBus));  

        }

    }

}

### **Logging System Integration**

* **Use ILoggingService** for all logging operations throughout the system  
* **Inject ILoggingService** via Reflex constructor injection into all services  
* **Log method entry/exit** for critical operations and performance monitoring using `LogInfo()`  
* **Include correlation IDs** for tracking operations across system boundaries  
* **Log errors with context** using `LogException(Exception, string)` with relevant state information  
* **Performance logging** \- Log timing information for operations that may impact user experience  
* **Register log targets** using `RegisterTarget(ILogTarget)` for custom logging destinations  
* **Use structured logging levels** \- Debug, Info, Warning, Error, Critical through dedicated methods  
* **Organize logging components functionally** \- All logging functionality resides in `AhBearStudios.Core.Logging`

// Example logging integration with functional organization

namespace AhBearStudios.Application.Services

{

    using AhBearStudios.Core.Logging; // Single functional namespace import

    

    public class OrderService : IOrderService

    {  

        private readonly ILoggingService \_logger;  

          

        public OrderService(ILoggingService logger)  

        {  

            \_logger \= logger ?? throw new ArgumentNullException(nameof(logger));  

        }  

          

        public void ProcessOrder(Order order)  

        {  

            \_logger.LogInfo($"Processing order {order.Id}");  

            try  

            {  

                // Process order logic

                \_logger.LogInfo($"Order {order.Id} processed successfully");  

            }  

            catch (Exception ex)  

            {  

                \_logger.LogException(ex, $"Failed to process order {order.Id}");  

                throw;  

            }  

        }

    }

}

### **Message Bus Integration**

* **Use IMessageBusService** for cross-component communication instead of direct coupling  
* **Inject via Reflex** using constructor injection  
* **Get publishers and subscribers** using `GetPublisher<T>()` and `GetSubscriber<T>()`  
* **Design messages as immutable records** with clear intent and minimal data implementing `IMessage`  
* **Handle message failures gracefully** with retry policies and dead letter queues  
* **Publish domain events** using `PublishMessage<T>(T message)` for significant business operations  
* **Subscribe to relevant system events** (startup, shutdown, configuration changes) using `SubscribeToMessage<T>()`  
* **Use keyed publishers/subscribers** for targeted message routing with `GetPublisher<TKey, TMessage>()`  
* **Organize messaging components functionally** \- All messaging functionality resides in `AhBearStudios.Core.Messaging`

// Example message bus integration with functional organization

namespace AhBearStudios.Application.Services

{

    using AhBearStudios.Core.Logging;

    using AhBearStudios.Core.Messaging; // Single functional namespace import

    

    public class OrderService : IOrderService

    {  

        private readonly IMessageBusService \_messageBus;  

        private readonly ILoggingService \_logger;  

          

        public OrderService(IMessageBusService messageBus, ILoggingService logger)  

        {  

            \_messageBus \= messageBus ?? throw new ArgumentNullException(nameof(messageBus));  

            \_logger \= logger ?? throw new ArgumentNullException(nameof(logger));  

        }  

          

        public void ProcessOrder(Order order)  

        {  

            \_logger.LogInfo($"Processing order {order.Id}");  

              

            // Process order logic  

              

            // Publish domain event using messaging system

            \_messageBus.PublishMessage(new OrderProcessedMessage  

            {  

                OrderId \= order.Id,  

                ProcessedAt \= DateTime.UtcNow  

            });  

        }

    }

}

### **Object Pooling Integration**

* **Use IPoolingService** for frequently allocated objects (especially in hot paths)  
* **Inject via Reflex** using constructor injection  
* **Register pooled services** using `RegisterService<T>(T service)` for objects designed for pooling  
* **Retrieve from pools** using `GetService<T>()` and validate availability with `HasService<T>()`  
* **Reset object state** properly when returning objects to pools  
* **Monitor pool utilization** and adjust pool sizes based on usage patterns  
* **Pool expensive resources** like database connections, HTTP clients, and large buffers  
* **Thread-safe pool implementations** for concurrent access scenarios  
* **Service location pattern** \- Use IPoolingService as a service locator for pool management  
* **Organize pooling components functionally** \- All pooling functionality resides in `AhBearStudios.Core.Pooling`

// Example pooling integration with functional organization

namespace AhBearStudios.Application.Services

{

    using AhBearStudios.Core.Logging;

    using AhBearStudios.Core.Pooling; // Single functional namespace import

    

    public class DataProcessor : IDataProcessor

    {  

        private readonly IPoolingService \_poolingService;  

        private readonly ILoggingService \_logger;  

          

        public DataProcessor(IPoolingService poolingService, ILoggingService logger)  

        {  

            \_poolingService \= poolingService ?? throw new ArgumentNullException(nameof(poolingService));  

            \_logger \= logger ?? throw new ArgumentNullException(nameof(logger));  

        }  

          

        public void ProcessLargeDataSet()  

        {  

            if (\_poolingService.HasService\<LargeBuffer\>())  

            {  

                var buffer \= \_poolingService.GetService\<LargeBuffer\>();  

                try  

                {  

                    \_logger.LogInfo("Using pooled buffer for data processing");  

                    // Use buffer for processing

                }  

                finally  

                {  

                    // Reset buffer state and return to pool

                    buffer.Reset();  

                }  

            }  

        }

    }

}

### **Reflex Service Registration Patterns**

* **Singleton registration** using `AsSingle()` for stateless services and shared state  
* **Transient registration** using `AsTransient()` for stateful objects that need fresh instances  
* **Factory registration** for complex object creation scenarios  
* **Instance registration** using `FromInstance()` for pre-configured objects  
* **Conditional binding** using `When()` for environment-specific implementations  
* **Multi-binding** for collections and plugin architectures  
* **Lazy binding** using `Lazy<T>` for deferred initialization  
* **Named bindings** using `WithId()` for multiple implementations of same interface  
* **Functional installer organization** \- Group registration logic by functional system

// Comprehensive Reflex registration example with functional organization

namespace AhBearStudios.Core.Infrastructure.DependencyInjection

{

    public class CoreSystemsInstaller : MonoInstaller  

    {  

        public override void InstallBindings(ContainerBuilder builder)  

        {  

            // Core system services as singletons \- organized by function

            builder.Bind\<ILoggingService\>().To\<LoggingService\>().AsSingle();  

            builder.Bind\<IMessageBusService\>().To\<MessageBusService\>().AsSingle();  

            builder.Bind\<IProfilerService\>().To\<ProfilerService\>().AsSingle();  

            builder.Bind\<IAlertService\>().To\<AlertService\>().AsSingle();  

            builder.Bind\<IHealthCheckService\>().To\<HealthCheckService\>().AsSingle();  

            builder.Bind\<IPoolingService\>().To\<PoolingService\>().AsSingle();  

              

            // Factory pattern registration per functional system

            builder.Bind\<ILoggingServiceFactory\>().To\<LoggingServiceFactory\>().AsSingle();

            builder.Bind\<IMessageBusFactory\>().To\<MessageBusFactory\>().AsSingle();

              

            // Conditional bindings for different environments  

            builder.Bind\<IDataRepository\>().To\<ProductionDataRepository\>().AsSingle()  

                .When(context \=\> \!Application.isEditor);  

            builder.Bind\<IDataRepository\>().To\<TestDataRepository\>().AsSingle()  

                .When(context \=\> Application.isEditor);  

              

            // Multi-binding for plugin architecture per functional area

            builder.Bind\<ILogTarget\>().To\<UnityConsoleLogTarget\>().AsSingle().WithId("unity");

            builder.Bind\<ILogTarget\>().To\<SerilogLogTarget\>().AsSingle().WithId("serilog");

              

            // Lazy initialization for expensive services  

            builder.Bind\<Lazy\<IExpensiveService\>\>().FromMethod(container \=\>   

                new Lazy\<IExpensiveService\>(() \=\> container.Resolve\<IExpensiveService\>())).AsSingle();  

        }

    }

}

### **Profiling and Monitoring Integration**

* **Use IProfilerService** for performance monitoring in critical paths  
* **Inject via Reflex** using constructor injection  
* **Begin performance scopes** using `BeginScope(ProfilerTag tag)` or `BeginSample(string name)`  
* **Register metric alerts** using `RegisterMetricAlert(ProfilerTag, double threshold)`  
* **Monitor resource usage** with `GetMetrics(ProfilerTag)` for capacity planning  
* **Profile memory allocations** in performance-critical paths using Unity Profiler integration  
* **Implement performance counters** for key metrics (throughput, latency, error rates)  
* **Use diagnostic activities** for distributed tracing across components  
* **Expose metrics endpoints** for external monitoring systems through `GetAllMetrics()`  
* **Organize profiling components functionally** \- All profiling functionality resides in `AhBearStudios.Core.Profiling`

// Example profiling integration with functional organization

namespace AhBearStudios.Application.Services

{

    using AhBearStudios.Core.Logging;

    using AhBearStudios.Core.Profiling; // Single functional namespace import

    

    public class PerformanceCriticalService : IPerformanceCriticalService

    {  

        private readonly IProfilerService \_profiler;  

        private readonly ILoggingService \_logger;  

        private static readonly ProfilerTag ProcessingTag \= new ProfilerTag("DataProcessing");  

          

        public PerformanceCriticalService(IProfilerService profiler, ILoggingService logger)  

        {  

            \_profiler \= profiler ?? throw new ArgumentNullException(nameof(profiler));  

            \_logger \= logger ?? throw new ArgumentNullException(nameof(logger));  

            \_profiler.RegisterMetricAlert(ProcessingTag, 100.0); // Alert if \> 100ms

        }  

          

        public void ProcessData()  

        {  

            using var session \= \_profiler.BeginScope(ProcessingTag);  

            \_logger.LogInfo("Starting performance-critical data processing");  

            // Performance-critical processing logic

        }

    }

}

### **Alert System Integration**

* **Use IAlertService** for critical system failures and performance degradation  
* **Inject via Reflex** using constructor injection  
* **Trigger alerts** using `RaiseAlert(FixedString128Bytes message, AlertSeverity severity, FixedString64Bytes source, FixedString64Bytes tag)`  
* **Use structured alert data** with severity levels (Info, Warning, Critical, Emergency) via `AlertSeverity` enum  
* **Include actionable context** using correlation IDs with `RaiseAlert()` overload including `groupId` and `correlationId`  
* **Alert on threshold breaches** for performance metrics, error rates, and resource utilization  
* **Implement alert suppression** using `SetMinimumSeverity(AlertSeverity)` to prevent alert storms  
* **Route alerts appropriately** based on severity and system ownership  
* **Use Unity Collections** \- All alert parameters use `FixedString` types for performance  
* **Organize alert components functionally** \- All alert functionality resides in `AhBearStudios.Core.Alerts`

// Example alert integration with functional organization

namespace AhBearStudios.Application.Services

{

    using AhBearStudios.Core.Logging;

    using AhBearStudios.Core.Alerts; // Single functional namespace import

    

    public class CriticalService : ICriticalService

    {  

        private readonly IAlertService \_alertService;  

        private readonly ILoggingService \_logger;  

        private readonly FixedString64Bytes \_serviceSource \= "CriticalService";  

          

        public CriticalService(IAlertService alertService, ILoggingService logger)  

        {  

            \_alertService \= alertService ?? throw new ArgumentNullException(nameof(alertService));  

            \_logger \= logger ?? throw new ArgumentNullException(nameof(logger));  

        }  

          

        public void ProcessCriticalOperation()  

        {  

            try  

            {  

                \_logger.LogInfo("Starting critical operation");  

                // Critical operation logic

            }  

            catch (Exception ex)  

            {  

                \_logger.LogException(ex, "Critical operation failed");  

                \_alertService.RaiseAlert(  

                    message: "Critical operation failed",  

                    severity: AlertSeverity.Critical,  

                    source: \_serviceSource,  

                    tag: "ProcessingFailure"  

                );  

                throw;  

            }  

        }

    }

}

### **Serialization System Integration**

#### **MemoryPack Primary Serialization**

* **Use ISerializer abstraction** for all serialization operations to allow flexibility  
* **MemoryPack as primary implementation** \- Leverage MemoryPack for high-performance binary serialization  
* **Inject ISerializer** via Reflex constructor injection into services requiring serialization  
* **Design serializable types** with MemoryPack attributes and patterns for optimal performance  
* **Support fallback serializers** through abstraction for compatibility scenarios  
* **Zero-allocation serialization** \- Leverage MemoryPack's high-performance characteristics  
* **Version-safe serialization** \- Design schemas that can evolve over time  
* **Organize serialization components functionally** \- All serialization functionality resides in `AhBearStudios.Core.Serialization`

// Example serialization abstraction with functional organization

namespace AhBearStudios.Core.Serialization

{

    public interface ISerializer

    {

        byte\[\] Serialize\<T\>(T obj);

        T Deserialize\<T\>(byte\[\] data);

        T Deserialize\<T\>(ReadOnlySpan\<byte\> data);

        bool TryDeserialize\<T\>(byte\[\] data, out T result);

        bool TryDeserialize\<T\>(ReadOnlySpan\<byte\> data, out T result);

    }

    // MemoryPack implementation in functional namespace

    public class MemoryPackSerializer : ISerializer

    {

        private readonly ILoggingService \_logger;

        public MemoryPackSerializer(ILoggingService logger)

        {

            \_logger \= logger ?? throw new ArgumentNullException(nameof(logger));

        }

        public byte\[\] Serialize\<T\>(T obj)

        {

            try

            {

                return MemoryPackSerializer.Serialize(obj);

            }

            catch (Exception ex)

            {

                \_logger.LogException(ex, $"Failed to serialize {typeof(T).Name}");

                throw;

            }

        }

        

        // Additional methods...

    }

}

// MemoryPack serializable message example

namespace AhBearStudios.Core.Messaging.Messages.ApplicationMessages

{

    \[MemoryPackable\]

    public partial record OrderProcessedMessage : IMessage

    {

        public Guid Id { get; init; } \= Guid.NewGuid();

        public long TimestampTicks { get; init; } \= DateTime.UtcNow.Ticks;

        public ushort TypeCode { get; init; }

        \[MemoryPackOrder(0)\]

        public Guid OrderId { get; init; }

        \[MemoryPackOrder(1)\]

        public DateTime ProcessedAt { get; init; }

        \[MemoryPackOrder(2)\]

        public decimal Amount { get; init; }

        \[MemoryPackOrder(3)\]

        public string CustomerEmail { get; init; } \= string.Empty;

    }

}

// Service using serialization with functional organization

namespace AhBearStudios.Application.Services

{

    using AhBearStudios.Core.Logging;

    using AhBearStudios.Core.Serialization; // Single functional namespace import

    

    public class DataPersistenceService : IDataPersistenceService

    {

        private readonly ISerializer \_serializer;

        private readonly ILoggingService \_logger;

        public DataPersistenceService(ISerializer serializer, ILoggingService logger)

        {

            \_serializer \= serializer ?? throw new ArgumentNullException(nameof(serializer));

            \_logger \= logger ?? throw new ArgumentNullException(nameof(logger));

        }

        public async Task SaveOrderAsync(Order order)

        {

            try

            {

                \_logger.LogInfo($"Serializing order {order.Id}");

                var serializedData \= \_serializer.Serialize(order);

                // Save to storage

                await SaveToStorageAsync(serializedData);

                \_logger.LogInfo($"Order {order.Id} successfully persisted");

            }

            catch (Exception ex)

            {

                \_logger.LogException(ex, $"Failed to save order {order.Id}");

                throw;

            }

        }

    }

}

// Reflex registration for serialization with functional organization

namespace AhBearStudios.Core.Serialization

{

    public class SerializationInstaller : MonoInstaller

    {

        public override void InstallBindings(ContainerBuilder builder)

        {

            // Primary MemoryPack serializer

            builder.Bind\<ISerializer\>().To\<MemoryPackSerializer\>().AsSingle();

            // Alternative serializers for specific scenarios

            builder.Bind\<ISerializer\>().To\<JsonSerializer\>().AsSingle().WithId("json");

            builder.Bind\<ISerializer\>().To\<XmlSerializer\>().AsSingle().WithId("xml");

            // Serialization factory in functional context

            builder.Bind\<ISerializationFactory\>().To\<SerializationFactory\>().AsSingle();

        }

    }

}

#### **Serialization Best Practices**

* **Use MemoryPackable attribute** for all serializable types to optimize performance  
* **Explicit member ordering** using `MemoryPackOrder` for version compatibility  
* **Nullable reference type support** \- Design schemas with proper null handling  
* **Performance monitoring** \- Profile serialization operations in hot paths  
* **Schema evolution** \- Design for backward/forward compatibility  
* **Memory management** \- Leverage MemoryPack's zero-allocation features  
* **Type safety** \- Use strongly-typed serialization interfaces  
* **Functional organization** \- Keep all serialization components within the serialization functional system

### **MessageBus System Integration (MessagePipe)**

#### **MessagePipe Integration**

* **Use MessagePipe as the underlying message bus implementation** for IMessageBusService  
* **Leverage MessagePipe's performance optimizations** including zero-allocation messaging  
* **Support both synchronous and asynchronous message patterns** through MessagePipe  
* **Integrate with Reflex DI** for automatic subscriber registration and lifecycle management  
* **Use MessagePipe's filtering and routing capabilities** for advanced message scenarios  
* **Implement reliable delivery patterns** using MessagePipe's built-in features  
* **Organize MessagePipe integration functionally** \- All MessagePipe wrapper code resides in `AhBearStudios.Core.Messaging`

// MessagePipe-based IMessageBusService implementation with functional organization

namespace AhBearStudios.Core.Messaging

{

    using AhBearStudios.Core.Logging;

    using AhBearStudios.Core.Profiling;

    

    public class MessagePipeMessageBusService : IMessageBusService

    {

        private readonly IServiceProvider \_serviceProvider;

        private readonly ILoggingService \_logger;

        private readonly IProfilerService \_profiler;

        private readonly ConcurrentDictionary\<Type, object\> \_publisherCache;

        private readonly ConcurrentDictionary\<Type, object\> \_subscriberCache;

        public MessagePipeMessageBusService(

            IServiceProvider serviceProvider,

            ILoggingService logger,

            IProfilerService profiler)

        {

            \_serviceProvider \= serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            \_logger \= logger ?? throw new ArgumentNullException(nameof(logger));

            \_profiler \= profiler ?? throw new ArgumentNullException(nameof(profiler));

            \_publisherCache \= new ConcurrentDictionary\<Type, object\>();

            \_subscriberCache \= new ConcurrentDictionary\<Type, object\>();

        }

        public IMessagePublisher\<TMessage\> GetPublisher\<TMessage\>()

        {

            return (IMessagePublisher\<TMessage\>)\_publisherCache.GetOrAdd(

                typeof(TMessage),

                \_ \=\> new MessagePipePublisher\<TMessage\>(

                    \_serviceProvider.GetService\<IPublisher\<TMessage\>\>(),

                    \_logger,

                    \_profiler));

        }

        public void PublishMessage\<TMessage\>(TMessage message) where TMessage : IMessage

        {

            using var session \= \_profiler.BeginScope(new ProfilerTag($"Publish\_{typeof(TMessage).Name}"));

            try

            {

                \_logger.LogInfo($"Publishing message of type {typeof(TMessage).Name}");

                var publisher \= GetPublisher\<TMessage\>();

                publisher.Publish(message);

            }

            catch (Exception ex)

            {

                \_logger.LogException(ex, $"Failed to publish message of type {typeof(TMessage).Name}");

                throw;

            }

        }

    }

    // MessagePipe publisher wrapper in functional namespace

    internal class MessagePipePublisher\<TMessage\> : IMessagePublisher\<TMessage\>

    {

        private readonly IPublisher\<TMessage\> \_publisher;

        private readonly ILoggingService \_logger;

        private readonly IProfilerService \_profiler;

        public MessagePipePublisher(

            IPublisher\<TMessage\> publisher,

            ILoggingService logger,

            IProfilerService profiler)

        {

            \_publisher \= publisher ?? throw new ArgumentNullException(nameof(publisher));

            \_logger \= logger ?? throw new ArgumentNullException(nameof(logger));

            \_profiler \= profiler ?? throw new ArgumentNullException(nameof(profiler));

        }

        public void Publish(TMessage message)

        {

            using var session \= \_profiler.BeginScope(new ProfilerTag($"MessagePipe\_Publish\_{typeof(TMessage).Name}"));

            try

            {

                \_publisher.Publish(message);

                \_logger.LogInfo($"Successfully published {typeof(TMessage).Name} message");

            }

            catch (Exception ex)

            {

                \_logger.LogException(ex, $"MessagePipe failed to publish {typeof(TMessage).Name}");

                throw;

            }

        }

    }

}

// Reflex registration for MessagePipe integration with functional organization

namespace AhBearStudios.Core.Messaging

{

    public class MessageBusInstaller : MonoInstaller

    {

        public override void InstallBindings(ContainerBuilder builder)

        {

            // Register MessagePipe services

            builder.Register\<MessagePipeOptions\>(resolver \=\>

            {

                var options \= MessagePipeOptions.Default;

                options.InstanceLifetime \= InstanceLifetime.Singleton;

                return options;

            }, Lifetime.Singleton);

            // Register MessagePipe DI integration

            builder.RegisterMessagePipe(options \=\>

            {

                options.InstanceLifetime \= InstanceLifetime.Singleton;

                options.RequestHandlerLifetime \= InstanceLifetime.Singleton;

            });

            // Register our abstraction layer in functional context

            builder.Bind\<IMessageBusService\>().To\<MessagePipeMessageBusService\>().AsSingle();

            builder.Bind\<IMessageRegistry\>().To\<MessageRegistry\>().AsSingle();

            // Register message serialization in functional context

            builder.Bind\<IMessageSerializer\>().To\<MemoryPackMessageSerializer\>().AsSingle();

        }

    }

}

#### **MessagePipe Best Practices**

* **Zero-allocation messaging** \- Leverage MessagePipe's performance optimizations  
* **Proper subscription lifecycle** \- Dispose subscriptions properly to prevent memory leaks  
* **Error handling in handlers** \- Wrap message handlers with try-catch and logging  
* **Message filtering** \- Use MessagePipe's built-in filtering for targeted subscriptions  
* **Async/await patterns** \- Use MessagePipe's async capabilities for non-blocking operations  
* **DI integration** \- Leverage MessagePipe's Reflex integration for automatic registration  
* **Functional organization** \- Keep all MessagePipe integration within the messaging functional system

### **Logging System Integration with Multiple Targets**

#### **ILogTarget Implementations**

* **Abstract logging through ILogTarget interface** to support multiple logging destinations  
* **Unity Console Target** \- Primary target for development and debugging in Unity Editor  
* **Serilog Target** \- Enterprise-grade logging target for production environments  
* **Support multiple concurrent targets** \- Route messages to different destinations based on configuration  
* **Target-specific filtering** \- Configure different log levels and filters per target  
* **Performance-optimized batching** \- Use efficient message batching for high-throughput scenarios  
* **Organize log targets functionally** \- All log target implementations reside in `AhBearStudios.Core.Logging.Targets`

// Unity Console logging target with functional organization

namespace AhBearStudios.Core.Logging.Targets

{

    using AhBearStudios.Core.Logging.Formatters;

    

    public class UnityConsoleLogTarget : ILogTarget

    {

        private readonly ILogFormatter \_formatter;

        private readonly object \_lock \= new object();

        public string Name \=\> "UnityConsole";

        public LogLevel MinimumLevel { get; set; } \= LogLevel.Debug;

        public bool IsEnabled { get; set; } \= true;

        public UnityConsoleLogTarget(ILogFormatter formatter)

        {

            \_formatter \= formatter ?? throw new ArgumentNullException(nameof(formatter));

        }

        public void Write(in LogMessage entry)

        {

            if (\!ShouldProcessMessage(entry)) return;

            lock (\_lock)

            {

                var formattedMessage \= \_formatter.Format(entry).ToString();

                switch (entry.Level)

                {

                    case LogLevel.Debug:

                    case LogLevel.Info:

                        Debug.Log(formattedMessage);

                        break;

                    case LogLevel.Warning:

                        Debug.LogWarning(formattedMessage);

                        break;

                    case LogLevel.Error:

                    case LogLevel.Critical:

                        Debug.LogError(formattedMessage);

                        break;

                }

            }

        }

        public bool ShouldProcessMessage(in LogMessage logMessage)

        {

            return IsEnabled && 

                   logMessage.Level \>= MinimumLevel;

        }

        public void Dispose()

        {

            // Unity Console target doesn't require disposal

        }

    }

}

// Multi-target logging service with functional organization

namespace AhBearStudios.Core.Logging

{

    using AhBearStudios.Core.Messaging;

    using AhBearStudios.Core.Logging.Targets;

    

    public class MultiTargetLoggingService : ILoggingService

    {

        private readonly List\<ILogTarget\> \_targets;

        private readonly IMessageBusService \_messageBus;

        private readonly object \_lock \= new object();

        public MultiTargetLoggingService(

            IEnumerable\<ILogTarget\> targets,

            IMessageBusService messageBus)

        {

            \_targets \= targets?.ToList() ?? throw new ArgumentNullException(nameof(targets));

            \_messageBus \= messageBus ?? throw new ArgumentNullException(nameof(messageBus));

        }

        public void Log(LogLevel level, string message)

        {

            var logMessage \= CreateLogMessage(level, message);

            lock (\_lock)

            {

                foreach (var target in \_targets.Where(t \=\> t.IsEnabled))

                {

                    try

                    {

                        target.Write(logMessage);

                    }

                    catch (Exception ex)

                    {

                        // Log target failure to Unity Console as fallback

                        Debug.LogError($"Log target {target.Name} failed: {ex.Message}");

                    }

                }

            }

            // Publish log event to message bus

            \_messageBus.PublishMessage(new LogEntryMessage

            {

                Level \= level,

                Message \= message,

                Timestamp \= DateTime.UtcNow,

                Source \= GetCallerInfo()

            });

        }

        // Additional methods...

    }

}

// Reflex registration for logging with multiple targets using functional organization

namespace AhBearStudios.Core.Logging

{

    public class LoggingInstaller : MonoInstaller

    {

        \[SerializeField\] private UnityConsoleLogConfig \_unityConsoleConfig;

        \[SerializeField\] private SerilogConfig \_serilogConfig;

        \[SerializeField\] private LoggingConfig \_loggingConfig;

        public override void InstallBindings(ContainerBuilder builder)

        {

            // Register configurations in functional context

            builder.Bind\<UnityConsoleLogConfig\>().FromInstance(\_unityConsoleConfig);

            builder.Bind\<SerilogConfig\>().FromInstance(\_serilogConfig);

            builder.Bind\<LoggingConfig\>().FromInstance(\_loggingConfig);

            // Register formatter in functional context

            builder.Bind\<ILogFormatter\>().To\<DefaultLogFormatter\>().AsSingle();

            // Register individual log targets in functional context

            builder.Bind\<ILogTarget\>().To\<UnityConsoleLogTarget\>().AsSingle().WithId("unity");

            builder.Bind\<ILogTarget\>().To\<SerilogLogTarget\>().AsSingle().WithId("serilog");

            // Register logging service with all targets

            builder.Bind\<ILoggingService\>().To\<MultiTargetLoggingService\>().AsSingle();

            // Register health check for logging system

            builder.Bind\<LoggingHealthCheck\>().AsSingle();

        }

        public override void Start()

        {

            // Validate that all targets are properly configured

            var loggingService \= Container.Resolve\<ILoggingService\>();

            var targets \= loggingService.GetRegisteredTargets();

            

            if (targets.Count \== 0\)

            {

                throw new InvalidOperationException("No log targets registered");

            }

            // Register health check

            var healthService \= Container.Resolve\<IHealthCheckService\>();

            var loggingHealthCheck \= Container.Resolve\<LoggingHealthCheck\>();

            healthService.RegisterHealthCheck(loggingHealthCheck);

            // Log successful initialization

            loggingService.LogInfo($"Logging system initialized with {targets.Count} targets: {string.Join(", ", targets.Select(t \=\> t.Name))}");

        }

    }

}

#### **Logging Target Best Practices**

* **Target isolation** \- Ensure failures in one target don't affect others  
* **Performance optimization** \- Use batching and async operations for high-throughput scenarios  
* **Configuration flexibility** \- Allow per-target configuration of levels, filters, and formatting  
* **Graceful degradation** \- Fall back to Unity Console if other targets fail  
* **Resource management** \- Properly dispose of targets that require cleanup (like file handles)  
* **Health monitoring** \- Include logging targets in health check system  
* **Structured logging support** \- Design targets to handle structured data when available  
* **Functional organization** \- Keep all target implementations within the logging functional system

### **HealthChecker System Integration**

* **Use IHealthCheckService** for comprehensive health monitoring  
* **Inject via Reflex** using constructor injection  
* **Register health checks** using `RegisterHealthCheck(IHealthCheck healthCheck, IHealthCheckConfig config)`  
* **Provide granular health status** through health check implementations  
* **Execute health checks** using `ExecuteHealthCheckAsync(FixedString64Bytes name)`  
* **Monitor overall system health** using `GetOverallHealthStatusAsync()`  
* **Include diagnostic information** in health check responses  
* **Implement dependency health checks** for external services and resources  
* **Set appropriate timeouts** for health check operations to prevent blocking  
* **Cache health check results** for frequently queried components  
* **Bulk operations** \- Use `ExecuteAllHealthChecksAsync()` and `RegisterHealthChecks()` for efficiency  
* **Historical data** \- Access health check history and statistics  
* **Organize health check components functionally** \- All health check functionality resides in `AhBearStudios.Core.HealthCheck`

// Example health check integration with functional organization

namespace AhBearStudios.Core.HealthCheck.Checks

{

    using AhBearStudios.Core.Logging;

    

    public class DatabaseHealthCheck : IHealthCheck

    {

        private readonly IDatabaseService \_database;

        private readonly ILoggingService \_logger;

        public FixedString64Bytes Name \=\> "DatabaseHealth";

        public DatabaseHealthCheck(IDatabaseService database, ILoggingService logger)

        {

            \_database \= database ?? throw new ArgumentNullException(nameof(database));

            \_logger \= logger ?? throw new ArgumentNullException(nameof(logger));

        }

        public async Task\<HealthCheckResult\> CheckHealthAsync(CancellationToken cancellationToken)

        {

            try

            {

                \_logger.LogInfo("Executing database health check");

                await \_database.PingAsync(cancellationToken);

                return HealthCheckResult.Healthy("Database connection is healthy");

            }

            catch (Exception ex)

            {

                \_logger.LogException(ex, "Database health check failed");

                return HealthCheckResult.Unhealthy("Database connection failed", ex);

            }

        }

    }

}

// Register health check using Reflex with functional organization

namespace AhBearStudios.Application.Services

{

    public class DatabaseServiceInstaller : MonoInstaller

    {

        public override void InstallBindings(ContainerBuilder builder)

        {

            builder.Bind\<IDatabaseService\>().To\<DatabaseService\>().AsSingle();

            builder.Bind\<DatabaseHealthCheck\>().AsSingle();

        }

        public override void Start()

        {

            var healthService \= Container.Resolve\<IHealthCheckService\>();

            var healthCheck \= Container.Resolve\<DatabaseHealthCheck\>();

            healthService.RegisterHealthCheck(healthCheck);

        }

    }

}

### **Bootstrapper System Integration with Reflex**

* **All systems must be bootstrappable** through Reflex MonoInstaller components  
* **Implement MonoInstaller** for components requiring initialization and registration  
* **Use InstallBindings()** for service registration with appropriate lifetimes  
* **Use Start()** method for post-installation initialization  
* **Define clear initialization order** using ScriptExecutionOrder or installer dependencies  
* **Support graceful shutdown** through proper resource cleanup  
* **Validate system readiness** before and after installation  
* **Handle bootstrap failures gracefully** with detailed error reporting  
* **Register shutdown hooks** for proper resource cleanup  
* **Use Reflex validation** to ensure all dependencies are properly registered  
* **Organize bootstrap components functionally** \- Bootstrap logic resides in `AhBearStudios.Core.Infrastructure.Bootstrap`

// Example Reflex installer for core systems with functional organization

namespace AhBearStudios.Core.Infrastructure.Bootstrap

{

    using AhBearStudios.Core.Logging;

    using AhBearStudios.Core.Messaging;

    using AhBearStudios.Core.Profiling;

    using AhBearStudios.Core.Alerts;

    using AhBearStudios.Core.HealthCheck;

    using AhBearStudios.Core.Pooling;

    

    \[DefaultExecutionOrder(-1000)\] // Execute early

    public class CoreSystemsInstaller : MonoInstaller

    {

        \[SerializeField\] private LoggingConfig \_loggingConfig;

        \[SerializeField\] private MessageBusConfig \_messageBusConfig;

        public override void InstallBindings(ContainerBuilder builder)

        {

            // Register configurations from functional systems

            builder.Bind\<LoggingConfig\>().FromInstance(\_loggingConfig);

            builder.Bind\<MessageBusConfig\>().FromInstance(\_messageBusConfig);

            // Register core services by functional system

            builder.Bind\<ILoggingService\>().To\<LoggingService\>().AsSingle();

            builder.Bind\<IMessageBusService\>().To\<MessageBusService\>().AsSingle();

            builder.Bind\<IProfilerService\>().To\<ProfilerService\>().AsSingle();

            builder.Bind\<IAlertService\>().To\<AlertService\>().AsSingle();

            builder.Bind\<IHealthCheckService\>().To\<HealthCheckService\>().AsSingle();

            builder.Bind\<IPoolingService\>().To\<PoolingService\>().AsSingle();

            // Register health checks from functional systems

            builder.Bind\<CoreSystemHealthCheck\>().AsSingle();

        }

        public override void Start()

        {

            // Validate that all critical services are registered

            ValidateCriticalServices();

            // Initialize services that require post-construction setup

            InitializeServices();

            // Register health checks from functional systems

            RegisterHealthChecks();

            var logger \= Container.Resolve\<ILoggingService\>();

            logger.LogInfo("Core systems successfully initialized");

        }

        private void ValidateCriticalServices()

        {

            var requiredServices \= new\[\]

            {

                typeof(ILoggingService),

                typeof(IMessageBusService),

                typeof(IProfilerService),

                typeof(IAlertService),

                typeof(IHealthCheckService),

                typeof(IPoolingService)

            };

            foreach (var serviceType in requiredServices)

            {

                if (\!Container.HasBinding(serviceType))

                {

                    throw new InvalidOperationException($"Critical service {serviceType.Name} is not registered");

                }

            }

        }

        private void InitializeServices()

        {

            // Start services that need initialization

            var messageBus \= Container.Resolve\<IMessageBusService\>();

            var profiler \= Container.Resolve\<IProfilerService\>();

            profiler.StartProfiling();

        }

        private void RegisterHealthChecks()

        {

            var healthService \= Container.Resolve\<IHealthCheckService\>();

            var coreHealthCheck \= Container.Resolve\<CoreSystemHealthCheck\>();

            healthService.RegisterHealthCheck(coreHealthCheck);

        }

    }

}

## **Reflex-Specific Patterns and Best Practices**

### **Registration Patterns**

* **Lifetime Management**: Use `AsSingle()` for stateless services, `AsTransient()` for stateful objects  
* **Factory Pattern**: Register factories as singletons and use them to create transient objects  
* **Conditional Binding**: Use `When()` for environment-specific implementations  
* **Multi-Registration**: Use `WithId()` for multiple implementations of the same interface  
* **Lazy Loading**: Use `Lazy<T>` for expensive-to-create services  
* **Functional Registration**: Organize registration logic by functional system using dedicated installers

### **Resolution Patterns**

* **Constructor Injection**: Primary pattern for dependency resolution  
* **Property/Field Injection**: Use `[Inject]` attribute sparingly, prefer constructor injection  
* **Manual Resolution**: Use `Container.Resolve<T>()` only when constructor injection isn't possible  
* **Collection Resolution**: Use `Container.ResolveAll<T>()` for plugin architectures  
* **Optional Dependencies**: Use `Container.TryResolve<T>()` for optional services  
* **Functional Dependencies**: Import functional namespaces to access registered services

### **Validation and Error Handling**

* **Binding Validation**: Use `Container.HasBinding<T>()` to check registrations  
* **Circular Dependency Detection**: Reflex handles this automatically, but design to avoid them  
* **Missing Dependency Handling**: Implement graceful fallbacks for optional dependencies  
* **Container Validation**: Validate critical services during startup in installer Start() methods  
* **Functional Validation**: Verify all functional systems are properly registered and operational

## **Production Readiness & Robustness**

* **Comprehensive error handling** \- Handle all possible failure scenarios gracefully  
* **Input validation** \- Validate all inputs at public API boundaries using `ArgumentNullException` and validation attributes  
* **Thread safety** \- Ensure thread-safe operations where concurrent access is possible  
* **Resource cleanup** \- Implement proper disposal patterns and resource management, especially for Unity.Collections  
* **Configuration validation** \- Validate all configuration objects at startup through Reflex installers  
* **Graceful degradation** \- Design systems to continue operating when non-critical components fail  
* **Circuit breaker patterns** \- Implement circuit breakers for external service calls  
* **Retry policies with exponential backoff** \- Handle transient failures appropriately  
* **Alert on system degradation** \- Use `IAlertService` to notify operators when systems enter degraded states  
* **Health check integration** \- Ensure all critical paths are covered by health checks registered with `IHealthCheckService`  
* **Functional system robustness** \- Design each functional system to operate independently where possible

## **Testability Requirements**

* **Constructor dependency injection** \- All dependencies must be injectable via Reflex for testing  
* **Interface segregation** \- Create focused interfaces that can be easily mocked  
* **Pure functions where possible** \- Minimize side effects and external dependencies  
* **Testable abstractions** \- Abstract external dependencies (file system, network, time, etc.)  
* **Deterministic behavior** \- Avoid random numbers, current time, or other non-deterministic inputs in core logic  
* **Factory pattern compliance** \- Ensure factories can be configured for test scenarios via Reflex  
* **Observable state** \- Provide ways to inspect internal state for verification  
* **Configurable behavior** \- Allow test-specific configuration through builders or config objects  
* **Mock-friendly core systems** \- Design logging, messaging, pooling, alerting, and health check integrations to be easily testable  
* **Test container support** \- Use Reflex test containers for unit and integration testing  
* **Functional system testing** \- Design tests to validate functional system boundaries and interactions

// Example test setup with Reflex following functional organization

namespace AhBearStudios.Tests.Unit.Application.Services

{

    using AhBearStudios.Core.Logging;

    using AhBearStudios.Core.Messaging;

    using AhBearStudios.Application.Services;

    

    \[Test\]

    public void ServiceProcessesDataCorrectly()

    {

        // Arrange

        var testContainer \= new Container();

        var builder \= new ContainerBuilder(testContainer);

        // Register mocks following functional organization

        var mockLogger \= new Mock\<ILoggingService\>();

        var mockMessageBus \= new Mock\<IMessageBusService\>();

        builder.Bind\<ILoggingService\>().FromInstance(mockLogger.Object);

        builder.Bind\<IMessageBusService\>().FromInstance(mockMessageBus.Object);

        builder.Bind\<IDataService\>().To\<DataService\>().AsSingle();

        builder.Build();

        // Act

        var service \= testContainer.Resolve\<IDataService\>();

        service.ProcessData();

        // Assert

        mockLogger.Verify(x \=\> x.LogInfo("Processing data"), Times.Once);

    }

}

## **Memory and Performance Optimization**

* **Prioritize low-GC allocations** and efficient memory layout  
* **Minimize memory overhead** in all implementations  
* **Burst-compatible code** where applicable  
* **Unity.Collections v2 only** \- Remove all Unity.Collections v1 usage, use `FixedString` types  
* **Leverage object pooling** through `IPoolingService` for allocation-heavy operations  
* **Profile and optimize hot paths** using `IProfilerService` integrated profiling tools  
* **Monitor and alert on performance regressions** through integrated systems  
* **Optimize Reflex resolution** \- Cache frequently resolved services, use singletons appropriately  
* **Functional system optimization** \- Optimize each functional system independently for performance characteristics

## **Core Systems Implementation Guidelines**

### **When to Implement Each System**

* **Logging (ILoggingService)**: Always implement for services, factories, and error-prone operations  
* **Message Bus (IMessageBusService)**: Use for decoupled communication between major components  
* **Pooling (IPoolingService)**: Implement for objects allocated frequently (\>1000 times per second)  
* **Reflex DI Integration**: Required for all components that have dependencies  
* **Profiling (IProfilerService)**: Add to performance-critical paths and resource-intensive operations  
* **Alerts (IAlertService)**: Implement for all failure scenarios that require operator intervention  
* **HealthChecker (IHealthCheckService)**: Required for all services, external dependencies, and critical resources  
* **Reflex Installers**: All systems and components must integrate with Reflex-based bootstrap process  
* **Functional organization**: Organize implementation by functional domain for all systems

### **System Integration Patterns**

* **Service Layer**: Full integration with all core systems (logging, Reflex DI, profiling, alerts, health checks)  
* **Domain Layer**: Minimal integration (logging, message bus for domain events via Reflex injection)  
* **Infrastructure Layer**: Complete integration with all core systems via Reflex  
* **Presentation Layer**: Logging, Reflex DI, health checks, and performance monitoring  
* **Bootstrap Layer**: Orchestrates initialization of all other systems using Reflex MonoInstaller pattern  
* **Functional Integration**: Each functional system integrates with others through well-defined interfaces

### **Reflex Integration Requirements**

* **Installer-based initialization** \- Components must use MonoInstaller for registration and setup  
* **Dependency declaration** \- Components must declare their dependencies through constructor parameters  
* **Configuration validation** \- All configuration must be validated during installer Start() methods  
* **Health check registration** \- Components must register their health checks during installer Start() phase  
* **Alert configuration** \- Alert rules and thresholds must be configured during bootstrap  
* **Cleanup registration** \- Components must implement proper disposal for graceful shutdown  
* **Functional installer organization** \- Use dedicated installers per functional system

## **Code Quality Standards**

* **Avoid redundancy** \- Reuse or refactor existing types rather than creating duplicates  
* **Clean using directives** \- Include only relevant usings, remove unused ones  
* **No obsolete APIs** \- Do not use deprecated Unity methods or .NET APIs  
* **SOLID principles** \- Follow Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, and Dependency Inversion  
* **Consistent core system usage** \- Use established patterns for logging, messaging, alerting, and resource management  
* **Reflex best practices** \- Follow Reflex conventions for registration and resolution  
* **Functional organization compliance** \- Ensure all code follows functional organization principles

## **Safety and Error Handling**

* **Include null checks** for all parameters and dependencies using `ArgumentNullException`  
* **Implement proper disposal** for unmanaged and Native containers, especially Unity.Collections  
* **Add safety handles** for resource management  
* **Use ArgumentNullException** for null parameter validation  
* **Exception safety guarantees** \- Ensure strong exception safety where possible  
* **Fail-fast principle** \- Detect and report errors as early as possible  
* **Log all exceptions** using `ILoggingService.LogException()` with appropriate context and correlation information  
* **Alert on critical exceptions** using `IAlertService` that indicate system instability  
* **Update health status** on error conditions using health checks to reflect system state  
* **Reflex resolution safety** \- Handle resolution failures gracefully with try-catch or validation  
* **Functional system isolation** \- Design error handling to prevent failures in one functional system from affecting others

## **Documentation Requirements**

* **XML documentation for all public members**  
* Document all public classes, structs, interfaces, fields, properties, and methods  
* Use `/// <summary>`, `/// <param>`, `/// <returns>` tags consistently  
* Include `/// <inheritdoc />` for interface implementations  
* **Document error conditions** \- Specify what exceptions can be thrown and when  
* **Usage examples** \- Include code examples for complex APIs  
* **Document core system integrations** \- Explain logging behavior, message contracts, pooling requirements, alert conditions, and health check implementations  
* **Document Reflex requirements** \- Specify installer dependencies, registration patterns, and resolution strategies  
* **Document bootstrap requirements** \- Specify initialization phases, dependencies, and shutdown procedures  
* **Document functional organization** \- Explain functional system boundaries, responsibilities, and interaction patterns

## **Separation of Concerns**

* Each class should have a single, well-defined responsibility  
* Separate configuration from implementation logic  
* Keep factory logic focused on object creation  
* Isolate service logic from infrastructure concerns  
* **Separate pure logic from side effects** \- Keep business logic separate from I/O operations  
* **Abstract core system dependencies** \- Use interfaces for logging, messaging, pooling, alerting, and health checking  
* **Isolate bootstrap logic** \- Keep initialization separate from runtime behavior using Reflex installers  
* **Functional separation** \- Maintain clear boundaries between functional systems while allowing controlled interaction

## **Dependency Management**

* Constructor injection for required dependencies via Reflex  
* Optional parameters with sensible defaults where appropriate  
* Validate all injected dependencies in constructors using null checks  
* Store dependencies as readonly fields when possible  
* **Use explicit dependency injection** \- Leverage Reflex constructor injection as the primary pattern  
* **Minimize dependency chains** \- Keep dependency graphs shallow and focused  
* **Register core system dependencies** \- Ensure logging, messaging, pooling, alerting, and health checking are properly configured in Reflex  
* **Reflex installer dependency registration** \- All dependencies must be resolvable during bootstrap through MonoInstaller components  
* **Functional dependency management** \- Manage dependencies within functional systems and minimize cross-functional dependencies

## **Testing Considerations**

* **Design for unit testing** \- Each component should be testable in isolation with Reflex test containers  
* **Integration test points** \- Identify and design clear integration boundaries  
* **Mock-friendly interfaces** \- Create interfaces that are easy to mock or stub in Reflex test scenarios  
* **Test data builders** \- Design builders to easily create test data scenarios  
* **Deterministic factories** \- Ensure factories can produce predictable outputs for testing  
* **Test core system integrations** \- Verify logging output, message publishing, alert triggering, health check responses, and resource usage  
* **Reflex testing** \- Provide test harnesses for bootstrap scenarios and failure conditions using test containers  
* **Health check testing** \- Verify health check behavior under various system states  
* **Container validation testing** \- Test that all required dependencies are properly registered and resolvable  
* **Functional system testing** \- Design tests to validate each functional system independently and their interactions

  # **Unity Integration Guidelines**

  ## **Core vs Unity Separation Principle**

* **Core systems remain Unity-agnostic** \- All business logic, algorithms, and data processing should be implemented as POCO classes in `AhBearStudios.Core.{FunctionalSystem}`  
* **Unity-specific implementations live separately** \- Unity integrations, MonoBehaviour wrappers, and Unity API usage reside in `AhBearStudios.Unity.{FunctionalSystem}`  
* **Performance-first core design** \- Core implementations prioritize Unity.Collections v2, Burst compatibility, and zero-allocation patterns  
* **Functional domain organization maintained** \- Both Core and Unity layers follow the same functional organization principles  
* **Seamless integration** \- Unity layer provides clean adapters and wrappers around core POCO implementations

  ## **Extended Namespace Organization for Unity Integration**

  ### **Core Layer Structure (Unity-Agnostic)**

  // Core POCO implementations (Unity-agnostic)  
  AhBearStudios.Core.{FunctionalSystem}/  
  ├── I{System}Service.cs              // Pure interfaces \- no Unity dependencies  
  ├── {System}Service.cs               // POCO implementations \- pure C\#  
  ├── Configs/                         // Pure config objects and builders  
  │   ├── {System}Config.cs           // Configuration data structures  
  │   └── {System}ConfigBuilder.cs    // Builder pattern implementations  
  ├── Builders/                        // POCO builders \- pure logic  
  │   ├── I{System}ConfigBuilder.cs   // Builder interfaces  
  │   └── {System}ConfigBuilder.cs    // Builder implementations  
  ├── Factories/                       // POCO factories \- creation logic  
  │   ├── I{System}ServiceFactory.cs  // Factory interfaces  
  │   └── {System}ServiceFactory.cs   // Factory implementations  
  ├── Services/                        // Additional supporting services  
  │   ├── {Additional}Service.cs      // Pure business logic services  
  │   └── {Support}Service.cs         // Supporting functionality  
  ├── Models/                          // Data structures and DTOs  
  │   ├── {System}Models.cs           // Core data models  
  │   └── {System}Events.cs           // Event data structures  
  └── HealthChecks/                    // Health monitoring \- pure logic

    └── {System}ServiceHealthCheck.cs

### **Unity Layer Structure (Unity-Specific)**

// Unity-specific implementations

AhBearStudios.Unity.{FunctionalSystem}/

├── {System}MonoBehaviour.cs         // MonoBehaviour wrappers

├── Installers/                      // Reflex MonoInstallers

│   ├── {System}Installer.cs        // Primary system installer

│   └── {System}UnityInstaller.cs   // Unity-specific registrations

├── Components/                      // Unity Components

│   ├── {System}Component.cs        // Scene-based components

│   └── {System}Manager.cs          // GameObject-based managers

├── Targets/                         // Unity-specific implementations

│   ├── Unity{Target}Target.cs      // Unity API integrations

│   └── {Unity}Adapter.cs           // Unity-specific adapters

├── ScriptableObjects/               // Unity configuration assets

│   ├── {System}ConfigAsset.cs      // ScriptableObject configs

│   └── {System}Settings.cs         // Unity-serializable settings

├── Editor/                          // Unity Editor integrations

│   ├── {System}Editor.cs           // Custom inspectors

│   └── {System}Window.cs           // Editor windows

└── HealthChecks/                    // Unity-specific health checks

    └── Unity{System}HealthCheck.cs  // Unity API health monitoring

### **Complete Functional System Example**

// Logging System \- Complete structure

AhBearStudios.Core.Logging/

├── ILoggingService.cs               // Core logging interface

├── LoggingService.cs                // POCO logging implementation

├── Configs/

│   ├── LoggingConfig.cs            // Core logging configuration

│   └── LogConfigBuilder.cs         // Configuration builder

├── Builders/

│   ├── ILogConfigBuilder.cs        // Builder interface

│   └── LogConfigBuilder.cs         // Builder implementation

├── Factories/

│   ├── ILogTargetFactory.cs        // Target factory interface

│   └── LogTargetFactory.cs         // Target factory implementation

├── Services/

│   ├── LogBatchingService.cs       // Batching logic

│   └── LogFormattingService.cs     // Formatting logic

├── Targets/

│   ├── ILogTarget.cs               // Target interface

│   ├── FileLogTarget.cs            // File-based target (pure C\#)

│   └── MemoryLogTarget.cs          // In-memory target

├── Models/

│   ├── LogMessage.cs               // Core log message structure

│   └── LogLevel.cs                 // Log level enumeration

└── HealthChecks/

    └── LoggingServiceHealthCheck.cs // Core health check logic

AhBearStudios.Unity.Logging/

├── UnityLoggingBehaviour.cs         // MonoBehaviour wrapper

├── Installers/

│   ├── LoggingInstaller.cs         // Core service registration

│   └── UnityLoggingInstaller.cs    // Unity-specific registration

├── Components/

│   ├── LoggingComponent.cs         // Scene logging component

│   └── LogDisplayComponent.cs      // UI log display

├── Targets/

│   ├── UnityConsoleLogTarget.cs    // Unity Debug.Log integration

│   ├── UnityUILogTarget.cs         // Unity UI integration

│   └── PlayerPrefsLogTarget.cs     // Unity PlayerPrefs storage

├── ScriptableObjects/

│   ├── LoggingConfigAsset.cs       // Unity-serializable config

│   └── LoggingSettings.cs          // Project settings

├── Editor/

│   ├── LoggingEditor.cs            // Custom inspector

│   └── LogViewerWindow.cs          // Editor log viewer

└── HealthChecks/

    └── UnityLoggingHealthCheck.cs   // Unity-specific health monitoring

## **Unity Integration Guidelines**

### **When to Use Core vs Unity Namespaces**

#### **Use Core Namespace For:**

* **Business logic and algorithms** \- Pure computational functions and data processing  
* **Cross-platform compatible code** \- Logic that can run outside Unity  
* **Performance-critical operations** \- Code that benefits from Burst compilation  
* **Testable service implementations** \- Logic that can be unit tested without Unity  
* **Data structures and models** \- Pure data objects and DTOs  
* **Configuration and builder logic** \- Object creation and configuration patterns  
* **Factory implementations** \- Object creation logic  
* **Abstract interfaces** \- Contracts and abstractions  
* **Core health check logic** \- Health monitoring without Unity dependencies

  #### **Use Unity Namespace For:**

* **MonoBehaviour wrappers and adapters** \- Unity lifecycle integration  
* **Unity-specific API integration** \- Debug.Log, PlayerPrefs, Resources, etc.  
* **Scene-based component implementations** \- GameObject-attached components  
* **Reflex MonoInstaller registration** \- DI container configuration  
* **Unity asset integration** \- ScriptableObjects, Resources, Addressables  
* **Unity lifecycle event handling** \- Start, Update, OnDestroy patterns  
* **Unity Editor integrations** \- Custom inspectors, editor windows, tools  
* **Unity-specific health checks** \- Monitoring Unity API availability and performance  
* **Unity UI integration** \- Canvas, UI components, event systems

  ### **Core Implementation Patterns**

  #### **POCO Service Implementation**

  csharp  
  *// Core POCO implementation \- No Unity dependencies*  
  namespace AhBearStudios.Core.Logging  
  {  
      public class LoggingService : ILoggingService  
      {  
          private readonly ILogTarget\[\] \_targets;  
          private readonly ILogFormatter \_formatter;  
          private readonly LoggingConfig \_config;  
            
          public LoggingService(  
              IEnumerable\<ILogTarget\> targets,  
              ILogFormatter formatter,  
              LoggingConfig config)  
          {  
              \_targets \= targets?.ToArray() ?? throw new ArgumentNullException(nameof(targets));  
              \_formatter \= formatter ?? throw new ArgumentNullException(nameof(formatter));  
              \_config \= config ?? throw new ArgumentNullException(nameof(config));  
          }  
            
          public void LogInfo(string message)  
          {  
              var logMessage \= new LogMessage  
              {  
                  Level \= LogLevel.Info,  
                  Message \= message,  
                  Timestamp \= DateTime.UtcNow,  
                  ThreadId \= Thread.CurrentThread.ManagedThreadId  
              };  
                
              ProcessLogMessage(logMessage);  
          }  
            
          private void ProcessLogMessage(in LogMessage logMessage)  
          {  
              *// Pure business logic \- no Unity API calls*  
              foreach (var target in \_targets)  
              {  
                  if (target.ShouldProcessMessage(logMessage))  
                  {  
                      target.Write(logMessage);  
                  }  
              }  
          }  
      }

}

#### **Performance-Optimized Core Target**

csharp

*// Core target implementation using Unity.Collections v2*

namespace AhBearStudios.Core.Logging.Targets

{

    using Unity.Collections;

    

    public class MemoryLogTarget : ILogTarget, IDisposable

    {

        private NativeList\<LogEntry\> \_logEntries;

        private readonly object \_lock \= new object();

        

        public MemoryLogTarget(int initialCapacity \= 1000)

        {

            \_logEntries \= new NativeList\<LogEntry\>(initialCapacity, Allocator.Persistent);

        }

        

        public void Write(in LogMessage logMessage)

        {

            var entry \= new LogEntry

            {

                Level \= logMessage.Level,

                TimestampTicks \= logMessage.Timestamp.Ticks,

                Message \= new FixedString512Bytes(logMessage.Message),

                ThreadId \= logMessage.ThreadId

            };

            

            lock (\_lock)

            {

                \_logEntries.Add(entry);

            }

        }

        

        public void Dispose()

        {

            if (\_logEntries.IsCreated)

            {

                \_logEntries.Dispose();

            }

        }

    }

}

### **Unity Integration Patterns**

#### **MonoBehaviour Wrapper Pattern**

csharp

*// Unity wrapper for core service*

namespace AhBearStudios.Unity.Logging

{

    using AhBearStudios.Core.Logging;

    

    public class UnityLoggingBehaviour : MonoBehaviour

    {

        \[Inject\] private ILoggingService \_loggingService;

        \[Inject\] private IMessageBusService \_messageBus;

        

        \[SerializeField\] private bool \_logUnityEvents \= true;

        \[SerializeField\] private bool \_logFrameRate \= false;

        

        private void Start()

        {

            if (\_logUnityEvents)

            {

                \_loggingService.LogInfo($"GameObject '{gameObject.name}' started");

            }

        }

        

        private void Update()

        {

            if (\_logFrameRate && Time.frameCount % 60 \== 0)

            {

                \_loggingService.LogInfo($"FPS: {1.0f / Time.deltaTime:F1}");

            }

        }

        

        private void OnDestroy()

        {

            if (\_logUnityEvents)

            {

                \_loggingService.LogInfo($"GameObject '{gameObject.name}' destroyed");

            }

        }

    }

}

#### **Unity-Specific Target Implementation**

csharp

*// Unity-specific log target*

namespace AhBearStudios.Unity.Logging.Targets

{

    using AhBearStudios.Core.Logging;

    using AhBearStudios.Core.Logging.Targets;

    using UnityEngine;

    

    public class UnityConsoleLogTarget : ILogTarget

    {

        private readonly ILogFormatter \_formatter;

        private readonly bool \_includeStackTrace;

        

        public string Name \=\> "UnityConsole";

        public LogLevel MinimumLevel { get; set; } \= LogLevel.Debug;

        public bool IsEnabled { get; set; } \= true;

        

        public UnityConsoleLogTarget(ILogFormatter formatter, bool includeStackTrace \= false)

        {

            \_formatter \= formatter ?? throw new ArgumentNullException(nameof(formatter));

            \_includeStackTrace \= includeStackTrace;

        }

        

        public void Write(in LogMessage logMessage)

        {

            if (\!ShouldProcessMessage(logMessage)) return;

            

            var formattedMessage \= \_formatter.Format(logMessage).ToString();

            

            *// Unity-specific API usage*

            switch (logMessage.Level)

            {

                case LogLevel.Debug:

                case LogLevel.Info:

                    Debug.Log(formattedMessage);

                    break;

                case LogLevel.Warning:

                    Debug.LogWarning(formattedMessage);

                    break;

                case LogLevel.Error:

                case LogLevel.Critical:

                    Debug.LogError(formattedMessage);

                    break;

            }

        }

        

        public bool ShouldProcessMessage(in LogMessage logMessage)

        {

            return IsEnabled && logMessage.Level \>= MinimumLevel;

        }

        

        public void Dispose()

        {

            *// Unity Console target doesn't require disposal*

        }

    }

}

#### **Reflex MonoInstaller Pattern**

csharp

*// Unity-specific installer*

namespace AhBearStudios.Unity.Logging.Installers

{

    using AhBearStudios.Core.Logging;

    using AhBearStudios.Core.Logging.Targets;

    using AhBearStudios.Unity.Logging.Targets;

    

    \[DefaultExecutionOrder(\-500)\]

    public class UnityLoggingInstaller : MonoInstaller

    {

        \[SerializeField\] private LoggingConfigAsset \_configAsset;

        \[SerializeField\] private bool \_enableUnityConsole \= true;

        \[SerializeField\] private bool \_enableFileLogging \= true;

        

        public override void InstallBindings(ContainerBuilder builder)

        {

            *// Register core configuration*

            var config \= \_configAsset \!= null ? \_configAsset.Config : LoggingConfig.Default;

            builder.Bind\<LoggingConfig\>().FromInstance(config);

            

            *// Register core services (POCO implementations)*

            builder.Bind\<ILogFormatter\>().To\<DefaultLogFormatter\>().AsSingle();

            builder.Bind\<ILoggingService\>().To\<LoggingService\>().AsSingle();

            

            *// Register Unity-specific targets*

            if (\_enableUnityConsole)

            {

                builder.Bind\<ILogTarget\>().To\<UnityConsoleLogTarget\>().AsSingle().WithId("unity");

            }

            

            if (\_enableFileLogging)

            {

                builder.Bind\<ILogTarget\>().To\<FileLogTarget\>().AsSingle().WithId("file");

            }

            

            *// Register Unity-specific health checks*

            builder.Bind\<UnityLoggingHealthCheck\>().AsSingle();

        }

        

        public override void Start()

        {

            *// Validate core services are registered*

            if (\!Container.HasBinding\<ILoggingService\>())

            {

                throw new InvalidOperationException("ILoggingService not registered");

            }

            

            *// Initialize Unity-specific functionality*

            var loggingService \= Container.Resolve\<ILoggingService\>();

            var healthService \= Container.Resolve\<IHealthCheckService\>();

            var unityHealthCheck \= Container.Resolve\<UnityLoggingHealthCheck\>();

            

            healthService.RegisterHealthCheck(unityHealthCheck);

            

            loggingService.LogInfo("Unity logging system initialized");

        }

    }

}

#### **ScriptableObject Configuration Pattern**

csharp

*// Unity-serializable configuration*

namespace AhBearStudios.Unity.Logging.ScriptableObjects

{

    using AhBearStudios.Core.Logging;

    using UnityEngine;

    

    \[CreateAssetMenu(menuName \= "AhBearStudios/Logging/Logging Config", fileName \= "LoggingConfig")\]

    public class LoggingConfigAsset : ScriptableObject

    {

        \[SerializeField\] private LogLevel \_minimumLevel \= LogLevel.Info;

        \[SerializeField\] private bool \_enableBatching \= true;

        \[SerializeField\] private int \_batchSize \= 100;

        \[SerializeField\] private float \_flushInterval \= 1.0f;

        \[SerializeField\] private bool \_enableStructuredLogging \= true;

        

        public LoggingConfig Config \=\> new LoggingConfig

        {

            MinimumLevel \= \_minimumLevel,

            EnableBatching \= \_enableBatching,

            BatchSize \= \_batchSize,

            FlushInterval \= TimeSpan.FromSeconds(\_flushInterval),

            EnableStructuredLogging \= \_enableStructuredLogging

        };

        

        private void OnValidate()

        {

            \_batchSize \= Mathf.Max(1, \_batchSize);

            \_flushInterval \= Mathf.Max(0.1f, \_flushInterval);

        }

    }

}

## **Performance Optimization Benefits**

### **Core Layer Optimizations**

* **Zero Unity overhead in business logic** \- Core operations run at maximum performance without GameObject/MonoBehaviour overhead  
* **Burst-compatible implementations** \- Critical paths can leverage Unity's Burst compiler for native performance  
* **Unity.Collections v2 usage** \- Memory-efficient, allocation-free data structures in core systems  
* **Reduced allocation pressure** \- Core systems avoid managed allocations through careful design  
* **Faster iteration cycles** \- Core logic changes don't require Unity domain reloads  
* **Profile-friendly code** \- Pure C\# implementations are easier to profile and optimize

  ### **Unity Layer Benefits**

* **Seamless Unity integration** \- Unity-specific functionality works naturally with Unity workflows  
* **Editor tooling support** \- Custom inspectors and editor windows enhance developer experience  
* **Scene-based configuration** \- GameObject-based setup and configuration options  
* **Unity lifecycle integration** \- Proper integration with Unity's execution order and lifecycle events  
* **Asset system integration** \- ScriptableObjects and Resources provide Unity-native configuration

  ### **Testing and Validation Benefits**

* **Independent core testing** \- Unit tests run without Unity Test Runner overhead  
* **Faster test execution** \- Core logic tests execute in milliseconds rather than seconds  
* **Better CI/CD integration** \- Tests can run in headless environments without Unity installation  
* **Mock-friendly design** \- Pure interfaces and POCO implementations are easy to mock  
* **Integration test clarity** \- Clear separation between core logic tests and Unity integration tests

  ## **Migration Guidelines for Existing Systems**

  ### **Identifying Core vs Unity Code**

  #### **Extract to Core When:**

* Code contains pure business logic or algorithms  
* No Unity API calls (Debug, GameObject, MonoBehaviour, etc.)  
* Code would benefit from faster unit testing  
* Logic is performance-critical and could benefit from Burst compilation  
* Code represents data processing or computational functions

  #### **Keep in Unity When:**

* Code directly uses Unity APIs  
* Implementation requires MonoBehaviour lifecycle events  
* Code manages GameObjects or Components  
* Implementation requires Unity Editor integration  
* Code handles Unity-specific serialization or asset management

  ### **Refactoring Process**

1. **Identify extraction candidates** \- Find classes with minimal Unity dependencies  
2. **Create core interfaces** \- Define pure interfaces in Core namespace  
3. **Implement POCO versions** \- Create Unity-agnostic implementations  
4. **Build Unity adapters** \- Create Unity-specific wrappers around core implementations  
5. **Update Reflex registration** \- Register both core services and Unity adapters  
6. **Update tests** \- Create fast unit tests for core logic, keep integration tests for Unity functionality  
7. **Validate performance** \- Ensure core implementations meet performance requirements

   ### **Validation Checklist**

When organizing code across Core/Unity boundaries, verify:

* Core implementations have zero Unity dependencies  
* Unity implementations properly wrap and adapt core services  
* Reflex registration covers both core services and Unity adapters  
* Health checks exist for both core functionality and Unity integration  
* Unit tests cover core logic without requiring Unity Test Runner  
* Integration tests validate Unity-specific functionality  
* Performance characteristics meet requirements in both layers  
* Functional organization is maintained across both Core and Unity namespaces

  ## **Anti-Patterns to Avoid**

  ### **Core Layer Anti-Patterns**

* **Unity API usage in core** \- Never use Debug.Log, GameObject, or other Unity APIs in core implementations  
* **MonoBehaviour inheritance in core** \- Core services should be pure POCO classes  
* **Unity-specific serialization** \- Use standard .NET serialization patterns in core  
* **Scene dependencies in core** \- Core logic should not depend on Unity scenes or GameObjects  
* **Unity lifecycle coupling** \- Core services should not depend on Start/Update/Destroy patterns

  ### **Unity Layer Anti-Patterns**

* **Business logic in MonoBehaviours** \- Keep complex logic in core services, use Unity layer as thin adapters  
* **Direct core modification** \- Don't modify core implementations to add Unity-specific functionality  
* **Mixed dependencies** \- Don't create services that depend on both core and Unity APIs  
* **Performance-critical Unity code** \- Move performance-sensitive operations to core layer  
* **Untestable Unity integration** \- Ensure Unity adapters remain simple and testable

  ### **Organizational Anti-Patterns**

* **Mixed namespaces** \- Don't put Unity-specific code in Core namespaces or vice versa  
* **Circular dependencies** \- Core should never depend on Unity layer  
* **Inconsistent functional organization** \- Both layers must follow the same functional domain structure  
* **Bypassing separation** \- Don't create direct dependencies between disparate functional systems

---

**Apply these guidelines consistently to every code file and implementation in the project, ensuring all dependency injection is handled through Reflex DI library and all code follows functional organization principles.**

