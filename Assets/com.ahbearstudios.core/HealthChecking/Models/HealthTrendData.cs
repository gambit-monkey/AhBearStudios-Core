using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Core.HealthChecking.Services;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Internal data structure for trend analysis
/// </summary>
internal sealed class HealthTrendData
{
    private readonly FixedString64Bytes _checkName;
    private readonly List<HealthTrendDataPoint> _dataPoints;
    private readonly object _lock = new object();

    public HealthTrendData(FixedString64Bytes checkName)
    {
        _checkName = checkName;
        _dataPoints = new List<HealthTrendDataPoint>();
    }

    public void AddDataPoint(HealthCheckResult result)
    {
        lock (_lock)
        {
            _dataPoints.Add(new HealthTrendDataPoint
            {
                Timestamp = result.Timestamp,
                Status = result.Status,
                Duration = result.Duration
            });

            // Keep only recent data points for trend analysis
            const int maxDataPoints = 100;
            if (_dataPoints.Count > maxDataPoints)
            {
                _dataPoints.RemoveRange(0, _dataPoints.Count - maxDataPoints);
            }
        }
    }

    public List<HealthTrendDataPoint> GetRecentDataPoints(TimeSpan window)
    {
        lock (_lock)
        {
            var cutoffTime = DateTime.UtcNow - window;
            return _dataPoints.Where(dp => dp.Timestamp >= cutoffTime).ToList();
        }
    }
}