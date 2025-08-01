using System;
using UnityEngine;
using AhBearStudios.Unity.Serialization.Components;
using AhBearStudios.Unity.Serialization.Models;
using Cysharp.Threading.Tasks;
using MemoryPack;

namespace AhBearStudios.Unity.Serialization.Components
{
    /// <summary>
    /// Specialized MonoBehaviour component for serializing Transform data with high performance.
    /// Optimized for frequent save/load operations in games that need to persist object positions.
    /// Uses MemoryPack for zero-allocation serialization of transform states.
    /// </summary>
    [AddComponentMenu("AhBearStudios/Serialization/Transform Serializer")]
    public class TransformSerializer : SerializableMonoBehaviour
    {
        [SerializeField]
        private bool _serializePosition = true;
        
        [SerializeField]
        private bool _serializeRotation = true;
        
        [SerializeField]
        private bool _serializeScale = true;
        
        [SerializeField]
        private bool _serializeLocalSpace = true;
        
        [SerializeField]
        private bool _useCompressedRotation = false;
        
        [SerializeField, Range(0.001f, 1.0f)]
        private float _positionPrecision = 0.01f;
        
        [SerializeField, Range(0.001f, 1.0f)]
        private float _scalePrecision = 0.01f;

        private TransformData _cachedTransformData;
        private bool _hasDataChanged = false;
        private float _lastUpdateTime;
        private const float UpdateCheckInterval = 0.1f; // Check for changes every 100ms

        /// <summary>
        /// Gets or sets whether position should be serialized.
        /// </summary>
        public bool SerializePosition
        {
            get => _serializePosition;
            set => _serializePosition = value;
        }

        /// <summary>
        /// Gets or sets whether rotation should be serialized.
        /// </summary>
        public bool SerializeRotation
        {
            get => _serializeRotation;
            set => _serializeRotation = value;
        }

        /// <summary>
        /// Gets or sets whether scale should be serialized.
        /// </summary>
        public bool SerializeScale
        {
            get => _serializeScale;
            set => _serializeScale = value;
        }

        /// <summary>
        /// Gets or sets whether to serialize local space coordinates (true) or world space (false).
        /// </summary>
        public bool SerializeLocalSpace
        {
            get => _serializeLocalSpace;
            set => _serializeLocalSpace = value;
        }

        /// <summary>
        /// Gets or sets whether to use compressed quaternion format for rotation.
        /// This reduces size but may slightly reduce precision.
        /// </summary>
        public bool UseCompressedRotation
        {
            get => _useCompressedRotation;
            set => _useCompressedRotation = value;
        }

        protected override void Awake()
        {
            base.Awake();
            CacheCurrentTransformData();
        }

        protected override async void Start()
        {
            base.Start();
            _lastUpdateTime = Time.time;
        }

        private void Update()
        {
            // Periodically check if transform data has changed
            if (Time.time - _lastUpdateTime >= UpdateCheckInterval)
            {
                CheckForTransformChanges();
                _lastUpdateTime = Time.time;
            }
        }

        /// <summary>
        /// Manually triggers a save if the transform data has changed.
        /// Useful for immediate persistence without waiting for automatic triggers.
        /// </summary>
        public async UniTask SaveIfChangedAsync()
        {
            CheckForTransformChanges();
            
            if (_hasDataChanged)
            {
                await SaveDataAsync();
                _hasDataChanged = false;
            }
        }

        /// <summary>
        /// Forces the transform to match a specific transform data structure.
        /// </summary>
        /// <param name="transformData">The transform data to apply</param>
        public async UniTask ApplyTransformDataAsync(TransformData transformData)
        {
            await SetSerializableDataAsync(transformData);
        }

        /// <summary>
        /// Gets the current transform data as a serializable structure.
        /// </summary>
        /// <returns>Current transform data</returns>
        public TransformData GetCurrentTransformData()
        {
            return CreateTransformData();
        }

        protected override object GetSerializableData()
        {
            return CreateTransformData();
        }

