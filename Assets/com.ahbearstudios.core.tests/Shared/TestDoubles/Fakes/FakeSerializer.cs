using System;
using System.IO;
using System.Text;
using System.Threading;
using Unity.Collections;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Serialization;
using AhBearStudios.Core.Serialization.Models;

namespace AhBearStudios.Core.Tests.Shared.TestDoubles.Fakes
{
    /// <summary>
    /// Fake implementation of ISerializer for TDD testing.
    /// Provides minimal serialization using UTF8 string conversion.
    /// Unity Test Runner compatible for both Edit Mode and Play Mode tests.
    /// </summary>
    public sealed class FakeSerializer : ISerializer
    {
        private bool _isDisposed;

        #region ISerializer Implementation

        public string Name => "FakeSerializer";
        public SerializationFormat Format => SerializationFormat.Binary;
        public bool IsEnabled => true;

        // Basic serialization methods
        public byte[] Serialize<T>(T obj)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(FakeSerializer));

            return Encoding.UTF8.GetBytes(obj?.ToString() ?? string.Empty);
        }

        public T Deserialize<T>(byte[] data)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(FakeSerializer));

            var str = Encoding.UTF8.GetString(data ?? Array.Empty<byte>());
            if (typeof(T) == typeof(string))
                return (T)(object)str;
            return default(T);
        }

        public T Deserialize<T>(ReadOnlySpan<byte> data)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(FakeSerializer));

            var str = Encoding.UTF8.GetString(data);
            if (typeof(T) == typeof(string))
                return (T)(object)str;
            return default(T);
        }

        // Async methods
        public async UniTask<byte[]> SerializeAsync<T>(T obj, CancellationToken cancellationToken = default)
        {
            // Unity Test Runner compatible async - no actual async work
            await UniTask.CompletedTask;
            return Serialize(obj);
        }

        public async UniTask<T> DeserializeAsync<T>(byte[] data, CancellationToken cancellationToken = default)
        {
            // Unity Test Runner compatible async - no actual async work
            await UniTask.CompletedTask;
            return Deserialize<T>(data);
        }

        // Stream operations
        public void SerializeToStream<T>(T obj, Stream stream)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(FakeSerializer));

            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            var data = Serialize(obj);
            stream.Write(data, 0, data.Length);
        }

        public T DeserializeFromStream<T>(Stream stream)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(FakeSerializer));

            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            var data = memoryStream.ToArray();
            return Deserialize<T>(data);
        }

        // NativeArray operations
        public NativeArray<byte> SerializeToNativeArray<T>(T obj, Allocator allocator) where T : unmanaged
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(FakeSerializer));

            var data = Serialize(obj);
            var nativeArray = new NativeArray<byte>(data.Length, allocator);
            nativeArray.CopyFrom(data);
            return nativeArray;
        }

        public T DeserializeFromNativeArray<T>(NativeArray<byte> data) where T : unmanaged
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(FakeSerializer));

            var bytes = data.ToArray();
            return Deserialize<T>(bytes);
        }

        // Try methods
        public bool TryDeserialize<T>(byte[] data, out T result)
        {
            try
            {
                result = Deserialize<T>(data);
                return true;
            }
            catch
            {
                result = default(T);
                return false;
            }
        }

        public bool TryDeserialize<T>(ReadOnlySpan<byte> data, out T result)
        {
            try
            {
                result = Deserialize<T>(data);
                return true;
            }
            catch
            {
                result = default(T);
                return false;
            }
        }

        // Type registration methods - no-op for fake
        public void RegisterType<T>()
        {
            // No-op: fake doesn't require type registration
        }

        public void RegisterType(Type type)
        {
            // No-op: fake doesn't require type registration
        }

        public bool IsRegistered<T>()
        {
            return true; // Fake always considers types registered
        }

        public bool IsRegistered(Type type)
        {
            return true; // Fake always considers types registered
        }

        // Statistics
        public SerializationStatistics GetStatistics()
        {
            return new SerializationStatistics
            {
                TotalSerializations = 0,
                TotalDeserializations = 0,
                FailedOperations = 0,
                TotalBytesProcessed = 0,
                AverageSerializationTimeMs = 1.0,
                AverageDeserializationTimeMs = 1.0,
                PeakMemoryUsage = 0,
                RegisteredTypeCount = 0,
                LastResetTime = DateTime.UtcNow,
                ValidationEnabled = false,
                EncryptionEnabled = false,
                EncryptionAlgorithm = "None",
                EncryptionKeySize = 0,
                TotalOperationTime = TimeSpan.Zero,
                AverageOperationTime = TimeSpan.FromMilliseconds(1.0)
            };
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
            }
        }

        #endregion
    }
}