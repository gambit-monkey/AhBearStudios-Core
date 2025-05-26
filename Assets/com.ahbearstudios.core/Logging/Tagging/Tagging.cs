using System;
using Unity.Collections;

namespace AhBearStudios.Core.Logging.Tags
{
    /// <summary>
    /// Provides tag-related functionality for the logging system.
    /// This class is Burst-compatible and can be used in jobs.
    /// </summary>
    public static class Tagging
    {
        /// <summary>
        /// Enumeration of all available log tag for categorizing log messages.
        /// </summary>
        public enum LogTag : byte
        {
            None = 0,
            
            // System categories (10-19)
            System = 10,
            Network = 11,
            Physics = 12,
            Audio = 13,
            Input = 14,
            Database = 15,
            IO = 16,
            Memory = 17,
            Job = 18,
            Unity = 19,
            
            // Application layers (20-29)
            UI = 20,
            Gameplay = 21,
            AI = 22,
            Animation = 23,
            Rendering = 24,
            Particles = 25,
            Graphics = 26,
            
            // Cross-cutting concerns (30-39)
            Loading = 30,
            Performance = 31,
            Analytics = 32,
            Profiler = 33,
            
            // Application features (40-49)
            SaveLoad = 40,
            Resources = 41,
            Events = 42,
            Localization = 43,
            Platform = 44,
            
            // Development (50-59)
            Editor = 50,
            Build = 51,
            Tests = 52,
            
            // Severity-related tag (90-97)
            Debug = 90,
            Info = 91,
            Warning = 92,
            Error = 93,
            Critical = 94,
            Exception = 95,
            Assert = 96,
            Trace = 97,
            
            // Special tag
            Custom = 100,
            Default = 255,
            Undefined = 254
        }
        
        /// <summary>
        /// Tag categories for filtering across multiple dimensions.
        /// </summary>
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
            Development = 1 << 6,
            Features = 1 << 7,
            All = byte.MaxValue
        }
        
        /// <summary>
        /// Gets the category for a specific tag.
        /// </summary>
        /// <param name="tag">The log tag to categorize.</param>
        /// <returns>The category of the tag.</returns>
        public static TagCategory GetTagCategory(LogTag tag)
        {
            switch (tag)
            {
                // System category
                case LogTag.System:
                case LogTag.Network:
                case LogTag.Physics:
                case LogTag.Audio:
                case LogTag.Input:
                case LogTag.Database:
                case LogTag.IO:
                case LogTag.Memory:
                case LogTag.Job:
                case LogTag.Unity:
                case LogTag.Platform:
                    return TagCategory.System;
                
                // UI category
                case LogTag.UI:
                    return TagCategory.UI;
                
                // Gameplay category
                case LogTag.Gameplay:
                case LogTag.AI:
                case LogTag.Animation:
                case LogTag.Particles:
                case LogTag.Graphics:
                case LogTag.Rendering:
                    return TagCategory.Gameplay;
                
                // Debug category
                case LogTag.Debug:
                case LogTag.Info:
                case LogTag.Trace:
                case LogTag.Performance:
                case LogTag.Profiler:
                    return TagCategory.Debug;
                
                // Error category
                case LogTag.Warning:
                case LogTag.Error:
                case LogTag.Critical:
                case LogTag.Exception:
                case LogTag.Assert:
                    return TagCategory.Error;
                
                // Development category
                case LogTag.Editor:
                case LogTag.Build:
                case LogTag.Tests:
                    return TagCategory.Development;
                
                // Features category
                case LogTag.SaveLoad:
                case LogTag.Resources:
                case LogTag.Events:
                case LogTag.Localization:
                case LogTag.Loading:
                case LogTag.Analytics:
                    return TagCategory.Features;
                
                // Custom category
                case LogTag.Custom:
                    return TagCategory.Custom;
                
                default:
                    return TagCategory.None;
            }
        }
        
        /// <summary>
        /// Gets a fixed string representation of the tag.
        /// </summary>
        /// <param name="tag">The log tag to convert.</param>
        /// <returns>A fixed string representation of the tag.</returns>
        public static FixedString32Bytes GetTagString(LogTag tag)
        {
            return tag.ToString();
        }
        
