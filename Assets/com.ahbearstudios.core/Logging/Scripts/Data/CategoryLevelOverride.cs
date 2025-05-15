using UnityEngine;

namespace AhBearStudios.Core.Logging.Data
{
    /// <summary>
    /// Represents a category-specific log level override.
    /// </summary>
    [System.Serializable]
    public struct CategoryLevelOverride
    {
        [SerializeField]
        private string _category;
        
        [SerializeField]
        private byte _level;
        
        public string Category => _category;
        public byte Level => _level;
    }
}