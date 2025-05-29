
using Unity.Collections;

namespace AhBearStudios.Core.Logging.Data
{
    /// <summary>
    /// Contains constant keys for log message properties used in structured logging.
    /// Provides a centralized collection of standardized property keys to ensure consistency
    /// across all logging operations. Supports both regular .NET code and Unity Burst-compiled code paths.
    /// 
    /// <para>
    /// <strong>Usage Examples:</strong>
    /// </para>
    /// <code>
    /// // Regular usage
    /// logger.Info("User logged in")
    ///     .WithProperty(LogPropertyKeys.UserId, "user123")
    ///     .WithProperty(LogPropertyKeys.Category, "authentication");
    /// 
    /// // Burst-compatible usage
    /// var categoryKey = LogPropertyKeys.Burst.Category;
    /// BurstLogger.Log(LogLevel.Info, "Job completed", categoryKey, "jobs");
    /// </code>
    /// 
    /// <para>
    /// <strong>Property Categories:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item><description>Core Logging: Category, Source, Subsystem, Component</description></item>
    /// <item><description>Debugging: MethodName, ClassName, FileName, LineNumber, ThreadId</description></item>
    /// <item><description>Timing: Timestamp, Time, Duration</description></item>
    /// <item><description>Performance: CpuUsage, MemoryUsage</description></item>
    /// <item><description>Session Context: SessionId, UserId, CorrelationId, RequestId</description></item>
    /// <item><description>Application: Environment, Version, Scene, AppData</description></item>
    /// <item><description>Error Handling: Exception, Error, Status, State</description></item>
    /// <item><description>General Purpose: Id, Name, Type, Value, Path, Position, Count</description></item>
    /// </list>
    /// </summary>
    public static class LogPropertyKeys
    {
        #region Core Logging Properties
        
        /// <summary>
        /// Logical grouping of log messages for categorization and filtering.
        /// <para>Examples: "networking", "ui", "gameplay", "authentication"</para>
        /// </summary>
        public const string Category = "category";
        
        /// <summary>
        /// The originating component or system that generated the log message.
        /// <para>Examples: "PlayerController", "NetworkManager", "DatabaseService"</para>
        /// </summary>
        public const string Source = "source";
        
        /// <summary>
        /// Higher-level system classification for organizing related components.
        /// <para>Examples: "networking", "rendering", "physics", "audio"</para>
        /// </summary>
        public const string Subsystem = "subsystem";
        
        /// <summary>
        /// Specific component within a subsystem for fine-grained organization.
        /// <para>Examples: "tcp-client", "mesh-renderer", "rigidbody", "audio-source"</para>
        /// </summary>
        public const string Component = "component";
        
        #endregion
        
        #region Debugging and Tracing Properties
        
        /// <summary>
        /// Thread identifier for concurrent debugging and performance analysis.
        /// <para>Automatically captured to help trace execution across multiple threads.</para>
        /// </summary>
        public const string ThreadId = "threadId";
        
        /// <summary>
        /// Method name that generated the log message for precise debugging.
        /// <para>Typically captured automatically using [CallerMemberName] attribute.</para>
        /// </summary>
        public const string MethodName = "methodName";
        
        /// <summary>
        /// Class name containing the logging call for code location tracking.
        /// <para>Helps identify the exact location of log statements in large codebases.</para>
        /// </summary>
        public const string ClassName = "className";
        
        /// <summary>
        /// Source file name for development and debugging purposes.
        /// <para>Typically captured automatically using [CallerFilePath] attribute.</para>
        /// </summary>
        public const string FileName = "fileName";
        
        /// <summary>
        /// Line number in source file for precise code location.
        /// <para>Typically captured automatically using [CallerLineNumber] attribute.</para>
        /// </summary>
        public const string LineNumber = "lineNumber";
        
        #endregion
        
        #region Timing and Performance Properties
        
        /// <summary>
        /// Timestamp when the log message was created for chronological ordering.
        /// <para>Usually formatted as ISO 8601 string or Unix timestamp.</para>
        /// </summary>
        public const string Timestamp = "timestamp";
        
        /// <summary>
        /// General time-related data for operations or events.
        /// <para>Examples: operation start time, event occurrence time</para>
        /// </summary>
        public const string Time = "time";
        
