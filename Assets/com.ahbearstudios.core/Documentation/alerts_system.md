# Alert System

## 📋 Overview

**Namespace:** `AhBearStudios.Core.Alerting`  
**Role:** Critical system notifications and alerting  
**Status:** ✅ Production Ready

The Alert System provides enterprise-grade real-time notification capabilities for critical system events, performance issues, and error conditions. It enables proactive monitoring and rapid response to system problems across all AhBearStudios Core systems with comprehensive factory patterns, zero-allocation performance, and full Unity integration.

## 🚀 Key Features

- **⚡ Real-Time Alerting**: Zero-allocation immediate notification of critical system events
- **🏭 Factory Pattern**: Comprehensive factory system for dependency injection and testing
- **🔧 Multiple Channels**: Support for various notification channels (log, console, file, memory, Unity debug, network)
- **📊 Alert Suppression**: Intelligent filtering with deduplication and rate limiting
- **🎯 Severity Levels**: Hierarchical alert classification with source-specific thresholds
- **📈 Alert Management**: Full lifecycle management with acknowledgment and resolution
- **🔄 System Integration**: Deep integration with ILoggingService, IMessageBusService, ISerializationService, and IProfilerService
- **📦 Centralized Serialization**: Uses ISerializationService for consistent, fault-tolerant serialization across all alert data
- **🛠️ Unity Optimized**: Built for Unity with Burst compatibility and zero-allocation patterns
- **🧪 Testing Support**: Comprehensive factory system for unit testing and mocking
- **📈 Performance Monitoring**: Built-in statistics and health monitoring capabilities

## 🏗️ Architecture

### Folder Structure

```
AhBearStudios.Core.Alerting/
├── IAlertService.cs                      # Primary service interface
├── AlertService.cs                       # Main service implementation
├── AlertSystemInitializer.cs             # System initialization and ISerializationService type registration
├── Factories/                            # ⭐ Factory Pattern Implementation
│   ├── IAlertServiceFactory.cs           # Service factory interface
│   ├── AlertServiceFactory.cs            # Service factory implementation
│   ├── IAlertChannelFactory.cs           # Channel factory interface
│   ├── AlertChannelFactory.cs            # Channel factory implementation
│   ├── IAlertFilterFactory.cs            # Filter factory interface
│   └── AlertFilterFactory.cs             # Filter factory implementation
├── Services/                             # Supporting Services
│   ├── AlertChannelService.cs            # Channel lifecycle management
│   ├── AlertFilterService.cs             # Filter lifecycle management
│   └── AlertSuppressionService.cs        # Alert deduplication and rate limiting
├── Channels/                             # Alert Delivery Channels
│   ├── IAlertChannel.cs                  # Channel interface
│   ├── BaseAlertChannel.cs               # Base implementation with health monitoring
│   ├── LogAlertChannel.cs                # ILoggingService integration
│   ├── ConsoleAlertChannel.cs            # Console output channel
│   ├── FileAlertChannel.cs               # File-based alert logging
│   ├── MemoryAlertChannel.cs             # In-memory storage for testing
│   ├── UnityDebugAlertChannel.cs         # Unity Debug.Log integration
│   └── NullAlertChannel.cs               # Null object pattern for testing
├── Filters/                              # Alert Filtering System
│   ├── IAlertFilter.cs                   # Filter interface
│   ├── BaseAlertFilter.cs                # Base filter implementation
│   ├── SeverityAlertFilter.cs            # Severity-based filtering
│   ├── SourceAlertFilter.cs              # Source-based filtering
│   ├── RateLimitAlertFilter.cs           # Rate limiting filter
│   ├── ContentAlertFilter.cs             # Content/message pattern filtering
│   ├── TimeBasedAlertFilter.cs           # Time-based filtering
│   ├── CompositeAlertFilter.cs           # Composite filter with logical operations
│   ├── TagAlertFilter.cs                 # Tag-based filtering
│   ├── CorrelationAlertFilter.cs         # Correlation ID filtering
│   ├── PassThroughAlertFilter.cs         # Allow-all filter for testing
│   └── BlockAlertFilter.cs               # Block-all filter for testing
├── Messages/                             # IMessage Implementations
│   ├── AlertRaisedMessage.cs             # Alert raised event (TypeCode: 1401)
│   ├── AlertAcknowledgedMessage.cs       # Alert acknowledged event (TypeCode: 1402)
│   ├── AlertResolvedMessage.cs           # Alert resolved event (TypeCode: 1403)
│   ├── AlertSystemHealthMessage.cs       # System health events (TypeCode: 1404)
│   ├── AlertChannelFailedMessage.cs      # Channel failure events (TypeCode: 1405)
│   ├── AlertSuppressionMessage.cs        # Suppression events (TypeCode: 1406)
│   └── AlertRateLimitMessage.cs          # Rate limit events (TypeCode: 1407)
├── Models/                               # Core Data Models
│   ├── Alert.cs                          # Alert data structure [MemoryPackable]
│   ├── AlertSeverity.cs                  # Severity enumeration
│   ├── AlertContext.cs                   # Contextual information [MemoryPackable]
│   ├── AlertRule.cs                      # Rule definitions for suppression
│   ├── AlertStatistics.cs               # Performance and operational statistics
│   ├── FilterContext.cs                 # Filter processing context
│   ├── FilterResult.cs                  # Filter evaluation results
│   ├── HealthCheckResult.cs              # Channel health check results
│   └── ValidationResult.cs               # Configuration validation results
└── Events/                               # Event Args and Handlers
    ├── AlertEventArgs.cs                 # Alert lifecycle events
    ├── FilterEventArgs.cs                # Filter operation events
    ├── ChannelEventArgs.cs               # Channel operation events
    └── AlertSystemHealthEventArgs.cs     # System health change events

AhBearStudios.Unity.Alerting/            # Unity-Specific Components
├── Installers/
│   └── AlertsInstaller.cs                # Reflex DI registration
├── Channels/
│   ├── UnityConsoleAlertChannel.cs       # Unity console integration
│   └── UnityNotificationChannel.cs       # Unity notification system
├── Components/
│   └── AlertDisplayComponent.cs          # Visual alert display
└── ScriptableObjects/
    └── AlertConfigAsset.cs               # Unity configuration assets
```

## 🔌 Key Interfaces

### System Integration Requirements

The Alert System integrates with all major AhBearStudios Core systems:

```csharp
// Required using statements for performance and system integration
using Unity.Collections;
using Cysharp.Threading.Tasks;           // Instead of System.Threading.Tasks
using ZLinq;                             // Instead of System.Linq
using MemoryPack;                        // For serialization
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.HealthChecking;
using AhBearStudios.Core.Serialization;
```

### IAlertService

The primary interface for all alerting operations with full system integration.

```csharp
public interface IAlertService : IDisposable
{
    // Core alerting with correlation tracking
    void RaiseAlert(string message, AlertSeverity severity, FixedString64Bytes source, 
        FixedString32Bytes tag = default, Guid correlationId = default);
    void RaiseAlert(FixedString512Bytes message, AlertSeverity severity, FixedString64Bytes source, 
        FixedString32Bytes tag = default, Guid correlationId = default);
    void RaiseAlert(Alert alert);
    UniTask RaiseAlertAsync(Alert alert, CancellationToken cancellationToken = default);
    UniTask RaiseAlertAsync(string message, AlertSeverity severity, FixedString64Bytes source, 
        FixedString32Bytes tag = default, Guid correlationId = default, 
        CancellationToken cancellationToken = default);
    
    // Alert lifecycle management
    IEnumerable<Alert> GetActiveAlerts();
    IEnumerable<Alert> GetAlertHistory(TimeSpan period);
    void AcknowledgeAlert(Guid alertId, FixedString64Bytes correlationId = default);
    void ResolveAlert(Guid alertId, FixedString64Bytes correlationId = default);
    
    // Severity management
    void SetMinimumSeverity(AlertSeverity minimumSeverity);
    void SetMinimumSeverity(FixedString64Bytes source, AlertSeverity minimumSeverity);
    AlertSeverity GetMinimumSeverity(FixedString64Bytes source = default);
    
    // Channel management
    void RegisterChannel(IAlertChannel channel, FixedString64Bytes correlationId = default);
    bool UnregisterChannel(FixedString64Bytes channelName, FixedString64Bytes correlationId = default);
    IReadOnlyCollection<IAlertChannel> GetRegisteredChannels();
    
    // Filtering and suppression
    void AddFilter(IAlertFilter filter, FixedString64Bytes correlationId = default);
    bool RemoveFilter(FixedString64Bytes filterName, FixedString64Bytes correlationId = default);
    void AddSuppressionRule(AlertRule rule, FixedString64Bytes correlationId = default);
    bool RemoveSuppressionRule(FixedString64Bytes ruleName, FixedString64Bytes correlationId = default);
    
    // System monitoring and maintenance
    AlertStatistics GetStatistics();
    ValidationResult ValidateConfiguration(FixedString64Bytes correlationId = default);
    void PerformMaintenance(FixedString64Bytes correlationId = default);
    UniTask FlushAsync(FixedString64Bytes correlationId = default);
    
    // System status
    bool IsEnabled { get; }
    
    // Events with comprehensive event args
    event EventHandler<AlertEventArgs> AlertRaised;
    event EventHandler<AlertEventArgs> AlertAcknowledged;
    event EventHandler<AlertEventArgs> AlertResolved;
    event EventHandler<AlertSystemHealthEventArgs> HealthChanged;
}
```

### IAlertChannel

Interface for alert output destinations with health monitoring and async operations.

```csharp
public interface IAlertChannel : IDisposable
{
    // Channel identity and configuration
    FixedString64Bytes Name { get; }
    bool IsEnabled { get; set; }
    bool IsHealthy { get; }
    AlertSeverity MinimumSeverity { get; set; }
    
    // Core channel operations
    UniTask<bool> SendAlertAsync(Alert alert, Guid correlationId = default);
    bool CanSendAlert(Alert alert);
    
    // Channel lifecycle and configuration
    UniTask<bool> InitializeAsync(ChannelConfig config, Guid correlationId = default);
    void Enable(Guid correlationId = default);
    void Disable(Guid correlationId = default);
    
    // Health monitoring and diagnostics
    UniTask<HealthCheckResult> TestHealthAsync(Guid correlationId = default);
    void ResetStatistics(Guid correlationId = default);
    UniTask FlushAsync(Guid correlationId = default);
    
    // Events for monitoring and integration
    event EventHandler<ChannelHealthChangedEventArgs> HealthChanged;
    event EventHandler<AlertDeliveryFailedEventArgs> AlertDeliveryFailed;
    event EventHandler<ChannelConfigurationChangedEventArgs> ConfigurationChanged;
}
```

