using System.Runtime.CompilerServices;
using System.Threading;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;

namespace AhBearStudios.Core.Pooling.Utilities
{
    /// <summary>
    /// Provides atomic operations optimized for Unity's Burst compiler.
    /// Uses system's built-in atomic operations where available.
    /// Falls back to safe implementations when necessary.
    /// </summary>
    [BurstCompile]
    public static class AtomicOperations
    {
        /// <summary>
        /// Atomically increments a value by 1
        /// </summary>
        /// <param name="location">Address of the value to increment</param>
        /// <returns>The incremented value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Increment(ref int location)
        {
            return Add(ref location, 1);
        }
        
        /// <summary>
        /// Atomically decrements a value by 1
        /// </summary>
        /// <param name="location">Address of the value to decrement</param>
        /// <returns>The decremented value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Decrement(ref int location)
        {
            return Add(ref location, -1);
        }
        
        /// <summary>
        /// Atomically adds a value
        /// </summary>
        /// <param name="location">Address of the value to add to</param>
        /// <param name="value">Value to add</param>
        /// <returns>The new value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Add(ref int location, int value)
        {
            // Use standard .NET Interlocked
            return System.Threading.Interlocked.Add(ref location, value);
        }
        
        /// <summary>
        /// Atomically exchanges two values
        /// </summary>
        /// <param name="location">Address of the value to exchange</param>
        /// <param name="value">New value</param>
        /// <returns>The original value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Exchange(ref int location, int value)
        {
            // Use standard .NET Interlocked
            return System.Threading.Interlocked.Exchange(ref location, value);
        }
        
        /// <summary>
        /// Atomically compares and exchanges values
        /// </summary>
        /// <param name="location">Address of the value to compare and exchange</param>
        /// <param name="value">New value</param>
        /// <param name="comparand">Value to compare against</param>
        /// <returns>The original value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CompareExchange(ref int location, int value, int comparand)
        {
            // Use standard .NET Interlocked
            return System.Threading.Interlocked.CompareExchange(ref location, value, comparand);
        }
        
        /// <summary>
        /// Atomically increments a long value by 1
        /// </summary>
        /// <param name="location">Address of the value to increment</param>
        /// <returns>The incremented value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Increment(ref long location)
        {
            return Add(ref location, 1);
        }
        
        /// <summary>
        /// Atomically decrements a long value by 1
        /// </summary>
        /// <param name="location">Address of the value to decrement</param>
        /// <returns>The decremented value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Decrement(ref long location)
        {
            return Add(ref location, -1);
        }
        
        /// <summary>
        /// Atomically adds a long value
        /// </summary>
        /// <param name="location">Address of the value to add to</param>
        /// <param name="value">Value to add</param>
        /// <returns>The new value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Add(ref long location, long value)
        {
            // Use standard .NET Interlocked
            return System.Threading.Interlocked.Add(ref location, value);
        }
        
        /// <summary>
        /// Atomically exchanges two long values
        /// </summary>
        /// <param name="location">Address of the value to exchange</param>
        /// <param name="value">New value</param>
        /// <returns>The original value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Exchange(ref long location, long value)
        {
            // Use standard .NET Interlocked
            return System.Threading.Interlocked.Exchange(ref location, value);
        }
        
        /// <summary>
        /// Atomically compares and exchanges long values
        /// </summary>
        /// <param name="location">Address of the value to compare and exchange</param>
        /// <param name="value">New value</param>
        /// <param name="comparand">Value to compare against</param>
        /// <returns>The original value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long CompareExchange(ref long location, long value, long comparand)
        {
            // Use standard .NET Interlocked
            return System.Threading.Interlocked.CompareExchange(ref location, value, comparand);
        }
        
        /// <summary>
        /// Atomically exchanges two float values
        /// </summary>
        /// <param name="location">Address of the value to exchange</param>
        /// <param name="value">New value</param>
        /// <returns>The original value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Exchange(ref float location, float value)
        {
            unsafe
            {
                int* locationAsInt = (int*)UnsafeUtility.AddressOf(ref location);
                int valueAsInt = UnsafeUtility.As<float, int>(ref value);
                int oldValueAsInt = Exchange(ref *locationAsInt, valueAsInt);
                return UnsafeUtility.As<int, float>(ref oldValueAsInt);
            }
        }
        
        /// <summary>
        /// Atomically compares and exchanges float values
        /// </summary>
        /// <param name="location">Address of the value to compare and exchange</param>
        /// <param name="value">New value</param>
        /// <param name="comparand">Value to compare against</param>
        /// <returns>The original value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CompareExchange(ref float location, float value, float comparand)
        {
            unsafe
            {
                int* locationAsInt = (int*)UnsafeUtility.AddressOf(ref location);
                int valueAsInt = UnsafeUtility.As<float, int>(ref value);
                int comparandAsInt = UnsafeUtility.As<float, int>(ref comparand);
                int oldValueAsInt = CompareExchange(ref *locationAsInt, valueAsInt, comparandAsInt);
                return UnsafeUtility.As<int, float>(ref oldValueAsInt);
            }
        }
        
        /// <summary>
        /// Atomically exchanges two double values
        /// </summary>
        /// <param name="location">Address of the value to exchange</param>
        /// <param name="value">New value</param>
        /// <returns>The original value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Exchange(ref double location, double value)
        {
            unsafe
            {
                long* locationAsLong = (long*)UnsafeUtility.AddressOf(ref location);
                long valueAsLong = UnsafeUtility.As<double, long>(ref value);
                long oldValueAsLong = Exchange(ref *locationAsLong, valueAsLong);
                return UnsafeUtility.As<long, double>(ref oldValueAsLong);
            }
        }
        
        /// <summary>
        /// Atomically compares and exchanges double values
        /// </summary>
        /// <param name="location">Address of the value to compare and exchange</param>
        /// <param name="value">New value</param>
        /// <param name="comparand">Value to compare against</param>
        /// <returns>The original value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double CompareExchange(ref double location, double value, double comparand)
        {
            unsafe
            {
                long* locationAsLong = (long*)UnsafeUtility.AddressOf(ref location);
                long valueAsLong = UnsafeUtility.As<double, long>(ref value);
                long comparandAsLong = UnsafeUtility.As<double, long>(ref comparand);
                long oldValueAsLong = CompareExchange(ref *locationAsLong, valueAsLong, comparandAsLong);
                return UnsafeUtility.As<long, double>(ref oldValueAsLong);
            }
        }
        
        /// <summary>
        /// Atomic memory barrier
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MemoryBarrier()
        {
            // Use standard .NET memory barrier, which is supported by Burst
            Thread.MemoryBarrier();
        }
    }
}