        /// <summary>
        /// Converts a string to a LogTag enum value.
        /// </summary>
        /// <param name="tagString">The string to convert.</param>
        /// <returns>The corresponding LogTag value, or Default if not found.</returns>
        public static LogTag GetLogTag(string tagString)
        {
            if (string.IsNullOrEmpty(tagString))
                return LogTag.Default;
            
            // Try direct enum parsing
            if (Enum.TryParse<LogTag>(tagString, true, out var tag))
            {
                return tag;
            }
            
            // Try common aliases
            switch (tagString.ToLowerInvariant())
            {
                case "save":
                    return LogTag.SaveLoad;
                case "resource":
                    return LogTag.Resources;
                case "event":
                    return LogTag.Events;
                case "test":
                    return LogTag.Tests;
                case "gfx":
                    return LogTag.Graphics;
                case "net":
                    return LogTag.Network;
                case "perf":
                    return LogTag.Performance;
                case "anim":
                    return LogTag.Animation;
                case "loc":
                case "l10n":
                    return LogTag.Localization;
                default:
                    return LogTag.Default;
            }
        }
        
        /// <summary>
        /// Checks if a tag belongs to a specific category.
        /// </summary>
        /// <param name="tag">The tag to check.</param>
        /// <param name="category">The category to check against.</param>
        /// <returns>True if the tag belongs to the category; otherwise, false.</returns>
        public static bool IsInCategory(LogTag tag, TagCategory category)
        {
            return (GetTagCategory(tag) & category) != 0;
        }
        
        /// <summary>
        /// Checks if a tag represents an error level.
        /// </summary>
        /// <param name="tag">The tag to check.</param>
        /// <returns>True if the tag represents an error; otherwise, false.</returns>
        public static bool IsError(LogTag tag)
        {
            return tag == LogTag.Error || tag == LogTag.Critical || tag == LogTag.Exception;
        }
        
        /// <summary>
        /// Checks if a tag represents a warning or more severe level.
        /// </summary>
        /// <param name="tag">The tag to check.</param>
        /// <returns>True if the tag represents a warning or worse; otherwise, false.</returns>
        public static bool IsWarningOrWorse(LogTag tag)
        {
            return tag == LogTag.Warning || IsError(tag);
        }
        
        /// <summary>
        /// Checks if a tag is system-related.
        /// </summary>
        /// <param name="tag">The tag to check.</param>
        /// <returns>True if the tag is system-related; otherwise, false.</returns>
        public static bool IsSystemRelated(LogTag tag)
        {
            return IsInCategory(tag, TagCategory.System);
        }
        
        /// <summary>
        /// Suggests a tag based on the content of the message (Burst-compatible version).
        /// </summary>
        /// <param name="message">The message to analyze.</param>
        /// <returns>A suggested tag based on the message content.</returns>
        public static LogTag SuggestTagFromContent(in FixedString128Bytes message)
        {
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
            
            if (Contains(message, "save") || Contains(message, "load"))
                return LogTag.SaveLoad;
            
            if (Contains(message, "resource") || Contains(message, "asset"))
                return LogTag.Resources;
            
            if (Contains(message, "event") || Contains(message, "trigger"))
                return LogTag.Events;
            
            if (Contains(message, "profile") || Contains(message, "perf"))
                return LogTag.Profiler;
            
            if (Contains(message, "memory") || Contains(message, "alloc"))
                return LogTag.Memory;
            
            if (Contains(message, "editor") || Contains(message, "tool"))
                return LogTag.Editor;
            
            if (Contains(message, "test") || Contains(message, "unit"))
                return LogTag.Tests;
            
            if (Contains(message, "build") || Contains(message, "compile"))
                return LogTag.Build;
            
            return LogTag.Default;
        }
        
