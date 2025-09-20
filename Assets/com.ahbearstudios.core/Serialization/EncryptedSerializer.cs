using System;
using System.IO;
using System.Security.Cryptography;
using CipherMode = System.Security.Cryptography.CipherMode;
using PaddingMode = System.Security.Cryptography.PaddingMode;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Serialization.Models;
using SerializationException = AhBearStudios.Core.Serialization.Models.SerializationException;

namespace AhBearStudios.Core.Serialization
{
    /// <summary>
    /// Decorator serializer that adds AES encryption/decryption to any ISerializer implementation.
    /// Provides secure serialization with configurable encryption algorithms and key management.
    /// </summary>
    public class EncryptedSerializer : ISerializer, IDisposable
    {
        private readonly ISerializer _innerSerializer;
        private readonly ILoggingService _logger;
        private readonly EncryptionConfig _encryptionConfig;
        private readonly ICryptoTransform _encryptor;
        private readonly ICryptoTransform _decryptor;
        private readonly Aes _aes;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of EncryptedSerializer.
        /// </summary>
        /// <param name="innerSerializer">The serializer to wrap with encryption</param>
        /// <param name="encryptionKey">Encryption key for AES encryption</param>
        /// <param name="logger">Logging service</param>
        /// <param name="config">Optional encryption configuration</param>
        /// <exception cref="ArgumentNullException">Thrown when any required parameter is null</exception>
        /// <exception cref="ArgumentException">Thrown when encryption key is invalid</exception>
        public EncryptedSerializer(
            ISerializer innerSerializer,
            FixedString128Bytes encryptionKey,
            ILoggingService logger,
            EncryptionConfig config = null)
        {
            _innerSerializer = innerSerializer ?? throw new ArgumentNullException(nameof(innerSerializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (encryptionKey.IsEmpty)
                throw new ArgumentException("Encryption key cannot be empty", nameof(encryptionKey));

            _encryptionConfig = config ?? EncryptionConfig.Default;

            // Initialize AES encryption
            _aes = Aes.Create();
            _aes.Mode = _encryptionConfig.CipherMode;
            _aes.Padding = _encryptionConfig.PaddingMode;
            _aes.KeySize = _encryptionConfig.KeySize;

            // Derive key and IV from the provided key
            var keyBytes = DeriveKeyFromString(encryptionKey.ToString(), _encryptionConfig.KeySize / 8);
            var ivBytes = DeriveIVFromKey(keyBytes, _aes.BlockSize / 8);

            _aes.Key = keyBytes;
            _aes.IV = ivBytes;

            _encryptor = _aes.CreateEncryptor();
            _decryptor = _aes.CreateDecryptor();

            var correlationId = GetCorrelationId();
            _logger.LogInfo($"EncryptedSerializer initialized wrapping {innerSerializer.GetType().Name} with {_encryptionConfig.Algorithm} encryption", correlationId: correlationId, sourceContext: null, properties: null);
        }

        /// <inheritdoc />
        public byte[] Serialize<T>(T obj)
        {
            ThrowIfDisposed();

            var correlationId = GetCorrelationId();
            
            try
            {
                _logger.LogInfo($"Encrypting serialized data for type {typeof(T).Name}", correlationId: correlationId, sourceContext: null, properties: null);

                // Serialize with inner serializer first
                var plainData = _innerSerializer.Serialize(obj);

                // Encrypt the serialized data
                var encryptedData = EncryptData(plainData);

                _logger.LogInfo($"Successfully encrypted {plainData.Length} bytes to {encryptedData.Length} bytes", correlationId: correlationId, sourceContext: null, properties: null);

                return encryptedData;
            }
            catch (Exception ex)
            {
                _logger.LogException($"Failed to encrypt serialized data for type {typeof(T).Name}", ex, correlationId);
                throw new SerializationException($"Encryption failed during serialization of type {typeof(T).Name}", typeof(T), "Encrypt", ex);
            }
        }

        /// <inheritdoc />
        public T Deserialize<T>(byte[] data)
        {
            ThrowIfDisposed();

            var correlationId = GetCorrelationId();

            try
            {
                _logger.LogInfo($"Decrypting data for type {typeof(T).Name} from {data?.Length ?? 0} bytes", correlationId: correlationId, sourceContext: null, properties: null);

                // Decrypt the data first
                var plainData = DecryptData(data);

                // Deserialize with inner serializer
                var result = _innerSerializer.Deserialize<T>(plainData);

                _logger.LogInfo($"Successfully decrypted and deserialized {data.Length} bytes to {typeof(T).Name}", correlationId: correlationId, sourceContext: null, properties: null);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogException($"Failed to decrypt data for type {typeof(T).Name}", ex, correlationId);
                throw new SerializationException($"Decryption failed during deserialization of type {typeof(T).Name}", typeof(T), "Decrypt", ex);
            }
        }

        /// <inheritdoc />
        public T Deserialize<T>(ReadOnlySpan<byte> data)
        {
            return Deserialize<T>(data.ToArray());
        }

        /// <inheritdoc />
        public bool TryDeserialize<T>(byte[] data, out T result)
        {
            result = default;

            try
            {
                result = Deserialize<T>(data);
                return true;
            }
            catch (Exception ex)
            {
                var correlationId = GetCorrelationId();
                _logger.LogError($"TryDeserialize with decryption failed for type {typeof(T).Name}: {ex.Message}", correlationId: correlationId, sourceContext: null, properties: null);
                return false;
            }
        }

        /// <inheritdoc />
        public bool TryDeserialize<T>(ReadOnlySpan<byte> data, out T result)
        {
            return TryDeserialize(data.ToArray(), out result);
        }

        /// <inheritdoc />
        public void RegisterType<T>()
        {
            _innerSerializer.RegisterType<T>();
        }

        /// <inheritdoc />
        public void RegisterType(Type type)
        {
            _innerSerializer.RegisterType(type);
        }

        /// <inheritdoc />
        public bool IsRegistered<T>()
        {
            return _innerSerializer.IsRegistered<T>();
        }

        /// <inheritdoc />
        public bool IsRegistered(Type type)
        {
            return _innerSerializer.IsRegistered(type);
        }

        /// <inheritdoc />
        public async UniTask<byte[]> SerializeAsync<T>(T obj, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            var correlationId = GetCorrelationId();

            try
            {
                _logger.LogInfo($"Async encrypting serialized data for type {typeof(T).Name}", correlationId: correlationId, sourceContext: null, properties: null);

                // Serialize with inner serializer first
                var plainData = await _innerSerializer.SerializeAsync(obj, cancellationToken);

                // Encrypt the serialized data (run on thread pool to avoid blocking)
                var encryptedData = await UniTask.RunOnThreadPool(() => EncryptData(plainData), cancellationToken: cancellationToken);

                _logger.LogInfo($"Successfully async encrypted {plainData.Length} bytes to {encryptedData.Length} bytes", correlationId: correlationId, sourceContext: null, properties: null);

                return encryptedData;
            }
            catch (Exception ex)
            {
                _logger.LogException($"Failed to async encrypt serialized data for type {typeof(T).Name}", ex, correlationId);
                throw new SerializationException($"Async encryption failed during serialization of type {typeof(T).Name}", typeof(T), "EncryptAsync", ex);
            }
        }

        /// <inheritdoc />
        public async UniTask<T> DeserializeAsync<T>(byte[] data, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            var correlationId = GetCorrelationId();

            try
            {
                _logger.LogInfo($"Async decrypting data for type {typeof(T).Name} from {data?.Length ?? 0} bytes", correlationId: correlationId, sourceContext: null, properties: null);

                // Decrypt the data first (run on thread pool to avoid blocking)
                var plainData = await UniTask.RunOnThreadPool(() => DecryptData(data), cancellationToken: cancellationToken);

                // Deserialize with inner serializer
                var result = await _innerSerializer.DeserializeAsync<T>(plainData, cancellationToken);

                _logger.LogInfo($"Successfully async decrypted and deserialized {data.Length} bytes to {typeof(T).Name}", correlationId: correlationId, sourceContext: null, properties: null);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogException($"Failed to async decrypt data for type {typeof(T).Name}", ex, correlationId);
                throw new SerializationException($"Async decryption failed during deserialization of type {typeof(T).Name}", typeof(T), "DecryptAsync", ex);
            }
        }

        /// <inheritdoc />
        public void SerializeToStream<T>(T obj, Stream stream)
        {
            ThrowIfDisposed();

            var correlationId = GetCorrelationId();

            try
            {
                _logger.LogInfo($"Encrypting serialized data for type {typeof(T).Name} to stream", correlationId: correlationId, sourceContext: null, properties: null);

                // Create a temporary memory stream for the inner serializer
                using var tempStream = new MemoryStream();
                _innerSerializer.SerializeToStream(obj, tempStream);
                
                var plainData = tempStream.ToArray();
                var encryptedData = EncryptData(plainData);

                // Write encrypted data to the target stream
                stream.Write(encryptedData, 0, encryptedData.Length);

                _logger.LogInfo($"Successfully encrypted {plainData.Length} bytes to stream", correlationId: correlationId, sourceContext: null, properties: null);
            }
            catch (Exception ex)
            {
                _logger.LogException($"Failed to encrypt serialized data to stream for type {typeof(T).Name}", ex, correlationId);
                throw;
            }
        }

        /// <inheritdoc />
        public T DeserializeFromStream<T>(Stream stream)
        {
            ThrowIfDisposed();

            var correlationId = GetCorrelationId();

            try
            {
                _logger.LogInfo($"Decrypting data for type {typeof(T).Name} from stream", correlationId: correlationId, sourceContext: null, properties: null);

                // Read all encrypted data from stream
                using var memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                var encryptedData = memoryStream.ToArray();

                // Decrypt the data
                var plainData = DecryptData(encryptedData);

                // Deserialize using inner serializer
                using var plainStream = new MemoryStream(plainData);
                var result = _innerSerializer.DeserializeFromStream<T>(plainStream);

                _logger.LogInfo($"Successfully decrypted and deserialized {encryptedData.Length} bytes from stream to {typeof(T).Name}", correlationId: correlationId, sourceContext: null, properties: null);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogException($"Failed to decrypt data from stream for type {typeof(T).Name}", ex, correlationId);
                throw;
            }
        }

        /// <inheritdoc />
        public NativeArray<byte> SerializeToNativeArray<T>(T obj, Allocator allocator) where T : unmanaged
        {
            ThrowIfDisposed();

            var encryptedData = Serialize(obj);
            var nativeArray = new NativeArray<byte>(encryptedData.Length, allocator);
            
            for (int i = 0; i < encryptedData.Length; i++)
            {
                nativeArray[i] = encryptedData[i];
            }

            return nativeArray;
        }

        /// <inheritdoc />
        public T DeserializeFromNativeArray<T>(NativeArray<byte> data) where T : unmanaged
        {
            ThrowIfDisposed();

            var managedArray = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                managedArray[i] = data[i];
            }

            return Deserialize<T>(managedArray);
        }

        /// <inheritdoc />
        public SerializationStatistics GetStatistics()
        {
            var baseStats = _innerSerializer.GetStatistics();
            
            // Add encryption-specific metadata
            return baseStats with
            {
                EncryptionEnabled = true,
                EncryptionAlgorithm = _encryptionConfig.Algorithm,
                EncryptionKeySize = _encryptionConfig.KeySize
            };
        }

        /// <summary>
        /// Gets the encryption configuration being used.
        /// </summary>
        /// <returns>Current encryption configuration</returns>
        public EncryptionConfig GetEncryptionConfig()
        {
            return _encryptionConfig;
        }

        private byte[] EncryptData(byte[] plainData)
        {
            if (plainData == null || plainData.Length == 0)
                return plainData;

            using var memoryStream = new MemoryStream();
            using var cryptoStream = new CryptoStream(memoryStream, _encryptor, CryptoStreamMode.Write);
            
            cryptoStream.Write(plainData, 0, plainData.Length);
            cryptoStream.FlushFinalBlock();
            
            return memoryStream.ToArray();
        }

        private byte[] DecryptData(byte[] encryptedData)
        {
            if (encryptedData == null || encryptedData.Length == 0)
                return encryptedData;

            using var memoryStream = new MemoryStream(encryptedData);
            using var cryptoStream = new CryptoStream(memoryStream, _decryptor, CryptoStreamMode.Read);
            using var resultStream = new MemoryStream();
            
            cryptoStream.CopyTo(resultStream);
            return resultStream.ToArray();
        }

        private static byte[] DeriveKeyFromString(string keyString, int keySize)
        {
            using var sha256 = SHA256.Create();
            var keyBytes = System.Text.Encoding.UTF8.GetBytes(keyString);
            var hashedKey = sha256.ComputeHash(keyBytes);
            
            // Ensure we have the right key size
            var finalKey = new byte[keySize];
            Array.Copy(hashedKey, finalKey, Math.Min(hashedKey.Length, keySize));
            
            return finalKey;
        }

        private static byte[] DeriveIVFromKey(byte[] key, int ivSize)
        {
            using var md5 = MD5.Create();
            var iv = md5.ComputeHash(key);
            
            // Ensure we have the right IV size
            var finalIV = new byte[ivSize];
            Array.Copy(iv, finalIV, Math.Min(iv.Length, ivSize));
            
            return finalIV;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(EncryptedSerializer));
        }

