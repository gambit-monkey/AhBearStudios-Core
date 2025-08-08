using System;
using System.Collections.Generic;

namespace AhBearStudios.Core.Alerting.Models;

/// <summary>
/// Strongly-typed settings for network-based alert channels.
/// Provides configuration options for HTTP/HTTPS webhook delivery of alerts.
/// </summary>
public sealed record NetworkChannelSettings : IChannelSettings
{
    /// <summary>
    /// Gets the webhook endpoint URL for alert delivery.
    /// Must be a valid HTTP or HTTPS URL.
    /// </summary>
    public string Endpoint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the HTTP method to use for webhook requests.
    /// </summary>
    public HttpMethod Method { get; init; } = HttpMethod.Post;

    /// <summary>
    /// Gets the content type for the HTTP request.
    /// </summary>
    public string ContentType { get; init; } = "application/json";

    /// <summary>
    /// Gets the User-Agent header value for HTTP requests.
    /// </summary>
    public string UserAgent { get; init; } = "AhBearStudios-AlertSystem/2.0";

    /// <summary>
    /// Gets the request timeout for HTTP requests.
    /// </summary>
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets the authentication method for the webhook endpoint.
    /// </summary>
    public NetworkAuthMethod AuthMethod { get; init; } = NetworkAuthMethod.None;

    /// <summary>
    /// Gets the authentication token or API key for authenticated endpoints.
    /// </summary>
    public string AuthToken { get; init; } = string.Empty;

    /// <summary>
    /// Gets additional HTTP headers to include in webhook requests.
    /// </summary>
    public IReadOnlyDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets whether SSL certificate validation should be enforced.
    /// Should be true in production environments.
    /// </summary>
    public bool ValidateSslCertificate { get; init; } = true;

    /// <summary>
    /// Gets whether request/response bodies should be logged for debugging.
    /// Should be false in production to avoid logging sensitive data.
    /// </summary>
    public bool EnableDebugLogging { get; init; } = false;

    /// <summary>
    /// Gets whether gzip compression should be used for request bodies.
    /// </summary>
    public bool EnableCompression { get; init; } = true;

    /// <summary>
    /// Gets the default network channel settings.
    /// </summary>
    public static NetworkChannelSettings Default => new();

    /// <summary>
    /// Validates the network channel settings.
    /// </summary>
    /// <returns>True if the settings are valid; otherwise, false.</returns>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Endpoint) && 
               RequestTimeout > TimeSpan.Zero;
    }
}

/// <summary>
/// Defines HTTP methods supported for network alert channels.
/// </summary>
public enum HttpMethod : byte
{
    /// <summary>
    /// HTTP GET method.
    /// </summary>
    Get = 0,

    /// <summary>
    /// HTTP POST method.
    /// </summary>
    Post = 1,

    /// <summary>
    /// HTTP PUT method.
    /// </summary>
    Put = 2,

    /// <summary>
    /// HTTP PATCH method.
    /// </summary>
    Patch = 3
}

/// <summary>
/// Defines authentication methods for network alert channels.
/// </summary>
public enum NetworkAuthMethod : byte
{
    /// <summary>
    /// No authentication required.
    /// </summary>
    None = 0,

    /// <summary>
    /// Bearer token authentication (Authorization: Bearer {token}).
    /// </summary>
    BearerToken = 1,

    /// <summary>
    /// API key authentication (typically in headers or query parameters).
    /// </summary>
    ApiKey = 2,

    /// <summary>
    /// Basic HTTP authentication (Authorization: Basic {credentials}).
    /// </summary>
    BasicAuth = 3
}