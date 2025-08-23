using System;
using System.Buffers;
using MemoryPack;
using MemoryPack.Formatters;
using UnityEngine;
using Unity.Collections;

namespace AhBearStudios.Unity.Serialization.Formatters
{
    /// <summary>
    /// High-performance MemoryPack formatter for Unity Vector3 type.
    /// Provides zero-allocation serialization optimized for 60+ FPS gameplay.
    /// </summary>
    public sealed class UnityVector3Formatter : MemoryPackFormatter<Vector3>
    {
        /// <summary>
        /// Serializes a Vector3 to the MemoryPack writer with zero allocations.
        /// </summary>
        /// <typeparam name="TBufferWriter">The buffer writer type</typeparam>
        /// <param name="writer">The MemoryPack writer</param>
        /// <param name="value">The Vector3 value to serialize</param>
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref Vector3 value)
            where TBufferWriter : IBufferWriter<byte>
        {
            // Write components directly as unmanaged data for maximum performance
            writer.WriteUnmanaged(value.x);
            writer.WriteUnmanaged(value.y);
            writer.WriteUnmanaged(value.z);
        }

        /// <summary>
        /// Deserializes a Vector3 from the MemoryPack reader with zero allocations.
        /// </summary>
        /// <param name="reader">The MemoryPack reader</param>
        /// <param name="value">The deserialized Vector3 value</param>
        public override void Deserialize(ref MemoryPackReader reader, scoped ref Vector3 value)
        {
            // Read components directly as unmanaged data for maximum performance
            reader.ReadUnmanaged(out float x);
            reader.ReadUnmanaged(out float y);
            reader.ReadUnmanaged(out float z);
            
            value = new Vector3(x, y, z);
        }
    }

    /// <summary>
    /// High-performance MemoryPack formatter for Unity Vector2 type.
    /// Provides zero-allocation serialization optimized for 60+ FPS gameplay.
    /// </summary>
    public sealed class UnityVector2Formatter : MemoryPackFormatter<Vector2>
    {
        /// <summary>
        /// Serializes a Vector2 to the MemoryPack writer with zero allocations.
        /// </summary>
        /// <typeparam name="TBufferWriter">The buffer writer type</typeparam>
        /// <param name="writer">The MemoryPack writer</param>
        /// <param name="value">The Vector2 value to serialize</param>
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref Vector2 value)
            where TBufferWriter : IBufferWriter<byte>
        {
            writer.WriteUnmanaged(value.x);
            writer.WriteUnmanaged(value.y);
        }

        /// <summary>
        /// Deserializes a Vector2 from the MemoryPack reader with zero allocations.
        /// </summary>
        /// <param name="reader">The MemoryPack reader</param>
        /// <param name="value">The deserialized Vector2 value</param>
        public override void Deserialize(ref MemoryPackReader reader, scoped ref Vector2 value)
        {
            reader.ReadUnmanaged(out float x);
            reader.ReadUnmanaged(out float y);
            
            value = new Vector2(x, y);
        }
    }

    /// <summary>
    /// High-performance MemoryPack formatter for Unity Vector4 type.
    /// Provides zero-allocation serialization optimized for 60+ FPS gameplay.
    /// </summary>
    public sealed class UnityVector4Formatter : MemoryPackFormatter<Vector4>
    {
        /// <summary>
        /// Serializes a Vector4 to the MemoryPack writer with zero allocations.
        /// </summary>
        /// <typeparam name="TBufferWriter">The buffer writer type</typeparam>
        /// <param name="writer">The MemoryPack writer</param>
        /// <param name="value">The Vector4 value to serialize</param>
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref Vector4 value)
            where TBufferWriter : IBufferWriter<byte>
        {
            writer.WriteUnmanaged(value.x);
            writer.WriteUnmanaged(value.y);
            writer.WriteUnmanaged(value.z);
            writer.WriteUnmanaged(value.w);
        }

        /// <summary>
        /// Deserializes a Vector4 from the MemoryPack reader with zero allocations.
        /// </summary>
        /// <param name="reader">The MemoryPack reader</param>
        /// <param name="value">The deserialized Vector4 value</param>
        public override void Deserialize(ref MemoryPackReader reader, scoped ref Vector4 value)
        {
            reader.ReadUnmanaged(out float x);
            reader.ReadUnmanaged(out float y);
            reader.ReadUnmanaged(out float z);
            reader.ReadUnmanaged(out float w);
            
            value = new Vector4(x, y, z, w);
        }
    }

    /// <summary>
    /// High-performance MemoryPack formatter for Unity Vector2Int type.
    /// Provides zero-allocation serialization optimized for 60+ FPS gameplay.
    /// </summary>
    public sealed class UnityVector2IntFormatter : MemoryPackFormatter<Vector2Int>
    {
        /// <summary>
        /// Serializes a Vector2Int to the MemoryPack writer with zero allocations.
        /// </summary>
        /// <typeparam name="TBufferWriter">The buffer writer type</typeparam>
        /// <param name="writer">The MemoryPack writer</param>
        /// <param name="value">The Vector2Int value to serialize</param>
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref Vector2Int value)
            where TBufferWriter : IBufferWriter<byte>
        {
            writer.WriteUnmanaged(value.x);
            writer.WriteUnmanaged(value.y);
        }

        /// <summary>
        /// Deserializes a Vector2Int from the MemoryPack reader with zero allocations.
        /// </summary>
        /// <param name="reader">The MemoryPack reader</param>
        /// <param name="value">The deserialized Vector2Int value</param>
        public override void Deserialize(ref MemoryPackReader reader, scoped ref Vector2Int value)
        {
            reader.ReadUnmanaged(out int x);
            reader.ReadUnmanaged(out int y);
            
            value = new Vector2Int(x, y);
        }
    }

    /// <summary>
    /// High-performance MemoryPack formatter for Unity Vector3Int type.
    /// Provides zero-allocation serialization optimized for 60+ FPS gameplay.
    /// </summary>
    public sealed class UnityVector3IntFormatter : MemoryPackFormatter<Vector3Int>
    {
        /// <summary>
        /// Serializes a Vector3Int to the MemoryPack writer with zero allocations.
        /// </summary>
        /// <typeparam name="TBufferWriter">The buffer writer type</typeparam>
        /// <param name="writer">The MemoryPack writer</param>
        /// <param name="value">The Vector3Int value to serialize</param>
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref Vector3Int value)
            where TBufferWriter : IBufferWriter<byte>
        {
            writer.WriteUnmanaged(value.x);
            writer.WriteUnmanaged(value.y);
            writer.WriteUnmanaged(value.z);
        }

        /// <summary>
        /// Deserializes a Vector3Int from the MemoryPack reader with zero allocations.
        /// </summary>
        /// <param name="reader">The MemoryPack reader</param>
        /// <param name="value">The deserialized Vector3Int value</param>
        public override void Deserialize(ref MemoryPackReader reader, scoped ref Vector3Int value)
        {
            reader.ReadUnmanaged(out int x);
            reader.ReadUnmanaged(out int y);
            reader.ReadUnmanaged(out int z);
            
            value = new Vector3Int(x, y, z);
        }
    }

    /// <summary>
    /// High-performance MemoryPack formatter for Unity Quaternion type with compression.
    /// Uses smallest-three compression to reduce quaternion from 16 to 10 bytes.
    /// Provides zero-allocation serialization optimized for 60+ FPS gameplay.
    /// </summary>
    public sealed class UnityQuaternionCompressedFormatter : MemoryPackFormatter<Quaternion>
    {
        /// <summary>
        /// Serializes a Quaternion using smallest-three compression with zero allocations.
        /// The largest component is omitted and reconstructed on deserialization.
        /// </summary>
        /// <typeparam name="TBufferWriter">The buffer writer type</typeparam>
        /// <param name="writer">The MemoryPack writer</param>
        /// <param name="value">The Quaternion value to serialize</param>
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref Quaternion value)
            where TBufferWriter : IBufferWriter<byte>
        {
            // Find largest component index
            float maxValue = 0f;
            byte maxIndex = 0;
            
            var absX = Mathf.Abs(value.x);
            var absY = Mathf.Abs(value.y);
            var absZ = Mathf.Abs(value.z);
            var absW = Mathf.Abs(value.w);
            
            if (absX >= maxValue) { maxValue = absX; maxIndex = 0; }
            if (absY >= maxValue) { maxValue = absY; maxIndex = 1; }
            if (absZ >= maxValue) { maxValue = absZ; maxIndex = 2; }
            if (absW >= maxValue) { maxValue = absW; maxIndex = 3; }
            
            // Write largest component index
            writer.WriteUnmanaged(maxIndex);
            
            // Write sign of largest component
            var sign = maxIndex switch
            {
                0 => value.x >= 0f,
                1 => value.y >= 0f,
                2 => value.z >= 0f,
                _ => value.w >= 0f
            };
            writer.WriteUnmanaged(sign);
            
            // Write the other three components
            switch (maxIndex)
            {
                case 0: // X is largest, write Y,Z,W
                    writer.WriteUnmanaged(value.y);
                    writer.WriteUnmanaged(value.z);
                    writer.WriteUnmanaged(value.w);
                    break;
                case 1: // Y is largest, write X,Z,W
                    writer.WriteUnmanaged(value.x);
                    writer.WriteUnmanaged(value.z);
                    writer.WriteUnmanaged(value.w);
                    break;
                case 2: // Z is largest, write X,Y,W
                    writer.WriteUnmanaged(value.x);
                    writer.WriteUnmanaged(value.y);
                    writer.WriteUnmanaged(value.w);
                    break;
                case 3: // W is largest, write X,Y,Z
                    writer.WriteUnmanaged(value.x);
                    writer.WriteUnmanaged(value.y);
                    writer.WriteUnmanaged(value.z);
                    break;
            }
        }

        /// <summary>
        /// Deserializes a compressed Quaternion from the MemoryPack reader with zero allocations.
        /// Reconstructs the largest component using the constraint that |q| = 1.
        /// </summary>
        /// <param name="reader">The MemoryPack reader</param>
        /// <param name="value">The deserialized Quaternion value</param>
        public override void Deserialize(ref MemoryPackReader reader, scoped ref Quaternion value)
        {
            // Read largest component info
            reader.ReadUnmanaged(out byte maxIndex);
            reader.ReadUnmanaged(out bool sign);
            
            // Read the three components
            reader.ReadUnmanaged(out float comp1);
            reader.ReadUnmanaged(out float comp2);
            reader.ReadUnmanaged(out float comp3);
            
            // Calculate largest component using |q|² = x² + y² + z² + w² = 1
            var sumSquares = comp1 * comp1 + comp2 * comp2 + comp3 * comp3;
            var largestComponent = Mathf.Sqrt(Mathf.Max(0f, 1f - sumSquares));
            if (!sign) largestComponent = -largestComponent;
            
            // Reconstruct quaternion
            switch (maxIndex)
            {
                case 0: // X was largest
                    value = new Quaternion(largestComponent, comp1, comp2, comp3);
                    break;
                case 1: // Y was largest
                    value = new Quaternion(comp1, largestComponent, comp2, comp3);
                    break;
                case 2: // Z was largest
                    value = new Quaternion(comp1, comp2, largestComponent, comp3);
                    break;
                default: // W was largest
                    value = new Quaternion(comp1, comp2, comp3, largestComponent);
                    break;
            }
        }
    }

    /// <summary>
    /// High-performance MemoryPack formatter for Unity Matrix4x4 type using TRS compression.
    /// Decomposes matrix into Translation, Rotation, and Scale components for more efficient storage.
    /// Provides zero-allocation serialization optimized for 60+ FPS gameplay.
    /// </summary>
    public sealed class UnityMatrix4x4TRSFormatter : MemoryPackFormatter<Matrix4x4>
    {
        /// <summary>
        /// Serializes a Matrix4x4 by decomposing it into TRS components with zero allocations.
        /// This is more efficient than storing the full 16-component matrix for transform matrices.
        /// </summary>
        /// <typeparam name="TBufferWriter">The buffer writer type</typeparam>
        /// <param name="writer">The MemoryPack writer</param>
        /// <param name="value">The Matrix4x4 value to serialize</param>
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref Matrix4x4 value)
            where TBufferWriter : IBufferWriter<byte>
        {
            // Decompose matrix into TRS components
            var translation = new Vector3(value.m03, value.m13, value.m23);
            
            // Extract rotation
            var rotation = Quaternion.LookRotation(
                new Vector3(value.m02, value.m12, value.m22),
                new Vector3(value.m01, value.m11, value.m21)
            );
            
            // Extract scale
            var scaleX = new Vector3(value.m00, value.m10, value.m20).magnitude;
            var scaleY = new Vector3(value.m01, value.m11, value.m21).magnitude;
            var scaleZ = new Vector3(value.m02, value.m12, value.m22).magnitude;
            var scale = new Vector3(scaleX, scaleY, scaleZ);
            
            // Write TRS components
            // Translation (3 floats)
            writer.WriteUnmanaged(translation.x);
            writer.WriteUnmanaged(translation.y);
            writer.WriteUnmanaged(translation.z);
            
            // Rotation (4 floats)
            writer.WriteUnmanaged(rotation.x);
            writer.WriteUnmanaged(rotation.y);
            writer.WriteUnmanaged(rotation.z);
            writer.WriteUnmanaged(rotation.w);
            
            // Scale (3 floats)
            writer.WriteUnmanaged(scale.x);
            writer.WriteUnmanaged(scale.y);
            writer.WriteUnmanaged(scale.z);
        }

        /// <summary>
        /// Deserializes a Matrix4x4 from TRS components with zero allocations.
        /// Reconstructs the full matrix from Translation, Rotation, and Scale.
        /// </summary>
        /// <param name="reader">The MemoryPack reader</param>
        /// <param name="value">The deserialized Matrix4x4 value</param>
        public override void Deserialize(ref MemoryPackReader reader, scoped ref Matrix4x4 value)
        {
            // Read TRS components
            // Translation
            reader.ReadUnmanaged(out float tx);
            reader.ReadUnmanaged(out float ty);
            reader.ReadUnmanaged(out float tz);
            var translation = new Vector3(tx, ty, tz);
            
            // Rotation
            reader.ReadUnmanaged(out float rx);
            reader.ReadUnmanaged(out float ry);
            reader.ReadUnmanaged(out float rz);
            reader.ReadUnmanaged(out float rw);
            var rotation = new Quaternion(rx, ry, rz, rw);
            
            // Scale
            reader.ReadUnmanaged(out float sx);
            reader.ReadUnmanaged(out float sy);
            reader.ReadUnmanaged(out float sz);
            var scale = new Vector3(sx, sy, sz);
            
            // Reconstruct matrix from TRS
            value = Matrix4x4.TRS(translation, rotation, scale);
        }
    }
}