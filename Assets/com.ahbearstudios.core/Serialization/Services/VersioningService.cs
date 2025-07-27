using System.Collections.Concurrent;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Serialization.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Serialization.Services;

/// <summary>
    /// Service for handling schema versioning and object migration.
    /// Provides type-safe migration capabilities with performance optimization.
    /// </summary>
    public class VersioningService : IVersioningService
    {
        private readonly ILoggingService _logger;
        private readonly ConcurrentDictionary<Type, int> _latestVersions;
        private readonly ConcurrentDictionary<string, Delegate> _migrations;
        private readonly object _migrationLock = new();

        /// <summary>
        /// Initializes a new instance of VersioningService.
        /// </summary>
        /// <param name="logger">Logging service</param>
        /// <exception cref="ArgumentNullException">Thrown when logger is null</exception>
        public VersioningService(ILoggingService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _latestVersions = new ConcurrentDictionary<Type, int>();
            _migrations = new ConcurrentDictionary<string, Delegate>();

            var correlationId = GetCorrelationId();
            _logger.LogInfo("VersioningService initialized", correlationId, sourceContext: null, properties: null);
        }

        /// <inheritdoc />
        public void RegisterMigration<T>(int fromVersion, int toVersion, Func<T, T> migrator)
        {
            if (migrator == null)
                throw new ArgumentNullException(nameof(migrator));
            if (fromVersion < 1 || toVersion < 1)
                throw new ArgumentOutOfRangeException("Versions must be positive");
            if (fromVersion >= toVersion)
                throw new ArgumentException("To version must be greater than from version");

            lock (_migrationLock)
            {
                var type = typeof(T);
                var key = $"{type.FullName}:{fromVersion}:{toVersion}";
                
                _migrations[key] = migrator;
                
                // Update latest version if necessary
                var currentLatest = _latestVersions.GetOrAdd(type, 1);
                if (toVersion > currentLatest)
                {
                    _latestVersions[type] = toVersion;
                }

                var correlationId = GetCorrelationId();
                _logger.LogInfo($"Registered migration for {type.Name} from v{fromVersion} to v{toVersion}", correlationId, sourceContext: null, properties: null);
            }
        }

        /// <inheritdoc />
        public T MigrateToLatest<T>(T obj, int currentVersion = 1)
        {
            if (obj == null)
                return obj;

            var type = typeof(T);
            var latestVersion = GetLatestVersion<T>();
            
            if (currentVersion >= latestVersion)
                return obj;

            var correlationId = GetCorrelationId();
            _logger.LogInfo($"Migrating {type.Name} from v{currentVersion} to v{latestVersion}", correlationId, sourceContext: null, properties: null);

            var result = obj;
            
            for (int version = currentVersion; version < latestVersion; version++)
            {
                var migrationKey = $"{type.FullName}:{version}:{version + 1}";
                
                if (_migrations.TryGetValue(migrationKey, out var migration))
                {
                    if (migration is Func<T, T> typedMigration)
                    {
                        result = typedMigration(result);
                        _logger.LogInfo($"Applied migration {type.Name} v{version} -> v{version + 1}", correlationId, sourceContext: null, properties: null);
                    }
                    else
                    {
                        throw new SerializationException($"Invalid migration delegate type for {type.Name}", type, "Migration");
                    }
                }
                else
                {
                    throw new SerializationException($"Missing migration from v{version} to v{version + 1} for {type.Name}", type, "Migration");
                }
            }

            return result;
        }

        /// <inheritdoc />
        public int GetLatestVersion<T>()
        {
            return _latestVersions.GetOrAdd(typeof(T), 1);
        }

        /// <inheritdoc />
        public bool RequiresMigration<T>(int version)
        {
            return version < GetLatestVersion<T>();
        }

        /// <inheritdoc />
        public void SetLatestVersion<T>(int version)
        {
            if (version < 1)
                throw new ArgumentOutOfRangeException(nameof(version), "Version must be positive");

            var type = typeof(T);
            _latestVersions[type] = version;

            var correlationId = GetCorrelationId();
            _logger.LogInfo($"Set latest version for {type.Name} to v{version}", correlationId, sourceContext: null, properties: null);
        }

        private FixedString64Bytes GetCorrelationId()
        {
            return new FixedString64Bytes(Guid.NewGuid().ToString("N")[..32]);
        }
    }