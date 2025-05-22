using System;

namespace AhBearStudios.Core.Messaging.Attributes
{
    /// <summary>
    /// Attribute used to mark classes that handle messages.
    /// This can be used for automatic registration of message handlers.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class MessageHandlerAttribute : Attribute
    {
        /// <summary>
        /// Gets the priority of this handler. Lower values run earlier.
        /// </summary>
        public int Priority { get; }
        
        /// <summary>
        /// Gets whether this handler should be automatically registered.
        /// </summary>
        public bool AutoRegister { get; }
        
        /// <summary>
        /// Initializes a new instance of the MessageHandlerAttribute class.
        /// </summary>
        public MessageHandlerAttribute() : this(0, true)
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the MessageHandlerAttribute class with the specified priority.
        /// </summary>
        /// <param name="priority">The priority of this handler. Lower values run earlier.</param>
        public MessageHandlerAttribute(int priority) : this(priority, true)
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the MessageHandlerAttribute class with the specified priority and auto-registration setting.
        /// </summary>
        /// <param name="priority">The priority of this handler. Lower values run earlier.</param>
        /// <param name="autoRegister">Whether this handler should be automatically registered.</param>
        public MessageHandlerAttribute(int priority, bool autoRegister)
        {
            Priority = priority;
            AutoRegister = autoRegister;
        }
    }
}