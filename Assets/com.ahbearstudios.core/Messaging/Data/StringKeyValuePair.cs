using System;

namespace AhBearStudios.Core.Messaging.Data
{
    /// <summary>
    /// Serializable string key-value pair for use with Unity's JsonUtility
    /// </summary>
    [Serializable]
    public class StringKeyValuePair
    {
        /// <summary>
        /// Gets or sets the key
        /// </summary>
        public string Key;
        
        /// <summary>
        /// Gets or sets the value
        /// </summary>
        public string Value;
    }
}