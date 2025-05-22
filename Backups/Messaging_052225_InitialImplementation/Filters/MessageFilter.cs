using System;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Interfaces;
using AhBearStudios.Core.Profiling.Interfaces;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Implementation of IMessageFilter that provides various filtering criteria for messages.
    /// Allows for filtering messages based on type, content, or metadata.
    /// </summary>
    /// <typeparam name="TMessage">The type of messages to filter.</typeparam>
    public class MessageFilter<TMessage> : IMessageFilter<TMessage> where TMessage : IMessage
    {
        private readonly Func<TMessage, bool> _filter;
        private readonly string _description;
        private readonly IBurstLogger _logger;
        private readonly IProfiler _profiler;
        private bool _isInverted;
        private bool _isDisposed;

        /// <summary>
        /// Gets a value indicating whether this filter is inverted (true becomes false and vice versa).
        /// </summary>
        public bool IsInverted => _isInverted;

        /// <summary>
        /// Gets a description of this filter.
        /// </summary>
        public string Description
        {
            get
            {
                if (_isInverted)
                {
                    return $"NOT ({_description})";
                }
                
                return _description;
            }
        }

        /// <summary>
        /// Initializes a new instance of the MessageFilter class with a filtering function.
        /// </summary>
        /// <param name="filter">The function that determines whether a message passes the filter.</param>
        /// <param name="description">A description of the filter.</param>
        /// <param name="logger">Optional logger for filter operations.</param>
        /// <param name="profiler">Optional profiler for performance monitoring.</param>
        public MessageFilter(Func<TMessage, bool> filter, string description, IBurstLogger logger = null, IProfiler profiler = null)
        {
            _filter = filter ?? throw new ArgumentNullException(nameof(filter));
            _description = description ?? "Custom Filter";
            _logger = logger;
            _profiler = profiler;
            _isInverted = false;
            _isDisposed = false;
        }

        /// <summary>
        /// Initializes a new instance of the MessageFilter class for a specific message type.
        /// </summary>
        /// <typeparam name="T">The specific message type to filter for.</typeparam>
        /// <param name="logger">Optional logger for filter operations.</param>
        /// <param name="profiler">Optional profiler for performance monitoring.</param>
        public static MessageFilter<TMessage> OfType<T>(IBurstLogger logger = null, IProfiler profiler = null) where T : TMessage
        {
            return new MessageFilter<TMessage>(
                message => message is T,
                $"Type Filter: {typeof(T).Name}",
                logger,
                profiler
            );
        }

        /// <summary>
        /// Creates a filter that accepts all messages.
        /// </summary>
        /// <param name="logger">Optional logger for filter operations.</param>
        /// <param name="profiler">Optional profiler for performance monitoring.</param>
        /// <returns>A filter that accepts all messages.</returns>
        public static MessageFilter<TMessage> AcceptAll(IBurstLogger logger = null, IProfiler profiler = null)
        {
            return new MessageFilter<TMessage>(
                _ => true,
                "Accept All",
                logger,
                profiler
            );
        }

        /// <summary>
        /// Creates a filter that rejects all messages.
        /// </summary>
        /// <param name="logger">Optional logger for filter operations.</param>
        /// <param name="profiler">Optional profiler for performance monitoring.</param>
        /// <returns>A filter that rejects all messages.</returns>
        public static MessageFilter<TMessage> RejectAll(IBurstLogger logger = null, IProfiler profiler = null)
        {
            return new MessageFilter<TMessage>(
                _ => false,
                "Reject All",
                logger,
                profiler
            );
        }

        /// <summary>
        /// Creates a filter based on a message property.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property.</typeparam>
        /// <param name="propertySelector">A function that selects the property from the message.</param>
        /// <param name="propertyFilter">A function that filters the property.</param>
        /// <param name="propertyName">The name of the property being filtered.</param>
        /// <param name="logger">Optional logger for filter operations.</param>
        /// <param name="profiler">Optional profiler for performance monitoring.</param>
        /// <returns>A filter based on the message property.</returns>
        public static MessageFilter<TMessage> Property<TProperty>(
            Func<TMessage, TProperty> propertySelector,
            Func<TProperty, bool> propertyFilter,
            string propertyName,
            IBurstLogger logger = null,
            IProfiler profiler = null)
        {
            if (propertySelector == null)
            {
                throw new ArgumentNullException(nameof(propertySelector));
            }
            
            if (propertyFilter == null)
            {
                throw new ArgumentNullException(nameof(propertyFilter));
            }
            
            if (string.IsNullOrEmpty(propertyName))
            {
                propertyName = "Property";
            }
            
            return new MessageFilter<TMessage>(
                message => propertyFilter(propertySelector(message)),
                $"Property Filter: {propertyName}",
                logger,
                profiler
            );
        }

        /// <inheritdoc/>
        public bool PassesFilter(TMessage message)
        {
            using (_profiler?.BeginSample("MessageFilter.PassesFilter"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessageFilter<TMessage>));
                }

                if (message == null)
                {
                    return false;
                }

                bool result = _filter(message);
                
                // Invert the result if the filter is inverted
                if (_isInverted)
                {
                    result = !result;
                }
                
                if (_logger != null)
                {
                    _logger.Debug($"Filter '{Description}' result for message {message.Id}: {result}");
                }
                
                return result;
            }
        }

        /// <inheritdoc/>
        public IMessageFilter<TMessage> Invert()
        {
            using (_profiler?.BeginSample("MessageFilter.Invert"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessageFilter<TMessage>));
                }

                _isInverted = !_isInverted;
                
                if (_logger != null)
                {
                    _logger.Debug($"Inverted filter: {Description}");
                }
                
                return this;
            }
        }

        /// <inheritdoc/>
        public IMessageFilter<TMessage> And(IMessageFilter<TMessage> other)
        {
            using (_profiler?.BeginSample("MessageFilter.And"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessageFilter<TMessage>));
                }

                if (other == null)
                {
                    throw new ArgumentNullException(nameof(other));
                }

                return new MessageFilter<TMessage>(
                    message => PassesFilter(message) && other.PassesFilter(message),
                    $"({Description}) AND ({other.Description})",
                    _logger,
                    _profiler
                );
            }
        }

        /// <inheritdoc/>
        public IMessageFilter<TMessage> Or(IMessageFilter<TMessage> other)
        {
            using (_profiler?.BeginSample("MessageFilter.Or"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessageFilter<TMessage>));
                }

                if (other == null)
                {
                    throw new ArgumentNullException(nameof(other));
                }

                return new MessageFilter<TMessage>(
                    message => PassesFilter(message) || other.PassesFilter(message),
                    $"({Description}) OR ({other.Description})",
                    _logger,
                    _profiler
                );
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            using (_profiler?.BeginSample("MessageFilter.Dispose"))
            {
                if (_isDisposed)
                {
                    return;
                }

                _isDisposed = true;
                
                if (_logger != null)
                {
                    _logger.Debug($"Filter disposed: {Description}");
                }
            }
        }

        /// <summary>
        /// Returns a string representation of this filter.
        /// </summary>
        /// <returns>A string representation of this filter.</returns>
        public override string ToString()
        {
            return Description;
        }
    }
}