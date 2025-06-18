using System;
using Unity.Collections;

namespace AhBearStudios.Core.Alerts
{
    public static class CorrelationContext
    {
        [ThreadStatic] private static FixedString32Bytes _current;

        public static FixedString32Bytes Current => _current;

        public static void Set(FixedString32Bytes correlationId) => _current = correlationId;

        public static void Clear() => _current = default;

        public static bool IsSet => !_current.Equals(default);

        public static FixedString32Bytes GenerateId(string prefix = "CORR")
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return new FixedString32Bytes($"{prefix}-{timestamp}");
        }

        public static CorrelationScope BeginScoped(string prefix = "CORR")
        {
            var id = GenerateId(prefix);
            Set(id);
            return new CorrelationScope(id);
        }

        public readonly struct CorrelationScope : IDisposable
        {
            private readonly FixedString32Bytes _previous;

            public CorrelationScope(FixedString32Bytes newId)
            {
                _previous = _current;
                _current = newId;
            }

            public void Dispose()
            {
                _current = _previous;
            }
        }
    }
}