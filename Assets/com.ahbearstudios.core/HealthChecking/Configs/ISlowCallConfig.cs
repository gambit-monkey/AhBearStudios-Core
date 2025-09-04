using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Configs
{
    /// <summary>
    /// Interface for slow call detection configuration
    /// </summary>
    public interface ISlowCallConfig
    {
        /// <summary>
        /// Duration threshold for considering a call slow
        /// </summary>
        TimeSpan SlowCallDurationThreshold { get; }

        /// <summary>
        /// Percentage of slow calls that triggers circuit opening (0-100)
        /// </summary>
        double SlowCallRateThreshold { get; }

        /// <summary>
        /// Minimum number of slow calls required before evaluating threshold
        /// </summary>
        int MinimumSlowCalls { get; }

        /// <summary>
        /// Whether slow calls should be considered as failures
        /// </summary>
        bool TreatSlowCallsAsFailures { get; }

        /// <summary>
        /// Validates slow call configuration
        /// </summary>
        /// <returns>List of validation errors</returns>
        List<string> Validate();
    }
}