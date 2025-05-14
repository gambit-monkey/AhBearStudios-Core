namespace AhBearStudios.Core.BootStrapping
{
    // Enum to represent the lifecycle states of the application
    public enum ApplicationState
    {
        Uninitialized,
        Initializing,
        Running,
        ShuttingDown,
        Paused,
        Stopped
    }
}