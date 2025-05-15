using System;
using System.Collections.Generic;
using AhBearStudios.Core.Logging.Data;
using UnityEngine.Profiling;

namespace AhBearStudios.Core.Logging.Middleware
{
    /// <summary>
    /// Manages a chain of middleware components that process log messages.
    /// </summary>
    public class LogMiddlewareManager
    {
        private ILogMiddleware _firstMiddleware;
        private readonly List<ILogMiddleware> _middlewareComponents = new List<ILogMiddleware>();
        
        /// <summary>
        /// Adds a middleware component to the chain.
        /// </summary>
        /// <param name="middleware">The middleware to add.</param>
        public void AddMiddleware(ILogMiddleware middleware)
        {
            if (middleware == null)
                throw new ArgumentNullException(nameof(middleware));
                
            _middlewareComponents.Add(middleware);
            
            // If this is the first middleware, set it as the start of the chain
            if (_firstMiddleware == null)
            {
                _firstMiddleware = middleware;
                return;
            }
            
            // Find the last middleware in the chain
            var current = _firstMiddleware;
            while (current.Next != null)
            {
                current = current.Next;
            }
            
            // Set the new middleware as the next in the chain
            current.Next = middleware;
        }
        
        /// <summary>
        /// Removes a middleware component from the chain.
        /// </summary>
        /// <param name="middleware">The middleware to remove.</param>
        /// <returns>True if the middleware was found and removed, false otherwise.</returns>
        public bool RemoveMiddleware(ILogMiddleware middleware)
        {
            if (middleware == null)
                return false;
                
            // Remove from the list
            if (!_middlewareComponents.Remove(middleware))
                return false;
                
            // Rebuild the chain
            RebuildChain();
            return true;
        }
        
        /// <summary>
        /// Processes a log message through the middleware chain.
        /// </summary>
        /// <param name="message">The log message to process.</param>
        /// <returns>True if the message should be processed by log targets, false otherwise.</returns>
        public bool ProcessMessage(ref LogMessage message)
        {
            Profiler.BeginSample("LogMiddlewareManager.ProcessMessage");
            
            try
            {
                if (_firstMiddleware == null)
                    return true;
                    
                return _firstMiddleware.Process(ref message);
            }
            finally
            {
                Profiler.EndSample();
            }
        }
        
        /// <summary>
        /// Clears all middleware components from the chain.
        /// </summary>
        public void ClearMiddleware()
        {
            _middlewareComponents.Clear();
            _firstMiddleware = null;
        }
        
        /// <summary>
        /// Rebuilds the middleware chain.
        /// </summary>
        private void RebuildChain()
        {
            _firstMiddleware = null;
            
            // Nothing to rebuild if empty
            if (_middlewareComponents.Count == 0)
                return;
                
            // Set the first middleware
            _firstMiddleware = _middlewareComponents[0];
            ILogMiddleware current = _firstMiddleware;
            
            // Connect the rest of the chain
            for (int i = 1; i < _middlewareComponents.Count; i++)
            {
                current.Next = _middlewareComponents[i];
                current = current.Next;
            }
            
            // Ensure the last middleware has no next
            current.Next = null;
        }
    }
}