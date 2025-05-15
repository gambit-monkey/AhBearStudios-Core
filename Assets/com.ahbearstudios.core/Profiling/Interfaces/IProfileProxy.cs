using System.Reflection;

namespace AhBearStudios.Core.Profiling.Interfaces
{
    /// <summary>
    /// Interface for profiling proxies
    /// </summary>
    public interface IProfileProxy
    {
        /// <summary>
        /// Original method this proxy wraps
        /// </summary>
        MethodInfo TargetMethod { get; }
        
        /// <summary>
        /// Profiler tag used for this method
        /// </summary>
        ProfilerTag Tag { get; }
        
        /// <summary>
        /// Apply this proxy to an instance
        /// </summary>
        void ApplyToInstance(object instance);
    }
}