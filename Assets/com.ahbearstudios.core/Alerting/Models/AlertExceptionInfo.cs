using System;
using Unity.Collections;

namespace AhBearStudios.Core.Alerting.Models
{
    /// <summary>
    /// Exception information for alerts using FixedString for performance.
    /// </summary>
    public sealed partial record AlertExceptionInfo
    {
        /// <summary>
        /// Exception type name.
        /// </summary>
        public FixedString128Bytes TypeName { get; init; }

        /// <summary>
        /// Exception message.
        /// </summary>
        public FixedString512Bytes Message { get; init; }

        /// <summary>
        /// Stack trace information (truncated for performance).
        /// </summary>
        public FixedString4096Bytes StackTrace { get; init; }

        /// <summary>
        /// Inner exception type if present.
        /// </summary>
        public FixedString128Bytes InnerExceptionType { get; init; }

        /// <summary>
        /// Creates exception info from an exception.
        /// </summary>
        /// <param name="exception">Exception to convert</param>
        /// <returns>Alert exception info</returns>
        public static AlertExceptionInfo FromException(Exception exception)
        {
            if (exception == null) return new AlertExceptionInfo();

            var stackTrace = exception.StackTrace ?? string.Empty;
            
            return new AlertExceptionInfo
            {
                TypeName = exception.GetType().Name,
                Message = exception.Message.Length <= 512 ? exception.Message : exception.Message[..512],
                StackTrace = stackTrace.Length <= 4096 ? stackTrace : stackTrace[..4096],
                InnerExceptionType = exception.InnerException?.GetType().Name ?? string.Empty
            };
        }
    }
}