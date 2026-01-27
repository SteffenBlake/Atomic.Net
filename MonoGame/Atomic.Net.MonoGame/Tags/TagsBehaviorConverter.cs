using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tags;

/// <summary>
/// JSON converter for TagsBehavior.
/// Validates tags and fires ErrorEvents for invalid entries (null, empty, whitespace, duplicates, invalid characters).
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
            EventBus<ErrorEvent>.Push(new ErrorEvent("Tag array cannot be null"));
            return default;
        }
        
        // Guard: not an array
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"Tags must be an array, found {reader.TokenType}"));
            return default;
        }
        
        var builder = ImmutableHashSet.CreateBuilder<string>(StringComparer.OrdinalIgnoreCase);
        var seenTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }
            
            // Guard: null tag in array
            if (reader.TokenType == JsonTokenType.Null)
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent("Tag array contains null value (skipped)"));
                continue;
            }
            
            // Guard: non-string tag in array
            if (reader.TokenType != JsonTokenType.String)
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent($"Tag array contains non-string value of type {reader.TokenType} (skipped)"));
                continue;
            }
            
            var tag = reader.GetString();
            
            // Guard: empty or whitespace tag
            if (string.IsNullOrWhiteSpace(tag))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent("Tag array contains empty or whitespace-only tag (skipped)"));
                continue;
            }
            
            // Normalize to lowercase for case-insensitive matching
            var normalizedTag = tag.Trim().ToLowerInvariant();
            
            // Guard: invalid characters in tag
            if (!ValidTagPattern().IsMatch(normalizedTag))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent($"Tag '{tag}' contains invalid characters (only a-z, A-Z, 0-9, -, _ allowed) (skipped)"));
                continue;
            }
            
            // Guard: duplicate tag
            if (!seenTags.Add(normalizedTag))
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent($"Duplicate tag '{tag}' in entity (skipped)"));
                continue;
            }
            
            builder.Add(normalizedTag);
        }
        
        return new TagsBehavior
        {
            Tags = builder.ToImmutable()
        };
    }
    
    public override void Write(Utf8JsonWriter writer, TagsBehavior value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var tag in value.Tags)
        {
            writer.WriteStringValue(tag);
        }
        writer.WriteEndArray();
    }
}
