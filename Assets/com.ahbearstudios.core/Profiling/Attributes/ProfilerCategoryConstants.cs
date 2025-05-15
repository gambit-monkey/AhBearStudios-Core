using Unity.Profiling;

namespace AhBearStudios.Core.Profiling
{
    /// <summary>
    /// Constants for ProfilerCategory names to use with profiling attributes
    /// </summary>
    public static class ProfilerCategories
    {
        // Unity's built-in categories
        public const string Render = "Render";
        public const string Scripts = "Scripts";
        public const string Memory = "Memory";
        public const string Audio = "Audio";
        public const string Physics = "Physics";
        public const string Animation = "Animation";
        public const string AI = "AI";
        public const string Input = "Input";
        public const string Network = "Network";
        public const string Loading = "Loading";
        public const string UI = "UI";
        public const string Video = "Video";
        public const string VirtualTexturing = "VirtualTexturing";
        
        // Custom categories can be added here
        public const string Gameplay = "Gameplay";
        public const string Logic = "Logic";
        public const string Database = "Database";
        public const string FileIO = "FileIO";
        
        /// <summary>
        /// Converts a category name to the corresponding ProfilerCategory
        /// </summary>
        /// <param name="categoryName">The category name</param>
        /// <returns>The corresponding ProfilerCategory</returns>
        public static ProfilerCategory GetCategory(string categoryName)
        {
            // Check for built-in categories first
            switch (categoryName)
            {
                case Render:
                    return ProfilerCategory.Render;
                case Scripts:
                    return ProfilerCategory.Scripts;
                case Memory:
                    return ProfilerCategory.Memory;
                case Audio:
                    return ProfilerCategory.Audio;
                case Physics:
                    return ProfilerCategory.Physics;
                case Animation:
                    return ProfilerCategory.Animation;
                case AI:
                    return ProfilerCategory.Ai;
                case Input:
                    return ProfilerCategory.Input;
                case Network:
                    return ProfilerCategory.Network;
                case Loading:
                    return ProfilerCategory.Loading;
                case UI:
                    return ProfilerCategory.Gui;
                case Video:
                    return ProfilerCategory.Video;
                case VirtualTexturing:
                    return ProfilerCategory.VirtualTexturing;
                default:
                    // For custom categories, create a new ProfilerCategory with the given name
                    return new ProfilerCategory(categoryName);
            }
        }
    }
}