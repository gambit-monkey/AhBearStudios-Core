using System;
using System.Text;
using System.Collections.Generic;
using Unity.Collections;
using ZLinq;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Serialization;
using AhBearStudios.Core.Serialization.Models;
using AhBearStudios.Core.Common.Models;

namespace AhBearStudios.Core.Tests.Shared.Mocks
{
    public sealed class MockSerializationService : ISerializationService
    {
        private readonly Dictionary<Type, Func<object, byte[]>> _customSerializers = new Dictionary<Type, Func<object, byte[]>>();
        private readonly Dictionary<Type, Func<byte[], object>> _customDeserializers = new Dictionary<Type, Func<byte[], object>>();

        public bool IsEnabled { get; set; } = true;
        public int SerializeCallCount { get; private set; }
        public int DeserializeCallCount { get; private set; }
        public bool ShouldThrowOnSerialize { get; set; }
        public bool ShouldThrowOnDeserialize { get; set; }
        public bool UseSimpleTextSerialization { get; set; } = true;

        public byte[] Serialize<T>(T data)
        {
            SerializeCallCount++;

            if (ShouldThrowOnSerialize)
                throw new InvalidOperationException("Mock serialization error");

            if (data == null)
                return Array.Empty<byte>();

            // Check for custom serializer
            var type = typeof(T);
            if (_customSerializers.TryGetValue(type, out var customSerializer))
            {
                return customSerializer(data);
            }

            // Simple text-based serialization for testing
            if (UseSimpleTextSerialization)
            {
                var json = System.Text.Json.JsonSerializer.Serialize(data);
                return Encoding.UTF8.GetBytes(json);
            }

            // Fallback: convert to string and encode
            return Encoding.UTF8.GetBytes(data.ToString() ?? string.Empty);
        }

        public T Deserialize<T>(byte[] data)
        {
            DeserializeCallCount++;

            if (ShouldThrowOnDeserialize)
                throw new InvalidOperationException("Mock deserialization error");

            if (data == null || data.Length == 0)
                return default(T);

            // Check for custom deserializer
            var type = typeof(T);
            if (_customDeserializers.TryGetValue(type, out var customDeserializer))
            {
                return (T)customDeserializer(data);
            }

            // Simple text-based deserialization for testing
            if (UseSimpleTextSerialization)
            {
                var json = Encoding.UTF8.GetString(data);
                return System.Text.Json.JsonSerializer.Deserialize<T>(json);
            }

            // Fallback: try to convert from string
            var str = Encoding.UTF8.GetString(data);
            if (typeof(T) == typeof(string))
                return (T)(object)str;

            return default(T);
        }

        public async UniTask<byte[]> SerializeAsync<T>(T data)
        {
            await UniTask.Yield();
            return Serialize(data);
        }

        public async UniTask<T> DeserializeAsync<T>(byte[] data)
        {
            await UniTask.Yield();
            return Deserialize<T>(data);
        }

        public bool CanSerialize<T>()
        {
            return IsEnabled && !ShouldThrowOnSerialize;
        }

        public bool CanDeserialize<T>()
        {
            return IsEnabled && !ShouldThrowOnDeserialize;
        }

        public void RegisterCustomSerializer<T>(Func<T, byte[]> serializer, Func<byte[], T> deserializer)
        {
            _customSerializers[typeof(T)] = obj => serializer((T)obj);
            _customDeserializers[typeof(T)] = bytes => deserializer(bytes);
        }

        public void ClearCustomSerializers()
        {
            _customSerializers.Clear();
            _customDeserializers.Clear();
        }

        public void Clear()
        {
            SerializeCallCount = 0;
            DeserializeCallCount = 0;
            ClearCustomSerializers();
        }

        public SerializationStatistics GetStatistics()
        {
            return SerializationStatistics.Create(SerializeCallCount, DeserializeCallCount, 0, 0);
        }

        public ValidationResult ValidateConfiguration()
        {
            return ValidationResult.Success("MockSerializationService");
        }

        public void Dispose()
        {
            Clear();
        }
    }
}