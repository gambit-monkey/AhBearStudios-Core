using System;

namespace AhBearStudios.Core.Alerting.Models
{
    /// <summary>
    /// Error handling settings for filters.
    /// </summary>
    public sealed class FilterErrorHandling
    {
        /// <summary>
        /// How to handle filter errors.
        /// </summary>
        public ErrorHandlingMode ErrorMode { get; set; } = ErrorHandlingMode.LogAndContinue;

        /// <summary>
        /// Maximum consecutive errors before disabling filter.
        /// </summary>
        public int MaxConsecutiveErrors { get; set; } = 5;

        /// <summary>
        /// Whether to retry failed operations.
        /// </summary>
        public bool EnableRetry { get; set; } = true;

        /// <summary>
        /// Retry delay for failed operations.
        /// </summary>
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Validates the error handling settings.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when settings are invalid.</exception>
        public void Validate()
        {
            if (MaxConsecutiveErrors < 0)
                throw new InvalidOperationException("Max consecutive errors cannot be negative.");

            if (RetryDelay < TimeSpan.Zero)
                throw new InvalidOperationException("Retry delay cannot be negative.");
        }
    }
}