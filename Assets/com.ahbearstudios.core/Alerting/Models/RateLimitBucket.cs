using Unity.Collections;

namespace AhBearStudios.Core.Alerting.Models;

/// <summary>
/// Internal class representing a rate limit bucket for a specific source.
/// </summary>
public class RateLimitBucket
{
    public FixedString64Bytes Source { get; set; }
    public double TokensPerMinute { get; set; }
    public int BurstSize { get; set; }
    public double AvailableTokens { get; set; }
    public DateTime LastRefill { get; set; }
    public DateTime LastAlertTime { get; set; }
    public long AlertCount { get; set; }
    public long SuppressedCount { get; set; }
}