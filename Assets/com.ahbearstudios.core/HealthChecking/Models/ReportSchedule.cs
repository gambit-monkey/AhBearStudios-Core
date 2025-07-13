using System.Collections.Generic;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Report schedule configuration for automated report generation
/// </summary>
public sealed record ReportSchedule
{
    /// <summary>
    /// Unique identifier for this schedule
    /// </summary>
    public FixedString64Bytes Id { get; init; } = GenerateId();

    /// <summary>
    /// Name of the scheduled report
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Cron expression for scheduling
    /// </summary>
    public string CronExpression { get; init; } = string.Empty;

    /// <summary>
    /// Type of report to generate
    /// </summary>
    public ReportType ReportType { get; init; } = ReportType.HealthSummary;

    /// <summary>
    /// Whether this schedule is enabled
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Output formats for this scheduled report
    /// </summary>
    public HashSet<ReportFormat> OutputFormats { get; init; } = new() { ReportFormat.Html };

    /// <summary>
    /// Recipients for this scheduled report
    /// </summary>
    public List<string> Recipients { get; init; } = new();

    /// <summary>
    /// Custom parameters for this scheduled report
    /// </summary>
    public Dictionary<string, object> Parameters { get; init; } = new();

    /// <summary>
    /// Time zone for cron expression evaluation
    /// </summary>
    public string TimeZone { get; init; } = "UTC";

    /// <summary>
    /// Priority of this scheduled report (higher numbers execute first)
    /// </summary>
    public int Priority { get; init; } = 100;

    /// <summary>
    /// Maximum execution time for this report
    /// </summary>
    public TimeSpan MaxExecutionTime { get; init; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Whether to retry failed report generation
    /// </summary>
    public bool EnableRetry { get; init; } = true;

    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    public int MaxRetryAttempts { get; init; } = 3;

    /// <summary>
    /// Delay between retry attempts
    /// </summary>
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Custom tags for this schedule
    /// </summary>
    public HashSet<FixedString64Bytes> Tags { get; init; } = new();

    /// <summary>
    /// Custom metadata for this schedule
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();

    /// <summary>
    /// Validates the report schedule
    /// </summary>
    /// <returns>List of validation error messages</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Schedule Name cannot be null or empty");

        if (string.IsNullOrWhiteSpace(CronExpression))
            errors.Add("CronExpression cannot be null or empty");

        if (!Enum.IsDefined(typeof(ReportType), ReportType))
            errors.Add($"Invalid report type: {ReportType}");

        if (OutputFormats.Count == 0)
            errors.Add("At least one output format must be specified");

        foreach (var format in OutputFormats)
        {
            if (!Enum.IsDefined(typeof(ReportFormat), format))
                errors.Add($"Invalid output format: {format}");
        }

        if (Priority < 0)
            errors.Add("Priority must be non-negative");

        if (MaxExecutionTime <= TimeSpan.Zero)
            errors.Add("MaxExecutionTime must be greater than zero");

        if (MaxRetryAttempts < 0)
            errors.Add("MaxRetryAttempts must be non-negative");

        if (RetryDelay < TimeSpan.Zero)
            errors.Add("RetryDelay must be non-negative");

        if (string.IsNullOrWhiteSpace(TimeZone))
            errors.Add("TimeZone cannot be null or empty");

        // Basic cron expression validation
        if (!IsValidCronExpression(CronExpression))
            errors.Add("Invalid cron expression format");

        return errors;
    }

    /// <summary>
    /// Creates a daily report schedule
    /// </summary>
    /// <param name="name">Schedule name</param>
    /// <param name="hour">Hour to run (0-23)</param>
    /// <param name="reportType">Type of report</param>
    /// <returns>Daily report schedule</returns>
    public static ReportSchedule Daily(string name, int hour, ReportType reportType = ReportType.HealthSummary)
    {
        if (hour < 0 || hour > 23)
            throw new ArgumentOutOfRangeException(nameof(hour), "Hour must be between 0 and 23");

        return new ReportSchedule
        {
            Name = name,
            CronExpression = $"0 {hour} * * *",
            ReportType = reportType,
            Priority = 500
        };
    }

    /// <summary>
    /// Creates a weekly report schedule
    /// </summary>
    /// <param name="name">Schedule name</param>
    /// <param name="dayOfWeek">Day of week to run</param>
    /// <param name="hour">Hour to run (0-23)</param>
    /// <param name="reportType">Type of report</param>
    /// <returns>Weekly report schedule</returns>
    public static ReportSchedule Weekly(string name, DayOfWeek dayOfWeek, int hour, ReportType reportType = ReportType.StatusTrends)
    {
        if (hour < 0 || hour > 23)
            throw new ArgumentOutOfRangeException(nameof(hour), "Hour must be between 0 and 23");

        var dayNum = (int)dayOfWeek;
        return new ReportSchedule
        {
            Name = name,
            CronExpression = $"0 {hour} * * {dayNum}",
            ReportType = reportType,
            Priority = 300
        };
    }

    /// <summary>
    /// Creates a monthly report schedule
    /// </summary>
    /// <param name="name">Schedule name</param>
    /// <param name="dayOfMonth">Day of month to run (1-31)</param>
    /// <param name="hour">Hour to run (0-23)</param>
    /// <param name="reportType">Type of report</param>
    /// <returns>Monthly report schedule</returns>
    public static ReportSchedule Monthly(string name, int dayOfMonth, int hour, ReportType reportType = ReportType.HistoricalAnalysis)
    {
        if (dayOfMonth < 1 || dayOfMonth > 31)
            throw new ArgumentOutOfRangeException(nameof(dayOfMonth), "Day of month must be between 1 and 31");

        if (hour < 0 || hour > 23)
            throw new ArgumentOutOfRangeException(nameof(hour), "Hour must be between 0 and 23");

        return new ReportSchedule
        {
            Name = name,
            CronExpression = $"0 {hour} {dayOfMonth} * *",
            ReportType = reportType,
            Priority = 100
        };
    }

    /// <summary>
    /// Creates a custom cron-based report schedule
    /// </summary>
    /// <param name="name">Schedule name</param>
    /// <param name="cronExpression">Custom cron expression</param>
    /// <param name="reportType">Type of report</param>
    /// <returns>Custom report schedule</returns>
    public static ReportSchedule Custom(string name, string cronExpression, ReportType reportType = ReportType.CustomReport)
    {
        return new ReportSchedule
        {
            Name = name,
            CronExpression = cronExpression,
            ReportType = reportType,
            Priority = 200
        };
    }

    /// <summary>
    /// Validates a cron expression format
    /// </summary>
    /// <param name="cronExpression">Cron expression to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    private static bool IsValidCronExpression(string cronExpression)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
            return false;

        var parts = cronExpression.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 5 && parts.Length <= 7; // Basic validation - could be more sophisticated
    }

    /// <summary>
    /// Generates a unique identifier for schedules
    /// </summary>
    /// <returns>Unique schedule ID</returns>
    private static FixedString64Bytes GenerateId()
    {
        return new FixedString64Bytes(Guid.NewGuid().ToString("N")[..16]);
    }
}