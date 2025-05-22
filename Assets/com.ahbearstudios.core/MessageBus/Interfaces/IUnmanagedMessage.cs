namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for unmanaged messages that can be used in Unity Jobs and Burst-compiled code.
    /// Must be implemented by structs containing only unmanaged types.
    /// </summary>
    public interface IUnmanagedMessage : IMessage
    {
        // This interface doesn't add new members, but serves as a marker
        // to indicate that implementing types are unmanaged
    }
}