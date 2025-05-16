using MemoryPack;
using UnityEngine;

namespace AhBearStudios.Core.com.ahbearstudios.core.Messaging.Serializers.Formatters
{
    /// <summary>
    /// MemoryPack formatter for Unity's Quaternion type
    /// </summary>
    public class QuaternionFormatter : MemoryPackFormatter<Quaternion>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, ref Quaternion value)
        {
            writer.WriteValue(value.x);
            writer.WriteValue(value.y);
            writer.WriteValue(value.z);
            writer.WriteValue(value.w);
        }

        public override void Deserialize(ref MemoryPackReader reader, ref Quaternion value)
        {
            value.x = reader.ReadValue<float>();
            value.y = reader.ReadValue<float>();
            value.z = reader.ReadValue<float>();
            value.w = reader.ReadValue<float>();
        }
    }
}