using System.Text.Json;
using System.Text.Json.Serialization;

namespace FinWise.Shared.Core;

/// <summary>
/// Centralized, cached JSON serialization options to avoid creating new instances.
/// Creating a new JsonSerializerOptions instance for every serialization operation
/// substantially degrades application performance. This class provides static, reusable instances.
/// </summary>
public static class JsonSerializationDefaults
{
    /// <summary>
    /// Default options with no special configuration.
    /// </summary>
    public static JsonSerializerOptions Default { get; } = new();

    /// <summary>
    /// Options for human-readable output (pretty-printed JSON).
    /// Used for MCP responses, debugging, and formatted output.
    /// </summary>
    public static JsonSerializerOptions PrettyPrint { get; } = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Options for case-insensitive deserialization.
    /// Used for parsing JSON requests where casing may vary.
    /// </summary>
    public static JsonSerializerOptions CaseInsensitive { get; } = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Options for API clients using snake_case property names.
    /// Used for Toshl API and similar external API integrations.
    /// </summary>
    public static JsonSerializerOptions SnakeCaseApi { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Options combining pretty-print and case-insensitive.
    /// Used for formatted output with flexible input parsing.
    /// </summary>
    public static JsonSerializerOptions PrettyPrintCaseInsensitive { get; } = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Options for web APIs with camelCase properties.
    /// Used for REST API responses.
    /// </summary>
    public static JsonSerializerOptions WebApi { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };
}
