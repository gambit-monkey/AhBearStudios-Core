using System;
using System.Buffers;
using MemoryPack;
using MemoryPack.Formatters;
using UnityEngine;
using Unity.Collections;

namespace AhBearStudios.Unity.Serialization.Formatters
{
    /// <summary>
    /// High-performance MemoryPack formatter for Unity LayerMask type.
    /// Provides zero-allocation serialization optimized for 60+ FPS gameplay.
    /// </summary>
    public sealed class UnityLayerMaskFormatter : MemoryPackFormatter<LayerMask>
    {
        /// <summary>
        /// Serializes a LayerMask to the MemoryPack writer with zero allocations.
        /// </summary>
        /// <typeparam name="TBufferWriter">The buffer writer type</typeparam>
        /// <param name="writer">The MemoryPack writer</param>
        /// <param name="value">The LayerMask value to serialize</param>
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref LayerMask value)
            where TBufferWriter : IBufferWriter<byte>
        {
            writer.WriteUnmanaged(value.value);
        }

        /// <summary>
        /// Deserializes a LayerMask from the MemoryPack reader with zero allocations.
        /// </summary>
        /// <param name="reader">The MemoryPack reader</param>
        /// <param name="value">The deserialized LayerMask value</param>
        public override void Deserialize(ref MemoryPackReader reader, scoped ref LayerMask value)
        {
            reader.ReadUnmanaged(out int layerMaskValue);
            value = layerMaskValue;
        }
    }

    /// <summary>
    /// High-performance MemoryPack formatter for Unity Bounds type.
    /// Provides zero-allocation serialization optimized for 60+ FPS gameplay.
    /// </summary>
    public sealed class UnityBoundsFormatter : MemoryPackFormatter<Bounds>
    {
        /// <summary>
        /// Serializes a Bounds to the MemoryPack writer with zero allocations.
        /// </summary>
        /// <typeparam name="TBufferWriter">The buffer writer type</typeparam>
        /// <param name="writer">The MemoryPack writer</param>
        /// <param name="value">The Bounds value to serialize</param>
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref Bounds value)
            where TBufferWriter : IBufferWriter<byte>
        {
            // Write center components
            writer.WriteUnmanaged(value.center.x);
            writer.WriteUnmanaged(value.center.y);
            writer.WriteUnmanaged(value.center.z);
            
            // Write size components
            writer.WriteUnmanaged(value.size.x);
            writer.WriteUnmanaged(value.size.y);
            writer.WriteUnmanaged(value.size.z);
        }

        /// <summary>
        /// Deserializes a Bounds from the MemoryPack reader with zero allocations.
        /// </summary>
        /// <param name="reader">The MemoryPack reader</param>
        /// <param name="value">The deserialized Bounds value</param>
        public override void Deserialize(ref MemoryPackReader reader, scoped ref Bounds value)
        {
            // Read center components
            reader.ReadUnmanaged(out float centerX);
            reader.ReadUnmanaged(out float centerY);
            reader.ReadUnmanaged(out float centerZ);
            
            // Read size components
            reader.ReadUnmanaged(out float sizeX);
            reader.ReadUnmanaged(out float sizeY);
            reader.ReadUnmanaged(out float sizeZ);
            
            value = new Bounds(
                new Vector3(centerX, centerY, centerZ),
                new Vector3(sizeX, sizeY, sizeZ)
            );
        }
    }

    /// <summary>
    /// High-performance MemoryPack formatter for Unity BoundsInt type.
    /// Provides zero-allocation serialization optimized for 60+ FPS gameplay.
    /// </summary>
    public sealed class UnityBoundsIntFormatter : MemoryPackFormatter<BoundsInt>
    {
        /// <summary>
        /// Serializes a BoundsInt to the MemoryPack writer with zero allocations.
        /// </summary>
        /// <typeparam name="TBufferWriter">The buffer writer type</typeparam>
        /// <param name="writer">The MemoryPack writer</param>
        /// <param name="value">The BoundsInt value to serialize</param>
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref BoundsInt value)
            where TBufferWriter : IBufferWriter<byte>
        {
            // Write position components
            writer.WriteUnmanaged(value.position.x);
            writer.WriteUnmanaged(value.position.y);
            writer.WriteUnmanaged(value.position.z);
            
            // Write size components
            writer.WriteUnmanaged(value.size.x);
            writer.WriteUnmanaged(value.size.y);
            writer.WriteUnmanaged(value.size.z);
        }

        /// <summary>
        /// Deserializes a BoundsInt from the MemoryPack reader with zero allocations.
        /// </summary>
        /// <param name="reader">The MemoryPack reader</param>
        /// <param name="value">The deserialized BoundsInt value</param>
        public override void Deserialize(ref MemoryPackReader reader, scoped ref BoundsInt value)
        {
            // Read position components
            reader.ReadUnmanaged(out int posX);
            reader.ReadUnmanaged(out int posY);
            reader.ReadUnmanaged(out int posZ);
            
            // Read size components
            reader.ReadUnmanaged(out int sizeX);
            reader.ReadUnmanaged(out int sizeY);
            reader.ReadUnmanaged(out int sizeZ);
            
            value = new BoundsInt(
                new Vector3Int(posX, posY, posZ),
                new Vector3Int(sizeX, sizeY, sizeZ)
            );
        }
    }

    /// <summary>
    /// High-performance MemoryPack formatter for Unity Rect type.
    /// Provides zero-allocation serialization optimized for 60+ FPS gameplay.
    /// </summary>
    public sealed class UnityRectFormatter : MemoryPackFormatter<Rect>
    {
        /// <summary>
        /// Serializes a Rect to the MemoryPack writer with zero allocations.
        /// </summary>
        /// <typeparam name="TBufferWriter">The buffer writer type</typeparam>
        /// <param name="writer">The MemoryPack writer</param>
        /// <param name="value">The Rect value to serialize</param>
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref Rect value)
            where TBufferWriter : IBufferWriter<byte>
        {
            writer.WriteUnmanaged(value.x);
            writer.WriteUnmanaged(value.y);
            writer.WriteUnmanaged(value.width);
            writer.WriteUnmanaged(value.height);
        }

        /// <summary>
        /// Deserializes a Rect from the MemoryPack reader with zero allocations.
        /// </summary>
        /// <param name="reader">The MemoryPack reader</param>
        /// <param name="value">The deserialized Rect value</param>
        public override void Deserialize(ref MemoryPackReader reader, scoped ref Rect value)
        {
            reader.ReadUnmanaged(out float x);
            reader.ReadUnmanaged(out float y);
            reader.ReadUnmanaged(out float width);
            reader.ReadUnmanaged(out float height);
            
            value = new Rect(x, y, width, height);
        }
    }

    /// <summary>
    /// High-performance MemoryPack formatter for Unity RectInt type.
    /// Provides zero-allocation serialization optimized for 60+ FPS gameplay.
    /// </summary>
    public sealed class UnityRectIntFormatter : MemoryPackFormatter<RectInt>
    {
        /// <summary>
        /// Serializes a RectInt to the MemoryPack writer with zero allocations.
        /// </summary>
        /// <typeparam name="TBufferWriter">The buffer writer type</typeparam>
        /// <param name="writer">The MemoryPack writer</param>
        /// <param name="value">The RectInt value to serialize</param>
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref RectInt value)
            where TBufferWriter : IBufferWriter<byte>
        {
            writer.WriteUnmanaged(value.x);
            writer.WriteUnmanaged(value.y);
            writer.WriteUnmanaged(value.width);
            writer.WriteUnmanaged(value.height);
        }

        /// <summary>
        /// Deserializes a RectInt from the MemoryPack reader with zero allocations.
        /// </summary>
        /// <param name="reader">The MemoryPack reader</param>
        /// <param name="value">The deserialized RectInt value</param>
        public override void Deserialize(ref MemoryPackReader reader, scoped ref RectInt value)
        {
            reader.ReadUnmanaged(out int x);
            reader.ReadUnmanaged(out int y);
            reader.ReadUnmanaged(out int width);
            reader.ReadUnmanaged(out int height);
            
            value = new RectInt(x, y, width, height);
        }
    }

    /// <summary>
    /// High-performance MemoryPack formatter for Unity RectOffset type.
    /// Provides zero-allocation serialization optimized for 60+ FPS gameplay.
    /// </summary>
    public sealed class UnityRectOffsetFormatter : MemoryPackFormatter<RectOffset>
    {
        /// <summary>
        /// Serializes a RectOffset to the MemoryPack writer with zero allocations.
        /// </summary>
        /// <typeparam name="TBufferWriter">The buffer writer type</typeparam>
        /// <param name="writer">The MemoryPack writer</param>
        /// <param name="value">The RectOffset value to serialize</param>
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref RectOffset value)
            where TBufferWriter : IBufferWriter<byte>
        {
            if (value == null)
            {
                writer.WriteUnmanaged(false); // null marker
                return;
            }
            
            writer.WriteUnmanaged(true); // not null marker
            writer.WriteUnmanaged(value.left);
            writer.WriteUnmanaged(value.right);
            writer.WriteUnmanaged(value.top);
            writer.WriteUnmanaged(value.bottom);
        }

        /// <summary>
        /// Deserializes a RectOffset from the MemoryPack reader with zero allocations.
        /// </summary>
        /// <param name="reader">The MemoryPack reader</param>
        /// <param name="value">The deserialized RectOffset value</param>
        public override void Deserialize(ref MemoryPackReader reader, scoped ref RectOffset value)
        {
            reader.ReadUnmanaged(out bool hasValue);
            
            if (!hasValue)
            {
                value = null;
                return;
            }
            
            reader.ReadUnmanaged(out int left);
            reader.ReadUnmanaged(out int right);
            reader.ReadUnmanaged(out int top);
            reader.ReadUnmanaged(out int bottom);
            
            value = new RectOffset(left, right, top, bottom);
        }
    }
}