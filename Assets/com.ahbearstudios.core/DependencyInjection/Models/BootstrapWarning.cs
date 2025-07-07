namespace AhBearStudios.Core.DependencyInjection.Models
{
    /// <summary>
    /// Represents a warning that occurred during bootstrap.
    /// </summary>
    public sealed class BootstrapWarning
    {
        /// <summary>
        /// Gets the installer name where the warning occurred.
        /// </summary>
        public string InstallerName { get; }
        
        /// <summary>
        /// Gets the warning message.
        /// </summary>
        public string Message { get; }
        
        /// <summary>
        /// Gets the bootstrap phase where the warning occurred.
        /// </summary>
        public BootstrapPhase Phase { get; }
        
        /// <summary>
        /// Initializes a new bootstrap warning.
        /// </summary>
        public BootstrapWarning(string installerName, string message, BootstrapPhase phase)
        {
            InstallerName = installerName ?? throw new ArgumentNullException(nameof(installerName));
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Phase = phase;
        }
        
        /// <summary>
        /// Returns a string representation of this warning.
        /// </summary>
        public override string ToString()
        {
            return $"[{Phase}] {InstallerName}: {Message}";
        }
    }
}