        /// <summary>
        /// Time elapsed for operations in milliseconds for performance monitoring.
        /// <para>Examples: method execution time, network request duration, file I/O time</para>
        /// </summary>
        public const string Duration = "duration";
        
        /// <summary>
        /// Memory usage information in bytes for performance analysis.
        /// <para>Examples: heap usage, allocated memory, memory delta</para>
        /// </summary>
        public const string MemoryUsage = "memoryUsage";
        
        /// <summary>
        /// CPU usage percentage for performance monitoring.
        /// <para>Examples: current CPU usage, peak usage during operation</para>
        /// </summary>
        public const string CpuUsage = "cpuUsage";
        
        #endregion
        
        #region Session and Context Properties
        
        /// <summary>
        /// Unique identifier linking related messages across operations for tracing.
        /// <para>Essential for distributed systems and complex operation flows.</para>
        /// </summary>
        public const string CorrelationId = "correlationId";
        
        /// <summary>
        /// Current user session identifier for user activity tracking.
        /// <para>Helps correlate all activities within a single user session.</para>
        /// </summary>
        public const string SessionId = "sessionId";
        
        /// <summary>
        /// User identifier for user-specific logging and analytics.
        /// <para>Examples: username, user ID, anonymous user token</para>
        /// </summary>
        public const string UserId = "userId";
        
        /// <summary>
        /// Request identifier for tracking web requests or API calls.
        /// <para>Essential for web applications and service-oriented architectures.</para>
        /// </summary>
        public const string RequestId = "requestId";
        
        #endregion
        
        #region Application Context Properties
        
        /// <summary>
        /// Custom application-specific data for domain-specific logging.
        /// <para>Examples: game state, business context, custom metadata</para>
        /// </summary>
        public const string AppData = "appData";
        
        /// <summary>
        /// Deployment environment information for environment-specific behavior.
        /// <para>Examples: "development", "staging", "production", "testing"</para>
        /// </summary>
        public const string Environment = "environment";
        
        /// <summary>
        /// Application version information for version-specific debugging.
        /// <para>Examples: "1.2.3", "2.0.0-beta", build numbers</para>
        /// </summary>
        public const string Version = "version";
        
        /// <summary>
        /// Unity scene information for game development context.
        /// <para>Examples: scene name, scene index, scene loading state</para>
        /// </summary>
        public const string Scene = "scene";
        
        #endregion
        
        #region Error Handling Properties
        
        /// <summary>
        /// Exception details and stack traces for error analysis.
        /// <para>Should include full exception information including inner exceptions.</para>
        /// </summary>
        public const string Exception = "exception";
        
        /// <summary>
        /// General error information for non-exception error conditions.
        /// <para>Examples: validation errors, business rule violations, warnings</para>
        /// </summary>
        public const string Error = "error";
        
        /// <summary>
        /// Operation status for tracking success/failure states.
        /// <para>Examples: "success", "failed", "pending", "cancelled"</para>
        /// </summary>
        public const string Status = "status";
        
        /// <summary>
        /// Current state information for state machine debugging.
        /// <para>Examples: connection state, game state, process state</para>
        /// </summary>
        public const string State = "state";
        
        #endregion
        
        #region General Purpose Properties
        
        /// <summary>
        /// Numeric count data for quantitative logging.
        /// <para>Examples: retry count, item count, iteration number</para>
        /// </summary>
        public const string Count = "count";
        
        /// <summary>
        /// Generic identifier for various entities.
        /// <para>Examples: entity ID, object ID, transaction ID</para>
        /// </summary>
        public const string Id = "id";
        
        /// <summary>
        /// Generic name field for human-readable identification.
        /// <para>Examples: object name, operation name, resource name</para>
        /// </summary>
        public const string Name = "name";
        
        /// <summary>
        /// Type classification for categorizing different kinds of entities.
        /// <para>Examples: object type, operation type, data type</para>
        /// </summary>
        public const string Type = "type";
        
        /// <summary>
        /// Generic value storage for various data types.
        /// <para>Examples: configuration values, computed results, user input</para>
        /// </summary>
        public const string Value = "value";
        
        /// <summary>
        /// File or resource paths for file system operations.
        /// <para>Examples: file paths, URL paths, resource paths</para>
        /// </summary>
        public const string Path = "path";
        
