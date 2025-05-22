using System;
using System.Runtime.InteropServices;
using AhBearStudios.Core.Messaging.Interfaces;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Message handler delegate for native message bus subscriptions.
    /// </summary>
    /// <param name="message">The message to handle.</param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MessageHandler<T>(T message) where T : unmanaged, IMessage;
    
    /// <summary>
    /// Type-erased message handler delegate for native message bus subscriptions.
    /// </summary>
    /// <param name="message">The message to handle as a void pointer.</param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MessageHandler(IntPtr message);
}