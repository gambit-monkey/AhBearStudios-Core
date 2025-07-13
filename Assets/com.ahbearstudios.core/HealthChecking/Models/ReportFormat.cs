namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Output formats for reports
/// </summary>
public enum ReportFormat
{
    /// <summary>
    /// JSON format for API consumption
    /// </summary>
    Json,

    /// <summary>
    /// HTML format for web viewing
    /// </summary>
    Html,

    /// <summary>
    /// PDF format for distribution
    /// </summary>
    Pdf,

    /// <summary>
    /// CSV format for data analysis
    /// </summary>
    Csv,

    /// <summary>
    /// XML format for structured data exchange
    /// </summary>
    Xml,

    /// <summary>
    /// Excel format for business analysis
    /// </summary>
    Excel,

    /// <summary>
    /// Plain text format for simple consumption
    /// </summary>
    Text,

    /// <summary>
    /// Markdown format for documentation
    /// </summary>
    Markdown
}