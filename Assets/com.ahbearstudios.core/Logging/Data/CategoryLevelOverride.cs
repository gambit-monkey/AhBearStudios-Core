using System;
using UnityEngine;

namespace AhBearStudios.Core.Logging.Data
{
    /// <summary>
    /// Represents a category-specific log level override.
    /// Allows specific log categories (custom string identifiers) to have different minimum levels.
    /// </summary>
    [Serializable]
    public class CategoryLevelOverride : IEquatable<CategoryLevelOverride>
    {
        [SerializeField, Tooltip("The category name to apply this override to")]
        private string _category = "";
        
        [SerializeField, Tooltip("The minimum log level for this category")]
        private LogLevel _level = LogLevel.Debug;
        
        /// <summary>
        /// Gets or sets the category name to override.
        /// </summary>
        public string Category 
        { 
            get => _category ?? ""; 
            set => _category = value ?? ""; 
        }
        
        /// <summary>
        /// Gets or sets the minimum log level for this category.
        /// </summary>
        public LogLevel Level 
        { 
            get => _level; 
            set => _level = value; 
        }
        
        /// <summary>
        /// Gets whether this override has a valid category name.
        /// </summary>
        public bool IsValid => !string.IsNullOrWhiteSpace(_category);
        
        /// <summary>
        /// Creates a new empty category level override.
        /// </summary>
        public CategoryLevelOverride()
        {
        }
        
        /// <summary>
        /// Creates a new category level override with the specified values.
        /// </summary>
        /// <param name="category">The category to override.</param>
        /// <param name="level">The minimum log level for this category.</param>
        public CategoryLevelOverride(string category, LogLevel level)
        {
            _category = category ?? "";
            _level = level;
        }
        
        /// <summary>
        /// Checks if this override applies to the specified category.
        /// Uses case-insensitive comparison.
        /// </summary>
        /// <param name="category">The category to check.</param>
        /// <returns>True if this override applies to the category.</returns>
        public bool AppliesTo(string category)
        {
            if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(_category))
                return false;
                
            return string.Equals(_category, category, StringComparison.OrdinalIgnoreCase);
        }
        
        /// <summary>
        /// Checks if the specified log level meets this override's requirements.
        /// </summary>
        /// <param name="level">The log level to check.</param>
        /// <returns>True if the level meets or exceeds the override level.</returns>
        public bool IsLevelSufficient(LogLevel level)
        {
            return level >= _level;
        }
        
        /// <summary>
        /// Checks if this override matches the specified category using different comparison modes.
        /// </summary>
        /// <param name="category">The category to check.</param>
        /// <param name="comparisonType">The type of string comparison to use.</param>
        /// <returns>True if this override applies to the category.</returns>
        public bool AppliesTo(string category, StringComparison comparisonType)
        {
            if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(_category))
                return false;
                
            return string.Equals(_category, category, comparisonType);
        }
        
        /// <summary>
        /// Checks if this override matches any of the specified categories.
        /// </summary>
        /// <param name="categories">The categories to check.</param>
        /// <returns>True if this override applies to any of the categories.</returns>
        public bool AppliesToAny(params string[] categories)
        {
            if (categories == null || categories.Length == 0)
                return false;
                
            foreach (var category in categories)
            {
                if (AppliesTo(category))
                    return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Creates a copy of this override.
        /// </summary>
        /// <returns>A new CategoryLevelOverride with the same values.</returns>
        public CategoryLevelOverride Clone()
        {
            return new CategoryLevelOverride(_category, _level);
        }
        
        /// <summary>
        /// Determines whether the specified CategoryLevelOverride is equal to this instance.
        /// </summary>
        /// <param name="other">The CategoryLevelOverride to compare with this instance.</param>
        /// <returns>True if the specified CategoryLevelOverride is equal to this instance; otherwise, false.</returns>
        public bool Equals(CategoryLevelOverride other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(_category, other._category, StringComparison.OrdinalIgnoreCase) && 
                   _level == other._level;
        }
        
        /// <summary>
        /// Determines whether the specified object is equal to this instance.
        /// </summary>
        /// <param name="obj">The object to compare with this instance.</param>
        /// <returns>True if the specified object is equal to this instance; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as CategoryLevelOverride);
        }
        
        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var categoryHash = _category?.ToLowerInvariant().GetHashCode() ?? 0;
                return (categoryHash * 397) ^ (int)_level;
            }
        }
        
        /// <summary>
        /// Implements the equality operator.
        /// </summary>
        public static bool operator ==(CategoryLevelOverride left, CategoryLevelOverride right)
        {
            return Equals(left, right);
        }
        
        /// <summary>
        /// Implements the inequality operator.
        /// </summary>
        public static bool operator !=(CategoryLevelOverride left, CategoryLevelOverride right)
        {
            return !Equals(left, right);
        }
        
        /// <summary>
        /// Returns a string representation of this override.
        /// </summary>
        /// <returns>A string that represents this override.</returns>
        public override string ToString()
        {
            return $"Category '{_category}' -> {_level}";
        }
        
        /// <summary>
        /// Returns a detailed description of this override.
        /// </summary>
        /// <returns>A detailed string description.</returns>
        public string GetDetailedDescription()
        {
            if (!IsValid)
                return "Invalid category override (empty category name)";
                
            return $"Category Override: '{_category}' requires minimum level {_level}";
        }
        
        /// <summary>
        /// Gets suggested categories based on common logging patterns.
        /// </summary>
        /// <returns>An array of commonly used category names.</returns>
        public static string[] GetSuggestedCategories()
        {
            return new[]
            {
                "Database",
                "Network",
                "Authentication", 
                "Authorization",
                "Performance",
                "Security",
                "Cache",
                "FileSystem",
                "Configuration",
                "Initialization",
                "Cleanup",
                "Validation",
                "Processing",
                "Integration",
                "External",
                "Internal",
                "Business",
                "Technical",
                "UserInterface",
                "Background"
            };
        }
    }
}