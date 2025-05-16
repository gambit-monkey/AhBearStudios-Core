using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Interfaces;
using AhBearStudios.Core.Profiling;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// File-based implementation of IMessageStore that persists messages to disk.
    /// Ensures messages can be recovered across application restarts.
    /// </summary>
    /// <typeparam name="TMessage">The type of messages to store.</typeparam>
    public class FileMessageStore<TMessage> : IMessageStore<TMessage>, IDisposable where TMessage : IMessage
    {
        private readonly string _storePath;
        private readonly IMessageSerializer<TMessage> _serializer;
        private readonly IBurstLogger _logger;
        private readonly IProfiler _profiler;
        private readonly Dictionary<Guid, StoredMessage> _messageCache;
        private readonly SemaphoreSlim _fileAccessSemaphore;
        private readonly FileSystemWatcher _watcher;
        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the FileMessageStore class.
        /// </summary>
        /// <param name="storePath">The directory path where messages will be stored.</param>
        /// <param name="serializer">The serializer used to convert messages to/from storage format.</param>
        /// <param name="logger">Optional logger for operations.</param>
        /// <param name="profiler">Optional profiler for performance monitoring.</param>
        public FileMessageStore(
            string storePath,
            IMessageSerializer<TMessage> serializer,
            IBurstLogger logger = null,
            IProfiler profiler = null)
        {
            _storePath = storePath ?? throw new ArgumentNullException(nameof(storePath));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _logger = logger;
            _profiler = profiler;
            _messageCache = new Dictionary<Guid, StoredMessage>();
            _fileAccessSemaphore = new SemaphoreSlim(1, 1);
            
            // Ensure the store directory exists
            Directory.CreateDirectory(_storePath);
            
            // Set up file system watcher to keep the cache in sync with the file system
            _watcher = new FileSystemWatcher(_storePath, "*.msg")
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                EnableRaisingEvents = true
            };
            
            _watcher.Created += OnFileChanged;
            _watcher.Changed += OnFileChanged;
            _watcher.Deleted += OnFileDeleted;
            
            // Load existing messages from disk
            LoadMessagesFromDisk();
            
            if (_logger != null)
            {
                _logger.Info($"FileMessageStore initialized at {_storePath}");
            }
        }

        /// <inheritdoc/>
        public void StoreMessage(TMessage message)
        {
            using (_profiler?.BeginSample("FileMessageStore.StoreMessage"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(FileMessageStore<TMessage>));
                }

                if (message == null)
                {
                    throw new ArgumentNullException(nameof(message));
                }

                try
                {
                    _fileAccessSemaphore.Wait();
                    
                    // Serialize the message
                    string serializedMessage = _serializer.Serialize(message);
                    
                    // Generate the file path
                    string filePath = GetMessageFilePath(message.Id);
                    
                    // Write the message to disk
                    File.WriteAllText(filePath, serializedMessage, Encoding.UTF8);
                    
                    // Cache the message
                    _messageCache[message.Id] = new StoredMessage
                    {
                        Id = message.Id,
                        Timestamp = message.Timestamp,
                        SerializedData = serializedMessage,
                        DeserializedMessage = message
                    };
                    
                    if (_logger != null)
                    {
                        _logger.Debug($"Stored message {message.Id} to file {filePath}");
                    }
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger.Error($"Error storing message {message.Id}: {ex.Message}");
                    }
                    
                    throw;
                }
                finally
                {
                    _fileAccessSemaphore.Release();
                }
            }
        }

        /// <inheritdoc/>
        public async Task StoreMessageAsync(TMessage message, CancellationToken cancellationToken = default)
        {
            using (_profiler?.BeginSample("FileMessageStore.StoreMessageAsync"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(FileMessageStore<TMessage>));
                }

                if (message == null)
                {
                    throw new ArgumentNullException(nameof(message));
                }

                try
                {
                    await _fileAccessSemaphore.WaitAsync(cancellationToken);
                    
                    // Serialize the message
                    string serializedMessage = _serializer.Serialize(message);
                    
                    // Generate the file path
                    string filePath = GetMessageFilePath(message.Id);
                    
                    // Write the message to disk
                    await File.WriteAllTextAsync(filePath, serializedMessage, Encoding.UTF8, cancellationToken);
                    
                    // Cache the message
                    _messageCache[message.Id] = new StoredMessage
                    {
                        Id = message.Id,
                        Timestamp = message.Timestamp,
                        SerializedData = serializedMessage,
                        DeserializedMessage = message
                    };
                    
                    if (_logger != null)
                    {
                        _logger.Debug($"Stored message {message.Id} to file {filePath} asynchronously");
                    }
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger.Error($"Error storing message {message.Id} asynchronously: {ex.Message}");
                    }
                    
                    throw;
                }
                finally
                {
                    _fileAccessSemaphore.Release();
                }
            }
        }

        /// <inheritdoc/>
        public TMessage GetMessage(Guid messageId)
        {
            using (_profiler?.BeginSample("FileMessageStore.GetMessage"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(FileMessageStore<TMessage>));
                }

                try
                {
                    _fileAccessSemaphore.Wait();
                    
                    // Check if the message is in the cache
                    if (_messageCache.TryGetValue(messageId, out StoredMessage storedMessage))
                    {
                        // If we already have a deserialized message, return it
                        if (storedMessage.DeserializedMessage != null)
                        {
                            return storedMessage.DeserializedMessage;
                        }
                        
                        // Otherwise, deserialize it
                        TMessage message = _serializer.Deserialize(storedMessage.SerializedData);
                        storedMessage.DeserializedMessage = message;
                        return message;
                    }
                    
                    // If not in cache, check if the file exists
                    string filePath = GetMessageFilePath(messageId);
                    if (File.Exists(filePath))
                    {
                        // Read and deserialize the message
                        string serializedMessage = File.ReadAllText(filePath, Encoding.UTF8);
                        TMessage message = _serializer.Deserialize(serializedMessage);
                        
                        // Cache the message
                        _messageCache[messageId] = new StoredMessage
                        {
                            Id = messageId,
                            Timestamp = message.Timestamp,
                            SerializedData = serializedMessage,
                            DeserializedMessage = message
                        };
                        
                        return message;
                    }
                    
                    // Message not found
                    if (_logger != null)
                    {
                        _logger.Warning($"Message {messageId} not found in store");
                    }
                    
                    return default;
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger.Error($"Error getting message {messageId}: {ex.Message}");
                    }
                    
                    throw;
                }
                finally
                {
                    _fileAccessSemaphore.Release();
                }
            }
        }

        /// <inheritdoc/>
        public async Task<TMessage> GetMessageAsync(Guid messageId, CancellationToken cancellationToken = default)
        {
            using (_profiler?.BeginSample("FileMessageStore.GetMessageAsync"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(FileMessageStore<TMessage>));
                }

                try
                {
                    await _fileAccessSemaphore.WaitAsync(cancellationToken);
                    
                    // Check if the message is in the cache
                    if (_messageCache.TryGetValue(messageId, out StoredMessage storedMessage))
                    {
                        // If we already have a deserialized message, return it
                        if (storedMessage.DeserializedMessage != null)
                        {
                            return storedMessage.DeserializedMessage;
                        }
                        
                        // Otherwise, deserialize it
                        TMessage message = _serializer.Deserialize(storedMessage.SerializedData);
                        storedMessage.DeserializedMessage = message;
                        return message;
                    }
                    
                    // If not in cache, check if the file exists
                    string filePath = GetMessageFilePath(messageId);
                    if (File.Exists(filePath))
                    {
                        // Read and deserialize the message
                        string serializedMessage = await File.ReadAllTextAsync(filePath, Encoding.UTF8, cancellationToken);
                        TMessage message = _serializer.Deserialize(serializedMessage);
                        
                        // Cache the message
                        _messageCache[messageId] = new StoredMessage
                        {
                            Id = messageId,
                            Timestamp = message.Timestamp,
                            SerializedData = serializedMessage,
                            DeserializedMessage = message
                        };
                        
                        return message;
                    }
                    
                    // Message not found
                    if (_logger != null)
                    {
                        _logger.Warning($"Message {messageId} not found in store asynchronously");
                    }
                    
                    return default;
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger.Error($"Error getting message {messageId} asynchronously: {ex.Message}");
                    }
                    
                    throw;
                }
                finally
                {
                    _fileAccessSemaphore.Release();
                }
            }
        }

        /// <inheritdoc/>
        public void RemoveMessage(Guid messageId)
        {
            using (_profiler?.BeginSample("FileMessageStore.RemoveMessage"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(FileMessageStore<TMessage>));
                }

                try
                {
                    _fileAccessSemaphore.Wait();
                    
                    // Generate the file path
                    string filePath = GetMessageFilePath(messageId);
                    
                    // Delete the file if it exists
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        
                        // Remove from cache
                        _messageCache.Remove(messageId);
                        
                        if (_logger != null)
                        {
                            _logger.Debug($"Removed message {messageId} from store");
                        }
                    }
                    else if (_logger != null)
                    {
                        _logger.Warning($"Message {messageId} not found in store for removal");
                    }
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger.Error($"Error removing message {messageId}: {ex.Message}");
                    }
                    
                    throw;
                }
                finally
                {
                    _fileAccessSemaphore.Release();
                }
            }
        }

        /// <inheritdoc/>
        public async Task RemoveMessageAsync(Guid messageId, CancellationToken cancellationToken = default)
        {
            using (_profiler?.BeginSample("FileMessageStore.RemoveMessageAsync"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(FileMessageStore<TMessage>));
                }

                try
                {
                    await _fileAccessSemaphore.WaitAsync(cancellationToken);
                    
                    // Generate the file path
                    string filePath = GetMessageFilePath(messageId);
                    
                    // Delete the file if it exists
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        
                        // Remove from cache
                        _messageCache.Remove(messageId);
                        
                        if (_logger != null)
                        {
                            _logger.Debug($"Removed message {messageId} from store asynchronously");
                        }
                    }
                    else if (_logger != null)
                    {
                        _logger.Warning($"Message {messageId} not found in store for asynchronous removal");
                    }
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger.Error($"Error removing message {messageId} asynchronously: {ex.Message}");
                    }
                    
                    throw;
                }
                finally
                {
                    _fileAccessSemaphore.Release();
                }
            }
        }

        /// <inheritdoc/>
        public int GetMessageCount()
        {
            using (_profiler?.BeginSample("FileMessageStore.GetMessageCount"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(FileMessageStore<TMessage>));
                }

                try
                {
                    _fileAccessSemaphore.Wait();
                    
                    return _messageCache.Count;
                }
                finally
                {
                    _fileAccessSemaphore.Release();
                }
            }
        }

        /// <inheritdoc/>
        public List<Guid> GetMessageIds()
        {
            using (_profiler?.BeginSample("FileMessageStore.GetMessageIds"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(FileMessageStore<TMessage>));
                }

                try
                {
                    _fileAccessSemaphore.Wait();
                    
                    return _messageCache.Keys.ToList();
                }
                finally
                {
                    _fileAccessSemaphore.Release();
                }
            }
        }

        /// <inheritdoc/>
        public List<TMessage> GetAllMessages()
        {
            using (_profiler?.BeginSample("FileMessageStore.GetAllMessages"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(FileMessageStore<TMessage>));
                }

                List<TMessage> messages = new List<TMessage>();
                
                try
                {
                    _fileAccessSemaphore.Wait();
                    
                    foreach (var messageId in _messageCache.Keys)
                    {
                        TMessage message = GetMessage(messageId);
                        if (message != null)
                        {
                            messages.Add(message);
                        }
                    }
                    
                    return messages;
                }
                finally
                {
                    _fileAccessSemaphore.Release();
                }
            }
        }

        /// <inheritdoc/>
        public async Task<List<TMessage>> GetAllMessagesAsync(CancellationToken cancellationToken = default)
        {
            using (_profiler?.BeginSample("FileMessageStore.GetAllMessagesAsync"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(FileMessageStore<TMessage>));
                }

                List<TMessage> messages = new List<TMessage>();
                
                try
                {
                    await _fileAccessSemaphore.WaitAsync(cancellationToken);
                    
                    foreach (var messageId in _messageCache.Keys)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        
                        TMessage message = await GetMessageAsync(messageId, cancellationToken);
                        if (message != null)
                        {
                            messages.Add(message);
                        }
                    }
                    
                    return messages;
                }
                finally
                {
                    _fileAccessSemaphore.Release();
                }
            }
        }

        /// <inheritdoc/>
        public void ClearMessages()
        {
            using (_profiler?.BeginSample("FileMessageStore.ClearMessages"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(FileMessageStore<TMessage>));
                }

                try
                {
                    _fileAccessSemaphore.Wait();
                    
                    // Temporarily disable the file watcher to avoid triggering events
                    _watcher.EnableRaisingEvents = false;
                    
                    // Get the message IDs before clearing the cache
                    var messageIds = _messageCache.Keys.ToList();
                    
                    // Clear the cache
                    _messageCache.Clear();
                    
                    // Delete all message files
                    foreach (var messageId in messageIds)
                    {
                        string filePath = GetMessageFilePath(messageId);
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                        }
                    }
                    
                    if (_logger != null)
                    {
                        _logger.Info($"Cleared {messageIds.Count} messages from store");
                    }
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger.Error($"Error clearing messages: {ex.Message}");
                    }
                    
                    throw;
                }
                finally
                {
                    // Re-enable the file watcher
                    _watcher.EnableRaisingEvents = true;
                    _fileAccessSemaphore.Release();
                }
            }
        }

        /// <inheritdoc/>
        public async Task ClearMessagesAsync(CancellationToken cancellationToken = default)
        {
            using (_profiler?.BeginSample("FileMessageStore.ClearMessagesAsync"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(FileMessageStore<TMessage>));
                }

                try
                {
                    await _fileAccessSemaphore.WaitAsync(cancellationToken);
                    
                    // Temporarily disable the file watcher to avoid triggering events
                    _watcher.EnableRaisingEvents = false;
                    
                    // Get the message IDs before clearing the cache
                    var messageIds = _messageCache.Keys.ToList();
                    
                    // Clear the cache
                    _messageCache.Clear();
                    
                    // Delete all message files
                    foreach (var messageId in messageIds)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        
                        string filePath = GetMessageFilePath(messageId);
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                        }
                    }
                    
                    if (_logger != null)
                    {
                        _logger.Info($"Cleared {messageIds.Count} messages from store asynchronously");
                    }
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger.Error($"Error clearing messages asynchronously: {ex.Message}");
                    }
                    
                    throw;
                }
                finally
                {
                    // Re-enable the file watcher
                    _watcher.EnableRaisingEvents = true;
                    _fileAccessSemaphore.Release();
                }
            }
        }

        /// <summary>
        /// Gets the full file path for a message with the specified ID.
        /// </summary>
        /// <param name="messageId">The ID of the message.</param>
        /// <returns>The full file path for the message.</returns>
        private string GetMessageFilePath(Guid messageId)
        {
            return Path.Combine(_storePath, $"{messageId}.msg");
        }

        /// <summary>
        /// Loads all existing messages from disk into the cache.
        /// </summary>
        private void LoadMessagesFromDisk()
        {
            using (_profiler?.BeginSample("FileMessageStore.LoadMessagesFromDisk"))
            {
                try
                {
                    _fileAccessSemaphore.Wait();
                    
                    // Get all message files
                    string[] messageFiles = Directory.GetFiles(_storePath, "*.msg");
                    
                    int loadedCount = 0;
                    
                    foreach (string filePath in messageFiles)
                    {
                        try
                        {
                            // Extract the message ID from the file name
                            string fileName = Path.GetFileNameWithoutExtension(filePath);
                            if (Guid.TryParse(fileName, out Guid messageId))
                            {
                                // Read the serialized message
                                string serializedMessage = File.ReadAllText(filePath, Encoding.UTF8);
                                
                                // Try to get the timestamp without fully deserializing
                                long timestamp = _serializer.GetTimestamp(serializedMessage);
                                
                                // Cache the message without deserializing it yet
                                _messageCache[messageId] = new StoredMessage
                                {
                                    Id = messageId,
                                    Timestamp = timestamp,
                                    SerializedData = serializedMessage,
                                    DeserializedMessage = null
                                };
                                
                                loadedCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            if (_logger != null)
                            {
                                _logger.Warning($"Error loading message from file {filePath}: {ex.Message}");
                            }
                        }
                    }
                    
                    if (_logger != null)
                    {
                        _logger.Info($"Loaded {loadedCount} messages from disk");
                    }
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger.Error($"Error loading messages from disk: {ex.Message}");
                    }
                }
                finally
                {
                    _fileAccessSemaphore.Release();
                }
            }
        }

        /// <summary>
        /// Handles the file system watcher's file changed or created event.
        /// </summary>
        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            using (_profiler?.BeginSample("FileMessageStore.OnFileChanged"))
            {
                try
                {
                    _fileAccessSemaphore.Wait();
                    
                    string fileName = Path.GetFileNameWithoutExtension(e.Name);
                    if (Guid.TryParse(fileName, out Guid messageId))
                    {
                        // Only update if the file exists
                        if (File.Exists(e.FullPath))
                        {
                            try
                            {
                                // Read the serialized message
                                string serializedMessage = File.ReadAllText(e.FullPath, Encoding.UTF8);
                                
                                // Try to get the timestamp without fully deserializing
                                long timestamp = _serializer.GetTimestamp(serializedMessage);
                                
                                // Update the cache
                                _messageCache[messageId] = new StoredMessage
                                {
                                    Id = messageId,
                                    Timestamp = timestamp,
                                    SerializedData = serializedMessage,
                                    DeserializedMessage = null
                                };
                                
                                if (_logger != null)
                                {
                                    _logger.Debug($"Updated message {messageId} in cache from file system change");
                                }
                            }
                            catch (Exception ex)
                            {
                                if (_logger != null)
                                {
                                    _logger.Warning($"Error updating message {messageId} from file: {ex.Message}");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger.Error($"Error handling file change event: {ex.Message}");
                    }
                }
                finally
                {
                    _fileAccessSemaphore.Release();
                }
            }
        }

        /// <summary>
        /// Handles the file system watcher's file deleted event.
        /// </summary>
        private void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            using (_profiler?.BeginSample("FileMessageStore.OnFileDeleted"))
            {
                try
                {
                    _fileAccessSemaphore.Wait();
                    
                    string fileName = Path.GetFileNameWithoutExtension(e.Name);
                    if (Guid.TryParse(fileName, out Guid messageId))
                    {
                        // Remove from cache if the file was deleted
                        _messageCache.Remove(messageId);
                        
                        if (_logger != null)
                        {
                            _logger.Debug($"Removed message {messageId} from cache due to file deletion");
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger.Error($"Error handling file deletion event: {ex.Message}");
                    }
                }
                finally
                {
                    _fileAccessSemaphore.Release();
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            using (_profiler?.BeginSample("FileMessageStore.Dispose"))
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Releases resources used by the file message store.
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
                // Dispose the file system watcher
                _watcher.Created -= OnFileChanged;
                _watcher.Changed -= OnFileChanged;
                _watcher.Deleted -= OnFileDeleted;
                _watcher.Dispose();
                
                // Dispose the semaphore
                _fileAccessSemaphore.Dispose();
                
                if (_logger != null)
                {
                    _logger.Info("FileMessageStore disposed");
                }
            }

            _isDisposed = true;
        }

        /// <summary>
        /// Finalizer to ensure resource cleanup.
        /// </summary>
        ~FileMessageStore()
        {
            Dispose(false);
        }

        /// <summary>
        /// Represents a stored message in the cache.
        /// </summary>
        private class StoredMessage
        {
            public Guid Id { get; set; }
            public long Timestamp { get; set; }
            public string SerializedData { get; set; }
            public TMessage DeserializedMessage { get; set; }
        }
    }
}