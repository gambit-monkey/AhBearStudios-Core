using AhBearStudios.Core.Logging.Data;
using Unity.Collections;

namespace AhBearStudios.Core.Logging.Formatters
{
    public class DefaultLogFormatter : ILogFormatter
    {
        public FixedString512Bytes Format(LogMessage message)
        {
            var timestamp = System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var formatted = new FixedString512Bytes($"[{timestamp}] [{message.Level}] [{message.GetTagString()}] {message.Message}");

            return formatted;
        }
    }
}