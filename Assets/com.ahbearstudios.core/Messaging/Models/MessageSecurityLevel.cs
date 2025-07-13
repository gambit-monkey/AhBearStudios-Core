namespace AhBearStudios.Core.com.ahbearstudios.core.Messaging.Models;

/// <summary>
/// Defines the security level for messages.
/// </summary>
public enum MessageSecurityLevel : byte
{
    /// <summary>
    /// No security requirements.
    /// </summary>
    None = 0,

    /// <summary>
    /// Basic authentication required.
    /// </summary>
    Authenticated = 1,

    /// <summary>
    /// Message integrity verification required.
    /// </summary>
    Signed = 2,

    /// <summary>
    /// Message encryption required.
    /// </summary>
    Encrypted = 3,

    /// <summary>
    /// Both signing and encryption required.
    /// </summary>
    SignedAndEncrypted = 4
}