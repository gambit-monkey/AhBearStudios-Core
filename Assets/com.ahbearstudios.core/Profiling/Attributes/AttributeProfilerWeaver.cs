using System.Reflection;
using Unity.Profiling;
using System.Collections.Concurrent;
using AhBearStudios.Core.Profiling.Unity;

namespace AhBearStudios.Core.Profiling.Attributes
{
    /// <summary>
    /// Provides attribute-based method profiling through IL weaving or reflection-based proxy generation
    /// </summary>
    public class AttributeProfilerWeaver
    {
        // Singleton instance
        private static AttributeProfilerWeaver _instance;
        
        // Cache for created profiler markers to avoid duplicate creation
        private readonly ConcurrentDictionary<string, ProfilerMarker> _markerCache =
            new ConcurrentDictionary<string, ProfilerMarker>();
            
        // Reference to profiler manager
        private readonly ProfileManager _profileManager;
        
        /// <summary>
        /// Get singleton instance
        /// </summary>
        public static AttributeProfilerWeaver Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new AttributeProfilerWeaver();
                }
                
                return _instance;
            }
        }
        
        /// <summary>
        /// Create a new attribute profiler weaver
        /// </summary>
        private AttributeProfilerWeaver()
        {
            _profileManager = ProfileManager.Instance;
        }
        
        /// <summary>
        /// Start a profiling session for a method
        /// </summary>
        /// <param name="methodBase">Method being profiled</param>
        /// <returns>Profiler session to be disposed</returns>
        public ProfilerSession BeginMethodProfile(MethodBase methodBase)
        {
            // Skip if profiling is not enabled
            if (!_profileManager.IsEnabled)
                return null;
                
            // Try to get method attribute
            var methodAttr = methodBase.GetCustomAttribute<ProfileMethodAttribute>();
            if (methodAttr != null)
            {
                var tag = CreateTagForMethod(methodBase, methodAttr);
                return _profileManager.BeginScope(tag);
            }
            
            // Try to get class attribute
            var type = methodBase.DeclaringType;
            if (type != null)
            {
                var classAttr = type.GetCustomAttribute<ProfileClassAttribute>();
                if (classAttr != null)
                {
                    // Check if method should be excluded
                    if (methodBase.GetCustomAttribute<DoNotProfileAttribute>() == null)
                    {
                        var tag = CreateTagForMethodFromClass(methodBase, classAttr);
                        return _profileManager.BeginScope(tag);
                    }
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Get a direct ProfilerMarker for a method (for manual begin/end usage)
        /// </summary>
        /// <param name="methodBase">Method to profile</param>
        /// <returns>ProfilerMarker for manual usage</returns>
        public ProfilerMarker GetMarkerForMethod(MethodBase methodBase)
        {
            // Try to get method attribute
            var methodAttr = methodBase.GetCustomAttribute<ProfileMethodAttribute>();
            if (methodAttr != null)
            {
                var tag = CreateTagForMethod(methodBase, methodAttr);
                return GetOrCreateMarker(tag.FullName);
            }
            
            // Try to get class attribute
            var type = methodBase.DeclaringType;
            if (type != null)
            {
                var classAttr = type.GetCustomAttribute<ProfileClassAttribute>();
                if (classAttr != null)
                {
                    // Check if method should be excluded
                    if (methodBase.GetCustomAttribute<DoNotProfileAttribute>() == null)
                    {
                        var tag = CreateTagForMethodFromClass(methodBase, classAttr);
                        return GetOrCreateMarker(tag.FullName);
                    }
                }
            }
            
            // Fallback, create default marker
            return GetOrCreateMarker($"Uncategorized.{methodBase.DeclaringType.Name}.{methodBase.Name}");
        }
        
        /// <summary>
        /// Create a tag for a method with a method attribute
        /// </summary>
        private ProfilerTag CreateTagForMethod(MethodBase methodBase, ProfileMethodAttribute attr)
        {
            string name = attr.Name;
            if (string.IsNullOrEmpty(name))
            {
                name = methodBase.Name;
            }
    
            // Get the category from the attribute
            ProfilerCategory category = attr.GetCategory();
            return new ProfilerTag(category, $"{methodBase.DeclaringType.Name}.{name}");
        }

        /// <summary>
        /// Create a tag for a method from a class attribute
        /// </summary>
        private ProfilerTag CreateTagForMethodFromClass(MethodBase methodBase, ProfileClassAttribute attr)
        {
            string prefix = string.IsNullOrEmpty(attr.Prefix) ? methodBase.DeclaringType.Name : attr.Prefix;
    
            // Convert the category name to a ProfilerCategory using our helper class
            ProfilerCategory category = ProfilerCategories.GetCategory(attr.CategoryName);
            return new ProfilerTag(category, $"{prefix}.{methodBase.Name}");
        }
        
        /// <summary>
        /// Get an existing marker or create a new one
        /// </summary>
        private ProfilerMarker GetOrCreateMarker(string name)
        {
            return _markerCache.GetOrAdd(name, n => new ProfilerMarker(n));
        }
    }
}