using System;
using AhBearStudios.Core.DependencyInjection.Attributes;
using UnityEngine;
using VContainer;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.MessageBus.Events;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.MessageBus.Unity
{
    /// <summary>
    /// Unity component that manages the message delivery service lifecycle.
    /// </summary>
    public sealed class MessageDeliveryServiceManager : MonoBehaviour
    {
        [Inject] private IMessageDeliveryService _deliveryService;
        [Inject] private IBurstLogger _logger;
        
        private void Start()
        {
            DontDestroyOnLoad(this);
            
            if (_deliveryService != null)
            {
                // Subscribe to service events
                _deliveryService.StatusChanged += OnDeliveryServiceStatusChanged;
                _deliveryService.MessageDelivered += OnMessageDelivered;
                _deliveryService.MessageDeliveryFailed += OnMessageDeliveryFailed;
                _deliveryService.MessageAcknowledged += OnMessageAcknowledged;
                
                // Start the service
                _ = _deliveryService.StartAsync();
                
                _logger.Log(LogLevel.Info, 
                    $"MessageDeliveryServiceManager started with {_deliveryService.Name} service",
                    "DeliveryServiceManager");
            }
        }
        
        private void OnDestroy()
        {
            if (_deliveryService != null)
            {
                // Unsubscribe from events
                _deliveryService.StatusChanged -= OnDeliveryServiceStatusChanged;
                _deliveryService.MessageDelivered -= OnMessageDelivered;
                _deliveryService.MessageDeliveryFailed -= OnMessageDeliveryFailed;
                _deliveryService.MessageAcknowledged -= OnMessageAcknowledged;
                
                // Stop the service
                _ = _deliveryService.StopAsync();
                
                // Dispose if disposable
                if (_deliveryService is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                
                _logger.Log(LogLevel.Info, "MessageDeliveryServiceManager stopped", "DeliveryServiceManager");
            }
        }
        
        private void OnDeliveryServiceStatusChanged(object sender, DeliveryServiceStatusChangedEventArgs e)
        {
            _logger.Log(LogLevel.Info, 
                $"Delivery service status changed from {e.PreviousStatus} to {e.CurrentStatus}: {e.Reason}",
                "DeliveryServiceManager");
        }
        
        private void OnMessageDelivered(object sender, MessageDeliveredEventArgs e)
        {
            _logger.Log(LogLevel.Debug, 
                $"Message {e.Message.Id} delivered successfully (attempts: {e.DeliveryAttempts})",
                "DeliveryServiceManager");
        }
        
        private void OnMessageDeliveryFailed(object sender, MessageDeliveryFailedEventArgs e)
        {
            var willRetryText = e.WillRetry ? " (will retry)" : " (no more retries)";
            
            _logger.Log(LogLevel.Warning, 
                $"Message {e.Message.Id} delivery failed: {e.ErrorMessage} (attempts: {e.DeliveryAttempts}){willRetryText}",
                "DeliveryServiceManager");
        }
        
        private void OnMessageAcknowledged(object sender, MessageAcknowledgedEventArgs e)
        {
            _logger.Log(LogLevel.Debug, 
                $"Message {e.MessageId} acknowledged (delivery ID: {e.DeliveryId})",
                "DeliveryServiceManager");
        }
    }
}