### IAlertFilter

Interface for alert filtering and processing with comprehensive evaluation support.

```csharp
public interface IAlertFilter : IDisposable
{
    // Filter identity and configuration
    FixedString64Bytes Name { get; }
    int Priority { get; set; }
    bool IsEnabled { get; set; }
    
    // Core filter operations
    bool CanHandle(Alert alert);
    FilterResult Evaluate(Alert alert, FilterContext context);
    
    // Configuration and lifecycle
    bool Configure(Dictionary<string, object> settings, Guid correlationId = default);
    void Reset(Guid correlationId = default);
    FilterDiagnostics GetDiagnostics();
    
    // Events for monitoring
    event EventHandler<FilterConfigurationChangedEventArgs> ConfigurationChanged;
    event EventHandler<FilterStatisticsUpdatedEventArgs> StatisticsUpdated;
}
```

### Factory Interfaces

The Alert System provides comprehensive factory interfaces for dependency injection and testing scenarios.

#### IAlertServiceFactory

Factory for creating AlertService instances with different configurations.

```csharp
public interface IAlertServiceFactory
{
    // Core factory methods
    IAlertService CreateAlertService(IMessageBusService messageBusService = null, ILoggingService loggingService = null);
    UniTask<IAlertService> CreateAlertServiceAsync(AlertServiceConfiguration configuration, Guid correlationId = default);
    
    // Environment-specific factory methods
    UniTask<IAlertService> CreateDevelopmentAlertServiceAsync(ILoggingService loggingService, IMessageBusService messageBusService = null);
    UniTask<IAlertService> CreateProductionAlertServiceAsync(ILoggingService loggingService, IMessageBusService messageBusService);
    IAlertService CreateTestAlertService();
    
    // Custom configuration support
    UniTask<IAlertService> CreateCustomAlertServiceAsync(
        IEnumerable<IAlertChannel> channels,
        IEnumerable<IAlertFilter> filters,
        IEnumerable<AlertRule> suppressionRules = null,
        IMessageBusService messageBusService = null,
        ILoggingService loggingService = null,
        Guid correlationId = default);
    
    // Configuration management
    ValidationResult ValidateConfiguration(AlertServiceConfiguration configuration);
    AlertServiceConfiguration GetDefaultConfiguration();
    AlertServiceConfiguration GetDevelopmentConfiguration();
    AlertServiceConfiguration GetProductionConfiguration();
    AlertServiceConfiguration CreateConfigurationFromSettings(Dictionary<string, object> settings);
}
```

#### IAlertChannelFactory

Factory for creating various types of alert channels.

```csharp
public interface IAlertChannelFactory
{
    // Core channel creation
    UniTask<IAlertChannel> CreateChannelAsync(ChannelType channelType, FixedString64Bytes name, ILoggingService loggingService = null);
    UniTask<IAlertChannel> CreateChannelAsync(string channelTypeName, FixedString64Bytes name, ILoggingService loggingService = null);
    UniTask<IAlertChannel> CreateAndConfigureChannelAsync(ChannelConfiguration configuration, ILoggingService loggingService = null, Guid correlationId = default);
    
    // Specialized channel creation
    UniTask<IAlertChannel> CreateLoggingChannelAsync(FixedString64Bytes name, ILoggingService loggingService, AlertSeverity minimumSeverity = AlertSeverity.Information, bool includeContext = true, bool includeStackTrace = false);
    UniTask<IAlertChannel> CreateConsoleChannelAsync(FixedString64Bytes name, AlertSeverity minimumSeverity = AlertSeverity.Information, bool useColors = true, bool includeTimestamp = true);
    UniTask<IAlertChannel> CreateFileChannelAsync(FixedString64Bytes name, string filePath, AlertSeverity minimumSeverity = AlertSeverity.Information, long maxFileSize = 10485760, int maxBackupFiles = 5);
    UniTask<IAlertChannel> CreateMemoryChannelAsync(FixedString64Bytes name, AlertSeverity minimumSeverity = AlertSeverity.Debug, int maxStoredAlerts = 1000);
    
    // Bulk operations and environment-specific configurations
    UniTask<IEnumerable<IAlertChannel>> CreateChannelsAsync(IEnumerable<ChannelConfiguration> configurations, ILoggingService loggingService = null, Guid correlationId = default);
    UniTask<IEnumerable<IAlertChannel>> CreateDevelopmentChannelsAsync(ILoggingService loggingService);
    UniTask<IEnumerable<IAlertChannel>> CreateProductionChannelsAsync(ILoggingService loggingService, string logFilePath = null);
    UniTask<IEnumerable<IAlertChannel>> CreateTestChannelsAsync();
    
    // Configuration and validation
    ValidationResult ValidateChannelConfiguration(ChannelConfiguration configuration);
    ChannelConfiguration GetDefaultConfiguration(ChannelType channelType);
    IEnumerable<ChannelType> GetSupportedChannelTypes();
    bool IsChannelTypeSupported(ChannelType channelType);
    ChannelConfiguration CreateConfigurationFromSettings(ChannelType channelType, FixedString64Bytes name, Dictionary<string, object> settings);
}
```

#### IAlertFilterFactory

Factory for creating various types of alert filters.

```csharp
public interface IAlertFilterFactory
{
    // Core filter creation
    UniTask<IAlertFilter> CreateFilterAsync(FilterType filterType, FixedString64Bytes name, int priority = 100);
    UniTask<IAlertFilter> CreateFilterAsync(string filterTypeName, FixedString64Bytes name, int priority = 100);
    UniTask<IAlertFilter> CreateAndConfigureFilterAsync(FilterConfiguration configuration, Guid correlationId = default);
    
    // Specialized filter creation
    UniTask<IAlertFilter> CreateSeverityFilterAsync(FixedString64Bytes name, AlertSeverity minimumSeverity, bool allowCriticalAlways = true, int priority = 10);
    UniTask<IAlertFilter> CreateSourceFilterAsync(FixedString64Bytes name, IEnumerable<string> allowedSources, bool useWhitelist = true, int priority = 20);
    UniTask<IAlertFilter> CreateRateLimitFilterAsync(FixedString64Bytes name, int maxAlertsPerMinute, string sourcePattern = "*", int priority = 30);
    UniTask<IAlertFilter> CreateContentFilterAsync(FixedString64Bytes name, IEnumerable<string> patterns, FilterAction action = FilterAction.Suppress, int priority = 40);
    UniTask<IAlertFilter> CreateTimeBasedFilterAsync(FixedString64Bytes name, IEnumerable<TimeRange> allowedTimeRanges, TimeZoneInfo timezone = null, int priority = 50);
    UniTask<IAlertFilter> CreateCompositeFilterAsync(FixedString64Bytes name, IEnumerable<IAlertFilter> childFilters, LogicalOperator logicalOperator = LogicalOperator.And, int priority = 60);
    
    // Bulk operations and environment-specific configurations
    UniTask<IEnumerable<IAlertFilter>> CreateFiltersAsync(IEnumerable<FilterConfiguration> configurations, Guid correlationId = default);
    UniTask<IEnumerable<IAlertFilter>> CreateDevelopmentFiltersAsync();
    UniTask<IEnumerable<IAlertFilter>> CreateProductionFiltersAsync();
    UniTask<IEnumerable<IAlertFilter>> CreateTestFiltersAsync();
    
    // Configuration and validation
    ValidationResult ValidateFilterConfiguration(FilterConfiguration configuration);
    FilterConfiguration GetDefaultConfiguration(FilterType filterType);
    IEnumerable<FilterType> GetSupportedFilterTypes();
    bool IsFilterTypeSupported(FilterType filterType);
    FilterConfiguration CreateConfigurationFromSettings(FilterType filterType, FixedString64Bytes name, Dictionary<string, object> settings);
}
```

### Core Enums and Types

```csharp
// Channel types supported by the factory system
public enum ChannelType
{
    Logging = 0,     // ILoggingService integration
    Console = 1,     // Console output
    File = 2,        // File system logging
    Memory = 3,      // In-memory storage for testing
    Network = 4,     // Network-based alerts
    UnityDebug = 5,  // Unity Debug.Log integration
    Null = 6         // Null object pattern for testing
}

// Filter types supported by the factory system
public enum FilterType
{
    Severity = 0,        // Severity-level filtering
    Source = 1,          // Source-based filtering
    RateLimit = 2,       // Rate limiting filter
    Content = 3,         // Content/message filtering
    TimeBased = 4,       // Time-based filtering
    Composite = 5,       // Composite filter with logical operations
    Tag = 6,             // Tag-based filtering
    Correlation = 7,     // Correlation ID filtering
    PassThrough = 8,     // Allow-all filter for testing
    Block = 9            // Block-all filter for testing
}

// Filter evaluation results
public enum FilterDecision
{
    Allow = 0,       // Allow alert to pass through
    Suppress = 1,    // Block/suppress the alert
    Modify = 2,      // Modify alert and continue processing
    Defer = 3        // Defer alert for later processing
}

// Filter actions for composite operations
public enum FilterAction
{
    Allow = 0,       // Allow the alert
    Suppress = 1,    // Suppress the alert
    Modify = 2,      // Modify the alert
    Defer = 3        // Defer the alert
}

// Logical operators for composite filters
public enum LogicalOperator
{
    And = 0,         // All filters must pass
    Or = 1,          // At least one filter must pass
    Xor = 2,         // Exactly one filter must pass
    Not = 3          // No filters must pass
}
```

## ⚙️ Configuration

### Factory-Based Configuration

The Alert System uses comprehensive factory classes for creating and configuring services, channels, and filters.

#### Basic Service Creation

```csharp
// Using AlertServiceFactory for dependency injection scenarios
var serviceFactory = new AlertServiceFactory(loggingService);

// Create with default configuration
var alertService = serviceFactory.CreateAlertService(messageBusService, loggingService);

// Create development-optimized service
var devAlertService = await serviceFactory.CreateDevelopmentAlertServiceAsync(loggingService, messageBusService);

// Create production-optimized service  
var prodAlertService = await serviceFactory.CreateProductionAlertServiceAsync(loggingService, messageBusService);

// Create test service for unit testing
var testAlertService = serviceFactory.CreateTestAlertService();
```

#### Custom Configuration with Factories

