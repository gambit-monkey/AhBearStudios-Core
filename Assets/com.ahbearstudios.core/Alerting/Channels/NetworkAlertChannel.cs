using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Serialization;

namespace AhBearStudios.Core.Alerting.Channels
{
    /// <summary>
    /// Network-based alert channel that sends alerts to HTTP endpoints via webhooks.
    /// Supports JSON payload delivery with retry logic and rate limiting.
    /// Designed for integration with external monitoring and logging services.
    /// </summary>
    internal sealed class NetworkAlertChannel : BaseAlertChannel
    {
        private readonly ISerializationService _serializationService;
        private readonly HttpClient _httpClient;
        private string _webhookUrl;
        private string _apiKey;
        private int _timeoutSeconds = 30;
        private int _maxRetries = 3;
        private Dictionary<string, string> _customHeaders;

        /// <summary>
        /// Gets the unique name identifier for this channel.
        /// </summary>
        public override FixedString64Bytes Name => "NetworkChannel";

        /// <summary>
        /// Initializes a new instance of the NetworkAlertChannel class.
        /// </summary>
        /// <param name="messageBusService">Message bus service for publishing channel events</param>
        /// <param name="serializationService">Serialization service for JSON payload creation</param>
        public NetworkAlertChannel(IMessageBusService messageBusService, ISerializationService serializationService = null) : base(messageBusService)
        {
            _serializationService = serializationService;
            _httpClient = new HttpClient();
            _customHeaders = new Dictionary<string, string>();
        }

        /// <summary>
        /// Core implementation for sending an alert synchronously.
        /// </summary>
        /// <param name="alert">The alert to send</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>True if successful</returns>
        protected override bool SendAlertCore(Alert alert, Guid correlationId)
        {
            // Network operations should be async, but we can use GetAwaiter().GetResult() for sync version
            try
            {
                return SendAlertAsyncCore(alert, correlationId, CancellationToken.None).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkAlertChannel] Sync send failed: {ex.Message}");
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
            if (string.IsNullOrEmpty(_webhookUrl))
            {
                Debug.LogError("[NetworkAlertChannel] Webhook URL not configured");
                return false;
            }

            var retryCount = 0;
            while (retryCount <= _maxRetries)
            {
                try
                {
                    var payload = CreateAlertPayload(alert, correlationId);
                    var jsonContent = SerializePayload(payload);
                    
                    using var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    
                    // Add custom headers
                    if (!string.IsNullOrEmpty(_apiKey))
                        content.Headers.Add("Authorization", $"Bearer {_apiKey}");
                    
                    foreach (var header in _customHeaders)
                        content.Headers.Add(header.Key, header.Value);

                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(TimeSpan.FromSeconds(_timeoutSeconds));

                    var response = await _httpClient.PostAsync(_webhookUrl, content, cts.Token);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        Debug.Log($"[NetworkAlertChannel] Alert sent successfully to {_webhookUrl} (Status: {response.StatusCode})");
                        return true;
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        Debug.LogWarning($"[NetworkAlertChannel] HTTP error {response.StatusCode}: {errorContent}");
                        
                        // Don't retry on client errors (4xx)
                        if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
                            return false;
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    Debug.LogWarning("[NetworkAlertChannel] Send operation was cancelled");
                    return false;
                }
                catch (HttpRequestException ex)
                {
                    Debug.LogWarning($"[NetworkAlertChannel] HTTP request failed (attempt {retryCount + 1}): {ex.Message}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[NetworkAlertChannel] Unexpected error (attempt {retryCount + 1}): {ex.Message}");
                }

                retryCount++;
                if (retryCount <= _maxRetries)
                {
                    var delayMs = (int)Math.Pow(2, retryCount) * 1000; // Exponential backoff
                    await UniTask.Delay(delayMs, cancellationToken);
                }
            }

            Debug.LogError($"[NetworkAlertChannel] Failed to send alert after {_maxRetries + 1} attempts");
            return false;
        }

