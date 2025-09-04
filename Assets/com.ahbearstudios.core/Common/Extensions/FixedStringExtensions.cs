using System;
using Unity.Collections;

namespace AhBearStudios.Core.Common.Extensions
{
    /// <summary>
    /// Extension methods for converting between FixedString types and common .NET types.
    /// Provides safe conversions that maintain Unity's zero-allocation benefits while enabling
    /// clean interoperability with standard .NET APIs.
    /// Designed for Unity game development with Job System and Burst compatibility.
    /// </summary>
    public static class FixedStringExtensions
    {
        /// <summary>
        /// Converts a Guid to a FixedString64Bytes using the "N" format (32 characters, no dashes).
        /// This format fits within the 64-byte capacity of FixedString64Bytes.
        /// </summary>
        /// <param name="guid">The Guid to convert</param>
        /// <returns>FixedString64Bytes representation of the Guid</returns>
        public static FixedString64Bytes ToFixedString64(this Guid guid)
        {
            if (guid == Guid.Empty)
                return new FixedString64Bytes();
                
            return new FixedString64Bytes(guid.ToString("N")[..32]);
        }

        /// <summary>
        /// Attempts to parse a FixedString64Bytes as a Guid.
        /// Supports both "N" format (32 chars) and standard format (36 chars with dashes).
        /// </summary>
        /// <param name="fixedString">The FixedString to parse</param>
        /// <param name="guid">The parsed Guid if successful</param>
        /// <returns>True if parsing succeeded, false otherwise</returns>
        public static bool TryParseGuid(this FixedString64Bytes fixedString, out Guid guid)
        {
            if (fixedString.IsEmpty)
            {
                guid = Guid.Empty;
                return true;
            }

            return Guid.TryParse(fixedString.ToString(), out guid);
        }

        /// <summary>
        /// Safely converts a string to FixedString64Bytes with automatic truncation.
        /// Ensures the string fits within the 64-byte capacity (approximately 61 UTF-8 characters).
        /// </summary>
        /// <param name="str">The string to convert</param>
        /// <returns>FixedString64Bytes representation, truncated if necessary</returns>
        public static FixedString64Bytes ToFixedString64(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return new FixedString64Bytes();

            // FixedString64Bytes can hold ~61 UTF-8 characters safely
            const int maxLength = 61;
            return new FixedString64Bytes(str.Length > maxLength ? str[..maxLength] : str);
        }

        /// <summary>
        /// Safely converts a string to FixedString512Bytes with automatic truncation.
        /// Ensures the string fits within the 512-byte capacity (approximately 509 UTF-8 characters).
        /// </summary>
        /// <param name="str">The string to convert</param>
        /// <returns>FixedString512Bytes representation, truncated if necessary</returns>
        public static FixedString512Bytes ToFixedString512(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return new FixedString512Bytes();

            // FixedString512Bytes can hold ~509 UTF-8 characters safely
            const int maxLength = 509;
            return new FixedString512Bytes(str.Length > maxLength ? str[..maxLength] : str);
        }

        /// <summary>
        /// Safely converts a string to FixedString32Bytes with automatic truncation.
        /// Ensures the string fits within the 32-byte capacity (approximately 29 UTF-8 characters).
        /// </summary>
        /// <param name="str">The string to convert</param>
        /// <returns>FixedString32Bytes representation, truncated if necessary</returns>
        public static FixedString32Bytes ToFixedString32(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return new FixedString32Bytes();

            // FixedString32Bytes can hold ~29 UTF-8 characters safely
            const int maxLength = 29;
            return new FixedString32Bytes(str.Length > maxLength ? str[..maxLength] : str);
        }

        /// <summary>
        /// Converts a FixedString to Guid, returning Guid.Empty if parsing fails.
        /// This is a safe alternative to TryParseGuid for cases where you want a fallback.
        /// </summary>
        /// <param name="fixedString">The FixedString to parse</param>
        /// <returns>Parsed Guid or Guid.Empty if parsing fails</returns>
        public static Guid ToGuid(this FixedString64Bytes fixedString)
        {
            return TryParseGuid(fixedString, out var guid) ? guid : Guid.Empty;
        }

        /// <summary>
        /// Checks if a FixedString contains a valid Guid format.
        /// Useful for validation before attempting conversion.
        /// </summary>
        /// <param name="fixedString">The FixedString to validate</param>
        /// <returns>True if the FixedString contains a valid Guid format</returns>
        public static bool IsValidGuid(this FixedString64Bytes fixedString)
        {
            return TryParseGuid(fixedString, out _);
        }

        /// <summary>
        /// Creates a FixedString64Bytes from a correlationId, handling both Guid and string sources.
        /// This is optimized for the common pattern of correlation tracking.
        /// </summary>
        /// <param name="correlationId">The correlation ID as Guid</param>
        /// <returns>FixedString64Bytes representation optimized for correlation tracking</returns>
        public static FixedString64Bytes ToCorrelationFixedString(this Guid correlationId)
        {
            return correlationId == Guid.Empty ? new FixedString64Bytes() : correlationId.ToFixedString64();
        }
    }
}