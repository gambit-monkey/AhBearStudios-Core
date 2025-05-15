using System;

namespace AhBearStudios.Core.Profiling.Attributes
{
    /// <summary>
    /// Attribute to enable profiling on a method
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ProfileMethodAttribute : Attribute
    {
        /// <summary>
        /// Category for this profiled method
        /// </summary>
        public ProfilerCategory Category { get; }
        
        /// <summary>
        /// Custom name for this profiled method (optional)
        /// If not specified, the method name will be used
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// Create a profiler attribute for a method
        /// </summary>
        /// <param name="category">Category for this profiled method</param>
        public ProfileMethodAttribute(ProfilerCategory category)
        {
            Category = category;
            Name = null;
        }
        
        /// <summary>
        /// Create a profiler attribute for a method with a custom name
        /// </summary>
        /// <param name="category">Category for this profiled method</param>
        /// <param name="name">Custom name for this profiled method</param>
        public ProfileMethodAttribute(ProfilerCategory category, string name)
        {
            Category = category;
            Name = name;
        }
    }
    
    /// <summary>
    /// Attribute to enable profiling on all methods of a class
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class ProfileClassAttribute : Attribute
    {
        /// <summary>
        /// Category for all methods in this class
        /// </summary>
        public ProfilerCategory Category { get; }
        
        /// <summary>
        /// Prefix to add to all method names (optional)
        /// </summary>
        public string Prefix { get; }
        
        /// <summary>
        /// Whether to profile inherited methods
        /// </summary>
        public bool IncludeInherited { get; }
        
        /// <summary>
        /// Whether to profile private methods
        /// </summary>
        public bool IncludePrivate { get; }
        
        /// <summary>
        /// Create a profiler attribute for a class
        /// </summary>
        /// <param name="category">Category for all methods in this class</param>
        public ProfileClassAttribute(ProfilerCategory category)
        {
            Category = category;
            Prefix = null;
            IncludeInherited = false;
            IncludePrivate = false;
        }
        
        /// <summary>
        /// Create a profiler attribute for a class with a prefix
        /// </summary>
        /// <param name="category">Category for all methods in this class</param>
        /// <param name="prefix">Prefix to add to all method names</param>
        /// <param name="includeInherited">Whether to profile inherited methods</param>
        /// <param name="includePrivate">Whether to profile private methods</param>
        public ProfileClassAttribute(ProfilerCategory category, string prefix, bool includeInherited = false, bool includePrivate = false)
        {
            Category = category;
            Prefix = prefix;
            IncludeInherited = includeInherited;
            IncludePrivate = includePrivate;
        }
    }
    
    /// <summary>
    /// Attribute to exclude a method from class-wide profiling
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class DoNotProfileAttribute : Attribute
    {
    }
}