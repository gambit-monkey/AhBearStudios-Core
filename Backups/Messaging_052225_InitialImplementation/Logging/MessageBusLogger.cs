// using System;
// using System.Collections.Generic;
// using System.Text;
// using System.Threading;
// using System.Threading.Tasks;
// using AhBearStudios.Core.Logging;
// using AhBearStudios.Core.Messaging.Interfaces;
// using AhBearStudios.Core.Profiling;
// using AhBearStudios.Core.Profiling.Interfaces;
//
// namespace AhBearStudios.Core.Messaging
// {
//     /// <summary>
//     /// Specialized logger for message bus activity.
//     /// Provides detailed logging of message flow and bus operations.
//     /// </summary>
//     /// <typeparam name="TMessage">The type of messages to log.</typeparam>
//     public class MessageBusLogger<TMessage> : IDisposable where TMessage : IMessage
//     {
//         private readonly IMessageBus<TMessage> _messageBus;
//         private readonly IBurstLogger _logger;
//         private readonly IProfiler _profiler;
//         private readonly List<ISubscriptionToken> _subscriptions;
//         private readonly bool _includeMessageDetails;
//         private readonly bool _includeStackTrace;
//         private readonly List<Func<TMessage, bool>> _filterRules;
//         private readonly object _filtersLock = new object();
//         private readonly HashSet<Type> _excludedTypes;
//         private readonly HashSet<Type> _includedTypes;
//         private bool _isEnabled;
//         private bool _isDisposed;
//
//         /// <summary>
//         /// Gets or sets a value indicating whether logging is enabled.
//         /// </summary>
//         public bool IsEnabled
//         {
//             get => _isEnabled;
//             set => _isEnabled = value;
//         }
//
//         /// <summary>
//         /// Initializes a new instance of the MessageBusLogger class.
//         /// </summary>
//         /// <param name="messageBus">The message bus to log.</param>
//         /// <param name="logger">The logger to use for logging.</param>
//         /// <param name="includeMessageDetails">Whether to include message details in log entries.</param>
//         /// <param name="includeStackTrace">Whether to include stack traces in log entries.</param>
//         /// <param name="profiler">Optional profiler for performance monitoring.</param>
//         public MessageBusLogger(
//             IMessageBus<TMessage> messageBus,
//             IBurstLogger logger,
//             bool includeMessageDetails = true,
//             bool includeStackTrace = false,
//             IProfiler profiler = null)
//         {
//             _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
//             _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//             _profiler = profiler;
//             _subscriptions = new List<ISubscriptionToken>();
//             _includeMessageDetails = includeMessageDetails;
//             _includeStackTrace = includeStackTrace;
//             _filterRules = new List<Func<TMessage, bool>>();
//             _excludedTypes = new HashSet<Type>();
//             _includedTypes = new HashSet<Type>();
//             _isEnabled = true;
//             _isDisposed = false;
//             
//             // Attach to message bus
//             AttachToMessageBus();
//             
//             _logger.Info("MessageBusLogger initialized");
//         }
//
//         /// <summary>
//         /// Adds a filter rule to determine which messages to log.
//         /// </summary>
//         /// <param name="filterRule">The filter rule function. Returns true if the message should be logged.</param>
//         public void AddFilterRule(Func<TMessage, bool> filterRule)
//         {
//             using (_profiler?.BeginSample("MessageBusLogger.AddFilterRule"))
//             {
//                 if (_isDisposed)
//                 {
//                     throw new ObjectDisposedException(nameof(MessageBusLogger<TMessage>));
//                 }
//
//                 if (filterRule == null)
//                 {
//                     throw new ArgumentNullException(nameof(filterRule));
//                 }
//
//                 lock (_filtersLock)
//                 {
//                     _filterRules.Add(filterRule);
//                 }
//                 
//                 _logger.Debug("Added message filter rule");
//             }
//         }
//
//         /// <summary>
//         /// Excludes a specific message type from logging.
//         /// </summary>
//         /// <typeparam name="T">The message type to exclude.</typeparam>
//         public void ExcludeType<T>() where T : TMessage
//         {
//             using (_profiler?.BeginSample("MessageBusLogger.ExcludeType"))
//             {
//                 if (_isDisposed)
//                 {
//                     throw new ObjectDisposedException(nameof(MessageBusLogger<TMessage>));
//                 }
//
//                 lock (_filtersLock)
//                 {
//                     _excludedTypes.Add(typeof(T));
//                     _includedTypes.Remove(typeof(T)); // Remove from included types if present
//                 }
//                 
//                 _logger.Debug($"Excluded message type {typeof(T).Name} from logging");
//             }
//         }
//
//         /// <summary>
//         /// Includes only a specific message type in logging.
//         /// </summary>
//         /// <typeparam name="T">The message type to include.</typeparam>
//         public void IncludeOnlyType<T>() where T : TMessage
//         {
//             using (_profiler?.BeginSample("MessageBusLogger.IncludeOnlyType"))
//             {
//                 if (_isDisposed)
//                 {
//                     throw new ObjectDisposedException(nameof(MessageBusLogger<TMessage>));
//                 }
//
//                 lock (_filtersLock)
//                 {
//                     _includedTypes.Add(typeof(T));
//                 }
//                 
//                 _logger.Debug($"Including only message type {typeof(T).Name} in logging");
//             }
//         }
//
//         /// <summary>
//         /// Clears all filter rules.
//         /// </summary>
//         public void ClearFilterRules()
//         {
//             using (_profiler?.BeginSample("MessageBusLogger.ClearFilterRules"))
//             {
//                 if (_isDisposed)
//                 {
//                     throw new ObjectDisposedException(nameof(MessageBusLogger<TMessage>));
//                 }
//
//                 lock (_filtersLock)
//                 {
//                     _filterRules.Clear();
//                     _excludedTypes.Clear();
//                     _includedTypes.Clear();
//                 }
//                 
//                 _logger.Debug("Cleared all message filter rules");
//             }
//         }
//
//         /// <summary>
//         /// Attaches the logger to the message bus by subscribing to messages.
//         /// </summary>
//         private void AttachToMessageBus()
//         {
//             using (_profiler?.BeginSample("MessageBusLogger.AttachToMessageBus"))
//             {
//                 try
//                 {
//                     // Create a wrapper for the original bus to intercept subscription operations
//                     var wrapped = new WrappedMessageBus(_messageBus, this);
//                     
//                     // Subscribe to all messages for logging
//                     var token = _messageBus.SubscribeToAll(OnMessagePublished);
//                     _subscriptions.Add(token);
//                     
//                     _logger.Debug("Attached to message bus for logging");
//                 }
//                 catch (Exception ex)
//                 {
//                     _logger.Error($"Error attaching to message bus: {ex.Message}");
//                 }
//             }
//         }
//
//         /// <summary>
//         /// Handler for messages published on the bus.
//         /// </summary>
//         /// <param name="message">The published message.</param>
//         private void OnMessagePublished(TMessage message)
//         {
//             using (_profiler?.BeginSample("MessageBusLogger.OnMessagePublished"))
//             {
//                 if (!_isEnabled || _isDisposed)
//                 {
//                     return;
//                 }
//
//                 // Check if this message type should be logged
//                 Type messageType = message.GetType();
//                 
//                 lock (_filtersLock)
//                 {
//                     // Skip if type is explicitly excluded
//                     if (_excludedTypes.Contains(messageType))
//                     {
//                         return;
//                     }
//                     
//                     // Skip if we're only including specific types and this isn't one of them
//                     if (_includedTypes.Count > 0 && !_includedTypes.Contains(messageType))
//                     {
//                         return;
//                     }
//                     
//                     // Apply custom filter rules
//                     foreach (var rule in _filterRules)
//                     {
//                         if (!rule(message))
//                         {
//                             return; // Skip if any rule returns false
//                         }
//                     }
//                 }
//                 
//                 // Build the log message
//                 var logBuilder = new StringBuilder();
//                 logBuilder.Append($"Message published: Type={messageType.Name}, ID={message.Id}");
//                 
//                 // Add additional details if requested
//                 if (_includeMessageDetails)
//                 {
//                     try
//                     {
//                         // Add basic message details - customize based on your message structure
//                         logBuilder.Append($", Timestamp={new DateTime(message.Timestamp.Ticks):yyyy-MM-dd HH:mm:ss.fff}");
//                         
//                         // Add custom properties based on message type if needed
//                         // This is just an example and should be customized for your message types
//                         AddCustomMessageDetails(message, logBuilder);
//                     }
//                     catch (Exception ex)
//                     {
//                         logBuilder.Append($", [Error extracting details: {ex.Message}]");
//                     }
//                 }
//                 
//                 // Add stack trace if requested
//                 if (_includeStackTrace)
//                 {
//                     logBuilder.AppendLine();
//                     logBuilder.Append("Stack Trace: ");
//                     logBuilder.Append(Environment.StackTrace);
//                 }
//                 
//                 // Log the message
//                 _logger.Debug(logBuilder.ToString());
//             }
//         }
//
//         /// <summary>
//         /// Adds custom message details to the log message based on message type.
//         /// </summary>
//         /// <param name="message">The message to extract details from.</param>
//         /// <param name="logBuilder">The string builder to append details to.</param>
//         private void AddCustomMessageDetails(TMessage message, StringBuilder logBuilder)
//         {
//             // This is a placeholder method that should be customized based on your message types
//             // You can use reflection to extract properties, or add specific handling for known types
//             
//             // Example: If your message implements a custom interface with additional properties
//             // if (message is IDetailedMessage detailedMessage)
//             // {
//             //     logBuilder.Append($", Priority={detailedMessage.Priority}");
//             //     logBuilder.Append($", Category={detailedMessage.Category}");
//             // }
//         }
//
//         /// <summary>
//         /// Logs a subscription operation.
//         /// </summary>
//         /// <typeparam name="T">The message type being subscribed to.</typeparam>
//         /// <param name="handler">The subscription handler.</param>
//         internal void LogSubscription<T>() where T : TMessage
//         {
//             using (_profiler?.BeginSample("MessageBusLogger.LogSubscription"))
//             {
//                 if (!_isEnabled || _isDisposed)
//                 {
//                     return;
//                 }
//
//                 Type messageType = typeof(T);
//                 
//                 var logBuilder = new StringBuilder();
//                 logBuilder.Append($"Subscription added: Type={messageType.Name}");
//                 
//                 // Add stack trace if requested
//                 if (_includeStackTrace)
//                 {
//                     logBuilder.AppendLine();
//                     logBuilder.Append("Stack Trace: ");
//                     logBuilder.Append(Environment.StackTrace);
//                 }
//                 
//                 _logger.Debug(logBuilder.ToString());
//             }
//         }
//
//         /// <summary>
//         /// Logs an unsubscription operation.
//         /// </summary>
//         /// <param name="token">The subscription token being unsubscribed.</param>
//         internal void LogUnsubscription(ISubscriptionToken token)
//         {
//             using (_profiler?.BeginSample("MessageBusLogger.LogUnsubscription"))
//             {
//                 if (!_isEnabled || _isDisposed)
//                 {
//                     return;
//                 }
//
//                 var logBuilder = new StringBuilder();
//                 logBuilder.Append($"Subscription removed: Token={token}");
//                 
//                 // Add stack trace if requested
//                 if (_includeStackTrace)
//                 {
//                     logBuilder.AppendLine();
//                     logBuilder.Append("Stack Trace: ");
//                     logBuilder.Append(Environment.StackTrace);
//                 }
//                 
//                 _logger.Debug(logBuilder.ToString());
//             }
//         }
//
//         /// <summary>
//         /// Logs an error condition.
//         /// </summary>
//         /// <param name="message">The error message.</param>
//         /// <param name="exception">The exception that caused the error, if any.</param>
//         internal void LogError(string message, Exception exception = null)
//         {
//             using (_profiler?.BeginSample("MessageBusLogger.LogError"))
//             {
//                 if (!_isEnabled || _isDisposed)
//                 {
//                     return;
//                 }
//
//                 var logBuilder = new StringBuilder();
//                 logBuilder.Append($"MessageBus Error: {message}");
//                 
//                 if (exception != null)
//                 {
//                     logBuilder.AppendLine();
//                     logBuilder.Append($"Exception: {exception.GetType().Name}: {exception.Message}");
//                     
//                     if (_includeStackTrace)
//                     {
//                         logBuilder.AppendLine();
//                         logBuilder.Append("Stack Trace: ");
//                         logBuilder.Append(exception.StackTrace);
//                     }
//                 }
//                 
//                 _logger.Error(logBuilder.ToString());
//             }
//         }
//
//         /// <summary>
//         /// Disposes the logger and releases all resources.
//         /// </summary>
//         public void Dispose()
//         {
//             using (_profiler?.BeginSample("MessageBusLogger.Dispose"))
//             {
//                 Dispose(true);
//                 GC.SuppressFinalize(this);
//             }
//         }
//
//         /// <summary>
//         /// Releases resources used by the message bus logger.
//         /// </summary>
//         /// <param name="disposing">True if called from Dispose(), false if called from finalizer.</param>
//         protected virtual void Dispose(bool disposing)
//         {
//             if (_isDisposed)
//             {
//                 return;
//             }
//
//             if (disposing)
//             {
//                 // Unsubscribe from message bus
//                 foreach (var subscription in _subscriptions)
//                 {
//                     subscription.Dispose();
//                 }
//                 
//                 _subscriptions.Clear();
//                 
//                 _logger.Info("MessageBusLogger disposed");
//             }
//
//             _isDisposed = true;
//         }
//
//         /// <summary>
//         /// Finalizer to ensure resource cleanup.
//         /// </summary>
//         ~MessageBusLogger()
//         {
//             Dispose(false);
//         }
//
//         protected void ThrowIfDisposed()
//         {
//             if (_isDisposed)
//             {
//                 throw new ObjectDisposedException(nameof(WrappedMessageBus));
//             }
//         }
//
//         /// <summary>
//         /// A wrapper around IMessageBus that intercepts subscription operations for logging.
//         /// </summary>
//         private class WrappedMessageBus : IMessageBus<TMessage>
//         {
//             private readonly IMessageBus<TMessage> _innerBus;
//             private readonly MessageBusLogger<TMessage> _logger;
//
//             /// <summary>
//             /// Initializes a new instance of the WrappedMessageBus class.
//             /// </summary>
//             /// <param name="innerBus">The inner message bus to wrap.</param>
//             /// <param name="logger">The message bus logger.</param>
//             public WrappedMessageBus(IMessageBus<TMessage> innerBus, MessageBusLogger<TMessage> logger)
//             {
//                 _innerBus = innerBus;
//                 _logger = logger;
//             }
//
//             /// <inheritdoc/>
//             public ISubscriptionToken Subscribe<T>(Action<T> handler) where T : TMessage
//             {
//                 var token = _innerBus.Subscribe(handler);
//                 _logger.LogSubscription<T>();
//                 return token;
//             }
//
//             /// <inheritdoc/>
//             public ISubscriptionToken SubscribeAsync<T>(Func<T, Task> handler) where T : TMessage
//             {
//                 var token = _innerBus.SubscribeAsync(handler);
//                 _logger.LogSubscription<T>();
//                 return token;
//             }
//
//             /// <inheritdoc/>
//             public ISubscriptionToken SubscribeToAll(Action<TMessage> handler)
//             {
//                 var token = _innerBus.SubscribeToAll(handler);
//                 _logger.LogSubscription<TMessage>();
//                 return token;
//             }
//
//             /// <inheritdoc/>
//             public ISubscriptionToken SubscribeToAllAsync(Func<TMessage, Task> handler)
//             {
//                 var token = _innerBus.SubscribeToAllAsync(handler);
//                 _logger.LogSubscription<TMessage>();
//                 return token;
//             }
//
//             /// <inheritdoc/>
//             public void Unsubscribe(ISubscriptionToken token)
//             {
//                 _innerBus.Unsubscribe(token);
//                 _logger.LogUnsubscription(token);
//             }
//
//             /// <inheritdoc/>
//             public void Publish(TMessage message)
//             {
//                 try
//                 {
//                     _innerBus.Publish(message);
//                 }
//                 catch (Exception ex)
//                 {
//                     _logger.LogError($"Error publishing message {message.Id}", ex);
//                     throw;
//                 }
//             }
//
//             /// <inheritdoc/>
//             public async Task PublishAsync(TMessage message, CancellationToken cancellationToken = default)
//             {
//                 try
//                 {
//                     await _innerBus.PublishAsync(message, cancellationToken);
//                 }
//                 catch (Exception ex)
//                 {
//                     _logger.LogError($"Error publishing message {message.Id} asynchronously", ex);
//                     throw;
//                 }
//             }
//
//             public void Dispose()
//             {
//                 if (_disposed)
//                 {
//                     return;
//                 }
//
//                 try
//                 {
//                     // Dispose the inner bus if it implements IDisposable
//                     if (_innerBus is IDisposable disposableBus)
//                     {
//                         disposableBus.Dispose();
//                     }
//
//                     // Dispose the logger if it implements IDisposable
//                     if (_logger is IDisposable disposableLogger)
//                     {
//                         disposableLogger.Dispose();
//                     }
//                 }
//                 catch (Exception ex)
//                 {
//                     _logger?.LogError("Error during disposal of WrappedMessageBus", ex);
//                     throw;
//                 }
//                 finally
//                 {
//                     _isdisposed = true;
//                 }
//             }
//         }
//     }
// }