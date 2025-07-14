using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Interface for validatable configurations
/// </summary>
public interface IValidatable
{
    List<string> Validate();
}