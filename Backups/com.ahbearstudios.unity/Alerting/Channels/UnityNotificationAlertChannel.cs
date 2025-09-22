using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using AhBearStudios.Core.Alerting.Channels;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Messaging;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AhBearStudios.Unity.Alerting.Channels
{
    /// <summary>
    /// Unity notification alert channel that displays alerts as Unity editor dialogs and runtime notifications.
    /// Provides visual feedback for critical alerts during development and testing.
    /// Designed for Unity game development workflow with editor and runtime support.
    /// </summary>
    internal sealed class UnityNotificationAlertChannel : BaseAlertChannel
    {
        private bool _showEditorDialogs = true;
        private bool _useRuntimeNotifications = true;
        private AlertSeverity _dialogSeverityThreshold = AlertSeverity.Critical;

        /// <summary>
        /// Gets the unique name identifier for this channel.
        /// </summary>
        public override FixedString64Bytes Name => "UnityNotificationChannel";

        /// <summary>
        /// Initializes a new instance of the UnityNotificationAlertChannel class.
        /// </summary>
        /// <param name="messageBusService">Message bus service for publishing channel events</param>
        public UnityNotificationAlertChannel(IMessageBusService messageBusService) : base(messageBusService)
        {
        }

        /// <summary>
        /// Core implementation for sending an alert synchronously.
        /// </summary>
        /// <param name="alert">The alert to send</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>True if successful</returns>
        protected override bool SendAlertCore(Alert alert, Guid correlationId)
        {
            try
            {
                // Always log to Unity console first
                LogToUnityConsole(alert);

                // Show editor dialog for critical alerts when in editor
#if UNITY_EDITOR
                if (_showEditorDialogs && alert.Severity >= _dialogSeverityThreshold && !Application.isPlaying)
                {
                    ShowEditorDialog(alert);
                }
#endif

                // For runtime, you could integrate with Unity's Mobile Notifications
                // or other notification systems here
                if (_useRuntimeNotifications && Application.isPlaying)
                {
                    // For now, just enhanced console logging in runtime
                    ShowRuntimeNotification(alert);
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnityNotificationAlertChannel] Failed to send notification: {ex.Message}");
                return false;
            }
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
            // Ensure we're on the main thread for Unity operations
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
                // Test notification display
                Debug.Log($"[AlertSystem] Unity notification channel health check - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
                return ChannelHealthResult.Healthy("Unity notification channel is operational");
            }
            catch (Exception ex)
            {
                return ChannelHealthResult.Unhealthy($"Failed to display notification: {ex.Message}", ex);
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
                if (config.Settings.TryGetValue("ShowEditorDialogs", out var dialogsStr) && 
                    bool.TryParse(dialogsStr, out var showDialogs))
                    _showEditorDialogs = showDialogs;
                
                if (config.Settings.TryGetValue("UseRuntimeNotifications", out var runtimeStr) && 
                    bool.TryParse(runtimeStr, out var useRuntime))
                    _useRuntimeNotifications = useRuntime;
                
                if (config.Settings.TryGetValue("DialogSeverityThreshold", out var thresholdStr) &&
                    Enum.TryParse<AlertSeverity>(thresholdStr, out var threshold))
                    _dialogSeverityThreshold = threshold;
            }

            Debug.Log($"[AlertSystem] Unity notification alert channel initialized - Correlation: {correlationId}");
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
                ChannelType = AlertChannelType.UnityNotification,
                IsEnabled = true,
                MinimumSeverity = AlertSeverity.Warning,
                Settings = new System.Collections.Generic.Dictionary<string, string>
                {
                    ["ShowEditorDialogs"] = "true",
                    ["UseRuntimeNotifications"] = "true",
                    ["DialogSeverityThreshold"] = "Critical"
                }
            };
        }

        /// <summary>
        /// Logs alert to Unity console with appropriate severity.
        /// </summary>
        /// <param name="alert">Alert to log</param>
        private void LogToUnityConsole(Alert alert)
        {
            var message = FormatAlertMessage(alert);
            
            switch (alert.Severity)
            {
                case AlertSeverity.Debug:
                case AlertSeverity.Info:
                case AlertSeverity.Low:
                    Debug.Log($"<color=#00BFFF>[NOTIFICATION]</color> {message}");
                    break;
                    
                case AlertSeverity.Medium:
                case AlertSeverity.Warning:
                    Debug.LogWarning($"[NOTIFICATION] {message}");
                    break;
                    
                case AlertSeverity.High:
                case AlertSeverity.Critical:
                case AlertSeverity.Emergency:
                    Debug.LogError($"[NOTIFICATION] {message}");
                    break;
                    
                default:
                    Debug.Log($"[NOTIFICATION] {message}");
                    break;
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Shows Unity editor dialog for critical alerts.
        /// </summary>
        /// <param name="alert">Alert to display</param>
        private void ShowEditorDialog(Alert alert)
        {
            var title = GetSeverityDisplayName(alert.Severity);
            var message = FormatAlertMessage(alert);
            var buttonText = alert.Severity >= AlertSeverity.Critical ? "Acknowledge" : "OK";

            // Use EditorUtility.DisplayDialog for blocking dialog
            if (alert.Severity >= AlertSeverity.Emergency)
            {
                EditorUtility.DisplayDialog($"🚨 {title} Alert", message, buttonText);
            }
            else if (alert.Severity >= AlertSeverity.Critical)
            {
                EditorUtility.DisplayDialog($"⚠️ {title} Alert", message, buttonText);
            }
            else
            {
                EditorUtility.DisplayDialog($"ℹ️ {title} Alert", message, buttonText);
            }
        }
#endif

        /// <summary>
        /// Shows runtime notification for alerts.
        /// </summary>
        /// <param name="alert">Alert to display</param>
        private void ShowRuntimeNotification(Alert alert)
        {
            // Enhanced console logging with visual indicators for runtime
            var icon = alert.Severity switch
            {
                AlertSeverity.Emergency => "🚨",
                AlertSeverity.Critical => "⚠️",
                AlertSeverity.High => "🔴",
                AlertSeverity.Warning => "🟡",
                AlertSeverity.Medium => "🟠",
                _ => "ℹ️"
            };

            var message = FormatAlertMessage(alert);
            Debug.Log($"<color=#FFD700><size=14>{icon} RUNTIME ALERT</size></color>\n{message}");

            // Future: Could integrate with Unity Mobile Notifications here
            // or custom in-game notification system
        }

        /// <summary>
        /// Formats an alert message for display.
        /// </summary>
        /// <param name="alert">Alert to format</param>
        /// <returns>Formatted message</returns>
        private string FormatAlertMessage(Alert alert)
        {
            var parts = new System.Collections.Generic.List<string>();
            
            parts.Add($"Source: {alert.Source}");
            parts.Add($"Message: {alert.Message}");
            
            if (!alert.Tag.IsEmpty)
                parts.Add($"Tag: {alert.Tag}");
            
            if (alert.Count > 1)
                parts.Add($"Count: {alert.Count}");
            
            parts.Add($"Time: {alert.Timestamp:yyyy-MM-dd HH:mm:ss}");
            
            if (alert.CorrelationId != default)
                parts.Add($"Correlation: {alert.CorrelationId.ToString().Substring(0, 8)}...");

            return string.Join("\n", parts);
        }

        /// <summary>
        /// Gets display name for alert severity.
        /// </summary>
        /// <param name="severity">Alert severity</param>
        /// <returns>Display name</returns>
        private string GetSeverityDisplayName(AlertSeverity severity)
        {
            return severity switch
            {
                AlertSeverity.Debug => "Debug",
                AlertSeverity.Info => "Information",
                AlertSeverity.Low => "Low Priority",
                AlertSeverity.Medium => "Medium Priority", 
                AlertSeverity.High => "High Priority",
                AlertSeverity.Warning => "Warning",
                AlertSeverity.Critical => "Critical",
                AlertSeverity.Emergency => "Emergency",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Disposes of the channel resources.
        /// </summary>
        protected override void DisposeCore()
        {
            Debug.Log("[AlertSystem] Unity notification alert channel disposed");
        }
    }
}