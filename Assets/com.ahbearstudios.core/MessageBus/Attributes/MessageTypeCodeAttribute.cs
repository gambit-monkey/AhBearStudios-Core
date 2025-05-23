using System;

namespace AhBearStudios.Core.MessageBus.Attributes
{
    /// <summary>
    /// Attribute used to specify an explicit type code for a message type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public sealed class MessageTypeCodeAttribute : Attribute
    {
        /// <summary>
        /// Gets the type code for the message type.
        /// </summary>
        public ushort TypeCode { get; }
        
        /// <summary>
        /// Initializes a new instance of the MessageTypeCodeAttribute class.
        /// </summary>
        /// <param name="typeCode">The type code for the message type.</param>
        public MessageTypeCodeAttribute(ushort typeCode)
        {
            TypeCode = typeCode;
        }
    }
}