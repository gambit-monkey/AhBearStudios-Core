using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Core.Logging.Targets
{
    /// <summary>
    /// A simple file-based log target for persistent logging.
    /// Provides basic file writing capabilities with thread-safe operations.
    /// </summary>
    internal sealed class FileLogTarget : ILogTarget
    {
        private readonly ILogTargetConfig _config;
        private readonly string _filePath;
        private readonly object _writeLock = new object();
        private readonly StringBuilder _stringBuilder = new StringBuilder(1024);
        private readonly bool _autoFlush;
        private readonly long _maxFileSize;
        private readonly int _maxBackupFiles;
        private readonly string _messageFormat;
        
        private StreamWriter _writer;
        private bool _disposed = false;
        private long _messagesWritten = 0;
        private long _messagesDropped = 0;
        private long _errorsEncountered = 0;
        private DateTime _lastWriteTime = DateTime.MinValue;
        private Exception _lastError = null;

        public string Name { get; }
        public LogLevel MinimumLevel { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsHealthy => !_disposed && _writer != null && File.Exists(_filePath);
        public IReadOnlyList<string> Channels { get; }

        /// <summary>
        /// Gets the number of messages successfully written to file.
        /// </summary>
        public long MessagesWritten => _messagesWritten;

        /// <summary>
        /// Gets the number of messages dropped due to errors or filtering.
        /// </summary>
        public long MessagesDropped => _messagesDropped;

        /// <summary>
        /// Gets the number of errors encountered during file operations.
        /// </summary>
        public long ErrorsEncountered => _errorsEncountered;

        /// <summary>
        /// Gets the path to the log file.
        /// </summary>
        public string FilePath => _filePath;

        public FileLogTarget(ILogTargetConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            
            Name = config.Name;
            MinimumLevel = config.MinimumLevel;
            IsEnabled = config.IsEnabled;
            Channels = config.Channels;

            // Get file path from configSo or use default
            _filePath = GetConfigProperty("FilePath", Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AhBearStudios", "Logs", $"{Name}.log"));

            // Configuration options
            _autoFlush = GetConfigProperty("AutoFlush", true);
            _maxFileSize = GetConfigProperty("MaxFileSize", 10 * 1024 * 1024L); // 10MB default
            _maxBackupFiles = GetConfigProperty("MaxBackupFiles", 5);
            _messageFormat = GetConfigProperty("MessageFormat", "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level}] [{Channel}] {Message}");

            InitializeFileWriter();
        }

        public void Write(in LogMessage logMessage)
        {
            if (!ShouldProcessMessage(logMessage)) 
            {
                Interlocked.Increment(ref _messagesDropped);
                return;
            }

            try
            {
                lock (_writeLock)
                {
                    if (_disposed || _writer == null)
                    {
                        Interlocked.Increment(ref _messagesDropped);
                        return;
                    }

                    // Check if file rotation is needed
                    if (ShouldRotateFile())
                    {
                        RotateFile();
                    }

                    var formattedMessage = FormatMessage(logMessage);
                    _writer.WriteLine(formattedMessage);
                    
                    if (_autoFlush)
                    {
                        _writer.Flush();
                    }

                    Interlocked.Increment(ref _messagesWritten);
                    _lastWriteTime = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _errorsEncountered);
                Interlocked.Increment(ref _messagesDropped);
                _lastError = ex;
                
                // Try to reinitialize writer on error
                TryReinitializeWriter();
            }
        }

        public void WriteBatch(IReadOnlyList<LogMessage> logMessages)
        {
            if (logMessages == null || logMessages.Count == 0) return;

            try
            {
                lock (_writeLock)
                {
                    if (_disposed || _writer == null) return;

                    // Check if file rotation is needed before batch write
                    if (ShouldRotateFile())
                    {
                        RotateFile();
                    }

                    foreach (var message in logMessages)
                    {
                        if (ShouldProcessMessage(message))
                        {
                            var formattedMessage = FormatMessage(message);
                            _writer.WriteLine(formattedMessage);
                            Interlocked.Increment(ref _messagesWritten);
                        }
                        else
                        {
                            Interlocked.Increment(ref _messagesDropped);
                        }
                    }

                    if (_autoFlush)
                    {
                        _writer.Flush();
                    }

                    _lastWriteTime = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _errorsEncountered);
                _lastError = ex;
                TryReinitializeWriter();
            }
        }

        public bool ShouldProcessMessage(in LogMessage logMessage)
        {
            if (!IsEnabled || _disposed) return false;
            
            if (logMessage.Level < MinimumLevel) return false;
            
            // Check channel filtering
            if (Channels.Count > 0)
            {
                var messageChannel = logMessage.Channel.ToString();
                var channelFound = false;
                
                foreach (var channel in Channels)
                {
                    if (string.Equals(channel, messageChannel, StringComparison.OrdinalIgnoreCase))
                    {
                        channelFound = true;
                        break;
                    }
                }
                
                if (!channelFound) return false;
            }
            
            return true;
        }

        public void Flush()
        {
            try
            {
                lock (_writeLock)
                {
                    _writer?.Flush();
                }
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _errorsEncountered);
                _lastError = ex;
            }
        }

        public bool PerformHealthCheck()
        {
            try
            {
                if (_disposed) return false;

                lock (_writeLock)
                {
                    // Check if file exists and is writable
                    if (!File.Exists(_filePath)) return false;
                    
                    // Check if writer is still functional
                    if (_writer == null) return false;

                    // Perform a test write
                    var testMessage = $"# Health check - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}";
                    _writer.WriteLine(testMessage);
                    _writer.Flush();
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                _lastError = ex;
                return false;
            }
        }

        /// <summary>
        /// Gets performance statistics for this target.
        /// </summary>
        /// <returns>A dictionary containing performance data</returns>
        public Dictionary<string, object> GetStatistics()
        {
            var fileInfo = File.Exists(_filePath) ? new FileInfo(_filePath) : null;
            var totalMessages = _messagesWritten + _messagesDropped;
            var errorRate = totalMessages > 0 ? (double)_errorsEncountered / totalMessages : 0.0;

            return new Dictionary<string, object>
            {
                ["MessagesWritten"] = _messagesWritten,
                ["MessagesDropped"] = _messagesDropped,
                ["ErrorsEncountered"] = _errorsEncountered,
                ["ErrorRate"] = errorRate,
                ["LastWriteTime"] = _lastWriteTime,
                ["FilePath"] = _filePath,
                ["FileSize"] = fileInfo?.Length ?? 0,
                ["FileExists"] = File.Exists(_filePath),
                ["AutoFlush"] = _autoFlush,
                ["MaxFileSize"] = _maxFileSize,
                ["LastError"] = _lastError?.Message
            };
        }

        private void InitializeFileWriter()
        {
            try
            {
                // Ensure directory exists
                var directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Create or append to file
                _writer = new StreamWriter(_filePath, append: true, encoding: Encoding.UTF8)
                {
                    AutoFlush = _autoFlush
                };

                // Write startup marker
                _writer.WriteLine($"# Log session started - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}");
                if (_autoFlush) _writer.Flush();
            }
            catch (Exception ex)
            {
                _lastError = ex;
                _writer = null;
            }
        }

        private void TryReinitializeWriter()
        {
            try
            {
                lock (_writeLock)
                {
                    _writer?.Dispose();
                    _writer = null;
                    InitializeFileWriter();
                }
            }
            catch (Exception ex)
            {
                _lastError = ex;
            }
        }

        private bool ShouldRotateFile()
        {
            if (_maxFileSize <= 0) return false;
            
            try
            {
                var fileInfo = new FileInfo(_filePath);
                return fileInfo.Exists && fileInfo.Length > _maxFileSize;
            }
            catch
            {
                return false;
            }
        }

        private void RotateFile()
        {
            try
            {
                _writer?.Dispose();
                _writer = null;

                // Rotate backup files
                for (int i = _maxBackupFiles - 1; i >= 1; i--)
                {
                    var oldFile = $"{_filePath}.{i}";
                    var newFile = $"{_filePath}.{i + 1}";
                    
                    if (File.Exists(oldFile))
                    {
                        if (File.Exists(newFile))
                        {
                            File.Delete(newFile);
                        }
                        File.Move(oldFile, newFile);
                    }
                }

                // Move current file to .1
                if (File.Exists(_filePath))
                {
                    var backupFile = $"{_filePath}.1";
                    if (File.Exists(backupFile))
                    {
                        File.Delete(backupFile);
                    }
                    File.Move(_filePath, backupFile);
                }

                // Reinitialize writer with new file
                InitializeFileWriter();
            }
            catch (Exception ex)
            {
                _lastError = ex;
                // Try to reinitialize anyway
                InitializeFileWriter();
            }
        }

        private string FormatMessage(in LogMessage logMessage)
        {
            lock (_stringBuilder)
            {
                _stringBuilder.Clear();
                
                var format = _messageFormat;
                
                // Replace placeholders
                format = format.Replace("{Timestamp:yyyy-MM-dd HH:mm:ss.fff}", logMessage.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                format = format.Replace("{Level}", logMessage.Level.ToString());
                format = format.Replace("{Channel}", logMessage.Channel.ToString());
                format = format.Replace("{Message}", logMessage.Message.ToString());
                
                if (!logMessage.CorrelationId.IsEmpty)
                {
                    format = format.Replace("{CorrelationId}", logMessage.CorrelationId.ToString());
                }
                
                if (!logMessage.SourceContext.IsEmpty)
                {
                    format = format.Replace("{SourceContext}", logMessage.SourceContext.ToString());
                }
                
                _stringBuilder.Append(format);
                
                // Add exception details if present
                if (logMessage.HasException && logMessage.Exception != null)
                {
                    _stringBuilder.AppendLine();
                    _stringBuilder.Append("Exception: ");
                    _stringBuilder.Append(logMessage.Exception.GetType().Name);
                    _stringBuilder.Append(": ");
                    _stringBuilder.Append(logMessage.Exception.Message);
                    
                    if (!string.IsNullOrEmpty(logMessage.Exception.StackTrace))
                    {
                        _stringBuilder.AppendLine();
                        _stringBuilder.Append("Stack Trace:");
                        _stringBuilder.AppendLine();
                        _stringBuilder.Append(logMessage.Exception.StackTrace);
                    }
                }
                
                return _stringBuilder.ToString();
            }
        }

        private T GetConfigProperty<T>(string key, T defaultValue)
        {
            if (_config.Properties != null && _config.Properties.TryGetValue(key, out var value))
            {
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                lock (_writeLock)
                {
                    if (!_disposed)
                    {
                        _disposed = true;
                        
                        try
                        {
                            // Write shutdown marker
                            _writer?.WriteLine($"# Log session ended - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}");
                            _writer?.Flush();
                            _writer?.Dispose();
                            _writer = null;
                        }
                        catch (Exception ex)
                        {
                            _lastError = ex;
                        }
                    }
                }
            }
        }
    }
}