using System;

namespace AhBearStudios.Core.Logging.Extensions
{
    /// <summary>
    /// Extension methods for the LogLevel enum, providing type conversion utilities.
    /// </summary>
    public static class LogLevelExtensions
    {
        /// <summary>
        /// Converts a LogLevel enum value to an integer.
        /// </summary>
        /// <param name="level">The LogLevel enum value to convert.</param>
        /// <returns>The integer representation of the LogLevel.</returns>
        public static int ToInt(this LogLevel level)
        {
            return (int)level;
        }
        
        /// <summary>
        /// Converts a LogLevel enum value to a byte.
        /// </summary>
        /// <param name="level">The LogLevel enum value to convert.</param>
        /// <returns>The byte representation of the LogLevel.</returns>
        public static byte ToByte(this LogLevel level)
        {
            return (byte)level;
        }
        
        /// <summary>
        /// Creates a bit mask for the specified LogLevel.
        /// Useful for bitwise operations involving LogLevel values.
        /// </summary>
        /// <param name="level">The LogLevel to create a bit mask for.</param>
        /// <returns>An integer with the bit corresponding to the LogLevel set.</returns>
        public static int ToBitMask(this LogLevel level)
        {
            return 1 << (int)level;
        }
        
        /// <summary>
        /// Converts an integer value to a LogLevel enum value.
        /// </summary>
        /// <param name="value">The integer value to convert.</param>
        /// <returns>The corresponding LogLevel enum value.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the value is outside the valid LogLevel range.</exception>
        public static LogLevel ToLogLevel(this int value)
        {
            if (Enum.IsDefined(typeof(LogLevel), value))
            {
                return (LogLevel)value;
            }
            
            throw new ArgumentOutOfRangeException(nameof(value), $"Value {value} is not a valid LogLevel.");
        }
        
        /// <summary>
        /// Converts a byte value to a LogLevel enum value.
        /// </summary>
        /// <param name="value">The byte value to convert.</param>
        /// <returns>The corresponding LogLevel enum value.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the value is outside the valid LogLevel range.</exception>
        public static LogLevel ToLogLevel(this byte value)
        {
            if (Enum.IsDefined(typeof(LogLevel), value))
            {
                return (LogLevel)value;
            }
            
            throw new ArgumentOutOfRangeException(nameof(value), $"Value {value} is not a valid LogLevel.");
        }
        
        /// <summary>
        /// Gets a human-readable name for a log level.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <returns>A human-readable name.</returns>
        public static string GetName(this LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Debug: return "Debug";
                case LogLevel.Info: return "Info";
                case LogLevel.Warning: return "Warning";
                case LogLevel.Error: return "Error";
                case LogLevel.Critical: return "Critical";
                default: return $"Level {(int)level}";
            }
        }
    }
}