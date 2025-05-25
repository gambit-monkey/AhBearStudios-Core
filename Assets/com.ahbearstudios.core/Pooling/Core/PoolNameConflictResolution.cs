namespace AhBearStudios.Pooling.Core
{
    /// <summary>
    /// Configuration options for handling pool naming conflicts
    /// </summary>
    public enum PoolNameConflictResolution
    {
        /// <summary>
        /// Throw an exception when a pool name conflict occurs
        /// </summary>
        ThrowException,
    
        /// <summary>
        /// Auto-rename the new pool with a unique name
        /// </summary>
        AutoRenameNew,
    
        /// <summary>
        /// Replace the existing pool with the same name
        /// </summary>
        Replace
    }
}