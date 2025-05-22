using System;
using System.Text.Json;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Interfaces;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Profiling.Interfaces;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// JSON implementation of IMessageSerializer for converting messages to/from a storage format.
    /// Optimized for performance and compatibility.
    /// </summary>
    /// <typeparam name="TMessage">The type of messages to serialize/deserialize.</typeparam>
    public class MessageSerializer<TMessage> : IMessageSerializer<TMessage> where TMessage : IMessage
    {
        private readonly JsonSerializerOptions _serializerOptions;
        private readonly IBurstLogger _logger;
        private readonly IProfiler _profiler;
        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the MessageSerializer class with default options.
        /// </summary>
        /// <param name="logger">Optional logger for serialization operations.</param>
        /// <param name="profiler">Optional profiler for performance monitoring.</param>
        public MessageSerializer(IBurstLogger logger = null, IProfiler profiler = null)
            : this(new JsonSerializerOptions { WriteIndented = false }, logger, profiler)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MessageSerializer class with custom options.
        /// </summary>
        /// <param name="serializerOptions">Custom JSON serializer options.</param>
        /// <param name="logger">Optional logger for serialization operations.</param>
        /// <param name="profiler">Optional profiler for performance monitoring.</param>
        public MessageSerializer(JsonSerializerOptions serializerOptions, IBurstLogger logger = null, IProfiler profiler = null)
        {
            _serializerOptions = serializerOptions ?? throw new ArgumentNullException(nameof(serializerOptions));
            _logger = logger;
            _profiler = profiler;
            _isDisposed = false;
            
            if (_logger != null)
            {
                _logger.Info("MessageSerializer initialized");
            }
        }

        /// <inheritdoc/>
        public string Serialize(TMessage message)
        {
            using (_profiler?.BeginSample("MessageSerializer.Serialize"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessageSerializer<TMessage>));
                }

                if (message == null)
                {
                    throw new ArgumentNullException(nameof(message));
                }

                try
                {
                    string serialized = JsonSerializer.Serialize(message, _serializerOptions);
                    return serialized;
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger.Error($"Error serializing message {message.Id}: {ex.Message}");
                    }
                    
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public TMessage Deserialize(string serializedMessage)
        {
            using (_profiler?.BeginSample("MessageSerializer.Deserialize"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessageSerializer<TMessage>));
                }

                if (string.IsNullOrEmpty(serializedMessage))
                {
                    throw new ArgumentException("Serialized message cannot be null or empty.", nameof(serializedMessage));
                }

                try
                {
                    TMessage message = JsonSerializer.Deserialize<TMessage>(serializedMessage, _serializerOptions);
                    return message;
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger.Error($"Error deserializing message: {ex.Message}");
                    }
                    
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public long GetTimestamp(string serializedMessage)
        {
            using (_profiler?.BeginSample("MessageSerializer.GetTimestamp"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessageSerializer<TMessage>));
                }

                if (string.IsNullOrEmpty(serializedMessage))
                {
                    throw new ArgumentException("Serialized message cannot be null or empty.", nameof(serializedMessage));
                }

                try
                {
                    // Try to extract just the timestamp without deserializing the entire message
                    using JsonDocument doc = JsonDocument.Parse(serializedMessage);
                    if (doc.RootElement.TryGetProperty("Timestamp", out JsonElement timestampElement) &&
                        timestampElement.TryGetInt64(out long timestamp))
                    {
                        return timestamp;
                    }
                    
                    // If we couldn't extract the timestamp directly, fall back to full deserialization
                    TMessage message = Deserialize(serializedMessage);
                    return message.Timestamp;
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger.Error($"Error getting timestamp from serialized message: {ex.Message}");
                    }
                    
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            using (_profiler?.BeginSample("MessageSerializer.Dispose"))
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Releases resources used by the message serializer.
        /// </summary>
        /// <param name="disposing">True if called from Dispose(), false if called from finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                // No unmanaged resources to clean up
                
                if (_logger != null)
                {
                    _logger.Info("MessageSerializer disposed");
                }
            }

            _isDisposed = true;
        }

        /// <summary>
        /// Finalizer to ensure resource cleanup.
        /// </summary>
        ~MessageSerializer()
        {
            Dispose(false);
        }
    }
}