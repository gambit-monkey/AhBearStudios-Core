using AhBearStudios.Core.Logging.Data;
using Unity.Collections;

namespace AhBearStudios.Core.Logging
{
    public interface ILogFormatter
    {
        FixedString512Bytes Format(LogMessage message);
    }
}