using System;
using AhBearStudios.Core.Logging.Configuration;
using AhBearStudios.Core.Logging.Interfaces;
using AhBearStudios.Core.Logging.LogTargets;

namespace AhBearStudios.Core.Logging.Factories
{
    /// <summary>
    /// Factory for creating log target instances from configuration objects.
    /// Provides a centralized way to instantiate log targets while maintaining
    /// separation of concerns between configuration and implementation.
    /// </summary>
    public static class LogTargetFactory
    {
        /// <summary>
        /// Creates a Serilog file log target from the provided configuration.
        /// </summary>
        /// <param name="targetConfig">The Serilog file configuration.</param>
        /// <returns>A configured ILogTarget instance for Serilog file logging.</returns>
        /// <exception cref="ArgumentNullException">Thrown when targetConfig is null.</exception>
        public static ILogTarget CreateSerilogTarget(SerilogFileTargetConfig targetConfig)
        {
            if (targetConfig == null)
                throw new ArgumentNullException(nameof(targetConfig));
                
            return new SerilogTarget(targetConfig);
        }
        
        /// <summary>
        /// Creates a Unity console log target from the provided configuration.
        /// </summary>
        /// <param name="config">The Unity console configuration.</param>
        /// <returns>A configured ILogTarget instance for Unity console logging.</returns>
        /// <exception cref="ArgumentNullException">Thrown when config is null.</exception>
        public static ILogTarget CreateUnityConsoleTarget(UnityConsoleTargetConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
                
            return new UnityConsoleTarget(config);
        }
        
        /// <summary>
        /// Creates a log target from a generic log target configuration.
        /// This method determines the appropriate target type based on the configuration type.
        /// </summary>
        /// <param name="config">The log target configuration.</param>
        /// <returns>A configured ILogTarget instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when config is null.</exception>
        /// <exception cref="NotSupportedException">Thrown when the configuration type is not supported.</exception>
        public static ILogTarget CreateFromConfig(ILogTargetConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
                
            return config switch
            {
                SerilogFileTargetConfig serilogConfig => CreateSerilogTarget(serilogConfig),
                UnityConsoleTargetConfig unityConfig => CreateUnityConsoleTarget(unityConfig),
                _ => throw new NotSupportedException($"Configuration type {config.GetType().Name} is not supported")
            };
        }
    }
}