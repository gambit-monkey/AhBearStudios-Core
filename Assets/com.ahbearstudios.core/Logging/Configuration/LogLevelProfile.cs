using AhBearStudios.Core.Logging.Data;
using UnityEngine;

namespace AhBearStudios.Core.Logging.Config
{
    /// <summary>
    /// Represents a log level profile that can be saved and loaded.
    /// </summary>
    [CreateAssetMenu(fileName = "LogLevelProfile", menuName = "AhBearStudios/Logging/Log Level Profile", order = 1)]
    public class LogLevelProfile : ScriptableObject
    {
        [SerializeField, Tooltip("Global minimum log level")]
        private byte _globalMinimumLevel = LogLevel.Debug;
        
        [SerializeField, Tooltip("Tag-specific level overrides")]
        private TagLevelOverride[] _tagLevelOverrides = new TagLevelOverride[0];
        
        [SerializeField, Tooltip("Category-specific level overrides")]
        private CategoryLevelOverride[] _categoryLevelOverrides = new CategoryLevelOverride[0];
        
        /// <summary>
        /// Global minimum log level.
        /// </summary>
        public byte GlobalMinimumLevel => _globalMinimumLevel;
        
        /// <summary>
        /// Tag-specific level overrides.
        /// </summary>
        public TagLevelOverride[] TagLevelOverrides => _tagLevelOverrides;
        
        /// <summary>
        /// Category-specific level overrides.
        /// </summary>
        public CategoryLevelOverride[] CategoryLevelOverrides => _categoryLevelOverrides;
    }
}