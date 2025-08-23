using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using AhBearStudios.Core.Serialization.Models;
using AhBearStudios.Core.Serialization.Configs;

namespace AhBearStudios.Core.Unity.Serialization.ScriptableObjects
{
    /// <summary>
    /// ScriptableObject asset for managing serialization configuration in the Unity Editor.
    /// Provides a user-friendly interface for configuring serialization settings that can be
    /// easily shared across team members and different build configurations.
    /// </summary>
    [CreateAssetMenu(
        fileName = "New Serialization Config", 
        menuName = "AhBearStudios/Core/Serialization/Serialization Config Asset",
        order = 100)]
    public class SerializationConfigAsset : ScriptableObject
    {
        [Header("Format Settings")]
        [SerializeField]
        [Tooltip("The primary serialization format to use")]
        private SerializationFormat _format = SerializationFormat.MemoryPack;

        [SerializeField]
        [Tooltip("Compression method for serialized data")]
        private CompressionType _compression = CompressionType.None;

        [SerializeField]
        [Tooltip("Serialization mode (synchronous or asynchronous)")]
        private SerializationMode _mode = SerializationMode.Synchronous;

        [Header("Performance Settings")]
        [SerializeField]
        [Tooltip("Enable performance monitoring and metrics collection")]
        private bool _enablePerformanceMonitoring = true;

        [SerializeField]
        [Tooltip("Maximum buffer size for pooled buffers (bytes)")]
        private int _maxBufferPoolSize = 1048576; // 1MB

        [SerializeField]
        [Tooltip("Threading mode for async operations")]
        private ThreadingMode _threadingMode = ThreadingMode.MainThread;

        [Header("Security Settings")]
        [SerializeField]
        [Tooltip("Enable encryption for serialized data")]
        private bool _enableEncryption = false;

        [SerializeField]
        [Tooltip("Encryption key (leave empty to generate automatically)")]
        private string _encryptionKey = "";

        [SerializeField]
        [Tooltip("Enable type validation during serialization/deserialization")]
        private bool _enableTypeValidation = true;

        [Header("Versioning Settings")]
        [SerializeField]
        [Tooltip("Enable schema versioning support")]
        private bool _enableVersioning = true;

        [SerializeField]
        [Tooltip("Use strict versioning (reject mismatched versions)")]
        private bool _strictVersioning = false;

        [SerializeField]
        [Tooltip("Current schema version")]
        private int _schemaVersion = 1;

        [Header("Type Safety")]
        [SerializeField]
        [Tooltip("Types that are explicitly allowed for serialization")]
        private string[] _typeWhitelist = new string[0];

        [SerializeField]
        [Tooltip("Types that are explicitly forbidden from serialization")]
        private string[] _typeBlacklist = new string[0];

        [Header("Unity Integration")]
        [SerializeField]
        [Tooltip("Enable Unity-specific type handling")]
        private bool _enableUnityTypeSupport = true;

        [SerializeField]
        [Tooltip("Automatically register Unity types on startup")]
        private bool _autoRegisterUnityTypes = true;

        [SerializeField]
        [Tooltip("Unity types to register automatically")]
        private UnityTypeRegistration[] _unityTypeRegistrations = new UnityTypeRegistration[0];

        [Header("Development Settings")]
        [SerializeField]
        [Tooltip("Enable debug logging for serialization operations")]
        private bool _enableDebugLogging = false;

        [SerializeField]
        [Tooltip("Validate serialization roundtrip in development builds")]
        private bool _validateRoundtrip = true;

        [SerializeField]
        [Tooltip("Profile name for this configuration")]
        private string _profileName = "Default";

        /// <summary>
        /// Gets the serialization format.
        /// </summary>
        public SerializationFormat Format => _format;

        /// <summary>
        /// Gets the compression type.
        /// </summary>
        public CompressionType Compression => _compression;

        /// <summary>
        /// Gets the serialization mode.
        /// </summary>
        public SerializationMode Mode => _mode;

        /// <summary>
        /// Gets whether performance monitoring is enabled.
        /// </summary>
        public bool EnablePerformanceMonitoring => _enablePerformanceMonitoring;

        /// <summary>
        /// Gets the maximum buffer pool size.
        /// </summary>
        public int MaxBufferPoolSize => _maxBufferPoolSize;

        /// <summary>
        /// Gets the threading mode.
        /// </summary>
        public ThreadingMode ThreadingMode => _threadingMode;

        /// <summary>
        /// Gets whether encryption is enabled.
        /// </summary>
        public bool EnableEncryption => _enableEncryption;

        /// <summary>
        /// Gets the encryption key.
        /// </summary>
        public string EncryptionKey => string.IsNullOrEmpty(_encryptionKey) ? GenerateEncryptionKey() : _encryptionKey;

        /// <summary>
        /// Gets whether type validation is enabled.
        /// </summary>
        public bool EnableTypeValidation => _enableTypeValidation;

