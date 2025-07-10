namespace AhBearStudios.Core.Logging.Interfaces
{
    /// <summary>
    /// Interface for log target configuration builders that implement the fluent pattern
    /// </summary>
    /// <typeparam name="TConfig">The configuration type being built</typeparam>
    /// <typeparam name="TBuilder">The builder type itself (for method chaining)</typeparam>
    public interface ILogTargetConfigBuilder<TConfig, TBuilder> 
        where TConfig : ILogTargetConfig
        where TBuilder : ILogTargetConfigBuilder<TConfig, TBuilder>
    {
        /// <summary>
        /// Gets or sets the configuration ID
        /// </summary>
        string ConfigId { get; set; }

        /// <summary>
        /// Sets the target name
        /// </summary>
        TBuilder WithTargetName(string name);
        
        /// <summary>
        /// Sets whether the target is enabled
        /// </summary>
        TBuilder WithEnabled(bool enabled);
        
        /// <summary>
        /// Sets the minimum log level
        /// </summary>
        TBuilder WithMinimumLevel(LogLevel level);
        
        /// <summary>
        /// Sets tag filtering options
        /// </summary>
        TBuilder WithTagFilters(string[] includedTags, string[] excludedTags, bool processUntagged = true);
        
        /// <summary>
        /// Sets timestamp formatting options
        /// </summary>
        TBuilder WithTimestamps(bool include, string format = "yyyy-MM-dd HH:mm:ss.fff");
        
        /// <summary>
        /// Sets performance options
        /// </summary>
        TBuilder WithPerformance(bool autoFlush = true, int bufferSize = 0, float flushInterval = 0f);
        
        /// <summary>
        /// Sets Unity integration options
        /// </summary>
        TBuilder WithUnityIntegration(bool captureUnityLogs = true);
        
        /// <summary>
        /// Sets message formatting options
        /// </summary>
        TBuilder WithMessageFormatting(bool includeStackTraces = true, bool includeSourceContext = true, bool includeThreadId = false);
        
        /// <summary>
        /// Sets structured logging options
        /// </summary>
        TBuilder WithStructuredLogging(bool enabled = false);
        
        /// <summary>
        /// Sets message length limiting options
        /// </summary>
        TBuilder WithMessageLengthLimit(bool limitLength = false, int maxLength = 8192);
        
        /// <summary>
        /// Builds the configuration
        /// </summary>
        TConfig Build();
    }
}