        protected override async UniTask SetSerializableDataAsync(object data)
        {
            if (data is TransformData transformData)
            {
                await ApplyTransformDataToTransform(transformData);
                CacheCurrentTransformData();
                _hasDataChanged = false;
            }
            else
            {
                throw new ArgumentException($"Expected TransformData, but received {data?.GetType().Name ?? "null"}");
            }
        }

        private TransformData CreateTransformData()
        {
            var data = new TransformData();

            if (_serializePosition)
            {
                var position = _serializeLocalSpace ? transform.localPosition : transform.position;
                data.Position = new SerializableVector3
                {
                    x = RoundToPrecision(position.x, _positionPrecision),
                    y = RoundToPrecision(position.y, _positionPrecision),
                    z = RoundToPrecision(position.z, _positionPrecision)
                };
                data.HasPosition = true;
            }

            if (_serializeRotation)
            {
                var rotation = _serializeLocalSpace ? transform.localRotation : transform.rotation;
                
                if (_useCompressedRotation)
                {
                    data.CompressedRotation = CompressQuaternion(rotation);
                    data.UseCompressedRotation = true;
                }
                else
                {
                    data.Rotation = new SerializableQuaternion
                    {
                        x = rotation.x,
                        y = rotation.y,
                        z = rotation.z,
                        w = rotation.w
                    };
                }
                data.HasRotation = true;
            }

            if (_serializeScale)
            {
                var scale = transform.localScale;
                data.Scale = new SerializableVector3
                {
                    x = RoundToPrecision(scale.x, _scalePrecision),
                    y = RoundToPrecision(scale.y, _scalePrecision),
                    z = RoundToPrecision(scale.z, _scalePrecision)
                };
                data.HasScale = true;
            }

            data.SerializeLocalSpace = _serializeLocalSpace;
            data.Timestamp = DateTime.UtcNow.Ticks;

            return data;
        }

        private async UniTask ApplyTransformDataToTransform(TransformData data)
        {
            // Ensure we're on the main thread for Unity API calls
            await UniTask.SwitchToMainThread();

            if (data.HasPosition)
            {
                var position = new Vector3(data.Position.x, data.Position.y, data.Position.z);
                if (data.SerializeLocalSpace)
                {
                    transform.localPosition = position;
                }
                else
                {
                    transform.position = position;
                }
            }

            if (data.HasRotation)
            {
                Quaternion rotation;
                
                if (data.UseCompressedRotation)
                {
                    rotation = DecompressQuaternion(data.CompressedRotation);
                }
                else
                {
                    rotation = new Quaternion(data.Rotation.x, data.Rotation.y, data.Rotation.z, data.Rotation.w);
                }

                if (data.SerializeLocalSpace)
                {
                    transform.localRotation = rotation;
                }
                else
                {
                    transform.rotation = rotation;
                }
            }

            if (data.HasScale)
            {
                var scale = new Vector3(data.Scale.x, data.Scale.y, data.Scale.z);
                transform.localScale = scale;
            }
        }

        private void CheckForTransformChanges()
        {
            var currentData = CreateTransformData();
            
            if (!_hasDataChanged && !AreTransformDataEqual(_cachedTransformData, currentData))
            {
                _hasDataChanged = true;
                CacheCurrentTransformData();
            }
        }

        private void CacheCurrentTransformData()
        {
            _cachedTransformData = CreateTransformData();
        }

        private bool AreTransformDataEqual(TransformData a, TransformData b)
        {
            if (a == null || b == null) return a == b;

            if (a.HasPosition != b.HasPosition || a.HasRotation != b.HasRotation || a.HasScale != b.HasScale)
                return false;

            if (a.HasPosition && !Vector3Approximately(
                new Vector3(a.Position.x, a.Position.y, a.Position.z),
                new Vector3(b.Position.x, b.Position.y, b.Position.z),
                _positionPrecision))
                return false;

            if (a.HasRotation)
            {
                Quaternion rotA, rotB;
                
                if (a.UseCompressedRotation && b.UseCompressedRotation)
                {
                    rotA = DecompressQuaternion(a.CompressedRotation);
                    rotB = DecompressQuaternion(b.CompressedRotation);
                }
                else if (!a.UseCompressedRotation && !b.UseCompressedRotation)
                {
                    rotA = new Quaternion(a.Rotation.x, a.Rotation.y, a.Rotation.z, a.Rotation.w);
                    rotB = new Quaternion(b.Rotation.x, b.Rotation.y, b.Rotation.z, b.Rotation.w);
                }
                else
                {
                    return false; // Different compression modes
                }

                if (Quaternion.Angle(rotA, rotB) > 0.1f) // 0.1 degree tolerance
                    return false;
            }

            if (a.HasScale && !Vector3Approximately(
                new Vector3(a.Scale.x, a.Scale.y, a.Scale.z),
                new Vector3(b.Scale.x, b.Scale.y, b.Scale.z),
                _scalePrecision))
                return false;

            return true;
        }

