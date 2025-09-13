using System;
using System.Collections.Generic;
using System.Linq;

namespace AhBearStudios.Core.Logging.Models
{
    /// <summary>
    /// Implementation of a log channel that provides domain-specific logging categorization.
    /// Channels allow for fine-grained control over logging behavior by domain or feature area.
    /// </summary>
    public sealed class LogChannel : ILogChannel
    {
        private readonly HashSet<string> _tags;
        private readonly HashSet<string> _targetNames;
        private readonly Dictionary<string, object> _properties;

        /// <summary>
        /// Gets the unique name of this log channel.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the minimum log level that this channel will process.
        /// </summary>
        public LogLevel MinimumLevel { get; }

        /// <summary>
        /// Gets whether this channel is enabled and should process log messages.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Gets whether this channel is currently healthy and operational.
        /// A channel is considered healthy if it's properly configured and functioning.
        /// </summary>
        public bool IsHealthy => !string.IsNullOrEmpty(Name);

        /// <summary>
        /// Gets the list of tags associated with this channel for categorization.
        /// </summary>
        public IReadOnlyList<string> Tags => _tags.ToList().AsReadOnly();

        /// <summary>
        /// Gets the list of target names that this channel should write to.
        /// Empty list means it writes to all targets.
        /// </summary>
        public IReadOnlyList<string> TargetNames => _targetNames.ToList().AsReadOnly();

        /// <summary>
        /// Gets additional properties associated with this channel.
        /// </summary>
        public IReadOnlyDictionary<string, object> Properties => _properties;

        /// <summary>
        /// Initializes a new instance of the LogChannel.
        /// </summary>
        /// <param name="name">The unique name of the channel</param>
        /// <param name="minimumLevel">The minimum log level for this channel</param>
        /// <param name="isEnabled">Whether the channel is enabled</param>
        /// <param name="tags">Tags associated with this channel</param>
        /// <param name="targetNames">Target names this channel should write to</param>
        /// <param name="properties">Additional properties for this channel</param>
        /// <exception cref="ArgumentNullException">Thrown when name is null</exception>
        /// <exception cref="ArgumentException">Thrown when name is empty</exception>
        public LogChannel(
            string name,
            LogLevel minimumLevel = LogLevel.Debug,
            bool isEnabled = true,
            IEnumerable<string> tags = null,
            IEnumerable<string> targetNames = null,
            IReadOnlyDictionary<string, object> properties = null)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Channel name cannot be empty or whitespace", nameof(name));

            Name = name;
            MinimumLevel = minimumLevel;
            IsEnabled = isEnabled;

            _tags = new HashSet<string>(tags ?? Enumerable.Empty<string>());
            _targetNames = new HashSet<string>(targetNames ?? Enumerable.Empty<string>());
            _properties = new Dictionary<string, object>(properties ?? new Dictionary<string, object>());
        }

        /// <summary>
        /// Determines whether this channel should process the given log message.
        /// </summary>
        /// <param name="logMessage">The log message to evaluate</param>
        /// <returns>True if the channel should process the message, false otherwise</returns>
        public bool ShouldProcessMessage(in LogMessage logMessage)
        {
            // Channel must be enabled
            if (!IsEnabled)
                return false;

            // Message level must meet minimum level
            if (logMessage.Level < MinimumLevel)
                return false;

            // If channel specifies target names, check if message is going to compatible targets
            if (_targetNames.Count > 0)
            {
                // This would need to be checked against the actual targets being used
                // For now, we'll allow it through and let the logging service handle target filtering
            }

            // Channel accepts this message
            return true;
        }