        /// <summary>
        /// Spatial or logical position data for location tracking.
        /// <para>Examples: world coordinates, array indices, cursor position</para>
        /// </summary>
        public const string Position = "position";
        
        #endregion
        
        /// <summary>
        /// Provides FixedString32Bytes versions of all property keys for Unity Burst-compatible code.
        /// These are automatically generated from the string constants to ensure consistency.
        /// 
        /// <para>
        /// <strong>Usage in Burst Jobs:</strong>
        /// </para>
        /// <code>
        /// [BurstCompile]
        /// public struct LoggingJob : IJob
        /// {
        ///     public void Execute()
        ///     {
        ///         var categoryKey = LogPropertyKeys.Burst.Category;
        ///         BurstLogger.Log(LogLevel.Info, "Job completed", categoryKey, "jobs");
        ///     }
        /// }
        /// </code>
        /// </summary>
        public static class Burst
        {
            /// <summary>Burst-compatible version of <see cref="LogPropertyKeys.Category"/>.</summary>
            public static readonly FixedString32Bytes Category = new FixedString32Bytes(LogPropertyKeys.Category);
            
            /// <summary>Burst-compatible version of <see cref="LogPropertyKeys.Source"/>.</summary>
            public static readonly FixedString32Bytes Source = new FixedString32Bytes(LogPropertyKeys.Source);
            
            /// <summary>Burst-compatible version of <see cref="LogPropertyKeys.ThreadId"/>.</summary>
            public static readonly FixedString32Bytes ThreadId = new FixedString32Bytes(LogPropertyKeys.ThreadId);
            
            /// <summary>Burst-compatible version of <see cref="LogPropertyKeys.Timestamp"/>.</summary>
            public static readonly FixedString32Bytes Timestamp = new FixedString32Bytes(LogPropertyKeys.Timestamp);
            
            /// <summary>Burst-compatible version of <see cref="LogPropertyKeys.MethodName"/>.</summary>
            public static readonly FixedString32Bytes MethodName = new FixedString32Bytes(LogPropertyKeys.MethodName);
            
            /// <summary>Burst-compatible version of <see cref="LogPropertyKeys.ClassName"/>.</summary>
            public static readonly FixedString32Bytes ClassName = new FixedString32Bytes(LogPropertyKeys.ClassName);
            
            /// <summary>Burst-compatible version of <see cref="LogPropertyKeys.FileName"/>.</summary>
            public static readonly FixedString32Bytes FileName = new FixedString32Bytes(LogPropertyKeys.FileName);
            
            /// <summary>Burst-compatible version of <see cref="LogPropertyKeys.LineNumber"/>.</summary>
            public static readonly FixedString32Bytes LineNumber = new FixedString32Bytes(LogPropertyKeys.LineNumber);
            
            /// <summary>Burst-compatible version of <see cref="LogPropertyKeys.Exception"/>.</summary>
            public static readonly FixedString32Bytes Exception = new FixedString32Bytes(LogPropertyKeys.Exception);
            
            /// <summary>Burst-compatible version of <see cref="LogPropertyKeys.CorrelationId"/>.</summary>
            public static readonly FixedString32Bytes CorrelationId = new FixedString32Bytes(LogPropertyKeys.CorrelationId);
            
            /// <summary>Burst-compatible version of <see cref="LogPropertyKeys.SessionId"/>.</summary>
            public static readonly FixedString32Bytes SessionId = new FixedString32Bytes(LogPropertyKeys.SessionId);
            
            /// <summary>Burst-compatible version of <see cref="LogPropertyKeys.UserId"/>.</summary>
            public static readonly FixedString32Bytes UserId = new FixedString32Bytes(LogPropertyKeys.UserId);
            
            /// <summary>Burst-compatible version of <see cref="LogPropertyKeys.RequestId"/>.</summary>
            public static readonly FixedString32Bytes RequestId = new FixedString32Bytes(LogPropertyKeys.RequestId);
            
            /// <summary>Burst-compatible version of <see cref="LogPropertyKeys.Duration"/>.</summary>
            public static readonly FixedString32Bytes Duration = new FixedString32Bytes(LogPropertyKeys.Duration);
            
