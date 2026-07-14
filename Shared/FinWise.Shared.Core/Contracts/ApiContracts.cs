using System.Text.Json.Serialization;

namespace FinWise.Shared.Core.Contracts;

/// <summary>
/// Standard response envelope for API calls.
/// </summary>
public record ApiResponse<T>(
    [property: JsonPropertyName("data")] T Data,
    [property: JsonPropertyName("status")] int Status,
    [property: JsonPropertyName("headers")] Dictionary<string, string> Headers
);

/// <summary>
/// Standard error response format.
/// </summary>
public record ApiError(
    [property: JsonPropertyName("status")] int Status,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("code")] string? Code = null,
    [property: JsonPropertyName("details")] object? Details = null
);
