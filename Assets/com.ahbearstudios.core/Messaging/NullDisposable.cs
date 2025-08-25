using System;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Null implementation of IDisposable for use in null service patterns.
    /// </summary>
    internal sealed class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new NullDisposable();
        
        private NullDisposable() { }
        
        public void Dispose() { /* No-op */ }
    }
}