        private FixedString64Bytes GetCorrelationId()
        {
            return new FixedString64Bytes(Guid.NewGuid().ToString("N")[..32]);
        }

        /// <summary>
        /// Disposes the encrypted serializer and releases cryptographic resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _encryptor?.Dispose();
                _decryptor?.Dispose();
                _aes?.Dispose();
                
                // Dispose inner serializer if it implements IDisposable
                if (_innerSerializer is IDisposable disposableSerializer)
                {
                    disposableSerializer.Dispose();
                }

                _disposed = true;

                var correlationId = GetCorrelationId();
                _logger.LogInfo("EncryptedSerializer disposed", correlationId: correlationId, sourceContext: null, properties: null);
            }
        }
    }

    /// <summary>
    /// Configuration for encryption settings.
    /// </summary>
    public record EncryptionConfig
    {
        /// <summary>
        /// The encryption algorithm name.
        /// </summary>
        public string Algorithm { get; init; } = "AES";

        /// <summary>
        /// The encryption key size in bits.
        /// </summary>
        public int KeySize { get; init; } = 256;

        /// <summary>
        /// The cipher mode for encryption.
        /// </summary>
        public CipherMode CipherMode { get; init; } = CipherMode.CBC;

        /// <summary>
        /// The padding mode for encryption.
        /// </summary>
        public PaddingMode PaddingMode { get; init; } = PaddingMode.PKCS7;

        /// <summary>
        /// Default encryption configuration with secure defaults.
        /// </summary>
        public static EncryptionConfig Default => new();

        /// <summary>
        /// High-security encryption configuration.
        /// </summary>
        public static EncryptionConfig HighSecurity => new()
        {
            Algorithm = "AES",
            KeySize = 256,
            CipherMode = CipherMode.CBC,
            PaddingMode = PaddingMode.PKCS7
        };

        /// <summary>
        /// Performance-optimized encryption configuration.
        /// </summary>
        public static EncryptionConfig Performance => new()
        {
            Algorithm = "AES",
            KeySize = 128,
            CipherMode = CipherMode.ECB,
            PaddingMode = PaddingMode.PKCS7
        };
    }
}