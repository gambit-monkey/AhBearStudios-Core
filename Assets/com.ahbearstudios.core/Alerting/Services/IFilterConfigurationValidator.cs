using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Alerting.Models;

namespace AhBearStudios.Core.Alerting.Services
{
    /// <summary>
    /// Interface for filter configuration validation.
    /// Separates complex validation logic from factory creation logic.
    /// </summary>
    public interface IFilterConfigurationValidator
    {
        /// <summary>
        /// Validates a filter configuration before creation.
        /// </summary>
        /// <param name="configuration">The configuration to validate</param>
        /// <returns>Validation result with any errors found</returns>
        AhBearStudios.Core.Common.Models.ValidationResult ValidateFilterConfiguration(FilterConfiguration configuration);
    }
}