        /// <summary>
        /// Gets whether versioning is enabled.
        /// </summary>
        public bool EnableVersioning => _enableVersioning;

        /// <summary>
        /// Gets whether strict versioning is enabled.
        /// </summary>
        public bool StrictVersioning => _strictVersioning;

        /// <summary>
        /// Gets the schema version.
        /// </summary>
        public int SchemaVersion => _schemaVersion;

        /// <summary>
        /// Gets the type whitelist.
        /// </summary>
        public string[] TypeWhitelist => _typeWhitelist ?? new string[0];

        /// <summary>
        /// Gets the type blacklist.
        /// </summary>
        public string[] TypeBlacklist => _typeBlacklist ?? new string[0];

        /// <summary>
        /// Gets whether Unity type support is enabled.
        /// </summary>
        public bool EnableUnityTypeSupport => _enableUnityTypeSupport;

        /// <summary>
        /// Gets whether Unity types should be auto-registered.
        /// </summary>
        public bool AutoRegisterUnityTypes => _autoRegisterUnityTypes;

        /// <summary>
        /// Gets the Unity type registrations.
        /// </summary>
        public UnityTypeRegistration[] UnityTypeRegistrations => _unityTypeRegistrations ?? new UnityTypeRegistration[0];

        /// <summary>
        /// Gets whether debug logging is enabled.
        /// </summary>
        public bool EnableDebugLogging => _enableDebugLogging;

        /// <summary>
        /// Gets whether roundtrip validation is enabled.
        /// </summary>
        public bool ValidateRoundtrip => _validateRoundtrip;

        /// <summary>
        /// Gets the profile name.
        /// </summary>
        public string ProfileName => _profileName;

        /// <summary>
        /// Converts this asset to a SerializationConfig instance.
        /// </summary>
        /// <returns>SerializationConfig instance</returns>
        public SerializationConfig ToSerializationConfig()
        {
            var config = new SerializationConfig
            {
                Format = _format,
                Compression = _compression,
                Mode = _mode,
                EnablePerformanceMonitoring = _enablePerformanceMonitoring,
                MaxBufferPoolSize = _maxBufferPoolSize,
                ThreadingMode = _threadingMode,
                EnableEncryption = _enableEncryption,
                EncryptionKey = new FixedString128Bytes(EncryptionKey),
                EnableTypeValidation = _enableTypeValidation,
                EnableVersioning = _enableVersioning,
                StrictVersioning = _strictVersioning,
                SchemaVersion = _schemaVersion,
                TypeWhitelist = new List<string>(_typeWhitelist ?? new string[0]),
                TypeBlacklist = new List<string>(_typeBlacklist ?? new string[0]),
                EnableDebugLogging = _enableDebugLogging,
                ValidateRoundtrip = _validateRoundtrip,
                ProfileName = _profileName
            };

            return config;
        }

