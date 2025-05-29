using AhBearStudios.Core.Logging.Configuration;

namespace AhBearStudios.Core.Logging.Builders
{
    /// <summary>
    /// Factory for creating various log configuration builders
    /// </summary>
    public static class LogConfigBuilderFactory
    {
        /// <summary>
        /// Creates a Serilog file configuration builder with default settings
        /// </summary>
        public static SerilogFileConfigBuilder SerilogFile(string filePath = "Logs/app.log")
        {
            return new SerilogFileConfigBuilder()
                .WithLogFilePath(filePath)
                .WithTargetName("SerilogFile");
        }

        /// <summary>
        /// Creates a high-performance Serilog file configuration
        /// </summary>
        public static SerilogFileConfigBuilder SerilogFileHighPerformance(string filePath = "Logs/app.log")
        {
            return new SerilogFileConfigBuilder()
                .WithLogFilePath(filePath)
                .WithTargetName("SerilogFileHP")
                .AsHighPerformance();
        }

        /// <summary>
        /// Creates a debug Serilog file configuration
        /// </summary>
        public static SerilogFileConfigBuilder SerilogFileDebug(string filePath = "Logs/debug.log")
        {
            return new SerilogFileConfigBuilder()
                .WithLogFilePath(filePath)
                .WithTargetName("SerilogFileDebug")
                .AsDebug();
        }

        /// <summary>
        /// Creates a Unity console configuration builder
        /// </summary>
        public static UnityConsoleConfigBuilder UnityConsole()
        {
            return new UnityConsoleConfigBuilder()
                .WithTargetName("UnityConsole");
        }

        /// <summary>
        /// Creates a Unity console configuration for development
        /// </summary>
        public static UnityConsoleConfigBuilder UnityConsoleDevelopment()
        {
            return new UnityConsoleConfigBuilder()
                .WithTargetName("UnityConsoleDev")
                .AsDevelopment();
        }

        /// <summary>
        /// Creates a Unity console configuration for production
        /// </summary>
        public static UnityConsoleConfigBuilder UnityConsoleProduction()
        {
            return new UnityConsoleConfigBuilder()
                .WithTargetName("UnityConsoleProd")
                .AsProduction();
        }

        /// <summary>
        /// Creates a combined file and console setup
        /// </summary>
        public static (SerilogFileConfigBuilder file, UnityConsoleConfigBuilder console) Combined(
            string filePath = "Logs/app.log", 
            LogLevel fileLevel = LogLevel.Debug, 
            LogLevel consoleLevel = LogLevel.Info)
        {
            var fileBuilder = SerilogFile(filePath).WithMinimumLevel(fileLevel);
            var consoleBuilder = UnityConsole().WithMinimumLevel(consoleLevel);
            
            return (fileBuilder, consoleBuilder);
        }

        /// <summary>
        /// Creates builder from existing ScriptableObject config
        /// </summary>
        public static SerilogFileConfigBuilder FromExisting(SerilogFileConfig config)
        {
            return new SerilogFileConfigBuilder().FromExisting(config);
        }

        /// <summary>
        /// Creates builder from existing ScriptableObject config
        /// </summary>
        public static UnityConsoleConfigBuilder FromExisting(UnityConsoleLogConfig config)
        {
            return new UnityConsoleConfigBuilder().FromExisting(config);
        }
    }
}