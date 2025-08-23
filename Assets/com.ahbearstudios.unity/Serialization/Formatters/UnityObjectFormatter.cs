using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections;
using UnityEngine;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Serialization.Models;
using ZLinq;

namespace AhBearStudios.Unity.Serialization.Formatters
{
    /// <summary>
    /// Specialized formatter for Unity-specific types and objects.
    /// Handles serialization of Unity components, ScriptableObjects, and other Unity-specific data types.
    /// Optimized for Unity game development with 60+ FPS performance requirements.
    /// </summary>
    public static class UnityObjectFormatter
    {
        private static readonly Dictionary<Type, IUnityTypeHandler> _typeHandlers;
        private static readonly HashSet<Type> _supportedUnityTypes;
        private static readonly object _lockObject = new();
        
        static UnityObjectFormatter()
        {
            _typeHandlers = new Dictionary<Type, IUnityTypeHandler>();
            _supportedUnityTypes = new HashSet<Type>();
            
            InitializeBuiltInHandlers();
        }

        /// <summary>
        /// Checks if a type is supported by the Unity object formatter.
        /// </summary>
        /// <param name="type">The type to check</param>
        /// <returns>True if the type is supported, false otherwise</returns>
        public static bool IsSupported(Type type)
        {
            if (type == null) return false;
            
            lock (_lockObject)
            {
                return _supportedUnityTypes.Contains(type) || 
                       _typeHandlers.ContainsKey(type) ||
                       IsUnityObjectType(type);
            }
        }

        /// <summary>
        /// Serializes a Unity object to a UnitySerializationData structure.
        /// </summary>
        /// <param name="obj">The Unity object to serialize</param>
        /// <param name="logger">Optional logging service</param>
        /// <returns>Serialized Unity data</returns>
        /// <exception cref="ArgumentNullException">Thrown when obj is null</exception>
        /// <exception cref="NotSupportedException">Thrown when the object type is not supported</exception>
        public static UnitySerializationData SerializeUnityObject(object obj, ILoggingService logger = null)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            var type = obj.GetType();
            var correlationId = GetCorrelationId();
            
            logger?.LogInfo($"Serializing Unity object of type {type.Name}", correlationId, sourceContext: null, properties: null);

            try
            {
                lock (_lockObject)
                {
                    if (_typeHandlers.TryGetValue(type, out var handler))
                    {
                        return handler.Serialize(obj);
                    }
                }

                // Handle Unity built-in types
                return SerializeBuiltInType(obj, type);
            }
            catch (Exception ex)
            {
                logger?.LogException($"Failed to serialize Unity object of type {type.Name}", ex, correlationId, sourceContext: null, properties: null);
                throw new SerializationException($"Unity serialization failed for type {type.Name}", type, "SerializeUnityObject", ex);
            }
        }

        /// <summary>
        /// Deserializes Unity data back to a Unity object.
        /// </summary>
        /// <typeparam name="T">The expected Unity object type</typeparam>
        /// <param name="data">The serialized Unity data</param>
        /// <param name="logger">Optional logging service</param>
        /// <returns>Deserialized Unity object</returns>
        /// <exception cref="ArgumentNullException">Thrown when data is null</exception>
        public static T DeserializeUnityObject<T>(UnitySerializationData data, ILoggingService logger = null)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            var targetType = typeof(T);
            var correlationId = GetCorrelationId();
            
            logger?.LogInfo($"Deserializing Unity object to type {targetType.Name}", correlationId, sourceContext: null, properties: null);