        /// <summary>
        /// Creates a new LogChannel with modified properties.
        /// </summary>
        /// <param name="minimumLevel">New minimum level</param>
        /// <param name="isEnabled">New enabled state</param>
        /// <param name="additionalTags">Additional tags to add</param>
        /// <param name="additionalTargets">Additional targets to add</param>
        /// <param name="additionalProperties">Additional properties to add</param>
        /// <returns>A new LogChannel with the modified properties</returns>
        public LogChannel WithModifications(
            LogLevel? minimumLevel = null,
            bool? isEnabled = null,
            IEnumerable<string> additionalTags = null,
            IEnumerable<string> additionalTargets = null,
            IReadOnlyDictionary<string, object> additionalProperties = null)
        {
            var newTags = new HashSet<string>(_tags);
            if (additionalTags != null)
            {
                foreach (var tag in additionalTags)
                {
                    newTags.Add(tag);
                }
            }

            var newTargets = new HashSet<string>(_targetNames);
            if (additionalTargets != null)
            {
                foreach (var target in additionalTargets)
                {
                    newTargets.Add(target);
                }
            }

            var newProperties = new Dictionary<string, object>(_properties);
            if (additionalProperties != null)
            {
                foreach (var kvp in additionalProperties)
                {
                    newProperties[kvp.Key] = kvp.Value;
                }
            }

            return new LogChannel(
                Name,
                minimumLevel ?? MinimumLevel,
                isEnabled ?? IsEnabled,
                newTags,
                newTargets,
                newProperties
            );
        }

        /// <summary>
        /// Determines if this channel has the specified tag.
        /// </summary>
        /// <param name="tag">The tag to check for</param>
        /// <returns>True if the channel has the tag, false otherwise</returns>
        public bool HasTag(string tag)
        {
            return !string.IsNullOrEmpty(tag) && _tags.Contains(tag);
        }

        /// <summary>
        /// Determines if this channel targets the specified target name.
        /// </summary>
        /// <param name="targetName">The target name to check for</param>
        /// <returns>True if the channel targets the specified target, false otherwise</returns>
        public bool TargetsSpecificTarget(string targetName)
        {
            return !string.IsNullOrEmpty(targetName) && _targetNames.Contains(targetName);
        }

        /// <summary>
        /// Determines if this channel targets all available targets.
        /// </summary>
        /// <returns>True if the channel targets all targets, false if it has specific targets</returns>
        public bool TargetsAllTargets()
        {
            return _targetNames.Count == 0;
        }

        /// <summary>
        /// Gets a property value by key.
        /// </summary>
        /// <typeparam name="T">The type of the property value</typeparam>
        /// <param name="key">The property key</param>
        /// <param name="defaultValue">The default value if the property is not found</param>
        /// <returns>The property value or default value</returns>
        public T GetProperty<T>(string key, T defaultValue = default(T))
        {
            if (string.IsNullOrEmpty(key) || !_properties.TryGetValue(key, out var value))
                return defaultValue;

            if (value is T typedValue)
                return typedValue;

            // Try to convert the value
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Returns a string representation of this channel.
        /// </summary>
        /// <returns>A string representation of the channel</returns>
        public override string ToString()
        {
            var enabledStatus = IsEnabled ? "Enabled" : "Disabled";
            var tagCount = _tags.Count;
            var targetCount = _targetNames.Count;
            
            return $"LogChannel '{Name}' ({enabledStatus}, MinLevel: {MinimumLevel}, Tags: {tagCount}, Targets: {targetCount})";
        }

        /// <summary>
        /// Determines equality with another LogChannel.
        /// </summary>
        /// <param name="other">The other LogChannel to compare with</param>
        /// <returns>True if the channels are equal, false otherwise</returns>
        public bool Equals(LogChannel other)
        {
            if (other == null) return false;
            if (ReferenceEquals(this, other)) return true;
            
            return Name == other.Name;
        }

        /// <summary>
        /// Determines equality with another object.
        /// </summary>
        /// <param name="obj">The object to compare with</param>
        /// <returns>True if the objects are equal, false otherwise</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as LogChannel);
        }

        /// <summary>
        /// Gets the hash code for this channel.
        /// </summary>
        /// <returns>The hash code</returns>
        public override int GetHashCode()
        {
            return Name?.GetHashCode() ?? 0;
        }
    }
}