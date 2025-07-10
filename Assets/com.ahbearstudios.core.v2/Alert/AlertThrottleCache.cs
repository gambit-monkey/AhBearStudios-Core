using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace AhBearStudios.Core.Alerts
{
    public sealed class AlertThrottleCache
    {
        private struct AlertKey : System.IEquatable<AlertKey>
        {
            public FixedString64Bytes Source;
            public FixedString64Bytes Tag;
            public AlertSeverity Severity;

            public bool Equals(AlertKey other) =>
                Source.Equals(other.Source) && Tag.Equals(other.Tag) && Severity == other.Severity;

            public override int GetHashCode() =>
                Source.GetHashCode() ^ Tag.GetHashCode() ^ (int)Severity;
        }

        private struct CacheEntry
        {
            public long LastRaisedTimestamp;
        }

        private UnsafeHashMap<AlertKey, CacheEntry> _cache;
        private readonly long _cooldownMillis;

        public AlertThrottleCache(long cooldownMillis, Allocator allocator)
        {
            _cooldownMillis = cooldownMillis;
            _cache = new UnsafeHashMap<AlertKey, CacheEntry>(16, allocator);
        }

        public bool ShouldSuppress(in Alert alert)
        {
            var key = new AlertKey { Source = alert.Source, Tag = alert.Tag, Severity = alert.Severity };
            if (_cache.TryGetValue(key, out var entry))
            {
                long now = alert.Timestamp;
                if (now - entry.LastRaisedTimestamp < _cooldownMillis)
                    return true;

                entry.LastRaisedTimestamp = now;
                _cache[key] = entry;
                return false;
            }

            _cache[key] = new CacheEntry { LastRaisedTimestamp = alert.Timestamp };
            return false;
        }

        public void Dispose()
        {
            if (_cache.IsCreated) _cache.Dispose();
        }
    }
}