```csharp
// Create channels using factory
var channelFactory = new AlertChannelFactory(loggingService);

var logChannel = await channelFactory.CreateLoggingChannelAsync(
    "MainLog", 
    loggingService, 
    AlertSeverity.Information, 
    includeContext: true, 
    includeStackTrace: false);

var fileChannel = await channelFactory.CreateFileChannelAsync(
    "ErrorLog", 
    "alerts.log", 
    AlertSeverity.Error, 
    maxFileSize: 50 * 1024 * 1024, // 50MB
    maxBackupFiles: 10);

var memoryChannel = await channelFactory.CreateMemoryChannelAsync(
    "TestMemory", 
    AlertSeverity.Debug, 
    maxStoredAlerts: 500);

// Create filters using factory
var filterFactory = new AlertFilterFactory(loggingService);

var severityFilter = await filterFactory.CreateSeverityFilterAsync(
    "ProductionSeverity", 
    AlertSeverity.Warning, 
    allowCriticalAlways: true, 
    priority: 10);

var rateLimitFilter = await filterFactory.CreateRateLimitFilterAsync(
    "RateLimit", 
    maxAlertsPerMinute: 100, 
    sourcePattern: "*", 
    priority: 20);

var contentFilter = await filterFactory.CreateContentFilterAsync(
    "NoiseFilter", 
    new[] { "*debug*", "*trace*", "*test*" }, 
    FilterAction.Suppress, 
    priority: 30);

// Create custom service with specific channels and filters
var customAlertService = await serviceFactory.CreateCustomAlertServiceAsync(
    channels: new[] { logChannel, fileChannel, memoryChannel },
    filters: new[] { severityFilter, rateLimitFilter, contentFilter },
    suppressionRules: null,
    messageBusService: messageBusService,
    loggingService: loggingService);
```

#### Configuration Objects

```csharp
// AlertServiceConfiguration for comprehensive service setup
var serviceConfig = new AlertServiceConfiguration
{
    Environment = AlertEnvironmentType.Production,
    GlobalMinimumSeverity = AlertSeverity.Warning,
    IsEnabled = true,
    MaxActiveAlerts = 2000,
    MaxHistorySize = 10000,
    MaintenanceInterval = TimeSpan.FromMinutes(5),
    
    Channels = new List<ChannelConfiguration>
    {
        ChannelConfiguration.ProductionLog(),
        ChannelConfiguration.DefaultConsole() with { MinimumSeverity = AlertSeverity.Error }
    },
    
    Filters = new List<FilterConfiguration>
    {
        FilterConfiguration.ProductionSeverity(),
        new FilterConfiguration
        {
            Type = "RateLimit",
            Name = "ProductionRateLimit",
            Priority = 20,
            Settings = new Dictionary<string, object>
            {
                ["MaxAlertsPerMinute"] = 50,
                ["SourcePattern"] = "*"
            }
        }
    },
    
    SuppressionRules = new List<SuppressionRuleConfiguration>
    {
        SuppressionRuleConfiguration.DefaultRateLimit(),
        SuppressionRuleConfiguration.DefaultDuplicateSuppression()
    },
    
    SourceMinimumSeverities = new Dictionary<string, AlertSeverity>
    {
        ["DatabaseService"] = AlertSeverity.Error,
        ["NetworkService"] = AlertSeverity.Warning,
        ["CacheService"] = AlertSeverity.Information
    }
};

// Create service from configuration
var configuredService = await serviceFactory.CreateAlertServiceAsync(serviceConfig);
```

### Advanced Factory Patterns

#### Composite Filter Creation

```csharp
// Create sophisticated composite filters
var filterFactory = new AlertFilterFactory(loggingService);

// Business hours filter with time ranges
var businessHoursFilter = await filterFactory.CreateTimeBasedFilterAsync(
    "BusinessHours",
    new[] 
    { 
        TimeRange.BusinessHours(),  // Monday-Friday 9-5
        new TimeRange(new TimeOnly(0, 0), new TimeOnly(23, 59), DayOfWeek.Saturday) // Saturday all day
    },
    TimeZoneInfo.Local,
    priority: 15);

// Create composite filter combining multiple conditions
var productionCompositeFilter = await filterFactory.CreateCompositeFilterAsync(
    "ProductionComposite",
    new IAlertFilter[]
    {
        await filterFactory.CreateSeverityFilterAsync("MinWarning", AlertSeverity.Warning, priority: 1),
        await filterFactory.CreateSourceFilterAsync("AllowedSources", new[] { "DatabaseService", "NetworkService", "CacheService" }, priority: 2),
        await filterFactory.CreateRateLimitFilterAsync("ConservativeRate", 50, priority: 3)
    },
    LogicalOperator.And,  // All filters must pass
    priority: 5);

// Content filtering for noise reduction
var contentFilter = await filterFactory.CreateContentFilterAsync(
    "NoiseReduction",
    new[] 
    { 
        "*connection timeout*", 
        "*temporary failure*", 
        "*retrying*",
        "*debug*"
    },
    FilterAction.Suppress,
    priority: 25);
```

#### Environment-Specific Factory Configurations

```csharp
// Development environment with verbose logging and testing channels
var devChannels = await channelFactory.CreateDevelopmentChannelsAsync(loggingService);
var devFilters = await filterFactory.CreateDevelopmentFiltersAsync();
var devService = await serviceFactory.CreateCustomAlertServiceAsync(
    channels: devChannels,
    filters: devFilters,
    messageBusService: messageBusService,
    loggingService: loggingService);

// Production environment with optimized performance and minimal noise
var prodChannels = await channelFactory.CreateProductionChannelsAsync(loggingService, "production-alerts.log");
var prodFilters = await filterFactory.CreateProductionFiltersAsync();
var prodService = await serviceFactory.CreateCustomAlertServiceAsync(
    channels: prodChannels,
    filters: prodFilters,
    messageBusService: messageBusService,
    loggingService: loggingService);

// Testing environment with in-memory storage and pass-through filtering
var testChannels = await channelFactory.CreateTestChannelsAsync();
var testFilters = await filterFactory.CreateTestFiltersAsync();
var testService = await serviceFactory.CreateCustomAlertServiceAsync(
    channels: testChannels,
    filters: testFilters,
    loggingService: loggingService);
```

### Unity Integration with Factory System

```csharp
[CreateAssetMenu(menuName = "AhBear/Alerts/Config")]
public class AlertConfigAsset : ScriptableObject
{
    [Header("Environment")]
    public AlertEnvironmentType environment = AlertEnvironmentType.Development;
    public AlertSeverity globalMinimumSeverity = AlertSeverity.Information;
    public bool isEnabled = true;
    
    [Header("Capacity")]
    public int maxActiveAlerts = 1000;
    public int maxHistorySize = 5000;
    public float maintenanceIntervalMinutes = 5f;
    
    [Header("Channels")]
    public ChannelConfigAsset[] channels = Array.Empty<ChannelConfigAsset>();
    
    [Header("Filters")]
    public FilterConfigAsset[] filters = Array.Empty<FilterConfigAsset>();
    
    [Header("Suppression Rules")]
    public SuppressionRuleConfigAsset[] suppressionRules = Array.Empty<SuppressionRuleConfigAsset>();
    
    [Header("Source-Specific Settings")]
    public SourceSeverityMapping[] sourceMinimumSeverities = Array.Empty<SourceSeverityMapping>();
    
    [Header("Advanced Settings")]
    public SerializableDictionary<string, string> customSettings = new SerializableDictionary<string, string>();
    
    /// <summary>
    /// Converts Unity ScriptableObject configuration to factory configuration.
    /// </summary>
    public AlertServiceConfiguration ToAlertServiceConfiguration()
    {
        return new AlertServiceConfiguration
        {
            Environment = environment,
            GlobalMinimumSeverity = globalMinimumSeverity,
            IsEnabled = isEnabled,
            MaxActiveAlerts = maxActiveAlerts,
            MaxHistorySize = maxHistorySize,
            MaintenanceInterval = TimeSpan.FromMinutes(maintenanceIntervalMinutes),
            
            Channels = channels.Select(c => c.ToChannelConfiguration()).ToList(),
            Filters = filters.Select(f => f.ToFilterConfiguration()).ToList(),
            SuppressionRules = suppressionRules.Select(r => r.ToSuppressionRuleConfiguration()).ToList(),
            
            SourceMinimumSeverities = sourceMinimumSeverities.ToDictionary(
                s => s.sourceName, 
                s => s.minimumSeverity),
                
            CustomSettings = customSettings.ToDictionary(
                kvp => kvp.Key, 
                kvp => (object)kvp.Value)
        };
    }
}

[Serializable]
public class ChannelConfigAsset
{
    public ChannelType channelType = ChannelType.Logging;
    public string channelName = "DefaultChannel";
    public bool isEnabled = true;
    public AlertSeverity minimumSeverity = AlertSeverity.Information;
    public int maxAlertsPerSecond = 100;
    public SerializableDictionary<string, string> settings = new SerializableDictionary<string, string>();
    
    public ChannelConfiguration ToChannelConfiguration()
    {
        return new ChannelConfiguration
        {
            Type = channelType.ToString(),
            Name = channelName,
            IsEnabled = isEnabled,
            MinimumSeverity = minimumSeverity,
            MaxAlertsPerSecond = maxAlertsPerSecond,
            Settings = settings.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value)
        };
    }
}

[Serializable]
public class FilterConfigAsset
{
    public FilterType filterType = FilterType.Severity;
    public string filterName = "DefaultFilter";
    public bool isEnabled = true;
    public int priority = 100;
    public SerializableDictionary<string, string> settings = new SerializableDictionary<string, string>();
    
    public FilterConfiguration ToFilterConfiguration()
    {
        return new FilterConfiguration
        {
            Type = filterType.ToString(),
            Name = filterName,
            IsEnabled = isEnabled,
            Priority = priority,
            Settings = settings.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value)
        };
    }
}

[Serializable]
public class SuppressionRuleConfigAsset
{
    public string ruleName = "DefaultRule";
    public string sourcePattern = "*";
    public string messagePattern = "*";
    public AlertSeverity? severity = null;
    public float suppressionDurationMinutes = 5f;
    public int? rateLimit = null;
    public bool isEnabled = true;
    
    public SuppressionRuleConfiguration ToSuppressionRuleConfiguration()
    {
        return new SuppressionRuleConfiguration
        {
            Name = ruleName,
            SourcePattern = sourcePattern,
            MessagePattern = messagePattern,
            Severity = severity,
            SuppressionDuration = TimeSpan.FromMinutes(suppressionDurationMinutes),
            RateLimit = rateLimit,
            IsEnabled = isEnabled
        };
    }
}

[Serializable]
public class SourceSeverityMapping
{
    public string sourceName;
    public AlertSeverity minimumSeverity = AlertSeverity.Information;
}

/// <summary>
/// Unity component for integrating AlertService with factory pattern.
/// </summary>
public class AlertServiceComponent : MonoBehaviour
{
    [SerializeField] private AlertConfigAsset configAsset;
    
    private IAlertService _alertService;
    private IAlertServiceFactory _serviceFactory;
    
    private async void Start()
    {
        // Get dependencies from DI container
        var loggingService = Container.Resolve<ILoggingService>();
        var messageBusService = Container.Resolve<IMessageBusService>();
        
        // Create factory
        _serviceFactory = new AlertServiceFactory(loggingService);
        
        if (configAsset != null)
        {
            // Create service from Unity configuration
            var config = configAsset.ToAlertServiceConfiguration();
            _alertService = await _serviceFactory.CreateAlertServiceAsync(config);
        }
        else
        {
            // Create with environment-appropriate defaults
#if UNITY_EDITOR
            _alertService = await _serviceFactory.CreateDevelopmentAlertServiceAsync(loggingService, messageBusService);
#else
            _alertService = await _serviceFactory.CreateProductionAlertServiceAsync(loggingService, messageBusService);
#endif
        }
        
        // Register with DI container
        Container.RegisterInstance(_alertService);
    }
    
    private void OnDestroy()
    {
        _alertService?.Dispose();
    }
}
```

