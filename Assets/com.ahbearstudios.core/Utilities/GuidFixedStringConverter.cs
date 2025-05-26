using System;
using Unity.Collections;

namespace AhBearStudios.Core.Utilities
{
    /// <summary>
    /// Provides conversion utilities for working with Guid in blittable contexts.
    /// Enables seamless integration between System.Guid and Unity's FixedString types.
    /// </summary>
    public static class GuidFixedStringConverter
    {
        /// <summary>
        /// Converts a Guid to a FixedString64Bytes representation.
        /// </summary>
        /// <param name="guid">The Guid to convert</param>
        /// <returns>A FixedString64Bytes representing the Guid</returns>
        public static FixedString64Bytes ToFixedString64Bytes(this Guid guid)
        {
            return new FixedString64Bytes(guid.ToString());
        }

        /// <summary>
        /// Attempts to convert a FixedString64Bytes back to a Guid.
        /// </summary>
        /// <param name="fixedString">The FixedString64Bytes to convert</param>
        /// <returns>The parsed Guid, or Guid.Empty if parsing fails</returns>
        public static Guid ToGuid(this FixedString64Bytes fixedString)
        {
            if (Guid.TryParse(fixedString.ToString(), out Guid result))
            {
                return result;
            }
            return Guid.Empty;
        }
        
        /// <summary>
        /// Attempts to convert a FixedString64Bytes to a Guid with validation.
        /// </summary>
        /// <param name="fixedString">The FixedString64Bytes to convert</param>
        /// <param name="result">The parsed Guid if successful</param>
        /// <returns>True if conversion was successful, false otherwise</returns>
        public static bool TryToGuid(this FixedString64Bytes fixedString, out Guid result)
        {
            return Guid.TryParse(fixedString.ToString(), out result);
        }
    }
}