namespace AhBearStudios.Core.Serialization.Services;

/// <summary>
/// Interface for schema versioning and migration support.
/// </summary>
public interface IVersioningService
{
    /// <summary>
    /// Registers a migration function for a type.
    /// </summary>
    /// <typeparam name="T">Type to register migration for</typeparam>
    /// <param name="fromVersion">Source version</param>
    /// <param name="toVersion">Target version</param>
    /// <param name="migrator">Migration function</param>
    void RegisterMigration<T>(int fromVersion, int toVersion, Func<T, T> migrator);

    /// <summary>
    /// Migrates an object to the latest version.
    /// </summary>
    /// <typeparam name="T">Type to migrate</typeparam>
    /// <param name="obj">Object to migrate</param>
    /// <param name="currentVersion">Current version of the object</param>
    /// <returns>Migrated object</returns>
    T MigrateToLatest<T>(T obj, int currentVersion = 1);

    /// <summary>
    /// Gets the latest version for a type.
    /// </summary>
    /// <typeparam name="T">Type to get version for</typeparam>
    /// <returns>Latest version number</returns>
    int GetLatestVersion<T>();

    /// <summary>
    /// Checks if a type requires migration from the specified version.
    /// </summary>
    /// <typeparam name="T">Type to check</typeparam>
    /// <param name="version">Version to check</param>
    /// <returns>True if migration is required</returns>
    bool RequiresMigration<T>(int version);

    /// <summary>
    /// Sets the latest version for a type.
    /// </summary>
    /// <typeparam name="T">Type to set version for</typeparam>
    /// <param name="version">Version number</param>
    void SetLatestVersion<T>(int version);
}