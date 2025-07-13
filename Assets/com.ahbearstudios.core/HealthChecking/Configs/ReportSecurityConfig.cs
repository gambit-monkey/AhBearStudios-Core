using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Configs;

public sealed record ReportSecurityConfig
{
    public bool EnableAuthentication { get; init; } = true;
    public bool EnableAuthorization { get; init; } = true;
    public bool EnableEncryption { get; init; } = true;
    public List<string> AllowedRoles { get; init; } = new();

    public static ReportSecurityConfig ForCompliance() => new()
    {
        EnableAuthentication = true,
        EnableAuthorization = true,
        EnableEncryption = true,
        AllowedRoles = new() { "Admin", "Auditor", "Compliance" }
    };

    public List<string> Validate() => new();
}