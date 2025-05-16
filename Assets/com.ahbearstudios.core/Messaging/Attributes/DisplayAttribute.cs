using System;

namespace AhBearStudios.Core.Messaging.Attributes
{
    /// <summary>
    /// Attribute for providing display information for a type or member
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field)]
    public class DisplayAttribute : Attribute
    {
        /// <summary>
        /// Gets the display name
        /// </summary>
        public string Name { get; }
       
        /// <summary>
        /// Gets the display description
        /// </summary>
        public string Description { get; }
       
        public DisplayAttribute(string name = null, string description = null)
        {
            Name = name;
            Description = description;
        }
    }
}