        private bool Vector3Approximately(Vector3 a, Vector3 b, float precision)
        {
            return Mathf.Abs(a.x - b.x) <= precision &&
                   Mathf.Abs(a.y - b.y) <= precision &&
                   Mathf.Abs(a.z - b.z) <= precision;
        }

        private float RoundToPrecision(float value, float precision)
        {
            return Mathf.Round(value / precision) * precision;
        }

        private CompressedQuaternion CompressQuaternion(Quaternion q)
        {
            // Normalize the quaternion first
            q = q.normalized;
            
            // Find the largest component
            var maxIndex = 0;
            var maxValue = Mathf.Abs(q.x);
            
            if (Mathf.Abs(q.y) > maxValue)
            {
                maxIndex = 1;
                maxValue = Mathf.Abs(q.y);
            }
            if (Mathf.Abs(q.z) > maxValue)
            {
                maxIndex = 2;
                maxValue = Mathf.Abs(q.z);
            }
            if (Mathf.Abs(q.w) > maxValue)
            {
                maxIndex = 3;
            }

            // Ensure the largest component is positive
            if ((maxIndex == 0 && q.x < 0) || (maxIndex == 1 && q.y < 0) || 
                (maxIndex == 2 && q.z < 0) || (maxIndex == 3 && q.w < 0))
            {
                q = new Quaternion(-q.x, -q.y, -q.z, -q.w);
            }

            // Pack the three smaller components
            var compressed = new CompressedQuaternion { LargestIndex = (byte)maxIndex };
            
            switch (maxIndex)
            {
                case 0: // x is largest
                    compressed.A = (short)(q.y * 32767f);
                    compressed.B = (short)(q.z * 32767f);
                    compressed.C = (short)(q.w * 32767f);
                    break;
                case 1: // y is largest
                    compressed.A = (short)(q.x * 32767f);
                    compressed.B = (short)(q.z * 32767f);
                    compressed.C = (short)(q.w * 32767f);
                    break;
                case 2: // z is largest
                    compressed.A = (short)(q.x * 32767f);
                    compressed.B = (short)(q.y * 32767f);
                    compressed.C = (short)(q.w * 32767f);
                    break;
                case 3: // w is largest
                    compressed.A = (short)(q.x * 32767f);
                    compressed.B = (short)(q.y * 32767f);
                    compressed.C = (short)(q.z * 32767f);
                    break;
            }

            return compressed;
        }

        private Quaternion DecompressQuaternion(CompressedQuaternion compressed)
        {
            var a = compressed.A / 32767f;
            var b = compressed.B / 32767f;
            var c = compressed.C / 32767f;

            // Calculate the missing component
            var missing = Mathf.Sqrt(1f - (a * a + b * b + c * c));

            switch (compressed.LargestIndex)
            {
                case 0: return new Quaternion(missing, a, b, c);
                case 1: return new Quaternion(a, missing, b, c);
                case 2: return new Quaternion(a, b, missing, c);
                case 3: return new Quaternion(a, b, c, missing);
                default: return Quaternion.identity;
            }
        }

        /// <summary>
        /// Manual method to mark transform as changed (for use when programmatically modifying transform).
        /// </summary>
        [ContextMenu("Mark Transform Changed")]
        public void MarkTransformChanged()
        {
            _hasDataChanged = true;
        }

        /// <summary>
        /// Gets whether the transform data has changed since the last save.
        /// </summary>
        public bool HasDataChanged => _hasDataChanged;
    }

}