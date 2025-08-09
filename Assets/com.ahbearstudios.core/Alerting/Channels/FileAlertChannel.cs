using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Common.Models;
using AhBearStudios.Core.Messaging;

namespace AhBearStudios.Core.Alerting.Channels
{
    /// <summary>
    /// File-based alert channel that writes alerts to disk.
    /// </summary>
    internal sealed class FileAlertChannel : BaseAlertChannel
    {
        private readonly string _filePath;

        public override FixedString64Bytes Name => "FileChannel";

        public FileAlertChannel(string filePath, IMessageBusService messageBusService) : base(messageBusService)
        {
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            MinimumSeverity = AlertSeverity.Info;
        }

        protected override bool SendAlertCore(Alert alert, Guid correlationId)
        {
            try
            {
                var message = FormatAlert(alert);
                File.AppendAllText(_filePath, message + Environment.NewLine);
                return true;
            }
            catch
            {
                return false;
            }
        }

        protected override async UniTask<bool> SendAlertAsyncCore(Alert alert, Guid correlationId, CancellationToken cancellationToken)
        {
            try
            {
                var message = FormatAlert(alert);
                await File.AppendAllTextAsync(_filePath, message + Environment.NewLine, cancellationToken);
                return true;
            }
            catch
            {
                return false;
            }
        }

        protected override async UniTask<ChannelHealthResult> TestHealthAsyncCore(Guid correlationId, CancellationToken cancellationToken)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                var directory = Path.GetDirectoryName(_filePath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                // Test write access
                var healthCheckFile = _filePath + ".healthcheck";
                await File.WriteAllTextAsync(healthCheckFile, "test", cancellationToken);
                File.Delete(healthCheckFile);
                
                var duration = DateTime.UtcNow - startTime;
                return ChannelHealthResult.Healthy($"File channel healthy: {_filePath}", duration);
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                return ChannelHealthResult.Unhealthy($"File channel unhealthy: {ex.Message}", ex, duration);
            }
        }

        protected override async UniTask<bool> InitializeAsyncCore(ChannelConfig config, Guid correlationId)
        {
            try
            {
                // Ensure directory exists
                var directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Test write access
                var testMessage = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] File channel initialized{Environment.NewLine}";
                await File.AppendAllTextAsync(_filePath, testMessage);
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        protected override ChannelConfig CreateDefaultConfiguration()
        {
            return new ChannelConfig
            {
                Name = Name,
                ChannelType = AlertChannelType.File,
                IsEnabled = true,
                MinimumSeverity = AlertSeverity.Info,
                MaximumSeverity = AlertSeverity.Emergency,
                MessageFormat = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Severity}] [{Source}] {Message}",
                EnableBatching = false,
                EnableHealthMonitoring = true,
                HealthCheckInterval = TimeSpan.FromMinutes(5),
                SendTimeout = TimeSpan.FromSeconds(10),
                Priority = 100
            };
        }

        private string FormatAlert(Alert alert)
        {
            return $"[{alert.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{alert.Severity}] [{alert.Source}] {alert.Message}";
        }
    }
}