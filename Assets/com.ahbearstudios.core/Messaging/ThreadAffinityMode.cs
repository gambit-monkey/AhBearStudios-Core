namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Enum defining thread affinity modes
    /// </summary>
    public enum ThreadAffinityMode
    {
        /// <summary>
        /// No thread affinity (messages can be processed on any thread)
        /// </summary>
        None,
    
        /// <summary>
        /// Main thread affinity (messages must be processed on the main thread)
        /// </summary>
        MainThread,
    
        /// <summary>
        /// Publisher thread affinity (messages are processed on the same thread that published them)
        /// </summary>
        PublisherThread,
    
        /// <summary>
        /// Custom thread affinity (messages are processed on a custom thread)
        /// </summary>
        CustomThread
    }
}