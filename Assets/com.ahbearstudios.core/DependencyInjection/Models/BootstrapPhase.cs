namespace AhBearStudios.Core.DependencyInjection.Models
{
    /// <summary>
    /// Enumeration of bootstrap phases.
    /// </summary>
    public enum BootstrapPhase
    {
        /// <summary>
        /// Validation phase before installation begins.
        /// </summary>
        Validation,
        
        /// <summary>
        /// Dependency ordering phase.
        /// </summary>
        DependencyOrdering,
        
        /// <summary>
        /// Pre-installation phase.
        /// </summary>
        PreInstall,
        
        /// <summary>
        /// Main installation phase.
        /// </summary>
        Install,
        
        /// <summary>
        /// Post-installation phase.
        /// </summary>
        PostInstall,
        
        /// <summary>
        /// Container build phase.
        /// </summary>
        Build,
        
        /// <summary>
        /// Final validation phase.
        /// </summary>
        FinalValidation
    }
}