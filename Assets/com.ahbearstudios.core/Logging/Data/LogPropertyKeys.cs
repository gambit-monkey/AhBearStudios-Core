// Assets/com.ahbearstudios.core/Logging/Scripts/Data/LogPropertyKeys.cs
using Unity.Collections;

namespace AhBearStudios.Core.Logging.Data
{
    /// <summary>
    /// Contains pre-allocated common property keys to reduce allocations.
    /// </summary>
    public static class LogPropertyKeys
    {
        public static readonly FixedString32Bytes Time = new FixedString32Bytes("time");
        public static readonly FixedString32Bytes Duration = new FixedString32Bytes("duration");
        public static readonly FixedString32Bytes Count = new FixedString32Bytes("count");
        public static readonly FixedString32Bytes Id = new FixedString32Bytes("id");
        public static readonly FixedString32Bytes Name = new FixedString32Bytes("name");
        public static readonly FixedString32Bytes Type = new FixedString32Bytes("type");
        public static readonly FixedString32Bytes Value = new FixedString32Bytes("value");
        public static readonly FixedString32Bytes Path = new FixedString32Bytes("path");
        public static readonly FixedString32Bytes Error = new FixedString32Bytes("error");
        public static readonly FixedString32Bytes Position = new FixedString32Bytes("position");
        public static readonly FixedString32Bytes Status = new FixedString32Bytes("status");
        public static readonly FixedString32Bytes State = new FixedString32Bytes("state");
        public static readonly FixedString32Bytes Scene = new FixedString32Bytes("scene");
        public static readonly FixedString32Bytes UserId = new FixedString32Bytes("userId");
        public static readonly FixedString32Bytes SessionId = new FixedString32Bytes("sessionId");
        
        // Special keys for log system control
        public static readonly FixedString32Bytes Category = new FixedString32Bytes("category");
        public static readonly FixedString32Bytes Subsystem = new FixedString32Bytes("subsystem");
        public static readonly FixedString32Bytes Component = new FixedString32Bytes("component");
    }
}