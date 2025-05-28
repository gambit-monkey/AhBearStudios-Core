using AhBearStudios.Core.Logging.Tags;
using UnityEngine;

namespace AhBearStudios.Core.Logging.Data
{
    /// <summary>
    /// Represents a tag-specific log level override.
    /// </summary>
    [System.Serializable]
    public struct TagLevelOverride
    {
        [SerializeField]
        private Tagging.LogTag _tag;
        
        [SerializeField]
        private LogLevel _level;
        
        public Tagging.LogTag Tag => _tag;
        public LogLevel Level => _level;
    }
}