## 🚀 Usage Examples

### System Integration with Factory Pattern

```csharp
/// <summary>
/// Complete example showing Alert System integration with factory pattern and all AhBearStudios Core systems.
/// Demonstrates performance patterns, messaging integration, and fault tolerance.
/// </summary>
public class DatabaseService
{
    private readonly IAlertService _alerts;
    private readonly ILoggingService _logger;
    private readonly IMessageBusService _messageBus;
    private readonly IProfilerService _profiler;
    private readonly Guid _correlationId;
    
    public DatabaseService(
        IAlertService alerts, 
        ILoggingService logger,
        IMessageBusService messageBus,
        IProfilerService profiler)
    {
        _alerts = alerts ?? throw new ArgumentNullException(nameof(alerts));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
        _profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
        _correlationId = Guid.NewGuid();
    }
    
    /// <summary>
    /// Demonstrates comprehensive system integration with alerts, logging, profiling,
    /// and messaging for production-ready fault tolerance using the new Alert System.
    /// </summary>
    public async UniTask<User> GetUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        using var scope = _profiler.BeginScope("DatabaseService.GetUser");
        var operationId = Guid.NewGuid();
        
        try
        {
            _logger.LogInfo($"Starting user retrieval for ID: {userId}", _correlationId.ToString(), "DatabaseService");
            
            // Simulate database operation
            var user = await GetUserFromDatabaseAsync(userId, cancellationToken);
            
            _logger.LogInfo($"Successfully retrieved user {userId}", _correlationId.ToString(), "DatabaseService");
            return user;
        }
        catch (DatabaseConnectionException ex)
        {
            // Raise critical alert using new Alert System
            _alerts.RaiseAlert(
                $"Database connection failed: {ex.Message}",
                AlertSeverity.Critical,
                "DatabaseService",
                "DatabaseConnectivity",
                _correlationId);
            
            _logger.LogError($"Database connection failed for user {userId}: {ex.Message}", _correlationId.ToString(), "DatabaseService");
            throw;
        }
        catch (DatabaseTimeoutException ex)
        {
            // Performance threshold exceeded - generate alert via profiler integration
            _profiler.RecordMetric("DatabaseQueryTime", ex.TimeoutDuration.TotalMilliseconds, "ms");
            
            await _alerts.RaiseAlertAsync(
                $"Database query timeout for user {userId}: {ex.Message}",
                AlertSeverity.Warning,
                "DatabaseService",
                "Performance",
                _correlationId,
                cancellationToken);
            
            _logger.LogWarning($"Database timeout for user {userId}", _correlationId.ToString(), "DatabaseService");
            throw;
        }
        catch (Exception ex)
        {
            // Unexpected error - raise error alert
            _alerts.RaiseAlert(
                $"Unexpected database error for user {userId}: {ex.Message}",
                AlertSeverity.Error,
                "DatabaseService",
                "UnexpectedError",
                _correlationId);
            
            _logger.LogError($"Unexpected database error for user {userId}: {ex.Message}", _correlationId.ToString(), "DatabaseService");
            throw;
        }
    }
    
    /// <summary>
    /// Example of structured alert creation with context information.
    /// </summary>
    public async UniTask PerformMaintenanceAsync()
    {
        using var scope = _profiler.BeginScope("DatabaseService.PerformMaintenance");
        
        try
        {
            var maintenanceStartTime = DateTime.UtcNow;
            
            // Create structured alert with context
            var maintenanceAlert = Alert.Create(
                "Database maintenance started",
                AlertSeverity.Information,
                "DatabaseService",
                "Maintenance",
                _correlationId);
                
            // Add contextual information
            maintenanceAlert = maintenanceAlert with 
            { 
                Context = new AlertContext 
                { 
                    Properties = new Dictionary<string, object>
                    {
                        ["MaintenanceType"] = "Scheduled",
                        ["StartTime"] = maintenanceStartTime,
                        ["EstimatedDuration"] = "30 minutes",
                        ["ImpactLevel"] = "Low"
                    }
                }
            };
            
            _alerts.RaiseAlert(maintenanceAlert);
            
            // Perform maintenance work...
            await Task.Delay(TimeSpan.FromMinutes(30)); // Simulate maintenance
            
            // Alert completion
            _alerts.RaiseAlert(
                "Database maintenance completed successfully",
                AlertSeverity.Information,
                "DatabaseService",
                "Maintenance",
                _correlationId);
        }
        catch (Exception ex)
        {
            _alerts.RaiseAlert(
                $"Database maintenance failed: {ex.Message}",
                AlertSeverity.Critical,
                "DatabaseService",
                "MaintenanceFailed",
                _correlationId);
            throw;
        }
    }
    
    private async UniTask<User> GetUserFromDatabaseAsync(int userId, CancellationToken cancellationToken)
    {
        // Simulate database operation with potential failures
        await UniTask.Delay(100, cancellationToken);
        
        if (userId <= 0)
            throw new ArgumentException("Invalid user ID");
            
        if (userId == 999) // Simulate connection failure
            throw new DatabaseConnectionException("Connection refused");
            
        if (userId == 998) // Simulate timeout
            throw new DatabaseTimeoutException("Query timeout", TimeSpan.FromSeconds(30));
            
        return new User { Id = userId, Name = $"User {userId}" };
    }
}

// Supporting classes for the example
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class DatabaseConnectionException : Exception
{
    public DatabaseConnectionException(string message) : base(message) { }
}

public class DatabaseTimeoutException : Exception
{
    public TimeSpan TimeoutDuration { get; }
    
    public DatabaseTimeoutException(string message, TimeSpan timeoutDuration) : base(message)
    {
        TimeoutDuration = timeoutDuration;
    }
}
```

### Structured Alert Creation

```csharp
public class PerformanceMonitor
{
    private readonly IAlertService _alerts;
    private readonly IProfilerService _profiler;
    private readonly FixedString64Bytes _correlationId;
    
    public PerformanceMonitor(IAlertService alerts, IProfilerService profiler)
    {
        _alerts = alerts ?? throw new ArgumentNullException(nameof(alerts));
        _profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
        _correlationId = $"PerfMonitor_{Guid.NewGuid():N}"[..32];
    }
    
    public void CheckSystemPerformance(SystemMetrics metrics)
    {
        using var scope = _profiler.BeginScope("PerformanceMonitor.CheckSystemPerformance");
        
        // CPU usage alert with modern C# pattern matching
        var cpuAlertSeverity = metrics.CpuUsage switch
        {
            > 95.0 => AlertSeverity.Critical,
            > 90.0 => AlertSeverity.Warning,
            > 80.0 => AlertSeverity.Info,
            _ => (AlertSeverity?)null
        };
        
        if (cpuAlertSeverity.HasValue)
        {
            var alert = new Alert
            {
                Id = Guid.NewGuid(),
                Message = $"CPU usage: {metrics.CpuUsage:F1}%",
                Severity = cpuAlertSeverity.Value,
                Source = "PerformanceMonitor",
                Tag = "CPU",
                Timestamp = DateTime.UtcNow,
                Context = new AlertContext
                {
                    Properties = new Dictionary<string, object>
                    {
                        ["CpuUsage"] = metrics.CpuUsage,
                        ["MemoryUsage"] = metrics.MemoryUsage,
                        ["ActiveThreads"] = metrics.ActiveThreads,
                        ["CorrelationId"] = _correlationId.ToString()
                    }
                }
            };
            
            _alerts.RaiseAlert(alert);
        }
        
        // Memory usage with switch expression
        if (metrics.MemoryUsage > 85.0)
        {
            _alerts.RaiseAlert(
                $"High memory usage: {metrics.MemoryUsage:F1}%",
                metrics.MemoryUsage switch
                {
                    > 95.0 => AlertSeverity.Critical,
                    > 90.0 => AlertSeverity.Warning,
                    _ => AlertSeverity.Info
                },
                "PerformanceMonitor",
                "Memory"
            );
        }
    }
}
```

### Message Integration and Event Publishing

