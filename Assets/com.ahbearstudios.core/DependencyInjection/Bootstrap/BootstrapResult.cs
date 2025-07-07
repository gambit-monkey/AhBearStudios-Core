using System.Collections.Generic;
using AhBearStudios.Core.DependencyInjection.Models;

namespace AhBearStudios.Core.DependencyInjection.Bootstrap
{
    /// <summary>
    /// Result of a bootstrap operation with detailed information.
    /// </summary>
    public sealed class BootstrapResult
    {
        /// <summary>
        /// Gets whether the bootstrap was successful.
        /// </summary>
        public bool IsSuccess { get; }
        
        /// <summary>
        /// Gets the framework that was used for bootstrap.
        /// </summary>
        public ContainerFramework Framework { get; }
        
        /// <summary>
        /// Gets the number of installers that were executed.
        /// </summary>
        public int ExecutedInstallers { get; }
        
        /// <summary>
        /// Gets the total time taken for bootstrap.
        /// </summary>
        public TimeSpan TotalTime { get; }
        
        /// <summary>
        /// Gets any errors that occurred during bootstrap.
        /// </summary>
        public IReadOnlyList<BootstrapError> Errors { get; }
        
        /// <summary>
        /// Gets any warnings that occurred during bootstrap.
        /// </summary>
        public IReadOnlyList<BootstrapWarning> Warnings { get; }
        
        /// <summary>
        /// Gets the names of installers that were executed successfully.
        /// </summary>
        public IReadOnlyList<string> SuccessfulInstallers { get; }
        
        /// <summary>
        /// Gets the names of installers that failed to execute.
        /// </summary>
        public IReadOnlyList<string> FailedInstallers { get; }
        
        /// <summary>
        /// Initializes a new bootstrap result.
        /// </summary>
        public BootstrapResult(
            bool isSuccess,
            ContainerFramework framework,
            int executedInstallers,
            TimeSpan totalTime,
            IReadOnlyList<BootstrapError> errors = null,
            IReadOnlyList<BootstrapWarning> warnings = null,
            IReadOnlyList<string> successfulInstallers = null,
            IReadOnlyList<string> failedInstallers = null)
        {
            IsSuccess = isSuccess;
            Framework = framework;
            ExecutedInstallers = executedInstallers;
            TotalTime = totalTime;
            Errors = errors ?? Array.Empty<BootstrapError>();
            Warnings = warnings ?? Array.Empty<BootstrapWarning>();
            SuccessfulInstallers = successfulInstallers ?? Array.Empty<string>();
            FailedInstallers = failedInstallers ?? Array.Empty<string>();
        }
        
        /// <summary>
        /// Creates a successful bootstrap result.
        /// </summary>
        public static BootstrapResult Success(
            ContainerFramework framework,
            int executedInstallers,
            TimeSpan totalTime,
            IReadOnlyList<string> successfulInstallers = null,
            IReadOnlyList<BootstrapWarning> warnings = null)
        {
            return new BootstrapResult(
                true,
                framework,
                executedInstallers,
                totalTime,
                null,
                warnings,
                successfulInstallers,
                null);
        }
        
        /// <summary>
        /// Creates a failed bootstrap result.
        /// </summary>
        public static BootstrapResult Failure(
            ContainerFramework framework,
            int executedInstallers,
            TimeSpan totalTime,
            IReadOnlyList<BootstrapError> errors,
            IReadOnlyList<string> successfulInstallers = null,
            IReadOnlyList<string> failedInstallers = null,
            IReadOnlyList<BootstrapWarning> warnings = null)
        {
            return new BootstrapResult(
                false,
                framework,
                executedInstallers,
                totalTime,
                errors,
                warnings,
                successfulInstallers,
                failedInstallers);
        }
        
        /// <summary>
        /// Returns a summary of the bootstrap result.
        /// </summary>
        public override string ToString()
        {
            var status = IsSuccess ? "SUCCESS" : "FAILURE";
            var summary = $"Bootstrap {status} ({Framework}): " +
                         $"{ExecutedInstallers} installers executed in {TotalTime.TotalMilliseconds:F1}ms";
            
            if (Errors.Count > 0)
                summary += $", {Errors.Count} errors";
            
            if (Warnings.Count > 0)
                summary += $", {Warnings.Count} warnings";
            
            return summary;
        }
    }
}