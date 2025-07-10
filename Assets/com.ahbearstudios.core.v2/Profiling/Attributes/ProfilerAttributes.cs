using System;
using Unity.Profiling;

namespace AhBearStudios.Core.Profiling.Attributes
{
    /// <summary>
    /// Attribute to enable profiling on a method
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ProfileMethodAttribute : Attribute
    {
        /// <summary>
        /// Category name for this profiled method
        /// </summary>
        public string CategoryName { get; }
        
        /// <summary>
        /// Custom category name (if using a custom category)
        /// </summary>
        public string CustomCategoryName { get; }
        
        /// <summary>
        /// Custom name for this profiled method (optional)
        /// If not specified, the method name will be used
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// Create a profiler attribute for a method with a standard category
        /// </summary>
        /// <param name="categoryName">Category name from ProfilerCategories constants</param>
        public ProfileMethodAttribute(string categoryName)
        {
            CategoryName = categoryName;
            CustomCategoryName = null;
            Name = null;
        }
        
        /// <summary>
        /// Create a profiler attribute for a method with a standard category and custom name
        /// </summary>
        /// <param name="categoryName">Category name from ProfilerCategories constants</param>
        /// <param name="name">Custom name for this profiled method</param>
        public ProfileMethodAttribute(string categoryName, string name)
        {
            CategoryName = categoryName;
            CustomCategoryName = null;
            Name = name;
        }
        
        /// <summary>
        /// Create a profiler attribute for a method with a custom category
        /// </summary>
        /// <param name="customCategoryName">Custom category name</param>
        /// <param name="name">Custom name for this profiled method</param>
        /// <param name="isCustomCategory">Must be true to indicate this is a custom category</param>
        public ProfileMethodAttribute(string customCategoryName, string name, bool isCustomCategory)
        {
            if (!isCustomCategory)
            {
                CategoryName = customCategoryName;
                CustomCategoryName = null;
            }
            else
            {
                CategoryName = null;
                CustomCategoryName = customCategoryName;
            }
            Name = name;
        }
        
        /// <summary>
        /// Gets the ProfilerCategory for this attribute
        /// </summary>
        /// <returns>The ProfilerCategory corresponding to the category name</returns>
        public ProfilerCategory GetCategory()
        {
            if (CustomCategoryName != null)
            {
                // Create a custom ProfilerCategory
                return new ProfilerCategory(CustomCategoryName);
            }
            
            return ProfilerCategories.GetCategory(CategoryName);
        }
    }
    
    /// <summary>
    /// Attribute to enable profiling on all methods of a class
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class ProfileClassAttribute : Attribute
    {
        /// <summary>
        /// Category name for all methods in this class
        /// </summary>
        public string CategoryName { get; }
        
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
        /// <param name="categoryName">Category name for all methods in this class</param>
        public ProfileClassAttribute(string categoryName)
        {
            CategoryName = categoryName;
            Prefix = null;
            IncludeInherited = false;
            IncludePrivate = false;
        }
        
        /// <summary>
        /// Create a profiler attribute for a class with a prefix
        /// </summary>
        /// <param name="categoryName">Category name for all methods in this class</param>
        /// <param name="prefix">Prefix to add to all method names</param>
        /// <param name="includeInherited">Whether to profile inherited methods</param>
        /// <param name="includePrivate">Whether to profile private methods</param>
        public ProfileClassAttribute(string categoryName, string prefix, bool includeInherited = false, bool includePrivate = false)
        {
            CategoryName = categoryName;
            Prefix = prefix;
            IncludeInherited = includeInherited;
            IncludePrivate = includePrivate;
        }
        
        /// <summary>
        /// Gets the ProfilerCategory for this attribute
        /// </summary>
        /// <returns>The ProfilerCategory corresponding to the category name</returns>
        public ProfilerCategory GetCategory()
        {
            return new ProfilerCategory(CategoryName);
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