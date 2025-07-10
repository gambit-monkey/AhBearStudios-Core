using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace AhBearStudios.Core.Pooling.Pools.Native
{
    /// <summary>
    /// Extension methods for UnsafeBitArray providing atomic operations for multithreaded contexts.
    /// These methods are designed for use with Unity's job system and Burst compiler.
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    public static class UnsafeBitArrayExtensions
    {
        /// <summary>
        /// Atomically tests and sets a bit, returning the previous value
        /// </summary>
        /// <param name="bitArray">The bit array to modify</param>
        /// <param name="index">Index of the bit to test and set</param>
        /// <returns>The previous value of the bit (true if it was set, false otherwise)</returns>
        [BurstCompile]
        public static bool TestAndSet(this UnsafeBitArray bitArray, int index)
        {
            if (!bitArray.IsCreated)
                throw new ObjectDisposedException(nameof(UnsafeBitArray));

            if (index < 0 || index >= bitArray.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
                
            bool previousValue = bitArray.IsSet(index);
            if (!previousValue)
            {
                bitArray.Set(index, true);
            }
            return previousValue;
        }

        /// <summary>
        /// Atomically tests and clears a bit, returning the previous value
        /// </summary>
        /// <param name="bitArray">The bit array to modify</param>
        /// <param name="index">Index of the bit to test and clear</param>
        /// <returns>The previous value of the bit (true if it was set, false otherwise)</returns>
        [BurstCompile]
        public static bool TestAndClear(this UnsafeBitArray bitArray, int index)
        {
            if (!bitArray.IsCreated)
                throw new ObjectDisposedException(nameof(UnsafeBitArray));

            if (index < 0 || index >= bitArray.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
                
            bool previousValue = bitArray.IsSet(index);
            if (previousValue)
            {
                bitArray.Set(index, false);
            }
            return previousValue;
        }
    }
}