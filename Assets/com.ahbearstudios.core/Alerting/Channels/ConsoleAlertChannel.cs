using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using UnityEngine;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Alerting.Factories;
using AhBearStudios.Core.Messaging;

namespace AhBearStudios.Core.Alerting.Channels
{
    /// <summary>
    /// Alert channel implementation that outputs alerts to the Unity console.
    /// Provides color-coded output based on alert severity with rich formatting.
    /// Designed for Unity game development with editor and runtime console support.
    /// </summary>
    public sealed class ConsoleAlertChannel : BaseAlertChannel
    {
        /// <summary>
        /// Initializes a new instance of the ConsoleAlertChannel class.
        /// </summary>
        /// <param name="messageBusService">Message bus service for publishing channel events</param>
        public ConsoleAlertChannel(IMessageBusService messageBusService) : base(messageBusService)
        {
        }
        private bool _useColors = true;
        private bool _includeTimestamp = true;
        private bool _includeSource = true;
        private bool _includeTag = true;
        private bool _expandContext = false;
        private string _timestampFormat = "HH:mm:ss.fff";
        
        /// <summary>
        /// Gets the unique name identifier for this channel.
        /// </summary>
        public override FixedString64Bytes Name => "ConsoleChannel";

        /// <summary>
        /// Core implementation for sending an alert synchronously.
        /// </summary>
        /// <param name="alert">The alert to send</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>True if successful</returns>
        protected override bool SendAlertCore(Alert alert, Guid correlationId)
        {
            var message = FormatAlertForConsole(alert);
            
            // Use Unity's Debug.Log methods which work in both editor and runtime
            switch (alert.Severity)
            {
                case AlertSeverity.Debug:
                    Debug.Log(message);
                    break;
                    
                case AlertSeverity.Info:
                    Debug.Log(message);
                    break;
                    
                case AlertSeverity.Warning:
                    Debug.LogWarning(message);
                    break;
                    
                case AlertSeverity.Critical:
                    Debug.LogError(message);
                    break;
                    
                case AlertSeverity.Emergency:
                    // Use error for emergency as Unity doesn't have an emergency level
                    Debug.LogError($"[EMERGENCY] {message}");
                    break;
                    
                default:
                    Debug.Log(message);
                    break;
            }

            // Also output expanded context if configured
            if (_expandContext && alert.Context != null)
            {
                OutputAlertContext(alert);
            }

            return true;
        }

        /// <summary>
        /// Core implementation for sending an alert asynchronously.
        /// </summary>
        /// <param name="alert">The alert to send</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>UniTask with result</returns>
        protected override async UniTask<bool> SendAlertAsyncCore(Alert alert, Guid correlationId, CancellationToken cancellationToken)
        {
            // Console output is synchronous, but we ensure we're on the main thread for Unity Debug.Log
            await UniTask.SwitchToMainThread(cancellationToken);
            return SendAlertCore(alert, correlationId);
        }

