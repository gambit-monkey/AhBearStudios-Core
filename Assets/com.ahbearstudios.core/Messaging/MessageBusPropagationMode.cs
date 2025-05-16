namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Enum defining message propagation modes for hierarchical buses
    /// </summary>
    public enum MessagePropagationMode
    {
        /// <summary>
        /// Messages are only handled locally, not propagated to parents or children
        /// </summary>
        None,
    
        /// <summary>
        /// Messages are propagated only to children (downstream)
        /// </summary>
        DownstreamOnly,
    
        /// <summary>
        /// Messages are propagated only to the parent (upstream)
        /// </summary>
        UpstreamOnly,
    
        /// <summary>
        /// Messages are propagated both to parents and children (bidirectional)
        /// </summary>
        Bidirectional
    }
}