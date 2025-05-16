using System;

namespace AhBearStudios.Core.Messaging.Attributes
{
    /// <summary>
    /// Attribute for specifying a default value for a message property
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class DefaultValueAttribute : Attribute
    {
        /// <summary>
        /// Gets the default value
        /// </summary>
        public object Value { get; }
       
        public DefaultValueAttribute(object value)
        {
            Value = value;
        }
    }
}