        /// <summary>
        /// Core implementation for health testing.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>UniTask with health result</returns>
        protected override async UniTask<ChannelHealthResult> TestHealthAsyncCore(Guid correlationId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_webhookUrl))
            {
                return ChannelHealthResult.Unhealthy("Webhook URL not configured");
            }

            try
            {
                // Send a simple health check payload
                var healthPayload = new Dictionary<string, object>
                {
                    ["type"] = "health_check",
                    ["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    ["correlationId"] = correlationId.ToString(),
                    ["source"] = "NetworkAlertChannel"
                };

                var jsonContent = SerializePayload(healthPayload);
                using var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                if (!string.IsNullOrEmpty(_apiKey))
                    content.Headers.Add("Authorization", $"Bearer {_apiKey}");

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(10)); // Shorter timeout for health check

                var response = await _httpClient.PostAsync(_webhookUrl, content, cts.Token);
                
                if (response.IsSuccessStatusCode)
                {
                    return ChannelHealthResult.Healthy($"Network channel is operational (HTTP {response.StatusCode})");
                }
                else
                {
                    return ChannelHealthResult.Degraded($"Network endpoint returned HTTP {response.StatusCode}");
                }
            }
            catch (OperationCanceledException)
            {
                return ChannelHealthResult.Unhealthy("Health check timed out");
            }
            catch (Exception ex)
            {
                return ChannelHealthResult.Unhealthy($"Health check failed: {ex.Message}", ex);
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
                if (config.Settings.TryGetValue("WebhookUrl", out var url))
                    _webhookUrl = url;
                
                if (config.Settings.TryGetValue("ApiKey", out var apiKey))
                    _apiKey = apiKey;
                
                if (config.Settings.TryGetValue("TimeoutSeconds", out var timeoutStr) &&
                    int.TryParse(timeoutStr, out var timeout))
                    _timeoutSeconds = Math.Max(1, Math.Min(300, timeout)); // 1-300 seconds
                
                if (config.Settings.TryGetValue("MaxRetries", out var retriesStr) &&
                    int.TryParse(retriesStr, out var retries))
                    _maxRetries = Math.Max(0, Math.Min(10, retries)); // 0-10 retries

                // Parse custom headers
                foreach (var kvp in config.Settings)
                {
                    if (kvp.Key.StartsWith("Header."))
                    {
                        var headerName = kvp.Key.Substring(7); // Remove "Header." prefix
                        _customHeaders[headerName] = kvp.Value;
                    }
                }
            }

            // Configure HttpClient
            _httpClient.Timeout = TimeSpan.FromSeconds(_timeoutSeconds + 5); // Slightly longer than request timeout
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AhBearStudios.AlertSystem/1.0");

            if (string.IsNullOrEmpty(_webhookUrl))
            {
                Debug.LogWarning("[NetworkAlertChannel] No webhook URL configured - channel will not send alerts");
                return false;
            }

