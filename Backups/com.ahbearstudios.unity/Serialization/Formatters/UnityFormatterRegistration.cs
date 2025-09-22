using System.Runtime.CompilerServices;
using MemoryPack;
using UnityEngine;

namespace AhBearStudios.Unity.Serialization.Formatters
{
    /// <summary>
    /// Automatic registration of Unity-specific MemoryPack formatters.
    /// This class ensures all Unity types are properly registered with MemoryPack
    /// for zero-allocation, high-performance serialization.
    /// </summary>
    public static class UnityFormatterRegistration
    {
        private static bool _isRegistered = false;
        private static readonly object _lockObject = new();

        /// <summary>
        /// Registers all Unity formatters with MemoryPack.
        /// This method is automatically called during module initialization.
        /// </summary>
        [ModuleInitializer]
        public static void RegisterFormatters()
        {
            lock (_lockObject)
            {
                if (_isRegistered) return;

                // Register Vector formatters
                MemoryPackFormatterProvider.Register(new UnityVector2Formatter());
                MemoryPackFormatterProvider.Register(new UnityVector3Formatter());
                MemoryPackFormatterProvider.Register(new UnityVector4Formatter());
                MemoryPackFormatterProvider.Register(new UnityVector2IntFormatter());
                MemoryPackFormatterProvider.Register(new UnityVector3IntFormatter());

                // Register Quaternion formatters
                MemoryPackFormatterProvider.Register(new UnityQuaternionFormatter());
                
                // Register Color formatters
                MemoryPackFormatterProvider.Register(new UnityColorFormatter());
                MemoryPackFormatterProvider.Register(new UnityColor32Formatter());

                // Register Bounds and Rect formatters
                MemoryPackFormatterProvider.Register(new UnityBoundsFormatter());
                MemoryPackFormatterProvider.Register(new UnityBoundsIntFormatter());
                MemoryPackFormatterProvider.Register(new UnityRectFormatter());
                MemoryPackFormatterProvider.Register(new UnityRectIntFormatter());
                MemoryPackFormatterProvider.Register(new UnityRectOffsetFormatter());

                // Register Matrix formatters
                MemoryPackFormatterProvider.Register(new UnityMatrix4x4Formatter());

                // Register LayerMask formatter
                MemoryPackFormatterProvider.Register(new UnityLayerMaskFormatter());

                _isRegistered = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log("[UnityFormatterRegistration] All Unity MemoryPack formatters registered successfully");
#endif
            }
        }

        /// <summary>
        /// Registers optimized formatters for network scenarios.
        /// Call this method when network bandwidth is more important than precision.
        /// </summary>
        public static void RegisterNetworkOptimizedFormatters()
        {
            lock (_lockObject)
            {
                // Register compressed formatters for network scenarios
                MemoryPackFormatterProvider.Register(new UnityQuaternionCompressedFormatter());
                MemoryPackFormatterProvider.Register(new UnityColorPackedFormatter());
                MemoryPackFormatterProvider.Register(new UnityMatrix4x4TRSFormatter());

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log("[UnityFormatterRegistration] Network-optimized Unity MemoryPack formatters registered");
#endif
            }
        }

        /// <summary>
        /// Registers HDR formatters for high dynamic range scenarios.
        /// Call this method when working with HDR lighting and post-processing.
        /// </summary>
        public static void RegisterHDRFormatters()
        {
            lock (_lockObject)
            {
                // Register HDR-capable formatters
                MemoryPackFormatterProvider.Register(new UnityColorHDRFormatter());

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log("[UnityFormatterRegistration] HDR Unity MemoryPack formatters registered");
#endif
            }
        }

        /// <summary>
        /// Checks if Unity formatters are registered.
        /// </summary>
        /// <returns>True if formatters are registered</returns>
        public static bool IsRegistered()
        {
            lock (_lockObject)
            {
                return _isRegistered;
            }
        }

        /// <summary>
        /// Gets the count of registered Unity formatters.
        /// </summary>
        /// <returns>Number of registered formatters</returns>
        public static int GetRegisteredFormatterCount()
        {
            lock (_lockObject)
            {
                if (!_isRegistered) return 0;
                
                // Count the basic formatters
                return 13; // Vector(5) + Quaternion(1) + Color(2) + Bounds/Rect(5) + Matrix(1) + LayerMask(1)
            }
        }
    }
}