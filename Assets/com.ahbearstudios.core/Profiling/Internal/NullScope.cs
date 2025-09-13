using System;

namespace AhBearStudios.Core.Profiling.Internal
{
    /// <summary>
    /// Null object pattern implementation for disabled profiling scenarios.
    /// Provides a no-op IDisposable for use when profiling is disabled or unavailable.
    /// </summary>
    internal sealed class NullScope : IDisposable
    {
        #region Singleton Pattern

        /// <summary>
        /// Shared instance to avoid unnecessary allocations.
        /// </summary>
        public static readonly NullScope Instance = new NullScope();

        /// <summary>
        /// Private constructor to enforce singleton pattern.
        /// </summary>
        private NullScope() { }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// No-op disposal implementation.
        /// </summary>
        public void Dispose()
        {
            // No-op - this is a null object pattern implementation
        }

        #endregion
    }
}