using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Atomic.Net.MonoGame.Tags;

/// <summary>
/// JSON converter for TagsBehavior.
/// Validates tags and throws JsonException for invalid entries.
/// </summary>
public partial class TagsBehaviorConverter : JsonConverter<TagsBehavior>
{
    // senior-dev: Regex for valid tag characters: a-z, A-Z, 0-9, -, _
    [GeneratedRegex("^[a-zA-Z0-9_-]+$")]
    private static partial Regex ValidTagPattern();

    public override TagsBehavior Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Guard: null value
        if (reader.TokenType == JsonTokenType.Null)
        {
            throw new JsonException("Tag array cannot be null");
        }

        // Guard: not an array
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException($"Tags must be an array, found {reader.TokenType}");
        }

        var tags = new FluentHashSet<string>(8, StringComparer.OrdinalIgnoreCase);

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            // Guard: null tag in array
            if (reader.TokenType == JsonTokenType.Null)
            {
                throw new JsonException("Tag array contains null value");
            }

            // Guard: non-string tag in array
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException($"Tag array contains non-string value of type {reader.TokenType}");
            }

            var tag = reader.GetString();

            // Guard: empty or whitespace tag
            if (string.IsNullOrWhiteSpace(tag))
            {
                throw new JsonException("Tag array contains empty or whitespace-only tag");
            }

            // Normalize to lowercase for case-insensitive matching
            var normalizedTag = tag.Trim().ToLower();

            // Guard: invalid characters in tag
            if (!ValidTagPattern().IsMatch(normalizedTag))
            {
                throw new JsonException($"Tag '{tag}' contains invalid characters (only a-z, A-Z, 0-9, -, _ allowed)");
            }

            // Guard: duplicate tag
            if (!tags.Add(normalizedTag))
            {
                throw new JsonException($"Duplicate tag '{tag}' in entity");
            }
        }

        return new TagsBehavior
        {
            Tags = tags
        };
    }

    public override void Write(Utf8JsonWriter writer, TagsBehavior value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.Tags, options);
    }
}
