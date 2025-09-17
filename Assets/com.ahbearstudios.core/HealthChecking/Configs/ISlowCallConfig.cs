using System;

namespace AhBearStudios.Core.HealthChecking.Configs
{
    /// <summary>
    /// Configuration interface for slow call detection in circuit breakers
    /// </summary>
    public interface ISlowCallConfig
    {
        /// <summary>
        /// Duration threshold for considering a call slow
        /// </summary>
        TimeSpan SlowCallDurationThreshold { get; }

        /// <summary>
        /// Rate threshold percentage for slow calls (0-100)
        /// </summary>
        double SlowCallRateThreshold { get; }

        /// <summary>
        /// Minimum number of slow calls before triggering circuit breaker
        /// </summary>
        int MinimumSlowCalls { get; }

        /// <summary>
        /// Whether to treat slow calls as failures for circuit breaker evaluation
        /// </summary>
        bool TreatSlowCallsAsFailures { get; }
    }
}