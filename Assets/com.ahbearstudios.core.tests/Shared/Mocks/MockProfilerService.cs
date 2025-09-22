using System;
using System.Collections.Generic;
using Unity.Collections;
using ZLinq;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Profiling.Models;
using AhBearStudios.Core.Common.Models;

namespace AhBearStudios.Core.Tests.Shared.Mocks
{
    public sealed class MockProfilerService : IProfilerService
    {
        private readonly List<ProfileSample> _samples = new List<ProfileSample>();
        private readonly Dictionary<string, float> _metrics = new Dictionary<string, float>();
        private readonly Dictionary<string, int> _scopeCounts = new Dictionary<string, int>();
        private readonly Stack<ProfileScope> _activeScopes = new Stack<ProfileScope>();

        public IReadOnlyList<ProfileSample> Samples => _samples.AsValueEnumerable().ToList();
        public IReadOnlyDictionary<string, float> Metrics => new Dictionary<string, float>(_metrics);
        public IReadOnlyDictionary<string, int> ScopeCounts => new Dictionary<string, int>(_scopeCounts);

        public bool IsEnabled { get; set; } = true;
        public bool IsProfilingActive { get; set; } = true;
        public int SampleCount => _samples.Count;
        public int MetricCount => _metrics.Count;
        public int ActiveScopeCount => _activeScopes.Count;

        public IDisposable BeginScope(string name)
        {
            if (!IsEnabled || !IsProfilingActive)
                return new MockProfileScope(this, name, false);

            if (!_scopeCounts.ContainsKey(name))
                _scopeCounts[name] = 0;
            _scopeCounts[name]++;

            var scope = new MockProfileScope(this, name, true);
            _activeScopes.Push(scope);
            return scope;
        }

        public void RecordSample(ProfilerTag tag, float value)
        {
            if (!IsEnabled || !IsProfilingActive)
                return;

            var sample = ProfileSample.Create(
                tag: tag,
                value: value,
                timestamp: DateTime.UtcNow,
                correlationId: Guid.NewGuid());

            _samples.Add(sample);
        }

        public void RecordMetric(string name, float value)
        {
            if (!IsEnabled || !IsProfilingActive)
                return;

            _metrics[name] = value;
        }

        public float GetMetricValue(string name)
        {
            return _metrics.TryGetValue(name, out var value) ? value : 0f;
        }

        public bool HasMetric(string name)
        {
            return _metrics.ContainsKey(name);
        }

        public int GetScopeCallCount(string name)
        {
            return _scopeCounts.TryGetValue(name, out var count) ? count : 0;
        }

        public IEnumerable<ProfileSample> GetSamplesForTag(ProfilerTag tag)
        {
            return _samples.AsValueEnumerable().Where(s => s.Tag.Equals(tag)).ToList();
        }

        public void Clear()
        {
            _samples.Clear();
            _metrics.Clear();
            _scopeCounts.Clear();
            _activeScopes.Clear();
        }

        public ProfilerStatistics GetStatistics()
        {
            return ProfilerStatistics.Create(
                totalSamples: _samples.Count,
                activeScopes: _activeScopes.Count,
                totalMetrics: _metrics.Count,
                isEnabled: IsEnabled);
        }

        public ValidationResult ValidateConfiguration()
        {
            return ValidationResult.Success("MockProfilerService");
        }

        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
        }

        public void SetProfilingActive(bool active)
        {
            IsProfilingActive = active;
        }

        public void Dispose()
        {
            Clear();
        }

        internal void EndScope(MockProfileScope scope)
        {
            if (_activeScopes.Count > 0 && _activeScopes.Peek() == scope)
            {
                _activeScopes.Pop();
            }
        }

        private sealed class MockProfileScope : IDisposable
        {
            private readonly MockProfilerService _service;
            private readonly string _name;
            private readonly bool _isActive;
            private readonly DateTime _startTime;
            private bool _disposed;

            public MockProfileScope(MockProfilerService service, string name, bool isActive)
            {
                _service = service;
                _name = name;
                _isActive = isActive;
                _startTime = DateTime.UtcNow;
            }

            public void Dispose()
            {
                if (_disposed || !_isActive)
                    return;

                _disposed = true;

                var duration = (float)(DateTime.UtcNow - _startTime).TotalMilliseconds;
                var tag = ProfilerTag.CreateMethodTag("Test", _name);
                _service.RecordSample(tag, duration);
                _service.EndScope(this);
            }

            public override bool Equals(object obj)
            {
                return ReferenceEquals(this, obj);
            }

            public override int GetHashCode()
            {
                return RuntimeHelpers.GetHashCode(this);
            }
        }
    }
}