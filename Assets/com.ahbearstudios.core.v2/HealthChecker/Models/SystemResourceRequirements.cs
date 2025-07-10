namespace AhBearStudios.Core.HealthCheck.Models;

/// <summary>
/// Represents system resource requirements for health check execution.
/// </summary>
public readonly record struct SystemResourceRequirements(
    /// <summary>
    /// Minimum available memory in bytes required for execution.
    /// </summary>
    long MinAvailableMemoryBytes = 0,

    /// <summary>
    /// Minimum available CPU percentage required for execution.
    /// </summary>
    double MinAvailableCpuPercent = 0.0,

    /// <summary>
    /// Minimum available disk space in bytes required for execution.
    /// </summary>
    long MinAvailableDiskSpaceBytes = 0,

    /// <summary>
    /// Whether network connectivity is required for execution.
    /// </summary>
    bool RequiresNetworkConnectivity = false,

    /// <summary>
    /// Whether external dependencies must be available for execution.
    /// </summary>
    bool RequiresExternalDependencies = false)
{
    /// <summary>
    /// Gets a default instance with no resource requirements.
    /// </summary>
    public static SystemResourceRequirements None => new();

    /// <summary>
    /// Gets an instance with minimal resource requirements.
    /// </summary>
    public static SystemResourceRequirements Minimal => new(
        MinAvailableMemoryBytes: 1024 * 1024, // 1 MB
        MinAvailableCpuPercent: 5.0,
        MinAvailableDiskSpaceBytes: 1024 * 1024 // 1 MB
    );

    /// <summary>
    /// Gets an instance with moderate resource requirements.
    /// </summary>
    public static SystemResourceRequirements Moderate => new(
        MinAvailableMemoryBytes: 10 * 1024 * 1024, // 10 MB
        MinAvailableCpuPercent: 10.0,
        MinAvailableDiskSpaceBytes: 10 * 1024 * 1024, // 10 MB
        RequiresNetworkConnectivity: true
    );

    /// <summary>
    /// Gets an instance with high resource requirements.
    /// </summary>
    public static SystemResourceRequirements High => new(
        MinAvailableMemoryBytes: 100 * 1024 * 1024, // 100 MB
        MinAvailableCpuPercent: 20.0,
        MinAvailableDiskSpaceBytes: 100 * 1024 * 1024, // 100 MB
        RequiresNetworkConnectivity: true,
        RequiresExternalDependencies: true
    );

    /// <summary>
    /// Determines if the current system meets these resource requirements.
    /// </summary>
    /// <param name="availableMemory">Currently available memory in bytes.</param>
    /// <param name="availableCpu">Currently available CPU percentage.</param>
    /// <param name="availableDiskSpace">Currently available disk space in bytes.</param>
    /// <param name="hasNetworkConnectivity">Whether network connectivity is available.</param>
    /// <param name="hasExternalDependencies">Whether external dependencies are available.</param>
    /// <returns>True if all requirements are met; otherwise, false.</returns>
    public bool AreMetBy(
        long availableMemory,
        double availableCpu,
        long availableDiskSpace,
        bool hasNetworkConnectivity = true,
        bool hasExternalDependencies = true)
    {
        return availableMemory >= MinAvailableMemoryBytes
               && availableCpu >= MinAvailableCpuPercent
               && availableDiskSpace >= MinAvailableDiskSpaceBytes
               && (!RequiresNetworkConnectivity || hasNetworkConnectivity)
               && (!RequiresExternalDependencies || hasExternalDependencies);
    }
}