using System;
using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Alerting.Filters;

namespace AhBearStudios.Core.Alerting.Models
{
    /// <summary>
    /// Result of filter creation operation.
    /// </summary>
    public sealed class FilterCreationResult
    {
        /// <summary>
        /// Gets or sets whether the creation was successful.
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Gets or sets the created filter instance.
        /// </summary>
        public IAlertFilter Filter { get; set; }

        /// <summary>
        /// Gets or sets any error that occurred during creation.
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Gets or sets the configuration used for creation.
        /// </summary>
        public FilterConfiguration Configuration { get; set; }

        /// <summary>
        /// Gets or sets the time taken for creation.
        /// </summary>
        public TimeSpan CreationTime { get; set; }

        /// <summary>
        /// Creates a successful result.
        /// </summary>
        /// <param name="filter">Created filter</param>
        /// <param name="configuration">Configuration used</param>
        /// <param name="creationTime">Time taken</param>
        /// <returns>Successful creation result</returns>
        public static FilterCreationResult Success(IAlertFilter filter, FilterConfiguration configuration, TimeSpan creationTime)
        {
            return new FilterCreationResult
            {
                IsSuccessful = true,
                Filter = filter,
                Configuration = configuration,
                CreationTime = creationTime
            };
        }

        /// <summary>
        /// Creates a failure result.
        /// </summary>
        /// <param name="error">Error message</param>
        /// <param name="configuration">Configuration that failed</param>
        /// <param name="creationTime">Time taken before failure</param>
        /// <returns>Failed creation result</returns>
        public static FilterCreationResult Failure(string error, FilterConfiguration configuration = null, TimeSpan creationTime = default)
        {
            return new FilterCreationResult
            {
                IsSuccessful = false,
                Error = error,
                Configuration = configuration,
                CreationTime = creationTime
            };
        }
    }
}