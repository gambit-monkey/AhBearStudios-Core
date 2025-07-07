namespace AhBearStudios.Core.DependencyInjection.Models
{
    /// <summary>
    /// Represents an error that occurred during bootstrap.
    /// </summary>
    public sealed class BootstrapError
    {
        /// <summary>
        /// Gets the installer name where the error occurred.
        /// </summary>
        public string InstallerName { get; }
        
        /// <summary>
        /// Gets the error message.
        /// </summary>
        public string Message { get; }
        
        /// <summary>
        /// Gets the underlying exception if any.
        /// </summary>
        public Exception Exception { get; }
        
        /// <summary>
        /// Gets the bootstrap phase where the error occurred.
        /// </summary>
        public BootstrapPhase Phase { get; }
        
        /// <summary>
        /// Initializes a new bootstrap error.
        /// </summary>
        public BootstrapError(
            string installerName,
            string message,
            BootstrapPhase phase,
            Exception exception = null)
        {
            InstallerName = installerName ?? throw new ArgumentNullException(nameof(installerName));
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Phase = phase;
            Exception = exception;
        }
        
        /// <summary>
        /// Returns a string representation of this error.
        /// </summary>
        public override string ToString()
        {
            return $"[{Phase}] {InstallerName}: {Message}";
        }
    }
}