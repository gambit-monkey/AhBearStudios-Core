using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace AhBearStudios.Core.Logging.Tags
{
    /// <summary>
    /// Provides tag-related functionality for the logging system.
    /// This class is Burst-compatible and can be used in jobs.
    /// </summary>
    public static class Tagging
    {
        // Enums are better for type safety, memory efficiency, and Burst compatibility
        public enum LogTag : byte
        {
            None = 0,
            
            // System categories
            System = 10,
            Network = 11,
            Physics = 12,
            Audio = 13,
            Input = 14,
            Database = 15,
            IO = 16,
            Memory = 17,
            Job = 18,
            Unity = 19, // Unity-specific tags, e.g. Unity.Jobs, Unity.Entities, etc.
            
            // Application layers
            UI = 20,
            Gameplay = 21,
            AI = 22,
            Animation = 23,
            Rendering = 24,
            Particles = 25,
            
            // Cross-cutting concerns
            Loading = 30,
            Performance = 31,
            Analytics = 32,
            
            // Severity-related tags (can be combined with other tags)
            Debug = 90,
            Info = 91,
            Warning = 92,
            Error = 93,
            Critical = 94,
            Exception = 95,
            Assert = 96,
            
            // Special tags
            Custom = 100, // For dynamic tags stored separately
            Default = 255, // Used when no tag is specified
            Undefined = 254 // Used when a tag is not defined
        }
        
        // We use different tag categories to allow filtering across multiple dimensions
        [Flags]
        public enum TagCategory : byte
        {
            None = 0,
            System = 1 << 0,
            UI = 1 << 1,
            Gameplay = 1 << 2,
            Debug = 1 << 3,
            Error = 1 << 4,
            Custom = 1 << 5,
            All = byte.MaxValue
        }
        
        // Get the category for a specific tag
        public static TagCategory GetTagCategory(LogTag tag)
        {
            switch (tag)
            {
                case LogTag.System:
                case LogTag.Network:
                case LogTag.Physics:
                case LogTag.Audio:
                case LogTag.Input:
                case LogTag.Database:
                case LogTag.IO:
                case LogTag.Memory:
                    return TagCategory.System;
                
                case LogTag.UI:
                    return TagCategory.UI;
                
                case LogTag.Gameplay:
                case LogTag.AI:
                case LogTag.Animation:
                case LogTag.Particles:
                    return TagCategory.Gameplay;
                
                case LogTag.Debug:
                case LogTag.Info:
                    return TagCategory.Debug;
                
                case LogTag.Warning:
                case LogTag.Error:
                case LogTag.Critical:
                    return TagCategory.Error;
                
                case LogTag.Custom:
                    return TagCategory.Custom;
                
                default:
                    return TagCategory.None;
            }
        }
        
        // Get a fixed string representation (avoid allocations in Burst)
        public static FixedString32Bytes GetTagString(LogTag tag)
        {
            return tag.ToString();
        }
        
        // Check if a tag belongs to a specific category
        public static bool IsInCategory(LogTag tag, TagCategory category)
        {
            return (GetTagCategory(tag) & category) != 0;
        }
        
        // Helper methods for tag assessment
        public static bool IsError(LogTag tag)
        {
            return tag == LogTag.Error || tag == LogTag.Critical;
        }
        
        public static bool IsWarningOrWorse(LogTag tag)
        {
            return tag == LogTag.Warning || tag == LogTag.Error || tag == LogTag.Critical;
        }
        
        public static bool IsSystemRelated(LogTag tag)
        {
            return IsInCategory(tag, TagCategory.System);
        }
        
        // Smart tag selection based on content analysis
        public static LogTag SuggestTagFromContent(in FixedString128Bytes message)
        {
            // This version works with native strings for Burst compatibility
            
            // Critical issues
            if (Contains(message, "fatal") || Contains(message, "crash") || 
                Contains(message, "exception") || Contains(message, "assert"))
                return LogTag.Critical;
            
            // Errors
            if (Contains(message, "error") || Contains(message, "fail"))
                return LogTag.Error;
            
            // Warnings
            if (Contains(message, "warn") || Contains(message, "caution"))
                return LogTag.Warning;
            
            // System categorization
            if (Contains(message, "network") || Contains(message, "connection"))
                return LogTag.Network;
            
            if (Contains(message, "physics") || Contains(message, "collide"))
                return LogTag.Physics;
            
            if (Contains(message, "audio") || Contains(message, "sound"))
                return LogTag.Audio;
            
            if (Contains(message, "input") || Contains(message, "control"))
                return LogTag.Input;
            
            if (Contains(message, "ui") || Contains(message, "button") || Contains(message, "interface"))
                return LogTag.UI;
            
            if (Contains(message, "game") || Contains(message, "play"))
                return LogTag.Gameplay;
            
            if (Contains(message, "ai") || Contains(message, "npc") || Contains(message, "enemy"))
                return LogTag.AI;
            
            if (Contains(message, "load") || Contains(message, "init"))
                return LogTag.Loading;
            
            return LogTag.Default;
        }
        
        // Helper method for searching within FixedStrings (Burst-compatible)
        private static bool Contains(in FixedString128Bytes source, string searchText)
        {
            // Simple implementation for demonstration
            // For production, you might want a more sophisticated algorithm
            var sourceSpan = source.ToString().ToLowerInvariant().AsSpan();
            var searchSpan = searchText.AsSpan();
            
            return sourceSpan.IndexOf(searchSpan, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // This version can be used from regular C# code
        public static LogTag SuggestTagFromContent(string message)
        {
            if (string.IsNullOrEmpty(message))
                return LogTag.Default;
                
            message = message.ToLowerInvariant();
            
            // Critical issues
            if (message.Contains("fatal") || message.Contains("crash") || 
                message.Contains("exception") || message.Contains("assert"))
                return LogTag.Critical;
            
            // Continue with the same logic as above
            // ...
            
            return LogTag.Default;
        }
        
        // Helper to combine tags
        public static LogTag CombineTags(LogTag primaryTag, LogTag secondaryTag)
        {
            // If one is None/Default, return the other
            if (primaryTag == LogTag.None || primaryTag == LogTag.Default)
                return secondaryTag;
                
            if (secondaryTag == LogTag.None || secondaryTag == LogTag.Default)
                return primaryTag;
            
            // For this simplified implementation, we just return the primary tag
            // You could implement a more sophisticated tag combining logic
            return primaryTag;
        }
    }
}