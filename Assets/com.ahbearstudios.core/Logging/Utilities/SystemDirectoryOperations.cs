using System.IO;
using AhBearStudios.Core.Logging.Interfaces;

namespace AhBearStudios.Core.Logging.Utilities
{
    /// <summary>
    /// Implementation of directory operations using System.IO
    /// </summary>
    public class SystemDirectoryOperations : IDirectoryOperations
    {
        /// <inheritdoc />
        public bool DirectoryExists(string path) => Directory.Exists(path);
            
        /// <inheritdoc />
        public void CreateDirectory(string path) => Directory.CreateDirectory(path);
            
        /// <inheritdoc />
        public string GetDirectoryName(string path) => Path.GetDirectoryName(path);
            
        /// <inheritdoc />
        public bool IsPathRooted(string path) => Path.IsPathRooted(path);
    }
}