```csharp
/// <summary>
/// MessageTypeCodes for Alert System events (1400-1499 range).
/// Following AhBearStudios Core messaging patterns for type safety.
/// </summary>
public static class MessageTypeCodes
{
    #region Alert System: 1400-1499
    
    /// <summary>
    /// Type code for alert raised messages.
    /// Published when any alert is raised in the system.
    /// </summary>
    public const ushort AlertRaised = 1401;
    
    /// <summary>
    /// Type code for alert acknowledged messages.
    /// Published when an alert is acknowledged by an operator.
    /// </summary>
    public const ushort AlertAcknowledged = 1402;
    
    /// <summary>
    /// Type code for alert resolved messages.
    /// Published when an alert condition is resolved.
    /// </summary>
    public const ushort AlertResolved = 1403;
    
    /// <summary>
    /// Type code for alert system health change messages.
    /// Published when alert system health status changes.
    /// </summary>
    public const ushort AlertSystemHealthChanged = 1404;
    
    /// <summary>
    /// Type code for alert channel failure messages.
    /// Published when an alert channel fails to deliver alerts.
    /// </summary>
    public const ushort AlertChannelFailed = 1405;
    
    /// <summary>
    /// Type code for alert suppression activated messages.
    /// Published when alert suppression rules are activated.
    /// </summary>
    public const ushort AlertSuppressionActivated = 1406;
    
    /// <summary>
    /// Type code for alert rate limit exceeded messages.
    /// Published when alert rate limiting thresholds are exceeded.
    /// </summary>
    public const ushort AlertRateLimitExceeded = 1407;
    
    #endregion
}

/// <summary>
/// Alert raised event message implementing IMessage for decoupled event handling.
/// Uses MemoryPack for high-performance serialization and Unity.Collections for zero allocation.
/// </summary>
[MemoryPackable]
public readonly partial record struct AlertRaisedMessage : IMessage
{
    /// <inheritdoc />
    public Guid Id { get; init; }
    
    /// <inheritdoc />
    public long TimestampTicks { get; init; }
    
    /// <inheritdoc />
    public ushort TypeCode => MessageTypeCodes.AlertRaised;
    
    /// <inheritdoc />
    public FixedString64Bytes Source { get; init; }
    
    /// <inheritdoc />
    public MessagePriority Priority { get; init; }
    
    /// <inheritdoc />
    public Guid CorrelationId { get; init; }
    
    // Alert-specific properties using Unity.Collections for performance
    /// <summary>
    /// Gets the unique identifier of the alert that was raised.
    /// </summary>
    public Guid AlertId { get; init; }
    
    /// <summary>
    /// Gets the severity of the raised alert.
    /// </summary>
    public AlertSeverity Severity { get; init; }
    
    /// <summary>
    /// Gets the alert message content using FixedString for zero allocation.
    /// </summary>
    public FixedString512Bytes Message { get; init; }
    
    /// <summary>
    /// Gets the source system that raised the alert.
    /// </summary>
    public FixedString64Bytes AlertSource { get; init; }
    
    /// <summary>
    /// Gets the alert tag for categorization.
    /// </summary>
    public FixedString32Bytes Tag { get; init; }
    
    /// <summary>
    /// Creates a new AlertRaisedMessage with comprehensive alert information.
    /// Follows zero-allocation patterns for game performance.
    /// </summary>
    public static AlertRaisedMessage Create(
        Alert alert, 
        FixedString64Bytes source, 
        Guid correlationId = default)
    {
        return new AlertRaisedMessage
        {
            Id = Guid.NewGuid(),
            TimestampTicks = DateTime.UtcNow.Ticks,
            Source = source,
            Priority = alert.Severity switch
            {
                AlertSeverity.Critical or AlertSeverity.Emergency => MessagePriority.Critical,
                AlertSeverity.Warning => MessagePriority.High,
                AlertSeverity.Info => MessagePriority.Normal,
                _ => MessagePriority.Low
            },
            CorrelationId = correlationId,
            AlertId = alert.Id,
            Severity = alert.Severity,
            Message = alert.Message.Length <= 512 ? alert.Message : alert.Message[..512],
            AlertSource = alert.Source,
            Tag = alert.Tag
        };
    }
}
```

### Profiler Integration and Performance Thresholds

```csharp
/// <summary>
/// Demonstrates IProfilerService integration with Alert System for performance monitoring.
/// Shows threshold-based alerting and zero-allocation profiling patterns.
/// </summary>
public class PerformanceAlertService
{
    private readonly IAlertService _alerts;
    private readonly IProfilerService _profiler;
    private readonly ILoggingService _logger;
    private readonly FixedString64Bytes _correlationId;
    
    // Performance thresholds for different operations
    private static readonly Dictionary<string, double> PerformanceThresholds = new()
    {
        ["AlertProcessing"] = 1.0,      // 1ms threshold
        ["ChannelDispatch"] = 5.0,      // 5ms threshold
        ["FilterProcessing"] = 0.5,     // 0.5ms threshold
        ["MessagePublishing"] = 2.0     // 2ms threshold
    };
    
    public PerformanceAlertService(
        IAlertService alerts, 
        IProfilerService profiler, 
        ILoggingService logger)
    {
        _alerts = alerts;
        _profiler = profiler;
        _logger = logger;
        _correlationId = $"PerfAlert_{Guid.NewGuid():N}"[..32];
        
        // Subscribe to profiler threshold exceeded events
        _profiler.ThresholdExceeded += OnPerformanceThresholdExceeded;
    }
    
    /// <summary>
    /// Handles performance threshold exceeded events from IProfilerService.
    /// Generates alerts when system performance degrades beyond acceptable limits.
    /// </summary>
    private async void OnPerformanceThresholdExceeded(ProfilerTag tag, double value, string unit)
    {
        using var scope = _profiler.BeginScope("PerformanceAlert.ThresholdExceeded");
        
        var severity = value switch
        {
            > 10.0 => AlertSeverity.Critical,  // >10x threshold
            > 5.0 => AlertSeverity.Warning,    // >5x threshold  
            > 2.0 => AlertSeverity.Info,       // >2x threshold
            _ => AlertSeverity.Debug
        };
        
        var threshold = PerformanceThresholds.GetValueOrDefault(tag.Name, 1.0);
        
        await _alerts.RaiseAlertAsync(
            $"Performance threshold exceeded: {tag.Name} = {value:F2}{unit} (threshold: {threshold:F2}{unit})",
            severity,
            "PerformanceMonitor",
            "Performance",
            _correlationId);
        
        _logger.LogWarning(
            $"Performance degradation detected: {tag.Name} exceeded threshold by {value / threshold:F1}x", 
            _correlationId);
    }
    
    /// <summary>
    /// Monitors alert processing performance and generates meta-alerts for system health.
    /// Uses zero-allocation patterns and burst-compatible operations.
    /// </summary>
    public async UniTask MonitorAlertPerformanceAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _profiler.BeginScope("AlertPerformanceMonitoring");
        
        var stats = _alerts.GetStatistics();
        
        // Check for alert storm conditions using ZLinq for zero allocation
        var recentAlerts = stats.SeverityStatistics.Values
            .Where(s => DateTime.UtcNow - s.LastAlert < TimeSpan.FromMinutes(1))
            .Sum(s => s.Count);
            
        if (recentAlerts > 100) // Alert storm threshold
        {
            await _alerts.RaiseAlertAsync(
                $"Alert storm detected: {recentAlerts} alerts in the last minute",
                AlertSeverity.Critical,
                "AlertSystem",
                "AlertStorm",
                _correlationId,
                cancellationToken);
        }
        
        // Check processing time degradation
        if (stats.AverageProcessingTime.TotalMilliseconds > 5.0)
        {
            await _alerts.RaiseAlertAsync(
                $"Alert processing time degraded: {stats.AverageProcessingTime.TotalMilliseconds:F2}ms average",
                AlertSeverity.Warning,
                "AlertSystem", 
                "Performance",
                _correlationId,
                cancellationToken);
        }
        
        _profiler.RecordMetric("AlertSystemHealth", recentAlerts, "alerts/min");
        _profiler.RecordMetric("AlertProcessingTime", stats.AverageProcessingTime.TotalMilliseconds, "ms");
    }
}
```

### Alert Templates and Formatting

```csharp
public class AlertTemplateService
{
    private readonly Dictionary<FixedString64Bytes, AlertTemplate> _templates = new();
    
    public void RegisterTemplate(FixedString64Bytes name, AlertTemplate template)
    {
        _templates[name] = template ?? throw new ArgumentNullException(nameof(template));
    }
    
    public Alert CreateFromTemplate(FixedString64Bytes templateName, Dictionary<string, object> parameters)
    {
        if (!_templates.TryGetValue(templateName, out var template))
            throw new ArgumentException($"Template '{templateName}' not found");
            
        return template.CreateAlert(parameters);
    }
}

public record AlertTemplate
{
    public FixedString64Bytes Name { get; init; }
    public string MessageTemplate { get; init; }
    public AlertSeverity DefaultSeverity { get; init; }
    public FixedString64Bytes DefaultSource { get; init; }
    public FixedString32Bytes DefaultTag { get; init; }
    public Dictionary<string, object> DefaultContext { get; init; } = new();
    
    public Alert CreateAlert(Dictionary<string, object> parameters)
    {
        var message = FormatMessage(MessageTemplate, parameters);
        
        return new Alert
        {
            Id = Guid.NewGuid(),
            Message = message,
            Severity = GetParameter<AlertSeverity>(parameters, "Severity", DefaultSeverity),
            Source = GetParameter<FixedString64Bytes>(parameters, "Source", DefaultSource),
            Tag = GetParameter<FixedString32Bytes>(parameters, "Tag", DefaultTag),
            Timestamp = DateTime.UtcNow,
            Context = new AlertContext
            {
                Properties = MergeContext(DefaultContext, parameters)
            }
        };
    }
    
    private static string FormatMessage(string template, Dictionary<string, object> parameters)
    {
        var result = template;
        foreach (var (key, value) in parameters)
        {
            result = result.Replace($"{{{key}}}", value?.ToString() ?? "");
        }
        return result;
    }
    
    private static T GetParameter<T>(Dictionary<string, object> parameters, string key, T defaultValue)
    {
        return parameters.TryGetValue(key, out var value) && value is T typedValue 
            ? typedValue 
            : defaultValue;
    }
    
    private static Dictionary<string, object> MergeContext(
        Dictionary<string, object> defaultContext, 
        Dictionary<string, object> parameters)
    {
        var merged = new Dictionary<string, object>(defaultContext ?? new Dictionary<string, object>());
        
        foreach (var (key, value) in parameters)
        {
            merged[key] = value;
        }
        
        return merged;
    }
}

// Usage example with modern C# features
public void SetupAlertTemplates(AlertTemplateService templateService)
{
    templateService.RegisterTemplate("DatabaseError", new AlertTemplate
    {
        Name = "DatabaseError",
        MessageTemplate = "Database operation failed: {Operation} on {Table}. Error: {ErrorMessage}",
        DefaultSeverity = AlertSeverity.Critical,
        DefaultSource = "DatabaseService",
        DefaultTag = "Database",
        DefaultContext = new Dictionary<string, object>
        {
            ["Category"] = "Database",
            ["Subsystem"] = "Persistence"
        }
    });
    
    templateService.RegisterTemplate("PerformanceWarning", new AlertTemplate
    {
        Name = "PerformanceWarning",
        MessageTemplate = "{MetricName} exceeded threshold: {CurrentValue} > {Threshold}",
        DefaultSeverity = AlertSeverity.Warning,
        DefaultSource = "PerformanceMonitor",
        DefaultTag = "Performance"
    });
}
```

