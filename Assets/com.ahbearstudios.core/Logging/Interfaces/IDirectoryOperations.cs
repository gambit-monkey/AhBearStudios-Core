namespace AhBearStudios.Core.Logging.Interfaces
{
    /// <summary>
    /// Interface for directory operations, supports dependency injection and testing
    /// </summary>
    public interface IDirectoryOperations
    {
        /// <summary>
        /// Checks if a directory exists
        /// </summary>
        /// <param name="path">Directory path to check</param>
        /// <returns>True if directory exists, false otherwise</returns>
        bool DirectoryExists(string path);
            
        /// <summary>
        /// Creates a directory and any necessary parent directories
        /// </summary>
        /// <param name="path">Path of directory to create</param>
        void CreateDirectory(string path);
            
        /// <summary>
        /// Gets the directory name from a file path
        /// </summary>
        /// <param name="path">File path to process</param>
        /// <returns>Directory containing the file</returns>
        string GetDirectoryName(string path);
            
        /// <summary>
        /// Checks if a path is a fully qualified (rooted) path
        /// </summary>
        /// <param name="path">Path to check</param>
        /// <returns>True if path is rooted, false otherwise</returns>
        bool IsPathRooted(string path);
    }
}