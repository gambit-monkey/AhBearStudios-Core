using MemoryPack;
using UnityEngine;

namespace AhBearStudios.Core.Messaging.Serializers.Formatters
{
    /// <summary>
    /// MemoryPack formatter for Unity's Vector3 type
    /// </summary>
    public class Vector3Formatter : MemoryPackFormatter<Vector3>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, ref Vector3 value)
        {
            writer.WriteValue(value.x);
            writer.WriteValue(value.y);
            writer.WriteValue(value.z);
        }

        public override void Deserialize(ref MemoryPackReader reader, ref Vector3 value)
        {
            value.x = reader.ReadValue<float>();
            value.y = reader.ReadValue<float>();
            value.z = reader.ReadValue<float>();
        }
    }
}