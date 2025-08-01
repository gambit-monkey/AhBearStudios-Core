namespace AhBearStudios.Core.Serialization.Configs
{
    /// <summary>
    /// Configuration options specific to FishNet network serialization.
    /// </summary>
    public record FishNetSerializationOptions
    {
        /// <summary>
        /// Whether to use global custom serializers for cross-assembly support.
        /// When true, generates serializers with [UseGlobalCustomSerializer] attribute.
        /// </summary>
        public bool UseGlobalCustomSerializers { get; init; } = true;
        
        /// <summary>
        /// Whether to automatically generate extension methods for registered types.
        /// </summary>
        public bool AutoGenerateExtensionMethods { get; init; } = true;
        
        /// <summary>
        /// Whether to validate data order consistency between Writer and Reader.
        /// Helps catch serialization bugs during development.
        /// </summary>
        public bool ValidateDataOrder { get; init; } = true;
        
        /// <summary>
        /// Whether to use compression for network packets.
        /// FishNet supports various compression algorithms.
        /// </summary>
        public bool EnableNetworkCompression { get; init; } = false;
        
        /// <summary>
        /// Maximum packet size in bytes for FishNet serialization.
        /// Default is 1400 bytes to fit within typical MTU.
        /// </summary>
        public int MaxPacketSize { get; init; } = 1400;
        
        /// <summary>
        /// Whether to pool Writers and Readers for performance.
        /// </summary>
        public bool EnablePooling { get; init; } = true;
        
        /// <summary>
        /// Initial pool size for Writers and Readers.
        /// </summary>
        public int InitialPoolSize { get; init; } = 10;
        
        /// <summary>
        /// Maximum pool size for Writers and Readers.
        /// </summary>
        public int MaxPoolSize { get; init; } = 100;
        
        /// <summary>
        /// Whether to enable detailed logging for network serialization.
        /// Useful for debugging but may impact performance.
        /// </summary>
        public bool EnableDetailedLogging { get; init; } = false;
        
        /// <summary>
        /// Whether to include type information in serialized data.
        /// Required for polymorphic serialization but increases packet size.
        /// </summary>
        public bool IncludeTypeInformation { get; init; } = false;
        
        /// <summary>
        /// Namespace patterns to scan for types that need FishNet serializers.
        /// Empty means scan all namespaces.
        /// </summary>
        public string[] TypeScanNamespaces { get; init; } = new[] { "AhBearStudios.*" };
        
        /// <summary>
        /// Whether to fail fast on serialization errors or attempt recovery.
        /// </summary>
        public bool FailFastOnErrors { get; init; } = true;
        
        /// <summary>
        /// Creates default options for production use.
        /// </summary>
        public static FishNetSerializationOptions Default => new();
        
        /// <summary>
        /// Creates options optimized for development and debugging.
        /// </summary>
        public static FishNetSerializationOptions Development => new()
        {
            ValidateDataOrder = true,
            EnableDetailedLogging = true,
            FailFastOnErrors = false,
            EnableNetworkCompression = false
        };
        
        /// <summary>
        /// Creates options optimized for production performance.
        /// </summary>
        public static FishNetSerializationOptions Production => new()
        {
            ValidateDataOrder = false,
            EnableDetailedLogging = false,
            FailFastOnErrors = true,
            EnableNetworkCompression = true,
            MaxPoolSize = 200
        };
    }
}