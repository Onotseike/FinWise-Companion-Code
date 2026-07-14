using System.Text.Json;
using System.Text.Json.Serialization;

namespace FinWise.Shared.Core.Telemetry;

/// <summary>
/// JSON converter for TokenMeasurementMode enum.
/// Handles case-insensitive string-to-enum conversion.
/// Converts both string names ("Hybrid", "hybrid", "HYBRID") and numeric values.
/// </summary>
public class TokenMeasurementModeJsonConverter : JsonConverter<TokenMeasurementMode>
{
    public override TokenMeasurementMode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                var stringValue = reader.GetString();
                if (string.IsNullOrWhiteSpace(stringValue))
                {
                    return TokenMeasurementMode.Hybrid; // Default fallback
                }

                if (string.Equals(stringValue, "estimate", StringComparison.OrdinalIgnoreCase))
                {
                    return TokenMeasurementMode.Estimated;
                }

                // Try case-insensitive enum parsing
                if (Enum.TryParse<TokenMeasurementMode>(stringValue, ignoreCase: true, out var result))
                {
                    return result;
                }

                // Fallback if parsing fails
                throw new JsonException($"Unable to convert \"{stringValue}\" to enum type \"{typeToConvert.Name}\"");

            case JsonTokenType.Number:
                // Handle numeric enum values (0, 1, 2)
                if (reader.TryGetInt32(out var intValue))
                {
                    if (Enum.IsDefined(typeof(TokenMeasurementMode), intValue))
                    {
                        return (TokenMeasurementMode)intValue;
                    }
                }
                throw new JsonException($"Unable to convert number {reader.GetInt32()} to enum type \"{typeToConvert.Name}\"");

            default:
                throw new JsonException($"Unexpected token {reader.TokenType} when parsing enum");
        }
    }

    public override void Write(Utf8JsonWriter writer, TokenMeasurementMode value, JsonSerializerOptions options)
    {
        // Write as string name for readability in JSON (e.g., "Hybrid" not 2)
        writer.WriteStringValue(value.ToString());
    }
}