        /// <summary>
        /// Core implementation for health testing.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>UniTask with health result</returns>
        protected override async UniTask<ChannelHealthResult> TestHealthAsyncCore(Guid correlationId, CancellationToken cancellationToken)
        {
            await UniTask.Yield(cancellationToken);
            
            try
            {
                // Test console output
                Debug.Log($"[AlertSystem] Console channel health check - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
                return ChannelHealthResult.Healthy("Console channel is operational");
            }
            catch (Exception ex)
            {
                return ChannelHealthResult.Unhealthy($"Failed to write to console: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Core implementation for channel initialization.
        /// </summary>
        /// <param name="config">Channel configuration</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>UniTask with initialization result</returns>
        protected override async UniTask<bool> InitializeAsyncCore(ChannelConfig config, Guid correlationId)
        {
            await UniTask.Yield();
            
            if (config?.Settings != null)
            {
                if (config.Settings.TryGetValue("UseColors", out var useColorsStr) && 
                    bool.TryParse(useColorsStr, out var useColors))
                    _useColors = useColors;
                
                if (config.Settings.TryGetValue("IncludeTimestamp", out var timestampStr) && 
                    bool.TryParse(timestampStr, out var includeTimestamp))
                    _includeTimestamp = includeTimestamp;
                
                if (config.Settings.TryGetValue("IncludeSource", out var sourceStr) && 
                    bool.TryParse(sourceStr, out var includeSource))
                    _includeSource = includeSource;
                
                if (config.Settings.TryGetValue("IncludeTag", out var tagStr) && 
                    bool.TryParse(tagStr, out var includeTag))
                    _includeTag = includeTag;
                
                if (config.Settings.TryGetValue("ExpandContext", out var contextStr) && 
                    bool.TryParse(contextStr, out var expandContext))
                    _expandContext = expandContext;
                
                if (config.Settings.TryGetValue("TimestampFormat", out var timestampFormat) && 
                    !string.IsNullOrWhiteSpace(timestampFormat))
                    _timestampFormat = timestampFormat;
            }

            Debug.Log($"[AlertSystem] Console alert channel initialized - Correlation: {correlationId}");
            return true;
        }

        /// <summary>
        /// Creates the default configuration for this channel.
        /// </summary>
        /// <returns>Default channel configuration</returns>
        protected override ChannelConfig CreateDefaultConfiguration()
        {
            return new ChannelConfig
            {
                Name = Name,
                ChannelType = AlertChannelType.Console,
                IsEnabled = true,
                MinimumSeverity = AlertSeverity.Debug,
                Settings = new Dictionary<string, string>
                {
                    ["UseColors"] = "true",
                    ["IncludeTimestamp"] = "true",
                    ["IncludeSource"] = "true",
                    ["IncludeTag"] = "true",
                    ["ExpandContext"] = "false",
                    ["TimestampFormat"] = "HH:mm:ss.fff",
                    ["MaxAlertsPerSecond"] = "100"
                }
            };
        }

        /// <summary>
        /// Formats an alert for console output with optional color coding.
        /// </summary>
        /// <param name="alert">Alert to format</param>
        /// <returns>Formatted console message</returns>
        private string FormatAlertForConsole(Alert alert)
        {
            var parts = new List<string>();
            
            // Add timestamp if configured
            if (_includeTimestamp)
            {
                parts.Add($"[{alert.Timestamp.ToString(_timestampFormat)}]");
            }

            // Add severity with color coding
            var severityText = GetSeverityText(alert.Severity);
            if (_useColors)
            {
                severityText = ApplyColorToText(severityText, GetSeverityColor(alert.Severity));
            }
            parts.Add(severityText);

            // Add source if configured
            if (_includeSource && !alert.Source.IsEmpty)
            {
                parts.Add($"[{alert.Source.ToString()}]");
            }

            // Add tag if configured and present
            if (_includeTag && !alert.Tag.IsEmpty)
            {
                var tagText = $"[{alert.Tag.ToString()}]";
                if (_useColors)
                {
                    tagText = ApplyColorToText(tagText, "#00CED1"); // Dark turquoise for tags
                }
                parts.Add(tagText);
            }

            // Add state indicator if not active
            if (alert.State != AlertState.Active)
            {
                var stateText = $"[{alert.State.ToString().ToUpper()}]";
                if (_useColors)
                {
                    stateText = alert.State switch
                    {
                        AlertState.Acknowledged => ApplyColorToText(stateText, "#FFA500"), // Orange
                        AlertState.Resolved => ApplyColorToText(stateText, "#00FF00"), // Green
                        AlertState.Suppressed => ApplyColorToText(stateText, "#808080"), // Gray
                        _ => stateText
                    };
                }
                parts.Add(stateText);
            }

            // Add message
            parts.Add(alert.Message.ToString());

            // Add count if > 1
            if (alert.Count > 1)
            {
                var countText = $"(x{alert.Count})";
                if (_useColors)
                {
                    countText = ApplyColorToText(countText, "#FFFF00"); // Yellow
                }
                parts.Add(countText);
            }

            // Add correlation ID in debug mode
            #if UNITY_EDITOR || DEBUG
            if (alert.CorrelationId != default)
            {
                parts.Add($"[Corr: {alert.CorrelationId.ToString().Substring(0, 8)}]");
            }
            #endif

            return string.Join(" ", parts);
        }

        /// <summary>
        /// Outputs detailed alert context to the console.
        /// </summary>
        /// <param name="alert">Alert with context to output</param>
        private void OutputAlertContext(Alert alert)
        {
            if (alert.Context == null)
                return;

            var contextLines = new List<string> { "  Alert Context:" };

            // Exception information
            if (alert.Context.Exception != null)
            {
                contextLines.Add($"    Exception: {alert.Context.Exception.TypeName.ToString()}");
                contextLines.Add($"    Message: {alert.Context.Exception.Message.ToString()}");
                if (!alert.Context.Exception.InnerExceptionType.IsEmpty)
                {
                    contextLines.Add($"    Inner: {alert.Context.Exception.InnerExceptionType.ToString()}");
                }
                if (!alert.Context.Exception.StackTrace.IsEmpty)
                {
                    contextLines.Add("    Stack Trace:");
                    var stackLines = alert.Context.Exception.StackTrace.ToString().Split('\n');
                    foreach (var line in stackLines)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                            contextLines.Add($"      {line.Trim()}");
                    }
                }
            }

            // Performance metrics
            if (alert.Context.Performance != null)
            {
                contextLines.Add($"    Duration: {alert.Context.Performance.Duration.TotalMilliseconds:F2}ms");
                if (alert.Context.Performance.MemoryUsageBytes > 0)
                    contextLines.Add($"    Memory: {FormatBytes(alert.Context.Performance.MemoryUsageBytes)}");
                if (alert.Context.Performance.CpuUsagePercent > 0)
                    contextLines.Add($"    CPU: {alert.Context.Performance.CpuUsagePercent:F1}%");
            }

            // System information
            if (alert.Context.System != null)
            {
                if (!alert.Context.System.MachineName.IsEmpty)
                    contextLines.Add($"    Machine: {alert.Context.System.MachineName.ToString()}");
                contextLines.Add($"    Process: {alert.Context.System.ProcessId} / Thread: {alert.Context.System.ThreadId}");
                if (alert.Context.System.AvailableMemoryBytes > 0)
                    contextLines.Add($"    Available Memory: {FormatBytes(alert.Context.System.AvailableMemoryBytes)}");
            }

            // User information
            if (alert.Context.User != null)
            {
                if (!alert.Context.User.UserId.IsEmpty)
                    contextLines.Add($"    User: {alert.Context.User.UserId.ToString()}");
                if (!alert.Context.User.SessionId.IsEmpty)
                    contextLines.Add($"    Session: {alert.Context.User.SessionId.ToString()}");
            }

            // Network information
            if (alert.Context.Network != null)
            {
                if (!alert.Context.Network.RequestUrl.IsEmpty)
                    contextLines.Add($"    URL: {alert.Context.Network.RequestUrl.ToString()}");
                if (alert.Context.Network.HttpStatusCode > 0)
                    contextLines.Add($"    HTTP Status: {alert.Context.Network.HttpStatusCode}");
                if (alert.Context.Network.RequestDurationMs > 0)
                    contextLines.Add($"    Request Duration: {alert.Context.Network.RequestDurationMs:F2}ms");
            }

            // Custom properties
            if (alert.Context.Properties != null && alert.Context.Properties.Count > 0)
            {
                contextLines.Add("    Custom Properties:");
                foreach (var kvp in alert.Context.Properties)
                {
                    contextLines.Add($"      {kvp.Key}: {kvp.Value}");
                }
            }

            // Output all context lines
            var contextMessage = string.Join("\n", contextLines);
            Debug.Log(contextMessage);
        }

        /// <summary>
        /// Gets the severity text representation.
        /// </summary>
        /// <param name="severity">Alert severity</param>
        /// <returns>Severity text</returns>
        private string GetSeverityText(AlertSeverity severity)
        {
            return severity switch
            {
                AlertSeverity.Debug => "[DEBUG]",
                AlertSeverity.Info => "[INFO]",
                AlertSeverity.Low => "[LOW]",
                AlertSeverity.Medium => "[MEDIUM]",
                AlertSeverity.High => "[HIGH]",
                AlertSeverity.Warning => "[WARN]",
                AlertSeverity.Critical => "[CRITICAL]",
                AlertSeverity.Emergency => "[EMERGENCY]",
                _ => "[UNKNOWN]"
            };
        }

        /// <summary>
        /// Gets the color for a severity level.
        /// </summary>
        /// <param name="severity">Alert severity</param>
        /// <returns>Hex color code</returns>
        private string GetSeverityColor(AlertSeverity severity)
        {
            return severity switch
            {
                AlertSeverity.Debug => "#808080",     // Gray
                AlertSeverity.Info => "#00BFFF",      // Deep sky blue
                AlertSeverity.Low => "#C0C0C0",       // Silver
                AlertSeverity.Medium => "#FFA500",    // Orange
                AlertSeverity.High => "#FF8C00",      // Dark orange
                AlertSeverity.Warning => "#FFD700",   // Gold
                AlertSeverity.Critical => "#FF6347",  // Tomato red
                AlertSeverity.Emergency => "#FF0000", // Red
                _ => "#FFFFFF"                        // White
            };
        }

        /// <summary>
        /// Applies color formatting to text for Unity console.
        /// </summary>
        /// <param name="text">Text to color</param>
        /// <param name="hexColor">Hex color code</param>
        /// <returns>Color-formatted text</returns>
        private string ApplyColorToText(string text, string hexColor)
        {
            // Unity console supports rich text with color tags
            return $"<color={hexColor}>{text}</color>";
        }

        /// <summary>
        /// Formats byte count as human-readable string.
        /// </summary>
        /// <param name="bytes">Number of bytes</param>
        /// <returns>Formatted string</returns>
        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:F2} {sizes[order]}";
        }

        /// <summary>
        /// Disposes of the channel resources.
        /// </summary>
        protected override void DisposeCore()
        {
            // Output disposal message
            Debug.Log("[AlertSystem] Console alert channel disposed");
        }
    }
}