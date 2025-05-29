using System;
using System.Collections.Generic;
using AhBearStudios.Core.Logging.Data;
using AhBearStudios.Core.Logging.Messages;
using AhBearStudios.Core.Logging.Tags;

namespace AhBearStudios.Core.Logging.Middleware
{
    /// <summary>
    /// Interceptor that filters log messages based on custom rules.
    /// </summary>
    public class LogFilterInterceptor : ILogInterceptor
    {
        private readonly List<Func<LogMessage, bool>> _includeRules = new List<Func<LogMessage, bool>>();
        private readonly List<Func<LogMessage, bool>> _excludeRules = new List<Func<LogMessage, bool>>();
        private readonly int _order;
        private bool _isEnabled = true;
        
        /// <summary>
        /// Creates a new log filter with the specified execution order.
        /// </summary>
        /// <param name="order">Execution order (lower values run earlier).</param>
        public LogFilterInterceptor(int order = 0)
        {
            _order = order;
        }
        
        /// <summary>
        /// Add a rule that determines whether a message should be included.
        /// </summary>
        /// <param name="rule">Function that returns true if message should be included.</param>
        /// <returns>This interceptor (for fluent chaining).</returns>
        public LogFilterInterceptor Include(Func<LogMessage, bool> rule)
        {
            _includeRules.Add(rule);
            return this;
        }
        
        /// <summary>
        /// Add a rule that determines whether a message should be excluded.
        /// </summary>
        /// <param name="rule">Function that returns true if message should be excluded.</param>
        /// <returns>This interceptor (for fluent chaining).</returns>
        public LogFilterInterceptor Exclude(Func<LogMessage, bool> rule)
        {
            _excludeRules.Add(rule);
            return this;
        }
        
        /// <summary>
        /// Include only messages with the specified level.
        /// </summary>
        /// <param name="level">Log level to include.</param>
        /// <returns>This interceptor (for fluent chaining).</returns>
        public LogFilterInterceptor WithLevel(LogLevel level)
        {
            return Include(msg => msg.Level == level);
        }
        
        /// <summary>
        /// Include only messages with the specified tag.
        /// </summary>
        /// <param name="tag">Tag to include.</param>
        /// <returns>This interceptor (for fluent chaining).</returns>
        public LogFilterInterceptor WithTag(Tagging.LogTag tag)
        {
            return Include(msg => msg.Tag == tag);
        }
        
        /// <summary>
        /// Process the log message by applying the filter rules.
        /// </summary>
        /// <param name="message">The log message to filter.</param>
        /// <returns>True if the message should continue, false if it should be dropped.</returns>
        public bool Process(ref LogMessage message)
        {
            if (!_isEnabled)
                return true;
                
            // Check exclude rules first (if any rule matches, exclude the message)
            foreach (var rule in _excludeRules)
            {
                if (rule(message))
                    return false;
            }
            
            // If there are no include rules, accept the message
            if (_includeRules.Count == 0)
                return true;
                
            // If there are include rules, at least one must match
            foreach (var rule in _includeRules)
            {
                if (rule(message))
                    return true;
            }
            
            // No include rules matched
            return false;
        }
        
        /// <summary>
        /// Gets the execution order for this interceptor.
        /// </summary>
        public int Order => _order;
        
        /// <summary>
        /// Gets or sets whether this interceptor is enabled.
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }
    }
}