using System;

namespace AhBearStudios.Core.Common.Models;

/// <summary>
/// Defines the storage types available for data archival operations.
/// Used across multiple systems for consistent archival configuration.
/// </summary>
public enum ArchivalStorageType : byte
{
    /// <summary>
    /// Store archived data on the local file system.
    /// </summary>
    LocalFileSystem = 0,

    /// <summary>
    /// Store archived data in cloud storage services (AWS, Azure, GCP).
    /// </summary>
    CloudStorage = 1,

    /// <summary>
    /// Store archived data in a database system.
    /// </summary>
    Database = 2,

    /// <summary>
    /// Store archived data in network-attached storage.
    /// </summary>
    NetworkStorage = 3
}