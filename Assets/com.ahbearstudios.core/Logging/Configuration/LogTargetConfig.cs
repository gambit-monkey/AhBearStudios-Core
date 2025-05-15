using UnityEngine;
using AhBearStudios.Core.Logging.Tags;

namespace AhBearStudios.Core.Logging.Config
{
    /// <summary>
    /// Base ScriptableObject for log target configuration.
    /// Provides common configuration settings for all log targets.
    /// </summary>
    public abstract class LogTargetConfig : ScriptableObject
    {
        [Tooltip("The name of the log target")]
        [SerializeField] protected string _targetName;
        
        [Tooltip("The minimum log level this target will process")]
        [SerializeField] protected byte _minimumLevel = LogLevel.Info;
        
        [Tooltip("Whether this target is enabled by default")]
        [SerializeField] protected bool _enabled = true;
        
        [Tooltip("Tag categories to filter (if empty, all tags will be logged)")]
        [SerializeField] protected Tagging.TagCategory[] _tagFilters = new Tagging.TagCategory[0];
        
        /// <summary>
        /// Gets the name of the log target.
        /// </summary>
        public string TargetName => string.IsNullOrEmpty(_targetName) ? name : _targetName;
        
        /// <summary>
        /// Gets the minimum log level.
        /// </summary>
        public byte MinimumLevel => _minimumLevel;
        
        /// <summary>
        /// Gets whether this target is enabled by default.
        /// </summary>
        public bool Enabled => _enabled;
        
        /// <summary>
        /// Gets the tag filters.
        /// </summary>
        public Tagging.TagCategory[] TagFilters => _tagFilters;
        
        /// <summary>
        /// Creates an ILogTarget instance from this configuration.
        /// </summary>
        /// <returns>A new ILogTarget instance.</returns>
        public abstract ILogTarget CreateTarget();
        
        /// <summary>
        /// Applies tag filters to the specified target.
        /// </summary>
        /// <param name="target">The target to configure.</param>
        protected void ApplyTagFilters(ILogTarget target)
        {
            if (target == null)
                return;
                
            target.ClearTagFilters();
            
            foreach (var filter in _tagFilters)
            {
                if (filter != Tagging.TagCategory.None)
                {
                    target.AddTagFilter(filter);
                }
            }
        }
    }
}