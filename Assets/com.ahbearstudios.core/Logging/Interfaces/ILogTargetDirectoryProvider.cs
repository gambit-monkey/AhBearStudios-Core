using System.Collections.Generic;

namespace AhBearStudios.Core.Logging.Interfaces
{
    /// <summary>
    /// Interface for log targets that require directory creation.
    /// Provides a clean way to extract directory paths from log target configurations.
    /// </summary>
    public interface ILogTargetDirectoryProvider
    {
        /// <summary>
        /// Gets all directory paths that this log target requires to exist.
        /// </summary>
        /// <returns>Collection of directory paths that need to be created.</returns>
        IEnumerable<string> GetRequiredDirectories();
        
        /// <summary>
        /// Gets whether this log target requires any directories to be created.
        /// </summary>
        bool RequiresDirectories { get; }
    }
}