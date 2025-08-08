using System.Collections.Generic;

namespace AhBearStudios.Core.Alerting.Models;

/// <summary>
/// Strongly-typed settings for email-based alert channels.
/// Provides configuration options for SMTP-based alert delivery.
/// </summary>
public sealed record EmailChannelSettings : IChannelSettings
{
    /// <summary>
    /// Gets the SMTP server hostname or IP address.
    /// </summary>
    public string SmtpServer { get; init; } = string.Empty;

    /// <summary>
    /// Gets the SMTP server port number.
    /// Common values: 25 (unencrypted), 587 (STARTTLS), 465 (SSL/TLS).
    /// </summary>
    public int SmtpPort { get; init; } = 587;

    /// <summary>
    /// Gets whether SSL/TLS encryption should be used for the SMTP connection.
    /// </summary>
    public bool EnableSsl { get; init; } = true;

    /// <summary>
    /// Gets the sender email address for alert emails.
    /// </summary>
    public string FromEmail { get; init; } = string.Empty;

    /// <summary>
    /// Gets the display name for the sender.
    /// </summary>
    public string FromDisplayName { get; init; } = "Alert System";

    /// <summary>
    /// Gets the collection of recipient email addresses.
    /// </summary>
    public IReadOnlyList<string> ToEmails { get; init; } = new List<string>();

    /// <summary>
    /// Gets the collection of CC (carbon copy) recipient email addresses.
    /// </summary>
    public IReadOnlyList<string> CcEmails { get; init; } = new List<string>();

    /// <summary>
    /// Gets the collection of BCC (blind carbon copy) recipient email addresses.
    /// </summary>
    public IReadOnlyList<string> BccEmails { get; init; } = new List<string>();

    /// <summary>
    /// Gets the email subject template.
    /// Supports placeholders: {Severity}, {Source}, {Timestamp}, {Tag}.
    /// </summary>
    public string Subject { get; init; } = "[ALERT] {Severity} - {Source}";

    /// <summary>
    /// Gets whether HTML formatting should be used for email content.
    /// When false, plain text emails are sent.
    /// </summary>
    public bool UseHtml { get; init; } = true;

    /// <summary>
    /// Gets the email template for HTML emails.
    /// Supports standard HTML markup and alert placeholders.
    /// </summary>
    public string HtmlTemplate { get; init; } = @"
<html>
<head><title>Alert Notification</title></head>
<body>
<h3>Alert Details</h3>
<table border='1' cellpadding='5'>
<tr><td><strong>Timestamp:</strong></td><td>{Timestamp:yyyy-MM-dd HH:mm:ss}</td></tr>
<tr><td><strong>Severity:</strong></td><td>{Severity}</td></tr>
<tr><td><strong>Source:</strong></td><td>{Source}</td></tr>
<tr><td><strong>Message:</strong></td><td>{Message}</td></tr>
<tr><td><strong>Correlation ID:</strong></td><td>{CorrelationId}</td></tr>
</table>
</body>
</html>";

    /// <summary>
    /// Gets the email template for plain text emails.
    /// </summary>
    public string PlainTextTemplate { get; init; } = @"
Alert Details
=============
Timestamp: {Timestamp:yyyy-MM-dd HH:mm:ss}
Severity: {Severity}
Source: {Source}
Message: {Message}
Correlation ID: {CorrelationId}
";

    /// <summary>
    /// Gets the SMTP authentication method.
    /// </summary>
    public SmtpAuthMethod AuthMethod { get; init; } = SmtpAuthMethod.Login;

    /// <summary>
    /// Gets the SMTP username for authentication.
    /// </summary>
    public string Username { get; init; } = string.Empty;

    /// <summary>
    /// Gets the SMTP password for authentication.
    /// </summary>
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// Gets the email priority level.
    /// </summary>
    public EmailPriority Priority { get; init; } = EmailPriority.Normal;

    /// <summary>
    /// Gets whether delivery status notifications should be requested.
    /// </summary>
    public bool RequestDeliveryNotification { get; init; } = false;

    /// <summary>
    /// Gets whether read receipts should be requested.
    /// </summary>
    public bool RequestReadReceipt { get; init; } = false;

    /// <summary>
    /// Gets the default email channel settings.
    /// </summary>
    public static EmailChannelSettings Default => new();

    /// <summary>
    /// Validates the email channel settings.
    /// </summary>
    /// <returns>True if the settings are valid; otherwise, false.</returns>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(SmtpServer) &&
               !string.IsNullOrWhiteSpace(FromEmail) &&
               ToEmails.Count > 0 &&
               SmtpPort > 0;
    }
}

/// <summary>
/// Defines SMTP authentication methods.
/// </summary>
public enum SmtpAuthMethod : byte
{
    /// <summary>
    /// No authentication required.
    /// </summary>
    None = 0,

    /// <summary>
    /// LOGIN authentication method.
    /// </summary>
    Login = 1,

    /// <summary>
    /// PLAIN authentication method.
    /// </summary>
    Plain = 2,

    /// <summary>
    /// CRAM-MD5 authentication method.
    /// </summary>
    CramMd5 = 3
}

/// <summary>
/// Defines email priority levels.
/// </summary>
public enum EmailPriority : byte
{
    /// <summary>
    /// Low priority email.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal priority email.
    /// </summary>
    Normal = 1,

    /// <summary>
    /// High priority email.
    /// </summary>
    High = 2
}