            try
            {
                lock (_lockObject)
                {
                    if (_typeHandlers.TryGetValue(targetType, out var handler))
                    {
                        return (T)handler.Deserialize(data, targetType);
                    }
                }

                // Handle Unity built-in types
                return (T)DeserializeBuiltInType(data, targetType);
            }
            catch (Exception ex)
            {
                logger?.LogException($"Failed to deserialize Unity object to type {targetType.Name}", ex, correlationId, sourceContext: null, properties: null);
                throw new SerializationException($"Unity deserialization failed for type {targetType.Name}", targetType, "DeserializeUnityObject", ex);
            }
        }

        /// <summary>
        /// Registers a custom type handler for Unity objects.
        /// </summary>
        /// <param name="type">The Unity type to handle</param>
        /// <param name="handler">The custom type handler</param>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null</exception>
        public static void RegisterTypeHandler(Type type, IUnityTypeHandler handler)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            lock (_lockObject)
            {
                _typeHandlers[type] = handler;
                _supportedUnityTypes.Add(type);
            }
        }

        /// <summary>
        /// Gets all supported Unity types.
        /// </summary>
        /// <returns>Array of supported Unity types</returns>
        public static Type[] GetSupportedTypes()
        {
            lock (_lockObject)
            {
                return _supportedUnityTypes.AsValueEnumerable().ToArray();
            }
        }

        private static void InitializeBuiltInHandlers()
        {
            // Register built-in Unity types
            var unityTypes = new[]
            {
                typeof(Vector2), typeof(Vector3), typeof(Vector4),
                typeof(Quaternion), typeof(Color), typeof(Color32),
                typeof(Bounds), typeof(Rect), typeof(Matrix4x4),
                typeof(LayerMask), typeof(AnimationCurve),
                typeof(Gradient), typeof(RectOffset)
            };

            foreach (var type in unityTypes)
            {
                _supportedUnityTypes.Add(type);
            }

            // Add handlers for complex Unity types
            RegisterTypeHandler(typeof(AnimationCurve), new AnimationCurveHandler());
            RegisterTypeHandler(typeof(Gradient), new GradientHandler());
        }

        private static bool IsUnityObjectType(Type type)
        {
            // Check if type inherits from UnityEngine.Object
            return typeof(UnityEngine.Object).IsAssignableFrom(type);
        }

        private static UnitySerializationData SerializeBuiltInType(object obj, Type type)
        {
            var data = new UnitySerializationData
            {
                TypeName = type.FullName,
                AssemblyName = type.Assembly.FullName,
                SerializationMethod = "BuiltIn"
            };

            // Use reflection to get all serializable fields
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .AsValueEnumerable()
                .Where(f => f.IsPublic && !f.IsInitOnly)
                .ToArray();

            data.FieldData = new Dictionary<string, object>();

            foreach (var field in fields)
            {
                var value = field.GetValue(obj);
                data.FieldData[field.Name] = value;
            }

            // Handle special Unity types with optimized serialization
            switch (obj)
            {
                case Vector2 v2:
                    data.OptimizedData = new float[] { v2.x, v2.y };
                    break;
                case Vector3 v3:
                    data.OptimizedData = new float[] { v3.x, v3.y, v3.z };
                    break;
                case Vector4 v4:
                    data.OptimizedData = new float[] { v4.x, v4.y, v4.z, v4.w };
                    break;
                case Quaternion q:
                    data.OptimizedData = new float[] { q.x, q.y, q.z, q.w };
                    break;
                case Color c:
                    data.OptimizedData = new float[] { c.r, c.g, c.b, c.a };
                    break;
                case Color32 c32:
                    data.OptimizedData = new byte[] { c32.r, c32.g, c32.b, c32.a };
                    break;
                case Bounds bounds:
                    data.OptimizedData = new float[] 
                    { 
                        bounds.center.x, bounds.center.y, bounds.center.z,
                        bounds.size.x, bounds.size.y, bounds.size.z 
                    };
                    break;
                case Rect rect:
                    data.OptimizedData = new float[] { rect.x, rect.y, rect.width, rect.height };
                    break;
            }

            return data;
        }

        private static object DeserializeBuiltInType(UnitySerializationData data, Type targetType)
        {
            // Use optimized data when available
            if (data.OptimizedData != null)
            {
                return DeserializeFromOptimizedData(data.OptimizedData, targetType);
            }

            // Fallback to reflection-based deserialization
            var instance = Activator.CreateInstance(targetType);
            
            if (data.FieldData != null)
            {
                foreach (var kvp in data.FieldData)
                {
                    var field = targetType.GetField(kvp.Key, BindingFlags.Public | BindingFlags.Instance);
                    if (field != null && !field.IsInitOnly && !field.IsLiteral)
                    {
                        try
                        {
                            var convertedValue = Convert.ChangeType(kvp.Value, field.FieldType);
                            field.SetValue(instance, convertedValue);
                        }
                        catch
                        {
                            // Skip fields that can't be converted
                        }
                    }
                }
            }

            return instance;
        }

        private static object DeserializeFromOptimizedData(object optimizedData, Type targetType)
        {
            switch (targetType.Name)
            {
                case nameof(Vector2) when optimizedData is float[] v2Data && v2Data.Length >= 2:
                    return new Vector2(v2Data[0], v2Data[1]);
                    
                case nameof(Vector3) when optimizedData is float[] v3Data && v3Data.Length >= 3:
                    return new Vector3(v3Data[0], v3Data[1], v3Data[2]);
                    
                case nameof(Vector4) when optimizedData is float[] v4Data && v4Data.Length >= 4:
                    return new Vector4(v4Data[0], v4Data[1], v4Data[2], v4Data[3]);
                    
                case nameof(Quaternion) when optimizedData is float[] qData && qData.Length >= 4:
                    return new Quaternion(qData[0], qData[1], qData[2], qData[3]);
                    
                case nameof(Color) when optimizedData is float[] cData && cData.Length >= 4:
                    return new Color(cData[0], cData[1], cData[2], cData[3]);
                    
                case nameof(Color32) when optimizedData is byte[] c32Data && c32Data.Length >= 4:
                    return new Color32(c32Data[0], c32Data[1], c32Data[2], c32Data[3]);
                    
                case nameof(Bounds) when optimizedData is float[] bData && bData.Length >= 6:
                    return new Bounds(
                        new Vector3(bData[0], bData[1], bData[2]),
                        new Vector3(bData[3], bData[4], bData[5]));
                        
                case nameof(Rect) when optimizedData is float[] rData && rData.Length >= 4:
                    return new Rect(rData[0], rData[1], rData[2], rData[3]);
                    
                default:
                    throw new NotSupportedException($"Optimized deserialization not supported for type {targetType.Name}");
            }
        }

        private static FixedString64Bytes GetCorrelationId()
        {
            return new FixedString64Bytes(Guid.NewGuid().ToString("N")[..32]);
        }
    }

    /// <summary>
    /// Interface for custom Unity type handlers.
    /// </summary>
    public interface IUnityTypeHandler
    {
        /// <summary>
        /// Serializes a Unity object to UnitySerializationData.
        /// </summary>
        /// <param name="obj">The object to serialize</param>
        /// <returns>Serialized data</returns>
        UnitySerializationData Serialize(object obj);

        /// <summary>
        /// Deserializes UnitySerializationData back to a Unity object.
        /// </summary>
        /// <param name="data">The serialized data</param>
        /// <param name="targetType">The target object type</param>
        /// <returns>Deserialized object</returns>
        object Deserialize(UnitySerializationData data, Type targetType);
    }

    /// <summary>
    /// Data structure for Unity object serialization.
    /// </summary>
    public class UnitySerializationData
    {
        /// <summary>
        /// The full type name of the serialized object.
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        /// The assembly name containing the type.
        /// </summary>
        public string AssemblyName { get; set; }

        /// <summary>
        /// The serialization method used.
        /// </summary>
        public string SerializationMethod { get; set; }

        /// <summary>
        /// Field data for reflection-based serialization.
        /// </summary>
        public Dictionary<string, object> FieldData { get; set; }

        /// <summary>
        /// Optimized data for performance-critical Unity types.
        /// </summary>
        public object OptimizedData { get; set; }

        /// <summary>
        /// Additional metadata.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; }
    }

    /// <summary>
    /// Handler for AnimationCurve serialization.
    /// </summary>
    internal class AnimationCurveHandler : IUnityTypeHandler
    {
        public UnitySerializationData Serialize(object obj)
        {
            var curve = (AnimationCurve)obj;
            var data = new UnitySerializationData
            {
                TypeName = typeof(AnimationCurve).FullName,
                AssemblyName = typeof(AnimationCurve).Assembly.FullName,
                SerializationMethod = "AnimationCurve"
            };

            var keyframes = new List<object>();
            foreach (var key in curve.keys)
            {
                keyframes.Add(new
                {
                    time = key.time,
                    value = key.value,
                    inTangent = key.inTangent,
                    outTangent = key.outTangent,
                    inWeight = key.inWeight,
                    outWeight = key.outWeight,
                    weightedMode = (int)key.weightedMode
                });
            }

            data.OptimizedData = keyframes.AsValueEnumerable().ToArray();
            data.Metadata = new Dictionary<string, object>
            {
                ["preWrapMode"] = (int)curve.preWrapMode,
                ["postWrapMode"] = (int)curve.postWrapMode
            };

            return data;
        }

        public object Deserialize(UnitySerializationData data, Type targetType)
        {
            var curve = new AnimationCurve();

            if (data.OptimizedData is object[] keyframes)
            {
                var keys = new List<Keyframe>();
                foreach (var keyframeObj in keyframes)
                {
                    // Use reflection to access anonymous object properties
                    var keyframeType = keyframeObj.GetType();
                    var time = (float)keyframeType.GetProperty("time").GetValue(keyframeObj);
                    var value = (float)keyframeType.GetProperty("value").GetValue(keyframeObj);
                    var inTangent = (float)keyframeType.GetProperty("inTangent").GetValue(keyframeObj);
                    var outTangent = (float)keyframeType.GetProperty("outTangent").GetValue(keyframeObj);
                    var inWeight = (float)keyframeType.GetProperty("inWeight").GetValue(keyframeObj);
                    var outWeight = (float)keyframeType.GetProperty("outWeight").GetValue(keyframeObj);
                    var weightedMode = (WeightedMode)(int)keyframeType.GetProperty("weightedMode").GetValue(keyframeObj);
                    
                    var key = new Keyframe
                    {
                        time = time,
                        value = value,
                        inTangent = inTangent,
                        outTangent = outTangent,
                        inWeight = inWeight,
                        outWeight = outWeight,
                        weightedMode = weightedMode
                    };
                    keys.Add(key);
                }
                curve.keys = keys.AsValueEnumerable().ToArray();
            }

            if (data.Metadata != null)
            {
                if (data.Metadata.TryGetValue("preWrapMode", out var preWrap))
                    curve.preWrapMode = (WrapMode)(int)preWrap;
                if (data.Metadata.TryGetValue("postWrapMode", out var postWrap))
                    curve.postWrapMode = (WrapMode)(int)postWrap;
            }

            return curve;
        }
    }

    /// <summary>
    /// Handler for Gradient serialization.
    /// </summary>
    internal class GradientHandler : IUnityTypeHandler
    {
        public UnitySerializationData Serialize(object obj)
        {
            var gradient = (Gradient)obj;
            var data = new UnitySerializationData
            {
                TypeName = typeof(Gradient).FullName,
                AssemblyName = typeof(Gradient).Assembly.FullName,
                SerializationMethod = "Gradient"
            };

            var colorKeys = gradient.colorKeys.AsValueEnumerable()
                .Select(k => new { time = k.time, color = new float[] { k.color.r, k.color.g, k.color.b, k.color.a } })
                .ToArray();
            var alphaKeys = gradient.alphaKeys.AsValueEnumerable()
                .Select(k => new { time = k.time, alpha = k.alpha })
                .ToArray();

            data.OptimizedData = new
            {
                colorKeys = colorKeys,
                alphaKeys = alphaKeys,
                mode = (int)gradient.mode
            };

            return data;
        }

        public object Deserialize(UnitySerializationData data, Type targetType)
        {
            var gradient = new Gradient();

            if (data.OptimizedData != null)
            {
                // Use reflection to access anonymous object properties
                var gradientDataType = data.OptimizedData.GetType();
                
                var colorKeysProperty = gradientDataType.GetProperty("colorKeys");
                if (colorKeysProperty != null)
                {
                    var colorKeysValue = colorKeysProperty.GetValue(data.OptimizedData);
                    if (colorKeysValue is object[] colorKeysArray)
                    {
                        var colorKeys = new List<GradientColorKey>();
                        foreach (var ckObj in colorKeysArray)
                        {
                            var ckType = ckObj.GetType();
                            var colorArray = (float[])ckType.GetProperty("color").GetValue(ckObj);
                            var time = (float)ckType.GetProperty("time").GetValue(ckObj);
                            
                            var colorKey = new GradientColorKey(
                                new Color(colorArray[0], colorArray[1], colorArray[2], colorArray[3]), 
                                time);
                            colorKeys.Add(colorKey);
                        }
                        gradient.colorKeys = colorKeys.AsValueEnumerable().ToArray();
                    }
                }

                var alphaKeysProperty = gradientDataType.GetProperty("alphaKeys");
                if (alphaKeysProperty != null)
                {
                    var alphaKeysValue = alphaKeysProperty.GetValue(data.OptimizedData);
                    if (alphaKeysValue is object[] alphaKeysArray)
                    {
                        var alphaKeys = new List<GradientAlphaKey>();
                        foreach (var akObj in alphaKeysArray)
                        {
                            var akType = akObj.GetType();
                            var alpha = (float)akType.GetProperty("alpha").GetValue(akObj);
                            var time = (float)akType.GetProperty("time").GetValue(akObj);
                            
                            var alphaKey = new GradientAlphaKey(alpha, time);
                            alphaKeys.Add(alphaKey);
                        }
                        gradient.alphaKeys = alphaKeys.AsValueEnumerable().ToArray();
                    }
                }

                var modeProperty = gradientDataType.GetProperty("mode");
                if (modeProperty != null)
                {
                    var mode = (int)modeProperty.GetValue(data.OptimizedData);
                    gradient.mode = (GradientMode)mode;
                }
            }

            return gradient;
        }
    }
}