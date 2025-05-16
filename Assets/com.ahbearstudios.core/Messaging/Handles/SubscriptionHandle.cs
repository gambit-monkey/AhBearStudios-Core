using System;
using Unity.Collections.LowLevel.Unsafe;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Represents a handle to a native message subscription.
    /// This struct is unmanaged and can be used with Burst-compiled code.
    /// </summary>
    public readonly struct SubscriptionHandle : IEquatable<SubscriptionHandle>
    {
        /// <summary>
        /// Invalid subscription handle value
        /// </summary>
        public static readonly SubscriptionHandle Invalid = new SubscriptionHandle(-1);

        /// <summary>
        /// Gets the ID of the subscription
        /// </summary>
        public readonly int Id;
        
        /// <summary>
        /// Gets a value indicating whether this handle is valid
        /// </summary>
        public bool IsValid => Id >= 0;

        /// <summary>
        /// Initializes a new instance of the SubscriptionHandle struct
        /// </summary>
        /// <param name="id">The ID of the subscription</param>
        public SubscriptionHandle(int id)
        {
            Id = id;
        }

        /// <summary>
        /// Determines whether this handle is equal to another handle
        /// </summary>
        /// <param name="other">The other handle to compare with</param>
        /// <returns>True if the handles are equal; otherwise, false</returns>
        public bool Equals(SubscriptionHandle other) => Id == other.Id;

        /// <summary>
        /// Determines whether this handle is equal to another object
        /// </summary>
        /// <param name="obj">The object to compare with</param>
        /// <returns>True if the object is a handle and is equal to this handle; otherwise, false</returns>
        public override bool Equals(object obj) => obj is SubscriptionHandle other && Equals(other);

        /// <summary>
        /// Gets a hash code for this handle
        /// </summary>
        /// <returns>A hash code for this handle</returns>
        public override int GetHashCode() => Id;

        /// <summary>
        /// Returns a string representation of this handle
        /// </summary>
        /// <returns>A string representation of this handle</returns>
        public override string ToString() => $"SubscriptionHandle(Id: {Id}, IsValid: {IsValid})";

        /// <summary>
        /// Determines whether two handles are equal
        /// </summary>
        /// <param name="left">The first handle</param>
        /// <param name="right">The second handle</param>
        /// <returns>True if the handles are equal; otherwise, false</returns>
        public static bool operator ==(SubscriptionHandle left, SubscriptionHandle right) => left.Equals(right);

        /// <summary>
        /// Determines whether two handles are not equal
        /// </summary>
        /// <param name="left">The first handle</param>
        /// <param name="right">The second handle</param>
        /// <returns>True if the handles are not equal; otherwise, false</returns>
        public static bool operator !=(SubscriptionHandle left, SubscriptionHandle right) => !left.Equals(right);
        
        /// <summary>
        /// Converts this handle to an IntPtr using the ID
        /// </summary>
        /// <returns>An IntPtr representing this handle</returns>
        public unsafe IntPtr ToIntPtr()
        {
            return new IntPtr(Id);
        }

        /// <summary>
        /// Creates a handle from an IntPtr
        /// </summary>
        /// <param name="ptr">The IntPtr to convert</param>
        /// <returns>A subscription handle</returns>
        public static unsafe SubscriptionHandle FromIntPtr(IntPtr ptr)
        {
            return new SubscriptionHandle(ptr.ToInt32());
        }
        
        /// <summary>
        /// Marshal to a NativeArray of SubscriptionHandles
        /// </summary>
        /// <param name="handles">The handles to marshal</param>
        /// <returns>A pointer to the marshalled data</returns>
        public static unsafe IntPtr MarshalHandles(SubscriptionHandle[] handles)
        {
            if (handles == null || handles.Length == 0)
                return IntPtr.Zero;
                
            int* ptr = (int*)UnsafeUtility.Malloc(
                handles.Length * sizeof(int), 
                UnsafeUtility.AlignOf<int>(), 
                Unity.Collections.Allocator.Persistent);
                
            for (int i = 0; i < handles.Length; i++)
            {
                ptr[i] = handles[i].Id;
            }
            
            return (IntPtr)ptr;
        }
        
        /// <summary>
        /// Unmarshal from a pointer to an array of SubscriptionHandles
        /// </summary>
        /// <param name="ptr">The pointer to unmarshal</param>
        /// <param name="length">The number of handles in the array</param>
        /// <returns>An array of subscription handles</returns>
        public static unsafe SubscriptionHandle[] UnmarshalHandles(IntPtr ptr, int length)
        {
            if (ptr == IntPtr.Zero || length <= 0)
                return Array.Empty<SubscriptionHandle>();
                
            var handles = new SubscriptionHandle[length];
            int* handlePtr = (int*)ptr;
            
            for (int i = 0; i < length; i++)
            {
                handles[i] = new SubscriptionHandle(handlePtr[i]);
            }
            
            return handles;
        }
        
        /// <summary>
        /// Free marshalled handle data
        /// </summary>
        /// <param name="ptr">The pointer to free</param>
        public static unsafe void FreeMarshalled(IntPtr ptr)
        {
            if (ptr != IntPtr.Zero)
            {
                UnsafeUtility.Free((void*)ptr, Unity.Collections.Allocator.Persistent);
            }
        }
    }
}