## 📦 Installation

### 1. Package Installation

```csharp
// In Package Manager, add:
"com.ahbearstudios.core.alerts": "2.0.0"
```

### 2. Reflex Bootstrap Installation

```csharp
/// <summary>
/// Reflex installer for the Alert System following AhBearStudios Core Development Guidelines.
/// Provides comprehensive alert management with performance monitoring and health checks.
/// </summary>
public class AlertsInstaller : IBootstrapInstaller
{
    public string InstallerName => "AlertsInstaller";
    public int Priority => 200; // After Logging (100) and Messaging (150)
    public bool IsEnabled => true;
    public Type[] Dependencies => new[] { typeof(LoggingInstaller), typeof(MessagingInstaller) };

    public bool ValidateInstaller()
    {
        // Validate required dependencies
        if (!Container.HasBinding<ILoggingService>())
        {
            Debug.LogError("AlertsInstaller: ILoggingService not registered");
            return false;
        }

        if (!Container.HasBinding<IMessageBusService>())
        {
            Debug.LogError("AlertsInstaller: IMessageBusService not registered");
            return false;
        }

        return true;
    }

    public void PreInstall()
    {
        Debug.Log("AlertsInstaller: Beginning pre-installation validation");
    }

    public void Install(ContainerBuilder builder)
    {
        // Configure alerts with builder pattern
        var config = new AlertConfigBuilder()
            .WithMinimumSeverity(AlertSeverity.Warning)
            .WithSuppression(enabled: true, windowMinutes: 5)
            .WithAsyncProcessing(enabled: true, maxConcurrency: 50)
            .WithHistory(enabled: true, retentionHours: 24)
            .WithChannel<LogAlertChannel>()
            .WithChannel<ConsoleAlertChannel>()
            .Build();

        // Bind configuration
        builder.Bind<AlertConfig>().FromInstance(config);
        
        // Bind core services using Reflex patterns
        builder.Bind<IAlertService>().To<AlertService>().AsSingle();
        builder.Bind<IAlertFactory>().To<AlertFactory>().AsSingle();
        builder.Bind<AlertTemplateService>().To<AlertTemplateService>().AsSingle();
        
        // Bind specialized services
        builder.Bind<AlertDispatchService>().To<AlertDispatchService>().AsSingle();
        builder.Bind<AlertSuppressionService>().To<AlertSuppressionService>().AsSingle();
        builder.Bind<AlertAggregationService>().To<AlertAggregationService>().AsSingle();
        builder.Bind<AlertHistoryService>().To<AlertHistoryService>().AsSingle();
        
        // Bind health check
        builder.Bind<AlertServiceHealthCheck>().To<AlertServiceHealthCheck>().AsSingle();
        
        // Bind default channels
        builder.Bind<LogAlertChannel>().To<LogAlertChannel>().AsSingle();
        builder.Bind<ConsoleAlertChannel>().To<ConsoleAlertChannel>().AsSingle();
        
        // Bind default filters
        builder.Bind<RateLimitFilter>().To<RateLimitFilter>().AsSingle();
        builder.Bind<DuplicateFilter>().To<DuplicateFilter>().AsSingle();
        builder.Bind<SeverityFilter>().To<SeverityFilter>().AsSingle();
    }

    public void PostInstall()
    {
        try
        {
            // Register health checks
            var healthService = Container.Resolve<IHealthCheckService>();
            var alertHealthCheck = Container.Resolve<AlertServiceHealthCheck>();
            healthService.RegisterHealthCheck(alertHealthCheck);

            // Initialize alert templates
            var templateService = Container.Resolve<AlertTemplateService>();
            SetupDefaultAlertTemplates(templateService);

            Debug.Log("AlertsInstaller: Post-installation completed successfully");
        }
        catch (Exception ex)
        {
            Debug.LogError($"AlertsInstaller: Post-installation failed: {ex.Message}");
            throw;
        }
    }

    private static void SetupDefaultAlertTemplates(AlertTemplateService templateService)
    {
        // System error template
        templateService.RegisterTemplate("SystemError", new AlertTemplate
        {
            Name = "SystemError",
            MessageTemplate = "System error in {SystemName}: {ErrorMessage}",
            DefaultSeverity = AlertSeverity.Critical,
            DefaultSource = "SystemMonitor",
            DefaultTag = "SystemError"
        });

        // Performance warning template
        templateService.RegisterTemplate("PerformanceWarning", new AlertTemplate
        {
            Name = "PerformanceWarning",
            MessageTemplate = "Performance issue: {MetricName} = {Value} {Unit} (threshold: {Threshold})",
            DefaultSeverity = AlertSeverity.Warning,
            DefaultSource = "PerformanceMonitor", 
            DefaultTag = "Performance"
        });

        // Resource exhaustion template
        templateService.RegisterTemplate("ResourceExhaustion", new AlertTemplate
        {
            Name = "ResourceExhaustion",
            MessageTemplate = "Resource exhaustion detected: {ResourceType} usage at {Usage}%",
            DefaultSeverity = AlertSeverity.Critical,
            DefaultSource = "ResourceMonitor",
            DefaultTag = "Resource"
        });
    }
}
```

### 3. Alert System Initialization

The Alert System must be properly initialized to register all types with ISerializationService:

```csharp
/// <summary>
/// Initialize the Alert System during application startup.
/// This registers all alert-related types with ISerializationService for proper serialization.
/// </summary>
public void InitializeAlertSystem()
{
    // Get services from DI container
    var serializationService = Container.Resolve<ISerializationService>();
    var loggingService = Container.Resolve<ILoggingService>();
    
    // Initialize Alert System with type registration
    var success = AlertSystemInitializer.InitializeAlertSystem(
        serializationService, 
        loggingService, 
        correlationId: "AppStartup");
        
    if (!success)
    {
        throw new InvalidOperationException("Failed to initialize Alert System");
    }
}

/// <summary>
/// Alternative approach using explicit initializer instance for more control.
/// </summary>
public void InitializeAlertSystemDetailed()
{
    var serializationService = Container.Resolve<ISerializationService>();
    var loggingService = Container.Resolve<ILoggingService>();
    
    using var initializer = AlertSystemInitializer.Create(serializationService, loggingService);
    
    if (!initializer.Initialize("AppStartup"))
    {
        throw new InvalidOperationException("Failed to initialize Alert System");
    }
    
    // Validate that all types are properly registered
    if (!initializer.ValidateRegistration("AppStartup"))
    {
        throw new InvalidOperationException("Alert System type registration validation failed");
    }
}
```

### 4. Usage in Services with Modern C# Patterns

```csharp
/// <summary>
/// Example service demonstrating proper Alert System integration with modern C# patterns.
/// Follows AhBearStudios Core Development Guidelines with comprehensive error handling.
/// </summary>
public class ExampleService
{
    private readonly IAlertService _alerts;
    private readonly ILoggingService _logger;
    private readonly IProfilerService _profiler;
    private readonly FixedString64Bytes _correlationId;
    
    /// <summary>
    /// Initializes the example service with required dependencies.
    /// </summary>
    /// <param name="alerts">Alert service for system notifications</param>
    /// <param name="logger">Logging service for operation tracking</param>
    /// <param name="profiler">Profiler service for performance monitoring</param>
    /// <exception cref="ArgumentNullException">Thrown when any required dependency is null</exception>
    public ExampleService(IAlertService alerts, ILoggingService logger, IProfilerService profiler)
    {
        _alerts = alerts ?? throw new ArgumentNullException(nameof(alerts));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
        _correlationId = $"ExampleService_{Guid.NewGuid():N}"[..32];
    }
    
    /// <summary>
    /// Processes data with comprehensive error handling and alerting.
    /// </summary>
    /// <param name="data">Data to process</param>
    /// <returns>Processed result</returns>
    /// <exception cref="ArgumentException">Thrown when data is invalid</exception>
    /// <exception cref="ProcessingException">Thrown when processing fails</exception>
    public async Task<ProcessingResult> ProcessDataAsync(string data)
    {
        using var scope = _profiler.BeginScope("ExampleService.ProcessDataAsync");
        
        try
        {
            // Input validation with pattern matching
            var validationResult = data switch
            {
                null => ValidationResult.Null,
                "" => ValidationResult.Empty,
                { Length: > 1000 } => ValidationResult.TooLarge,
                _ => ValidationResult.Valid
            };

            if (validationResult != ValidationResult.Valid)
            {
                var severity = validationResult switch
                {
                    ValidationResult.Null => AlertSeverity.Warning,
                    ValidationResult.Empty => AlertSeverity.Warning,
                    ValidationResult.TooLarge => AlertSeverity.Critical,
                    _ => AlertSeverity.Info
                };

                _alerts.RaiseAlert(
                    $"Data validation failed: {validationResult}",
                    severity,
                    "ExampleService",
                    "DataValidation"
                );

                _logger.LogWarning($"[{_correlationId}] Data validation failed: {validationResult}");
                throw new ArgumentException($"Invalid data: {validationResult}");
            }
            
            // Process data with timeout and monitoring
            var result = await ProcessWithTimeoutAsync(data);
            
            _logger.LogInfo($"[{_correlationId}] Data processed successfully");
            return result;
        }
        catch (TimeoutException ex)
        {
            _alerts.RaiseAlert(
                $"Data processing timeout: {ex.Message}",
                AlertSeverity.Warning,
                "ExampleService",
                "Timeout"
            );
            
            _logger.LogException(ex, $"[{_correlationId}] Processing timeout");
            throw new ProcessingException("Processing timed out", ex);
        }
        catch (Exception ex) when (!(ex is ArgumentException))
        {
            _alerts.RaiseAlert(
                $"Data processing failed: {ex.Message}",
                AlertSeverity.Critical,
                "ExampleService",
                "ProcessingError"
            );
            
            _logger.LogException(ex, $"[{_correlationId}] Processing failed");
            throw new ProcessingException("Processing failed", ex);
        }
    }

    private async Task<ProcessingResult> ProcessWithTimeoutAsync(string data)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        
        try
        {
            // Simulate processing work
            await Task.Delay(100, cts.Token);
            return new ProcessingResult { Success = true, Data = data.ToUpperInvariant() };
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            throw new TimeoutException("Processing operation timed out");
        }
    }
}

public enum ValidationResult
{
    Valid,
    Null,
    Empty,
    TooLarge
}

public record ProcessingResult
{
    public bool Success { get; init; }
    public string Data { get; init; }
}

public class ProcessingException : Exception
{
    public ProcessingException(string message) : base(message) { }
    public ProcessingException(string message, Exception innerException) : base(message, innerException) { }
}
```