        /// <summary>
        /// Updates this asset from a SerializationConfig instance.
        /// </summary>
        /// <param name="config">The configuration to copy from</param>
        public void FromSerializationConfig(SerializationConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            _format = config.Format;
            _compression = config.Compression;
            _mode = config.Mode;
            _enablePerformanceMonitoring = config.EnablePerformanceMonitoring;
            _maxBufferPoolSize = config.MaxBufferPoolSize;
            _threadingMode = config.ThreadingMode;
            _enableEncryption = config.EnableEncryption;
            _encryptionKey = config.EncryptionKey.ToString();
            _enableTypeValidation = config.EnableTypeValidation;
            _enableVersioning = config.EnableVersioning;
            _strictVersioning = config.StrictVersioning;
            _schemaVersion = config.SchemaVersion;
            _typeWhitelist = config.TypeWhitelist?.ToArray() ?? new string[0];
            _typeBlacklist = config.TypeBlacklist?.ToArray() ?? new string[0];
            _enableDebugLogging = config.EnableDebugLogging;
            _validateRoundtrip = config.ValidateRoundtrip;
            _profileName = config.ProfileName;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// Validates the current configuration and returns any issues found.
        /// </summary>
        /// <returns>List of validation issues</returns>
        public List<string> ValidateConfiguration()
        {
            var issues = new List<string>();

            // Validate encryption settings
            if (_enableEncryption && string.IsNullOrEmpty(_encryptionKey))
            {
                issues.Add("Encryption is enabled but no encryption key is provided. A key will be auto-generated.");
            }

            // Validate buffer pool size
            if (_maxBufferPoolSize <= 0)
            {
                issues.Add("Max buffer pool size must be greater than 0.");
            }

            if (_maxBufferPoolSize > 100 * 1024 * 1024) // 100MB
            {
                issues.Add("Max buffer pool size is very large (>100MB). This may cause memory issues.");
            }

            // Validate schema version
            if (_enableVersioning && _schemaVersion <= 0)
            {
                issues.Add("Schema version must be greater than 0 when versioning is enabled.");
            }

            // Validate type lists
            if (_typeWhitelist != null && _typeBlacklist != null)
            {
                var commonTypes = new HashSet<string>(_typeWhitelist);
                commonTypes.IntersectWith(_typeBlacklist);
                if (commonTypes.Count > 0)
                {
                    issues.Add($"Types appear in both whitelist and blacklist: {string.Join(", ", commonTypes)}");
                }
            }

            // Validate Unity type registrations
            if (_unityTypeRegistrations != null)
            {
                for (int i = 0; i < _unityTypeRegistrations.Length; i++)
                {
                    var registration = _unityTypeRegistrations[i];
                    if (string.IsNullOrEmpty(registration.TypeName))
                    {
                        issues.Add($"Unity type registration {i} has empty type name.");
                    }
                }
            }

            return issues;
        }

        /// <summary>
        /// Resets the configuration to default values.
        /// </summary>
        [ContextMenu("Reset to Defaults")]
        public void ResetToDefaults()
        {
            _format = SerializationFormat.MemoryPack;
            _compression = CompressionType.None;
            _mode = SerializationMode.Synchronous;
            _enablePerformanceMonitoring = true;
            _maxBufferPoolSize = 1048576; // 1MB
            _threadingMode = ThreadingMode.MainThread;
            _enableEncryption = false;
            _encryptionKey = "";
            _enableTypeValidation = true;
            _enableVersioning = true;
            _strictVersioning = false;
            _schemaVersion = 1;
            _typeWhitelist = new string[0];
            _typeBlacklist = new string[0];
            _enableUnityTypeSupport = true;
            _autoRegisterUnityTypes = true;
            _unityTypeRegistrations = new UnityTypeRegistration[0];
            _enableDebugLogging = false;
            _validateRoundtrip = true;
            _profileName = "Default";

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// Creates a high-performance configuration preset.
        /// </summary>
        [ContextMenu("Apply High Performance Preset")]
        public void ApplyHighPerformancePreset()
        {
            _format = SerializationFormat.MemoryPack;
            _compression = CompressionType.None;
            _mode = SerializationMode.Synchronous;
            _enablePerformanceMonitoring = false; // Disabled for max performance
            _maxBufferPoolSize = 4194304; // 4MB
            _threadingMode = ThreadingMode.MainThread;
            _enableEncryption = false;
            _enableTypeValidation = false; // Disabled for performance
            _enableVersioning = false; // Disabled for performance
            _enableDebugLogging = false;
            _validateRoundtrip = false;
            _profileName = "High Performance";

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// Creates a security-focused configuration preset.
        /// </summary>
        [ContextMenu("Apply High Security Preset")]
        public void ApplyHighSecurityPreset()
        {
            _format = SerializationFormat.Json; // More transparent format
            _compression = CompressionType.Gzip;
            _mode = SerializationMode.Synchronous;
            _enablePerformanceMonitoring = true;
            _enableEncryption = true;
            _encryptionKey = GenerateEncryptionKey();
            _enableTypeValidation = true;
            _enableVersioning = true;
            _strictVersioning = true;
            _validateRoundtrip = true;
            _profileName = "High Security";

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        private string GenerateEncryptionKey()
        {
            // Generate a secure random key
            var keyBytes = new byte[32]; // 256-bit key
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(keyBytes);
            }
            return Convert.ToBase64String(keyBytes);
        }

        private void OnValidate()
        {
            // Ensure valid values during editor changes
            _maxBufferPoolSize = Mathf.Max(1024, _maxBufferPoolSize); // At least 1KB
            _schemaVersion = Mathf.Max(1, _schemaVersion);

            if (string.IsNullOrEmpty(_profileName))
                _profileName = "Unnamed Profile";
        }

#if UNITY_EDITOR
        private void Reset()
        {
            ResetToDefaults();
        }
#endif
    }

    /// <summary>
    /// Configuration for Unity type registration.
    /// </summary>
    [Serializable]
    public class UnityTypeRegistration
    {
        [SerializeField]
        [Tooltip("The full name of the Unity type to register")]
        public string TypeName;

        [SerializeField]
        [Tooltip("Whether to register this type automatically on startup")]
        public bool AutoRegister = true;

        [SerializeField]
        [Tooltip("Priority for registration order (higher values register first)")]
        public int Priority = 0;

        [SerializeField]
        [Tooltip("Optional assembly name hint")]
        public string AssemblyName;

        public UnityTypeRegistration()
        {
        }

        public UnityTypeRegistration(string typeName, bool autoRegister = true, int priority = 0)
        {
            TypeName = typeName;
            AutoRegister = autoRegister;
            Priority = priority;
        }
    }

    /// <summary>
    /// Threading mode for serialization operations.
    /// </summary>
    public enum ThreadingMode
    {
        /// <summary>
        /// Perform all operations on the main thread.
        /// </summary>
        MainThread,

        /// <summary>
        /// Use background threads for CPU-intensive operations.
        /// </summary>
        BackgroundThread,

        /// <summary>
        /// Use the Unity Job System for parallel operations.
        /// </summary>
        JobSystem
    }
}