            Debug.Log($"[AlertSystem] Network alert channel initialized - URL: {_webhookUrl} - Correlation: {correlationId}");
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
                ChannelType = AlertChannelType.Network,
                IsEnabled = true,
                MinimumSeverity = AlertSeverity.Critical,
                Settings = new Dictionary<string, string>
                {
                    ["WebhookUrl"] = "",
                    ["ApiKey"] = "",
                    ["TimeoutSeconds"] = "30",
                    ["MaxRetries"] = "3",
                    ["Header.Content-Type"] = "application/json"
                }
            };
        }

        /// <summary>
        /// Creates the JSON payload for an alert.
        /// </summary>
        /// <param name="alert">Alert to serialize</param>
        /// <param name="correlationId">Correlation ID</param>
        /// <returns>Payload dictionary</returns>
        private Dictionary<string, object> CreateAlertPayload(Alert alert, Guid correlationId)
        {
            var payload = new Dictionary<string, object>
            {
                ["type"] = "alert",
                ["timestamp"] = alert.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                ["severity"] = alert.Severity.ToString(),
                ["source"] = alert.Source.ToString(),
                ["message"] = alert.Message.ToString(),
                ["correlationId"] = correlationId.ToString(),
                ["alertId"] = alert.Id.ToString(),
                ["state"] = alert.State.ToString()
            };

            if (!alert.Tag.IsEmpty)
                payload["tag"] = alert.Tag.ToString();

            if (alert.Count > 1)
                payload["count"] = alert.Count;

            if (alert.Context != null)
            {
                var context = new Dictionary<string, object>();

                if (alert.Context.Exception != null)
                {
                    context["exception"] = new Dictionary<string, object>
                    {
                        ["type"] = alert.Context.Exception.TypeName.ToString(),
                        ["message"] = alert.Context.Exception.Message.ToString()
                    };
                }

                if (alert.Context.Performance != null)
                {
                    context["performance"] = new Dictionary<string, object>
                    {
                        ["duration"] = alert.Context.Performance.Duration.TotalMilliseconds,
                        ["memoryUsage"] = alert.Context.Performance.MemoryUsageBytes,
                        ["cpuUsage"] = alert.Context.Performance.CpuUsagePercent
                    };
                }

                if (alert.Context.System != null)
                {
                    context["system"] = new Dictionary<string, object>
                    {
                        ["machineName"] = alert.Context.System.MachineName.ToString(),
                        ["processId"] = alert.Context.System.ProcessId,
                        ["threadId"] = alert.Context.System.ThreadId
                    };
                }

                if (context.Count > 0)
                    payload["context"] = context;
            }

            return payload;
        }

        /// <summary>
        /// Serializes payload to JSON string.
        /// </summary>
        /// <param name="payload">Payload to serialize</param>
        /// <returns>JSON string</returns>
        private string SerializePayload(Dictionary<string, object> payload)
        {
            if (_serializationService != null)
            {
                try
                {
                    var bytes = _serializationService.Serialize(payload);
                    return Encoding.UTF8.GetString(bytes);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[NetworkAlertChannel] Serialization service failed, using fallback: {ex.Message}");
                }
            }

            // Fallback to simple JSON serialization
            return JsonUtility.ToJson(new AlertPayloadWrapper(payload));
        }

        /// <summary>
        /// Disposes of the channel resources.
        /// </summary>
        protected override void DisposeCore()
        {
            _httpClient?.Dispose();
            Debug.Log("[AlertSystem] Network alert channel disposed");
        }

        /// <summary>
        /// Wrapper for Unity's JsonUtility compatibility.
        /// </summary>
        [Serializable]
        private class AlertPayloadWrapper
        {
            public string type;
            public string timestamp;
            public string severity;
            public string source;
            public string message;
            public string correlationId;
            public string alertId;
            public string state;
            public string tag;
            public int count;

            public AlertPayloadWrapper(Dictionary<string, object> payload)
            {
                type = payload.GetValueOrDefault("type")?.ToString();
                timestamp = payload.GetValueOrDefault("timestamp")?.ToString();
                severity = payload.GetValueOrDefault("severity")?.ToString();
                source = payload.GetValueOrDefault("source")?.ToString();
                message = payload.GetValueOrDefault("message")?.ToString();
                correlationId = payload.GetValueOrDefault("correlationId")?.ToString();
                alertId = payload.GetValueOrDefault("alertId")?.ToString();
                state = payload.GetValueOrDefault("state")?.ToString();
                tag = payload.GetValueOrDefault("tag")?.ToString();
                count = payload.ContainsKey("count") ? Convert.ToInt32(payload["count"]) : 0;
            }
        }
    }

    /// <summary>
    /// Extension method for dictionary value retrieval.
    /// </summary>
    internal static class DictionaryExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default)
        {
            return dictionary.TryGetValue(key, out var value) ? value : defaultValue;
        }
    }
}