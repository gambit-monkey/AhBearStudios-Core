using System.Collections.Generic;

namespace AhBearStudios.Core.Bootstrap.Models;

/// <summary>
/// Result of recovery attempt with detailed status information.
/// </summary>
public readonly record struct RecoveryResult
{
    /// <summary>Gets whether the recovery attempt was successful.</summary>
    public readonly bool IsSuccessful;

    /// <summary>Gets the recovery option that was attempted.</summary>
    public readonly RecoveryOption AttemptedOption;

    /// <summary>Gets services that were successfully registered during recovery.</summary>
    public readonly IReadOnlyList<Type> RecoveredServices;

    /// <summary>Gets services that could not be recovered.</summary>
    public readonly IReadOnlyList<Type> FailedServices;

    /// <summary>Gets detailed error messages from the recovery attempt.</summary>
    public readonly IReadOnlyList<string> ErrorMessages;

    /// <summary>Gets performance metrics from the recovery operation.</summary>
    public readonly InstallationMetrics RecoveryMetrics;

    /// <summary>Gets whether the system can continue operating with the recovered state.</summary>
    public readonly bool CanContinueOperation;

    /// <summary>
    /// Initializes a new recovery result.
    /// </summary>
    public RecoveryResult(bool isSuccessful, RecoveryOption attemptedOption,
        IReadOnlyList<Type> recoveredServices, IReadOnlyList<Type> failedServices,
        IReadOnlyList<string> errorMessages, InstallationMetrics recoveryMetrics,
        bool canContinueOperation)
    {
        IsSuccessful = isSuccessful;
        AttemptedOption = attemptedOption;
        RecoveredServices = recoveredServices;
        FailedServices = failedServices;
        ErrorMessages = errorMessages;
        RecoveryMetrics = recoveryMetrics;
        CanContinueOperation = canContinueOperation;
    }

    /// <summary>Creates a successful recovery result.</summary>
    public static RecoveryResult Success(RecoveryOption option, IReadOnlyList<Type> recoveredServices,
        InstallationMetrics metrics, bool canContinue = true) =>
        new(true, option, recoveredServices, Array.Empty<Type>(),
            Array.Empty<string>(), metrics, canContinue);

    /// <summary>Creates a failed recovery result.</summary>
    public static RecoveryResult Failure(RecoveryOption option, IReadOnlyList<string> errors,
        IReadOnlyList<Type> failedServices, InstallationMetrics metrics) =>
        new(false, option, Array.Empty<Type>(), failedServices, errors, metrics, false);
}