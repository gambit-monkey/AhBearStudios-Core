namespace AhBearStudios.Core.Messaging.Extensions
{
    /// <summary>
    /// Extension methods for the MessagePropagationMode enum.
    /// </summary>
    public static class MessagePropagationModeExtensions
    {
        /// <summary>
        /// Checks if the propagation mode includes upward propagation (child to parent).
        /// </summary>
        /// <param name="mode">The propagation mode to check.</param>
        /// <returns>True if upward propagation is enabled, false otherwise.</returns>
        public static bool IncludesUpwardPropagation(this MessagePropagationMode mode)
        {
            return (mode & MessagePropagationMode.UpwardOnly) == MessagePropagationMode.UpwardOnly;
        }
        
        /// <summary>
        /// Checks if the propagation mode includes downward propagation (parent to child).
        /// </summary>
        /// <param name="mode">The propagation mode to check.</param>
        /// <returns>True if downward propagation is enabled, false otherwise.</returns>
        public static bool IncludesDownwardPropagation(this MessagePropagationMode mode)
        {
            return (mode & MessagePropagationMode.DownwardOnly) == MessagePropagationMode.DownwardOnly;
        }
        
        /// <summary>
        /// Checks if the propagation mode includes sibling propagation (child to siblings).
        /// </summary>
        /// <param name="mode">The propagation mode to check.</param>
        /// <returns>True if sibling propagation is enabled, false otherwise.</returns>
        public static bool IncludesSiblingPropagation(this MessagePropagationMode mode)
        {
            return (mode & MessagePropagationMode.SiblingAware) == MessagePropagationMode.SiblingAware;
        }
        
        /// <summary>
        /// Gets a human-readable description of the propagation mode.
        /// </summary>
        /// <param name="mode">The propagation mode to describe.</param>
        /// <returns>A description of the propagation mode.</returns>
        public static string GetDescription(this MessagePropagationMode mode)
        {
            switch (mode)
            {
                case MessagePropagationMode.None:
                    return "No propagation (isolated buses)";
                case MessagePropagationMode.UpwardOnly:
                    return "Upward propagation only (child to parent)";
                case MessagePropagationMode.DownwardOnly:
                    return "Downward propagation only (parent to children)";
                case MessagePropagationMode.Bidirectional:
                    return "Bidirectional propagation (parent to children and children to parent)";
                case MessagePropagationMode.SiblingAware:
                    return "Sibling-aware propagation (child to parent to siblings)";
                case MessagePropagationMode.BroadcastAll:
                    return "Broadcast all (messages propagate in all directions)";
                default:
                    return $"Custom propagation mode ({mode})";
            }
        }
    }
}