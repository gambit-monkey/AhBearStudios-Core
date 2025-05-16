using System;

namespace AhBearStudios.Core.Messaging.Attributes
{
    /// <summary>
    /// Attribute for providing a description for a type or member
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field)]
    public class DescriptionAttribute : Attribute
    {
        /// <summary>
        /// Gets the description
        /// </summary>
        public string Description { get; }
       
        public DescriptionAttribute(string description)
        {
            Description = description;
        }
    }
}