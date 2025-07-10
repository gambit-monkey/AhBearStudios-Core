using System.Collections.Generic;
using AhBearStudios.Core.Logging.Data;
using AhBearStudios.Core.Logging.Tags;

namespace AhBearStudios.Core.Logging.Configuration
{
    /// <summary>
    /// Core interface that defines the contract for log level profiles.
    /// This allows both Unity and non-Unity implementations to share the same interface.
    /// </summary>
    public interface ILogLevelProfile
    {
        /// <summary>
        /// Gets the profile name.
        /// </summary>
        string ProfileName { get; }

        /// <summary>
        /// Gets the profile description.
        /// </summary>  
        string Description { get; }

        /// <summary>
        /// Gets the global minimum log level.
        /// </summary>
        LogLevel GlobalMinimumLevel { get; }

        /// <summary>
        /// Gets the tag-specific level overrides.
        /// </summary>
        IReadOnlyList<TagLevelOverride> TagLevelOverrides { get; }

        /// <summary>
        /// Gets the category-specific level overrides.
        /// </summary>
        IReadOnlyList<CategoryLevelOverride> CategoryLevelOverrides { get; }

        /// <summary>
        /// Gets whether this profile should be applied automatically on startup.
        /// </summary>
        bool AutoApplyOnStartup { get; }

        /// <summary>
        /// Gets the priority when multiple profiles are available.
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Gets the environments where this profile is applicable.
        /// </summary>
        IReadOnlyList<string> ApplicableEnvironments { get; }

        /// <summary>
        /// Checks if a log level should be processed based on this profile's configuration.
        /// </summary>
        bool ShouldLog(LogLevel level, Tagging.LogTag tag, string category = null);

        /// <summary>
        /// Gets the effective minimum level for a specific tag and category combination.
        /// </summary>
        LogLevel GetEffectiveLevel(Tagging.LogTag tag, string category = null);

        /// <summary>
        /// Checks if this profile is applicable for the given environment.
        /// </summary>
        bool IsApplicableForEnvironment(string environment);
    }
}