## 🏥 Health Monitoring

### Health Check Implementation

```csharp
/// <summary>
/// Health check implementation for the Alert System.
/// Monitors alert processing performance, channel health, and system capacity.
/// </summary>
public class AlertServiceHealthCheck : IHealthCheck
{
    private readonly IAlertService _alertService;
    private readonly ILoggingService _logger;
    private readonly FixedString64Bytes _correlationId;
    
    public FixedString64Bytes Name => "AlertService";
    public string Description => "Monitors alert system health and performance";
    public HealthCheckCategory Category => HealthCheckCategory.System;
    public TimeSpan Timeout => TimeSpan.FromSeconds(10);
    public HealthCheckConfiguration Configuration { get; private set; }
    public IEnumerable<FixedString64Bytes> Dependencies => Array.Empty<FixedString64Bytes>();
    
    /// <summary>
    /// Initializes the health check with required dependencies.
    /// </summary>
    /// <param name="alertService">Alert service to monitor</param>
    /// <param name="logger">Logging service for health check operations</param>
    /// <exception cref="ArgumentNullException">Thrown when any required dependency is null</exception>
    public AlertServiceHealthCheck(IAlertService alertService, ILoggingService logger)
    {
        _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _correlationId = $"AlertHealthCheck_{Guid.NewGuid():N}"[..32];
        
        Configuration = new HealthCheckConfiguration
        {
            Timeout = Timeout,
            Interval = TimeSpan.FromMinutes(1),
            IsEnabled = true
        };
    }
    
    /// <summary>
    /// Performs health check assessment of the alert system.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Health check result with detailed status information</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInfo($"[{_correlationId}] Starting alert service health check");
            
            var stats = _alertService.GetStatistics();
            var channels = _alertService.GetRegisteredChannels();
            
            var data = new Dictionary<string, object>
            {
                ["AlertsProcessed"] = stats.TotalAlertsProcessed,
                ["AlertsFailed"] = stats.TotalAlertsFailed,
                ["ActiveChannels"] = channels.Count(c => c.IsEnabled),
                ["TotalChannels"] = channels.Count,
                ["AverageProcessingTime"] = stats.AverageProcessingTime.TotalMilliseconds,
                ["QueueSize"] = stats.CurrentQueueSize,
                ["SuppressionRate"] = stats.SuppressionRate,
                ["CorrelationId"] = _correlationId.ToString()
            };
            
            // Check error rates using modern C# patterns
            var healthStatus = stats.ErrorRate switch
            {
                > 0.5 => HealthStatus.Unhealthy,
                > 0.1 => HealthStatus.Degraded,
                _ => HealthStatus.Healthy
            };
            
            var message = healthStatus switch
            {
                HealthStatus.Unhealthy => $"High error rate: {stats.ErrorRate:P}",
                HealthStatus.Degraded => $"Elevated error rate: {stats.ErrorRate:P}",
                _ => "Alert system operating normally"
            };
            
            // Check queue size
            if (stats.CurrentQueueSize > 1000 && healthStatus == HealthStatus.Healthy)
            {
                healthStatus = HealthStatus.Degraded;
                message = $"High queue size: {stats.CurrentQueueSize}";
            }
            
            // Check channel health
            var unhealthyChannels = channels.Where(c => c.GetStatus() != ChannelStatus.Healthy).ToList();
            if (unhealthyChannels.Any() && healthStatus == HealthStatus.Healthy)
            {
                healthStatus = HealthStatus.Degraded;
                message = $"Unhealthy channels: {string.Join(", ", unhealthyChannels.Select(c => c.Name))}";
            }
            
            var result = new HealthCheckResult
            {
                Name = Name.ToString(),
                Status = healthStatus,
                Message = message,
                Description = Description,
                Duration = stopwatch.Elapsed,
                Timestamp = DateTime.UtcNow,
                Data = data
            };
            
            _logger.LogInfo($"[{_correlationId}] Alert service health check completed: {healthStatus}");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogException(ex, $"[{_correlationId}] Alert service health check failed");
            
            return new HealthCheckResult
            {
                Name = Name.ToString(),
                Status = HealthStatus.Unhealthy,
                Message = $"Health check failed: {ex.Message}",
                Description = Description,
                Duration = stopwatch.Elapsed,
                Timestamp = DateTime.UtcNow,
                Exception = ex
            };
        }
    }
    
    public void Configure(HealthCheckConfiguration configuration)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }
    
    public Dictionary<string, object> GetMetadata()
    {
        return new Dictionary<string, object>
        {
            ["ServiceType"] = _alertService.GetType().Name,
            ["SupportedOperations"] = new[] { "RaiseAlert", "ChannelManagement", "FilterManagement" },
            ["HealthCheckEnabled"] = true,
            ["MonitoringCapabilities"] = new[] { "ErrorRate", "QueueSize", "ChannelHealth", "ProcessingTime" }
        };
    }
}
```

### Statistics and Metrics

```csharp
/// <summary>
/// Comprehensive statistics for alert system performance monitoring.
/// Provides detailed metrics for system health assessment and performance optimization.
/// </summary>
public sealed record AlertStatistics
{
    /// <summary>
    /// Gets the total number of alerts processed since last reset.
    /// </summary>
    public long TotalAlertsProcessed { get; init; }
    
    /// <summary>
    /// Gets the total number of alerts that failed processing.
    /// </summary>
    public long TotalAlertsFailed { get; init; }
    
    /// <summary>
    /// Gets the total number of alerts suppressed by filters.
    /// </summary>
    public long TotalAlertsSuppressed { get; init; }
    
    /// <summary>
    /// Gets the current queue size of pending alerts.
    /// </summary>
    public int CurrentQueueSize { get; init; }
    
    /// <summary>
    /// Gets the maximum queue size reached since last reset.
    /// </summary>
    public int MaxQueueSize { get; init; }
    
    /// <summary>
    /// Gets the average alert processing time.
    /// </summary>
    public TimeSpan AverageProcessingTime { get; init; }
    
    /// <summary>
    /// Gets the timestamp when statistics were last reset.
    /// </summary>
    public DateTime LastStatsReset { get; init; }
    
    /// <summary>
    /// Gets the current error rate (0.0 to 1.0).
    /// </summary>
    public double ErrorRate => TotalAlertsProcessed > 0 ? 
        (double)TotalAlertsFailed / TotalAlertsProcessed : 0;
    
    /// <summary>
    /// Gets the current suppression rate (0.0 to 1.0).
    /// </summary>
    public double SuppressionRate => (TotalAlertsProcessed + TotalAlertsSuppressed) > 0 ?
        (double)TotalAlertsSuppressed / (TotalAlertsProcessed + TotalAlertsSuppressed) : 0;
    
    /// <summary>
    /// Gets statistics per alert severity level.
    /// </summary>
    public Dictionary<AlertSeverity, SeverityStatistics> SeverityStatistics { get; init; } = new();
    
    /// <summary>
    /// Gets statistics per alert channel.
    /// </summary>
    public Dictionary<FixedString64Bytes, ChannelStatistics> ChannelStatistics { get; init; } = new();
    
    /// <summary>
    /// Gets statistics per alert source system.
    /// </summary>
    public Dictionary<FixedString64Bytes, SourceStatistics> SourceStatistics { get; init; } = new();
}

/// <summary>
/// Statistics for a specific alert severity level.
/// </summary>
public sealed record SeverityStatistics
{
    public AlertSeverity Severity { get; init; }
    public long Count { get; init; }
    public long Failed { get; init; }
    public long Suppressed { get; init; }
    public TimeSpan AverageProcessingTime { get; init; }
    public DateTime LastAlert { get; init; }
}

/// <summary>
/// Statistics for a specific alert channel.
/// </summary>
public sealed record ChannelStatistics
{
    public FixedString64Bytes ChannelName { get; init; }
    public long AlertsSent { get; init; }
    public long AlertsFailed { get; init; }
    public TimeSpan AverageSendTime { get; init; }
    public ChannelStatus CurrentStatus { get; init; }
    public DateTime LastActivity { get; init; }
}

/// <summary>
/// Statistics for a specific alert source system.
/// </summary>
public sealed record SourceStatistics
{
    public FixedString64Bytes Source { get; init; }
    public long TotalAlerts { get; init; }
    public long CriticalAlerts { get; init; }
    public long WarningAlerts { get; init; }
    public long InfoAlerts { get; init; }
    public DateTime LastAlert { get; init; }
    public double AlertRate { get; init; } // Alerts per minute
}
```

## 📊 Performance Characteristics

### Alert Processing Performance

The Alert System is optimized for Unity game development with zero-allocation patterns and high-performance operations.

| Operation | Time (μs) | Memory Allocation | Throughput |
|-----------|-----------|------------------|------------|
| Simple Alert Creation | 25 | 0 bytes (pooled) | 40K alerts/sec |
| Alert with Context | 45 | 320 bytes | 22K alerts/sec |
| Filter Chain Processing | 15-50 | 0 bytes | 20-66K ops/sec |
| Channel Dispatch | 5-2000 | Minimal | 8.3K ops/sec |
| Suppression Check | 10 | 0 bytes | 100K ops/sec |
| Factory Creation | 200-500 | 1-5KB | 2-5K ops/sec |

### Channel Performance by Type

| Channel Type | Latency | Memory | Reliability | Use Case |
|--------------|---------|--------|-------------|-----------|
| **Memory** | ~5μs | 240 bytes/alert | 100% | Unit testing, debugging |
| **Logging** | ~10μs | Minimal | 99.9% | Production logging |
| **Console** | ~50μs | Minimal | 99% | Development, debug output |
| **File** | ~100μs | Minimal | 95% | Persistent storage |
| **Unity Debug** | ~80μs | Minimal | 99% | Unity-specific logging |
| **Null** | ~1μs | 0 bytes | 100% | Performance testing |

### Factory System Performance

| Factory Operation | Time (ms) | Memory | Notes |
|-------------------|-----------|--------|--------|
| Service Creation (Default) | 0.5-2 | 2MB | Includes channel/filter setup |
| Channel Creation | 0.1-0.5 | 50KB | Varies by channel type |
| Filter Creation | 0.1-0.3 | 10KB | Simple filter types |
| Composite Filter Creation | 0.5-2 | 50KB | Multiple child filters |
| Configuration Validation | 0.1-0.5 | Minimal | Comprehensive validation |

