using MemoryPack;
using UnityEngine;

namespace AhBearStudios.Core.Messaging.Serializers.Formatters
{
    /// <summary>
    /// MemoryPack formatter for Unity's Color type
    /// </summary>
    public class ColorFormatter : MemoryPackFormatter<Color>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, ref Color value)
        {
            writer.WriteValue(value.r);
            writer.WriteValue(value.g);
            writer.WriteValue(value.b);
            writer.WriteValue(value.a);
        }

        public override void Deserialize(ref MemoryPackReader reader, ref Color value)
        {
            value.r = reader.ReadValue<float>();
            value.g = reader.ReadValue<float>();
            value.b = reader.ReadValue<float>();
            value.a = reader.ReadValue<float>();
        }
    }
}