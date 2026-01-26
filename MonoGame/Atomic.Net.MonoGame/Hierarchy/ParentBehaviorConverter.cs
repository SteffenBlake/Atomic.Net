using System.Text.Json;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Selectors;

namespace Atomic.Net.MonoGame.Hierarchy;

/// <summary>
/// JSON converter for ParentBehavior.
/// Converts selector string (e.g., "@player") to ParentBehavior with EntitySelector.
/// </summary>
public class ParentBehaviorConverter : JsonConverter<ParentBehavior>
{
    public override ParentBehavior Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected string for parent selector, got {reader.TokenType}");
        }

        var selectorString = reader.GetString();
        if (string.IsNullOrWhiteSpace(selectorString))
        {
            throw new JsonException("Parent selector cannot be empty or whitespace");
        }

        // Parse the selector string using SelectorRegistry
        if (!SelectorRegistry.Instance.TryParse(selectorString.AsSpan(), out var selector))
        {
            throw new JsonException($"Failed to parse parent selector: '{selectorString}'");
        }

        return new ParentBehavior(selector);
    }

    public override void Write(Utf8JsonWriter writer, ParentBehavior value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
