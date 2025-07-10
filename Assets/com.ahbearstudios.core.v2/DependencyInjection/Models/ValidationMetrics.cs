using System;

namespace AhBearStudios.Core.DependencyInjection.Models
{
    /// <summary>
    /// Performance metrics for validation operations.
    /// </summary>
    public sealed class ValidationMetrics
    {
        /// <summary>
        /// Gets the total number of validations performed.
        /// </summary>
        public long TotalValidations { get; internal set; }
        
        /// <summary>
        /// Gets the total time spent on validation.
        /// </summary>
        public TimeSpan TotalValidationTime { get; internal set; }
        
        /// <summary>
        /// Gets the average validation time.
        /// </summary>
        public double AverageValidationTimeMs => 
            TotalValidations > 0 ? TotalValidationTime.TotalMilliseconds / TotalValidations : 0.0;
        
        /// <summary>
        /// Gets the peak validation time.
        /// </summary>
        public TimeSpan PeakValidationTime { get; internal set; }
        
        /// <summary>
        /// Gets the number of registrations validated.
        /// </summary>
        public long TotalRegistrationsValidated { get; internal set; }
        
        /// <summary>
        /// Gets the number of validation errors found.
        /// </summary>
        public long TotalErrorsFound { get; internal set; }
        
        /// <summary>
        /// Gets the number of validation warnings found.
        /// </summary>
        public long TotalWarningsFound { get; internal set; }
        
        /// <summary>
        /// Gets the number of circular dependencies detected.
        /// </summary>
        public long CircularDependenciesDetected { get; internal set; }
        
        /// <summary>
        /// Records a validation operation.
        /// </summary>
        internal void RecordValidation(TimeSpan validationTime, int registrationsValidated, int errorsFound, int warningsFound, bool hasCircularDependencies)
        {
            TotalValidations++;
            TotalValidationTime = TotalValidationTime.Add(validationTime);
            TotalRegistrationsValidated += registrationsValidated;
            TotalErrorsFound += errorsFound;
            TotalWarningsFound += warningsFound;
            
            if (hasCircularDependencies)
                CircularDependenciesDetected++;
            
            if (validationTime > PeakValidationTime)
                PeakValidationTime = validationTime;
        }
        
        /// <summary>
        /// Resets all metrics.
        /// </summary>
        public void Reset()
        {
            TotalValidations = 0;
            TotalValidationTime = TimeSpan.Zero;
            PeakValidationTime = TimeSpan.Zero;
            TotalRegistrationsValidated = 0;
            TotalErrorsFound = 0;
            TotalWarningsFound = 0;
            CircularDependenciesDetected = 0;
        }
    }
}