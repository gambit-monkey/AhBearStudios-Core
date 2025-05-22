using System;

namespace AhBearStudios.Core.Messaging.Attributes
{
    /// <summary>
    /// Attribute that specifies the domain to which a message or group of messages belongs.
    /// Applied to a containing class or namespace to group related messages.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class MessageDomainAttribute : Attribute
    {
        /// <summary>
        /// Gets the name of the domain.
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// Gets the optional description of the domain.
        /// </summary>
        public string Description { get; }
        
        /// <summary>
        /// Initializes a new instance of the MessageDomainAttribute class.
        /// </summary>
        /// <param name="name">The name of the domain.</param>
        /// <param name="description">Optional description of the domain.</param>
        public MessageDomainAttribute(string name, string description = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description;
        }
    }
}