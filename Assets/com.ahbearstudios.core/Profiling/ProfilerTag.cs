using System;
using System.Collections.Generic;
using Unity.Collections;

namespace AhBearStudios.Core.Profiling
{
    /// <summary>
    /// Immutable profiler tag that combines a category and a name
    /// </summary>
    public readonly struct ProfilerTag : IEquatable<ProfilerTag>
    {
        /// <summary>
        /// The category this tag belongs to
        /// </summary>
        public readonly ProfilerCategory Category;
        
        /// <summary>
        /// The name of this profiler tag
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// The fully qualified name (Category.Name)
        /// </summary>
        public string FullName => $"{Category}.{Name}";

        /// <summary>
        /// Creates a new ProfilerTag
        /// </summary>
        public ProfilerTag(ProfilerCategory category, string name)
        {
            Category = category;
            Name = name;
        }

        public override string ToString() => FullName;

        public bool Equals(ProfilerTag other)
        {
            return Category == other.Category && Name == other.Name;
        }

        public override bool Equals(object obj)
        {
            return obj is ProfilerTag other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Category * 397) ^ (Name != null ? Name.GetHashCode() : 0);
            }
        }

        public static bool operator ==(ProfilerTag left, ProfilerTag right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ProfilerTag left, ProfilerTag right)
        {
            return !left.Equals(right);
        }
        
        #region Common Tags
        // Pre-defined common tags to prevent string allocations
        public static readonly ProfilerTag Uncategorized = new ProfilerTag(ProfilerCategory.Uncategorized, "Default");
        public static readonly ProfilerTag RenderingMain = new ProfilerTag(ProfilerCategory.Rendering, "Main");
        public static readonly ProfilerTag PhysicsUpdate = new ProfilerTag(ProfilerCategory.Physics, "Update");
        public static readonly ProfilerTag AnimationUpdate = new ProfilerTag(ProfilerCategory.Animation, "Update");
        public static readonly ProfilerTag AIUpdate = new ProfilerTag(ProfilerCategory.AI, "Update");
        public static readonly ProfilerTag GameplayUpdate = new ProfilerTag(ProfilerCategory.Gameplay, "Update");
        public static readonly ProfilerTag UIUpdate = new ProfilerTag(ProfilerCategory.UI, "Update");
        public static readonly ProfilerTag LoadingMain = new ProfilerTag(ProfilerCategory.Loading, "Main");
        public static readonly ProfilerTag MemoryAllocation = new ProfilerTag(ProfilerCategory.Memory, "Allocation");
        public static readonly ProfilerTag NetworkSend = new ProfilerTag(ProfilerCategory.Network, "Send");
        public static readonly ProfilerTag NetworkReceive = new ProfilerTag(ProfilerCategory.Network, "Receive");
        #endregion
    }
}