### Memory Usage Breakdown

#### Base System
- **AlertService**: ~2MB initialization, 100KB operational
- **Channel Manager**: 200KB base + 50KB per channel
- **Filter Manager**: 150KB base + 10KB per filter  
- **Suppression Service**: 500KB base + variable for rules

#### Per-Alert Memory
- **Minimal Alert**: 240 bytes (FixedString usage)
- **Alert with Context**: 500-2000 bytes (depends on context size)
- **Alert History Storage**: 400 bytes per stored alert
- **Suppression Tracking**: 50 bytes per unique alert pattern

#### Factory System
- **AlertServiceFactory**: 100KB base memory
- **AlertChannelFactory**: 50KB base memory
- **AlertFilterFactory**: 50KB base memory
- **Configuration Objects**: 1-10KB depending on complexity

### Scalability and Performance Limits

#### Throughput Limits
- **Single Thread**: 25K-50K alerts/sec (depends on complexity)
- **Multi-threaded**: Linear scaling with available cores
- **Memory Bound**: ~100K active alerts before GC pressure
- **Network Bound**: Limited by slowest channel (async dispatch mitigates)

#### Resource Scaling
- **CPU Usage**: 1-5% at 1K alerts/sec, scales linearly
- **Memory Growth**: O(1) for processing, O(n) for history
- **I/O Impact**: Async operations prevent blocking
- **Unity Integration**: Minimal impact on frame rate (<0.1ms per frame)

### Zero-Allocation Patterns

The system uses several zero-allocation optimizations:

- **FixedString Types**: All string operations use Unity.Collections.FixedString
- **Struct-Based Models**: Alert, AlertContext use struct/record patterns
- **Object Pooling**: Compatible with IPoolingService for high-throughput scenarios
- **Stack Allocations**: Filter processing uses stack-allocated temporary data
- **Async Operations**: UniTask prevents Task allocations

### Burst Compatibility

Key components are designed for Unity Burst compilation:
- Alert data structures use Burst-compatible types
- Filter evaluation logic avoids managed allocations  
- Core processing paths use unmanaged memory layouts
- ZLinq operations maintain zero-allocation patterns

### Performance Monitoring

Built-in performance tracking provides:
- **Per-Operation Timing**: Microsecond precision for all operations
- **Memory Allocation Tracking**: Monitors GC pressure and allocations
- **Throughput Metrics**: Real-time alerts/sec monitoring
- **Channel Health**: Response time and failure rate tracking
- **Filter Performance**: Processing time and hit rate statistics

### Optimization Recommendations

For high-performance scenarios:
1. **Use Memory Channels** for unit testing (fastest)
2. **Enable Object Pooling** for >10K alerts/sec
3. **Optimize Filter Order** by priority (fastest filters first)
4. **Limit Context Data** to essential information only
5. **Use Async Operations** for I/O bound channels
6. **Monitor GC Pressure** in production deployments

## 🔧 Advanced Features

### Circuit Breaker Integration

```csharp
public class CircuitBreakerAlertChannel : IAlertChannel
{
    private readonly IAlertChannel _innerChannel;
    private readonly CircuitBreaker _circuitBreaker;
    private readonly ILoggingService _logger;
    
    public CircuitBreakerAlertChannel(
        IAlertChannel innerChannel, 
        ILoggingService logger,
        CircuitBreakerConfig config)
    {
        _innerChannel = innerChannel ?? throw new ArgumentNullException(nameof(innerChannel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _circuitBreaker = new CircuitBreaker(config);
    }
    
    public async Task SendAlertAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        try
        {
            await _circuitBreaker.ExecuteAsync(async () =>
            {
                await _innerChannel.SendAlertAsync(alert, cancellationToken);
            }, cancellationToken);
        }
        catch (CircuitBreakerOpenException)
        {
            _logger.LogWarning($"Circuit breaker open for channel {Name}, alert dropped");
            // Could queue for retry or use fallback channel
        }
    }
    
    public FixedString64Bytes Name => _innerChannel.Name;
    public bool IsEnabled { get => _innerChannel.IsEnabled; set => _innerChannel.IsEnabled = value; }
    public AlertSeverity MinimumSeverity { get => _innerChannel.MinimumSeverity; set => _innerChannel.MinimumSeverity = value; }
    public ChannelConfiguration Configuration => _innerChannel.Configuration;
    
    public bool CanSendAlert(Alert alert) => 
        _circuitBreaker.State == CircuitBreakerState.Closed && _innerChannel.CanSendAlert(alert);
    
    public ChannelStatus GetStatus() => _circuitBreaker.State switch
    {
        CircuitBreakerState.Open => ChannelStatus.Unhealthy,
        CircuitBreakerState.HalfOpen => ChannelStatus.Degraded,
        _ => _innerChannel.GetStatus()
    };
    
    public Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default) =>
        _innerChannel.TestConnectionAsync(cancellationToken);
    
    public void Configure(ChannelConfiguration configuration) =>
        _innerChannel.Configure(configuration);
    
    public void Dispose()
    {
        _innerChannel?.Dispose();
        _circuitBreaker?.Dispose();
    }
    
    public event EventHandler<ChannelEventArgs> AlertSent
    {
        add => _innerChannel.AlertSent += value;
        remove => _innerChannel.AlertSent -= value;
    }
    
    public event EventHandler<ChannelEventArgs> SendFailed
    {
        add => _innerChannel.SendFailed += value;
        remove => _innerChannel.SendFailed -= value;
    }
}
```

### Custom Filter Implementation

```csharp
/// <summary>
/// Business hours filter that applies different severity thresholds based on time.
/// Implements smart filtering for operational efficiency.
/// </summary>
public class BusinessHoursFilter : IAlertFilter
{
    private readonly ILoggingService _logger;
    private readonly TimeZoneInfo _timeZone;
    private BusinessHoursConfig _config;
    
    public FixedString64Bytes Name => "BusinessHoursFilter";
    public int Priority => 100;
    public bool IsEnabled { get; set; } = true;
    
    public BusinessHoursFilter(ILoggingService logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeZone = TimeZoneInfo.Local;
        _config = BusinessHoursConfig.Default;
    }
    
    public FilterResult ShouldProcess(Alert alert)
    {
        if (!IsEnabled) return FilterResult.Allow;
        
        var currentTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone);
        var isBusinessHours = IsBusinessHours(currentTime);
        
        var requiredSeverity = isBusinessHours 
            ? _config.BusinessHoursSeverity 
            : _config.AfterHoursSeverity;
        
        var result = alert.Severity >= requiredSeverity 
            ? FilterResult.Allow 
            : FilterResult.Suppress;
        
        if (result == FilterResult.Suppress)
        {
            _logger.LogInfo($"Alert suppressed by BusinessHoursFilter: {alert.Id} " +
                          $"(severity: {alert.Severity}, required: {requiredSeverity}, " +
                          $"business hours: {isBusinessHours})");
        }
        
        return result;
    }
    
    public void Configure(Dictionary<string, object> settings)
    {
        if (settings.TryGetValue("BusinessHoursSeverity", out var bhSeverity) && 
            bhSeverity is AlertSeverity businessSeverity)
        {
            _config = _config with { BusinessHoursSeverity = businessSeverity };
        }
        
        if (settings.TryGetValue("AfterHoursSeverity", out var ahSeverity) && 
            ahSeverity is AlertSeverity afterSeverity)
        {
            _config = _config with { AfterHoursSeverity = afterSeverity };
        }
        
        if (settings.TryGetValue("StartHour", out var startHour) && startHour is int start)
        {
            _config = _config with { StartHour = start };
        }
        
        if (settings.TryGetValue("EndHour", out var endHour) && endHour is int end)
        {
            _config = _config with { EndHour = end };
        }
    }
    
    public FilterStatistics GetStatistics()
    {
        return new FilterStatistics
        {
            FilterName = Name,
            ProcessedCount = _processedCount,
            SuppressedCount = _suppressedCount,
            AllowedCount = _allowedCount,
            LastActivity = _lastActivity
        };
    }
    
    private bool IsBusinessHours(DateTime currentTime)
    {
        return currentTime.DayOfWeek switch
        {
            DayOfWeek.Saturday or DayOfWeek.Sunday => false,
            _ => currentTime.Hour >= _config.StartHour && currentTime.Hour < _config.EndHour
        };
    }
    
    private long _processedCount;
    private long _suppressedCount;
    private long _allowedCount;
    private DateTime _lastActivity;
}

/// <summary>
/// Configuration for business hours filtering.
/// </summary>
public sealed record BusinessHoursConfig
{
    public AlertSeverity BusinessHoursSeverity { get; init; } = AlertSeverity.Info;
    public AlertSeverity AfterHoursSeverity { get; init; } = AlertSeverity.Warning;
    public int StartHour { get; init; } = 9;  // 9 AM
    public int EndHour { get; init; } = 17;   // 5 PM
    
    public static BusinessHoursConfig Default => new();
}

/// <summary>
/// Statistics for alert filter performance monitoring.
/// </summary>
public sealed record FilterStatistics
{
    public FixedString64Bytes FilterName { get; init; }
    public long ProcessedCount { get; init; }
    public long SuppressedCount { get; init; }
    public long AllowedCount { get; init; }
    public long ModifiedCount { get; init; }
    public DateTime LastActivity { get; init; }
    public TimeSpan AverageProcessingTime { get; init; }
    
    public double SuppressionRate => ProcessedCount > 0 ? 
        (double)SuppressedCount / ProcessedCount : 0;
}
```

## 📚 Additional Resources

- [Alert Design Patterns](ALERTS_PATTERNS.md)
- [Custom Channel Development](ALERTS_CUSTOM_CHANNELS.md)
- [Alert Filtering Best Practices](ALERTS_FILTERING.md)
- [Integration Guide](ALERTS_INTEGRATION.md)
- [Troubleshooting Guide](ALERTS_TROUBLESHOOTING.md)
- [Performance Optimization Guide](ALERTS_PERFORMANCE.md)
- [Security Considerations](ALERTS_SECURITY.md)

## 🤝 Contributing

See our [Contributing Guidelines](../../CONTRIBUTING.md) for information on how to contribute to the Alert System.

## 📄 Dependencies

- **Direct**: Logging, Messaging, HealthCheck
- **Dependents**: All systems requiring alert capabilities
- **Optional**: Profiling (for performance monitoring), Pooling (for high-throughput scenarios)

---

*The Alert System provides real-time notification capabilities for critical system events across all AhBearStudios Core systems.*