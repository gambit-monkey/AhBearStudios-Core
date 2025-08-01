using System;
using System.Buffers;
using MemoryPack;
using MemoryPack.Formatters;
using UnityEngine;
using Unity.Collections;

namespace AhBearStudios.Unity.Serialization.Formatters
{
    /// <summary>
    /// High-performance MemoryPack formatter for Unity Quaternion type.
    /// Provides zero-allocation serialization optimized for 60+ FPS gameplay.
    /// Includes optional quaternion compression for network scenarios.
    /// </summary>
    public sealed class UnityQuaternionFormatter : MemoryPackFormatter<Quaternion>
    {
        /// <summary>
        /// Serializes a Quaternion to the MemoryPack writer with zero allocations.
        /// </summary>
        /// <typeparam name="TBufferWriter">The buffer writer type</typeparam>
        /// <param name="writer">The MemoryPack writer</param>
        /// <param name="value">The Quaternion value to serialize</param>
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref Quaternion value)
            where TBufferWriter : IBufferWriter<byte>
        {
            // Normalize quaternion to ensure valid data
            var normalized = value.normalized;
            
            // Write components directly as unmanaged data for maximum performance
            writer.WriteUnmanaged(normalized.x);
            writer.WriteUnmanaged(normalized.y);
            writer.WriteUnmanaged(normalized.z);
            writer.WriteUnmanaged(normalized.w);
        }

        /// <summary>
        /// Deserializes a Quaternion from the MemoryPack reader with zero allocations.
        /// </summary>
        /// <param name="reader">The MemoryPack reader</param>
        /// <param name="value">The deserialized Quaternion value</param>
        public override void Deserialize(ref MemoryPackReader reader, scoped ref Quaternion value)
        {
            // Read components directly as unmanaged data for maximum performance
            reader.ReadUnmanaged(out float x);
            reader.ReadUnmanaged(out float y);
            reader.ReadUnmanaged(out float z);
            reader.ReadUnmanaged(out float w);
            
            value = new Quaternion(x, y, z, w);
            
            // Ensure the quaternion is normalized
            if (value.sqrMagnitude > 0f)
            {
                value = value.normalized;
            }
            else
            {
                // Fallback to identity if invalid quaternion
                value = Quaternion.identity;
            }
        }
    }

    /// <summary>
    /// Compressed MemoryPack formatter for Unity Quaternion type.
    /// Uses smallest-three compression to reduce network bandwidth.
    /// Suitable for network scenarios where bandwidth is more important than precision.
    /// </summary>
    public sealed class UnityQuaternionCompressedFormatter : MemoryPackFormatter<Quaternion>
    {
        private const float COMPRESSION_SCALE = 32767f; // Max value for int16
        private const float DECOMPRESSION_SCALE = 1f / COMPRESSION_SCALE;

        /// <summary>
        /// Serializes a Quaternion using smallest-three compression.
        /// Reduces size from 16 bytes to 7 bytes with minimal precision loss.
        /// </summary>
        /// <typeparam name="TBufferWriter">The buffer writer type</typeparam>
        /// <param name="writer">The MemoryPack writer</param>
        /// <param name="value">The Quaternion value to serialize</param>
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref Quaternion value)
            where TBufferWriter : IBufferWriter<byte>
        {
            var normalized = value.normalized;
            
            // Find the component with the largest absolute value
            var maxIndex = 0;
            var maxValue = Math.Abs(normalized.x);
            
            if (Math.Abs(normalized.y) > maxValue)
            {
                maxIndex = 1;
                maxValue = Math.Abs(normalized.y);
            }
            if (Math.Abs(normalized.z) > maxValue)
            {
                maxIndex = 2;
                maxValue = Math.Abs(normalized.z);
            }
            if (Math.Abs(normalized.w) > maxValue)
            {
                maxIndex = 3;
            }

            // Write the index of the largest component
            writer.WriteUnmanaged((byte)maxIndex);
            
            // Write the sign of the largest component
            var sign = maxIndex switch
            {
                0 => normalized.x >= 0,
                1 => normalized.y >= 0,
                2 => normalized.z >= 0,
                _ => normalized.w >= 0
            };
            writer.WriteUnmanaged(sign);

            // Write the three smallest components as compressed values
            switch (maxIndex)
            {
                case 0: // Skip x, write y, z, w
                    writer.WriteUnmanaged((short)(normalized.y * COMPRESSION_SCALE));
                    writer.WriteUnmanaged((short)(normalized.z * COMPRESSION_SCALE));
                    writer.WriteUnmanaged((short)(normalized.w * COMPRESSION_SCALE));
                    break;
                case 1: // Skip y, write x, z, w
                    writer.WriteUnmanaged((short)(normalized.x * COMPRESSION_SCALE));
                    writer.WriteUnmanaged((short)(normalized.z * COMPRESSION_SCALE));
                    writer.WriteUnmanaged((short)(normalized.w * COMPRESSION_SCALE));
                    break;
                case 2: // Skip z, write x, y, w
                    writer.WriteUnmanaged((short)(normalized.x * COMPRESSION_SCALE));
                    writer.WriteUnmanaged((short)(normalized.y * COMPRESSION_SCALE));
                    writer.WriteUnmanaged((short)(normalized.w * COMPRESSION_SCALE));
                    break;
                case 3: // Skip w, write x, y, z
                    writer.WriteUnmanaged((short)(normalized.x * COMPRESSION_SCALE));
                    writer.WriteUnmanaged((short)(normalized.y * COMPRESSION_SCALE));
                    writer.WriteUnmanaged((short)(normalized.z * COMPRESSION_SCALE));
                    break;
            }
        }

        /// <summary>
        /// Deserializes a compressed Quaternion from the MemoryPack reader.
        /// </summary>
        /// <param name="reader">The MemoryPack reader</param>
        /// <param name="value">The deserialized Quaternion value</param>
        public override void Deserialize(ref MemoryPackReader reader, scoped ref Quaternion value)
        {
            // Read the index of the largest component
            reader.ReadUnmanaged(out byte maxIndex);
            
            // Read the sign of the largest component
            reader.ReadUnmanaged(out bool sign);

            // Read the three compressed components
            reader.ReadUnmanaged(out short comp1);
            reader.ReadUnmanaged(out short comp2);
            reader.ReadUnmanaged(out short comp3);

            // Decompress components
            var a = comp1 * DECOMPRESSION_SCALE;
            var b = comp2 * DECOMPRESSION_SCALE;
            var c = comp3 * DECOMPRESSION_SCALE;

            // Calculate the largest component using the quaternion constraint
            var largestSqr = 1f - (a * a + b * b + c * c);
            var largest = largestSqr > 0f ? Mathf.Sqrt(largestSqr) : 0f;
            if (!sign) largest = -largest;

            // Reconstruct the quaternion
            value = maxIndex switch
            {
                0 => new Quaternion(largest, a, b, c),
                1 => new Quaternion(a, largest, b, c),
                2 => new Quaternion(a, b, largest, c),
                _ => new Quaternion(a, b, c, largest)
            };

            // Ensure normalized result
            if (value.sqrMagnitude > 0f)
            {
                value = value.normalized;
            }
            else
            {
                value = Quaternion.identity;
            }
        }
    }
}