        /// <summary>
        /// Helper method for searching within FixedStrings (Burst-compatible).
        /// </summary>
        private static bool Contains(in FixedString128Bytes source, string searchText)
        {
            var sourceSpan = source.ToString().ToLowerInvariant().AsSpan();
            var searchSpan = searchText.AsSpan();
            
            return sourceSpan.IndexOf(searchSpan, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Suggests a tag based on the content of the message (managed version).
        /// </summary>
        /// <param name="message">The message to analyze.</param>
        /// <returns>A suggested tag based on the message content.</returns>
        public static LogTag SuggestTagFromContent(string message)
        {
            if (string.IsNullOrEmpty(message))
                return LogTag.Default;
                
            message = message.ToLowerInvariant();
            
            // Critical issues
            if (message.Contains("fatal") || message.Contains("crash") || 
                message.Contains("exception") || message.Contains("assert"))
                return LogTag.Critical;
            
            // Errors
            if (message.Contains("error") || message.Contains("fail"))
                return LogTag.Error;
            
            // Warnings
            if (message.Contains("warn") || message.Contains("caution"))
                return LogTag.Warning;
            
            // System categorization
            if (message.Contains("network") || message.Contains("connection"))
                return LogTag.Network;
            
            if (message.Contains("physics") || message.Contains("collide"))
                return LogTag.Physics;
            
            if (message.Contains("audio") || message.Contains("sound"))
                return LogTag.Audio;
            
            if (message.Contains("input") || message.Contains("control"))
                return LogTag.Input;
            
            if (message.Contains("ui") || message.Contains("button") || message.Contains("interface"))
                return LogTag.UI;
            
            if (message.Contains("game") || message.Contains("play"))
                return LogTag.Gameplay;
            
            if (message.Contains("ai") || message.Contains("npc") || message.Contains("enemy"))
                return LogTag.AI;
            
            if (message.Contains("load") || message.Contains("init"))
                return LogTag.Loading;
            
            if (message.Contains("save"))
                return LogTag.SaveLoad;
            
            if (message.Contains("resource") || message.Contains("asset"))
                return LogTag.Resources;
            
            if (message.Contains("event") || message.Contains("trigger"))
                return LogTag.Events;
            
            if (message.Contains("profile") || message.Contains("perf"))
                return LogTag.Profiler;
            
            if (message.Contains("memory") || message.Contains("alloc"))
                return LogTag.Memory;
            
            if (message.Contains("editor") || message.Contains("tool"))
                return LogTag.Editor;
            
            if (message.Contains("test") || message.Contains("unit"))
                return LogTag.Tests;
            
            if (message.Contains("build") || message.Contains("compile"))
                return LogTag.Build;
            
            if (message.Contains("graphic") || message.Contains("render") || message.Contains("shader"))
                return LogTag.Graphics;
            
            if (message.Contains("anim"))
                return LogTag.Animation;
            
            if (message.Contains("local") || message.Contains("translate") || message.Contains("language"))
                return LogTag.Localization;
            
            if (message.Contains("platform") || message.Contains("device"))
                return LogTag.Platform;
            
            return LogTag.Default;
        }
        
        /// <summary>
        /// Combines two tag, preferring the more specific one.
        /// </summary>
        /// <param name="primaryTag">The primary tag.</param>
        /// <param name="secondaryTag">The secondary tag.</param>
        /// <returns>The combined tag result.</returns>
        public static LogTag CombineTags(LogTag primaryTag, LogTag secondaryTag)
        {
            // If one is None/Default/Undefined, return the other
            if (primaryTag == LogTag.None || primaryTag == LogTag.Default || primaryTag == LogTag.Undefined)
                return secondaryTag;
                
            if (secondaryTag == LogTag.None || secondaryTag == LogTag.Default || secondaryTag == LogTag.Undefined)
                return primaryTag;
            
            // Prefer error-level tag
            if (IsError(secondaryTag) && !IsError(primaryTag))
                return secondaryTag;
                
            if (IsError(primaryTag))
                return primaryTag;
            
            // Prefer warning tag
            if (secondaryTag == LogTag.Warning && primaryTag != LogTag.Warning)
                return secondaryTag;
                
            // Otherwise, return the primary tag
            return primaryTag;
        }
    }
}