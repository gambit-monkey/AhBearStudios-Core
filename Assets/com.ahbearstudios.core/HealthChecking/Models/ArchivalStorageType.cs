namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Types of archival storage
/// </summary>
public enum ArchivalStorageType
{
    LocalFileSystem,
    NetworkFileSystem,
    CloudStorage,
    DatabaseArchive,
    TapeStorage
}