            /// <summary>Burst-compatible version of <see cref="LogPropertyKeys.MemoryUsage"/>.</summary>
            public static readonly FixedString32Bytes MemoryUsage = new FixedString32Bytes(LogPropertyKeys.MemoryUsage);
            
            /// <summary>Burst-compatible version of <see cref="LogPropertyKeys.CpuUsage"/>.</summary>
            public static readonly FixedString32Bytes CpuUsage = new FixedString32Bytes(LogPropertyKeys.CpuUsage);
            
            /// <summary>Burst-compatible version of <see cref="LogPropertyKeys.AppData"/>.</summary>
            public static readonly FixedString32Bytes AppData = new FixedString32Bytes(LogPropertyKeys.AppData);
            
            /// <summary>Burst-compatible version of <see cref="LogPropertyKeys.Environment"/>.</summary>
            public static readonly FixedString32Bytes Environment = new FixedString32Bytes(LogPropertyKeys.Environment);
            
            /// <summary>Burst-compatible version of <see cref="LogPropertyKeys.Version"/>.</summary>
            public static readonly FixedString32Bytes Version = new FixedString32Bytes(LogPropertyKeys.Version);
            
            /// <summary>Burst-compatible version of <see cref="LogPropertyKeys.Time"/>.</summary>
            public static readonly FixedString32Bytes Time = new FixedString32Bytes(LogPropertyKeys.Time);
            
            /// <summary>Burst-compatible version of <see cref="LogPropertyKeys.Count"/>.</summary>
            public static readonly FixedString32Bytes Count = new FixedString32Bytes(LogPropertyKeys.Count);
            
            /// <summary>Burst-compatible version of <see cref="LogPropertyKeys.Id"/>.</summary>
            public static readonly FixedString32Bytes Id = new FixedString32Bytes(LogPropertyKeys.Id);
            
            /// <summary>Burst-compatible version of <see cref="LogPropertyKeys.Name"/>.</summary>
            public static readonly FixedString32Bytes Name = new FixedString32Bytes(LogPropertyKeys.Name);
            
            /// <summary>Burst-compatible version of <see cref="LogPropertyKeys.Type"/>.</summary>
            public static readonly FixedString32Bytes Type = new FixedString32Bytes(LogPropertyKeys.Type);
            
            /// <summary>Burst-compatible version of <see cref="LogPropertyKeys.Value"/>.</summary>
            public static readonly FixedString32Bytes Value = new FixedString32Bytes(LogPropertyKeys.Value);
            
            /// <summary>Burst-compatible version of <see cref="LogPropertyKeys.Path"/>.</summary>
            public static readonly FixedString32Bytes Path = new FixedString32Bytes(LogPropertyKeys.Path);
            
            /// <summary>Burst-compatible version of <see cref="LogPropertyKeys.Error"/>.</summary>
            public static readonly FixedString32Bytes Error = new FixedString32Bytes(LogPropertyKeys.Error);
            
            /// <summary>Burst-compatible version of <see cref="LogPropertyKeys.Position"/>.</summary>
            public static readonly FixedString32Bytes Position = new FixedString32Bytes(LogPropertyKeys.Position);
            
            /// <summary>Burst-compatible version of <see cref="LogPropertyKeys.Status"/>.</summary>
            public static readonly FixedString32Bytes Status = new FixedString32Bytes(LogPropertyKeys.Status);
            
            /// <summary>Burst-compatible version of <see cref="LogPropertyKeys.State"/>.</summary>
            public static readonly FixedString32Bytes State = new FixedString32Bytes(LogPropertyKeys.State);
            
            /// <summary>Burst-compatible version of <see cref="LogPropertyKeys.Scene"/>.</summary>
            public static readonly FixedString32Bytes Scene = new FixedString32Bytes(LogPropertyKeys.Scene);
            
            /// <summary>Burst-compatible version of <see cref="LogPropertyKeys.Subsystem"/>.</summary>
            public static readonly FixedString32Bytes Subsystem = new FixedString32Bytes(LogPropertyKeys.Subsystem);
            
            /// <summary>Burst-compatible version of <see cref="LogPropertyKeys.Component"/>.</summary>
            public static readonly FixedString32Bytes Component = new FixedString32Bytes(LogPropertyKeys.Component);
        }
    }
}