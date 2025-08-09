namespace AhBearStudios.Core.Alerting.Models;

/// <summary>
/// Strongly-typed settings for file-based alert channels.
/// Provides configuration options for file output and rotation.
/// </summary>
public sealed record FileChannelSettings : IChannelSettings
{
    /// <summary>
    /// Gets the file path where alerts will be written.
    /// Can include environment variables and date formatting placeholders.
    /// </summary>
    public string FilePath { get; init; } = "alerts.log";

    /// <summary>
    /// Gets the maximum file size in bytes before rotation occurs.
    /// Set to 0 for no size limit.
    /// </summary>
    public long MaxFileSize { get; init; } = 10_485_760; // 10MB

    /// <summary>
    /// Gets the maximum number of backup files to keep during rotation.
    /// Older files are deleted when this limit is exceeded.
    /// </summary>
    public int MaxBackupFiles { get; init; } = 5;

    /// <summary>
    /// Gets whether file output should be automatically flushed after each write.
    /// When enabled, ensures data is written immediately but may impact performance.
    /// </summary>
    public bool AutoFlush { get; init; } = true;

    /// <summary>
    /// Gets whether timestamps should be included in file output.
    /// </summary>
    public bool IncludeTimestamp { get; init; } = true;

    /// <summary>
    /// Gets the date format string used for timestamps.
    /// Standard .NET DateTime format strings are supported.
    /// </summary>
    public string DateFormat { get; init; } = "yyyy-MM-dd HH:mm:ss.fff";

    /// <summary>
    /// Gets whether the file should be created if it doesn't exist.
    /// </summary>
    public bool CreateIfNotExists { get; init; } = true;

    /// <summary>
    /// Gets whether to append to existing files or overwrite them.
    /// </summary>
    public bool AppendMode { get; init; } = true;

    /// <summary>
    /// Gets the encoding to use for file output.
    /// </summary>
    public string Encoding { get; init; } = "UTF-8";

    /// <summary>
    /// Gets whether to include stack traces for error-level alerts.
    /// </summary>
    public bool IncludeStackTrace { get; init; } = true;

    /// <summary>
    /// Gets the buffer size for file writing operations.
    /// Larger buffers can improve performance but increase memory usage.
    /// </summary>
    public int BufferSize { get; init; } = 4096;

    /// <summary>
    /// Gets the default file channel settings.
    /// </summary>
    public static FileChannelSettings Default => new();

    /// <summary>
    /// Validates the file channel settings.
    /// </summary>
    /// <returns>True if the settings are valid; otherwise, false.</returns>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(FilePath) &&
               MaxFileSize >= 0 &&
               MaxBackupFiles >= 0 &&
               BufferSize > 0 &&
               !string.IsNullOrWhiteSpace(DateFormat) &&
               !string.IsNullOrWhiteSpace(Encoding);
    }
}