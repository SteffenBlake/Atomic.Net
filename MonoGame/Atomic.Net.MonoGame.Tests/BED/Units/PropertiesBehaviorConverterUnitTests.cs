using System.Text.Json;
using Xunit;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Properties;

namespace Atomic.Net.MonoGame.Tests.BED.Units;

/// <summary>
/// Unit tests for PropertiesBehaviorConverter JSON deserialization.
/// Tests validation logic, error handling, and edge cases for property dictionaries.
/// </summary>
[Collection("NonParallel")]
[Trait("Category", "Unit")]
public sealed class PropertiesBehaviorConverterUnitTests : IDisposable
{
    public PropertiesBehaviorConverterUnitTests()
    {
        AtomicSystem.Initialize();
        EventBus<InitializeEvent>.Push(new());
    }

    public void Dispose()
    {
        EventBus<ShutdownEvent>.Push(new());
    }

    [Fact]
    public void Deserialize_ValidProperties_CreatesDictionary()
    {
        // Arrange
        var json = """
        {
            "enemy-type": "goblin",
            "max-health": 100,
            "is-boss": false
        }
        """;

        // Act
        var behavior = JsonSerializer.Deserialize<PropertiesBehavior>(json);

        // Assert
        Assert.NotNull(behavior.Properties);
        Assert.Equal(3, behavior.Properties.Count);
        
        Assert.True(behavior.Properties.ContainsKey("enemy-type"));
        Assert.True(behavior.Properties["enemy-type"].TryMatch(out string? enemyType));
        Assert.Equal("goblin", enemyType);
        
        Assert.True(behavior.Properties.ContainsKey("max-health"));
        Assert.True(behavior.Properties["max-health"].TryMatch(out float health));
        Assert.Equal(100f, health);
        
        Assert.True(behavior.Properties.ContainsKey("is-boss"));
        Assert.True(behavior.Properties["is-boss"].TryMatch(out bool isBoss));
        Assert.False(isBoss);
    }

    [Fact]
    public void Deserialize_EmptyObject_CreatesEmptyDictionary()
    {
        // Arrange
        var json = "{}";

        // Act
        var behavior = JsonSerializer.Deserialize<PropertiesBehavior>(json);

        // Assert
        Assert.NotNull(behavior.Properties);
        Assert.Empty(behavior.Properties);
    }

    [Fact]
    public void Deserialize_NullValue_SkipsProperty()
    {
        // Arrange
        var json = """
        {
            "valid-key": "value",
            "null-key": null,
            "another-valid": 123
        }
        """;

        // Act
        var behavior = JsonSerializer.Deserialize<PropertiesBehavior>(json);

        // Assert
        // test-architect: Per requirements, null values should be skipped (not added to dictionary)
        Assert.NotNull(behavior.Properties);
        Assert.Equal(2, behavior.Properties.Count);
        Assert.True(behavior.Properties.ContainsKey("valid-key"));
        Assert.True(behavior.Properties.ContainsKey("another-valid"));
        Assert.False(behavior.Properties.ContainsKey("null-key"));
    }

    [Fact]
    public void Deserialize_EmptyStringKey_ThrowsJsonException()
    {
        // Arrange
        using var errorListener = new FakeEventListener<ErrorEvent>();
        var json = """
        {
            "": "value"
        }
        """;

        // Act & Assert
        // test-architect: Per requirements, empty string keys should fire ErrorEvent and throw
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<PropertiesBehavior>(json));
        
        // test-architect: ErrorEvent should have been fired
        Assert.NotEmpty(errorListener.ReceivedEvents);
    }

    [Fact]
    public void Deserialize_WhitespaceOnlyKey_ThrowsJsonException()
    {
        // Arrange
        using var errorListener = new FakeEventListener<ErrorEvent>();
        var json = """
        {
            "   ": "value"
        }
        """;

        // Act & Assert
        // test-architect: Per requirements, whitespace-only keys should fire ErrorEvent and throw
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<PropertiesBehavior>(json));
        
        // test-architect: ErrorEvent should have been fired
        Assert.NotEmpty(errorListener.ReceivedEvents);
    }

    [Fact]
    public void Deserialize_DuplicateKeys_ThrowsJsonException()
    {
        // Arrange
        using var errorListener = new FakeEventListener<ErrorEvent>();
        var json = """
        {
            "faction": "horde",
            "faction": "alliance"
        }
        """;

        // Act & Assert
        // test-architect: System.Text.Json allows duplicate keys (last write wins) by default,
        // but our converter now detects and throws JsonException with ErrorEvent
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<PropertiesBehavior>(json));
        
        // test-architect: ErrorEvent should have been fired
        Assert.NotEmpty(errorListener.ReceivedEvents);
    }

    [Fact]
    public void Deserialize_CaseInsensitiveKeys_TreatsAsDuplicates()
    {
        // Arrange
        using var errorListener = new FakeEventListener<ErrorEvent>();
        var json = """
        {
            "Faction": "horde",
            "faction": "alliance"
        }
        """;

        // Act & Assert
        // test-architect: Per requirements, keys are case-insensitive
        // Dictionary uses StringComparer.OrdinalIgnoreCase, so "Faction" and "faction" are same key
        // Now that duplicate detection is implemented, should throw JsonException with ErrorEvent
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<PropertiesBehavior>(json));
        
        // test-architect: ErrorEvent should have been fired
        Assert.NotEmpty(errorListener.ReceivedEvents);
    }

    [Fact]
    public void Deserialize_ArrayValue_SkipsProperty()
    {
        // Arrange
        var json = """
        {
            "valid-key": "value",
            "array-key": [1, 2, 3],
            "another-valid": 123
        }
        """;

        // Act
        var behavior = JsonSerializer.Deserialize<PropertiesBehavior>(json);

        // Assert
        // test-architect: Per requirements, arrays are unsupported and should be skipped
        Assert.NotNull(behavior.Properties);
        Assert.Equal(2, behavior.Properties.Count);
        Assert.True(behavior.Properties.ContainsKey("valid-key"));
        Assert.True(behavior.Properties.ContainsKey("another-valid"));
        Assert.False(behavior.Properties.ContainsKey("array-key"));
    }

    [Fact]
    public void Deserialize_ObjectValue_SkipsProperty()
    {
        // Arrange
        var json = """
        {
            "valid-key": "value",
            "object-key": {"nested": "value"},
            "another-valid": 123
        }
        """;

        // Act
        var behavior = JsonSerializer.Deserialize<PropertiesBehavior>(json);

        // Assert
        // test-architect: Per requirements, objects are unsupported and should be skipped
        Assert.NotNull(behavior.Properties);
        Assert.Equal(2, behavior.Properties.Count);
        Assert.True(behavior.Properties.ContainsKey("valid-key"));
        Assert.True(behavior.Properties.ContainsKey("another-valid"));
        Assert.False(behavior.Properties.ContainsKey("object-key"));
    }

    [Fact]
    public void Deserialize_MixedValidAndInvalidTypes_KeepsOnlyValid()
    {
        // Arrange
        var json = """
        {
            "string-prop": "valid",
            "array-prop": [1, 2],
            "float-prop": 42.5,
            "object-prop": {"x": 1},
            "bool-prop": true,
            "null-prop": null
        }
        """;

        // Act
        var behavior = JsonSerializer.Deserialize<PropertiesBehavior>(json);

        // Assert
        Assert.NotNull(behavior.Properties);
        Assert.Equal(3, behavior.Properties.Count); // Only string, float, bool
        Assert.True(behavior.Properties.ContainsKey("string-prop"));
        Assert.True(behavior.Properties.ContainsKey("float-prop"));
        Assert.True(behavior.Properties.ContainsKey("bool-prop"));
        Assert.False(behavior.Properties.ContainsKey("array-prop"));
        Assert.False(behavior.Properties.ContainsKey("object-prop"));
        Assert.False(behavior.Properties.ContainsKey("null-prop"));
    }

    [Fact]
    public void Deserialize_SpecialCharactersInKeys_PreservesKeys()
    {
        // Arrange
        var json = """
        {
            "enemy-type": "goblin",
            "max_health": 100,
            "is.boss": false
        }
        """;

        // Act
        var behavior = JsonSerializer.Deserialize<PropertiesBehavior>(json);

        // Assert
        Assert.NotNull(behavior.Properties);
        Assert.Equal(3, behavior.Properties.Count);
        Assert.True(behavior.Properties.ContainsKey("enemy-type"));
        Assert.True(behavior.Properties.ContainsKey("max_health"));
        Assert.True(behavior.Properties.ContainsKey("is.boss"));
    }

    [Fact]
    public void Deserialize_LargeNumberOfProperties_HandlesCorrectly()
    {
        // Arrange
        var props = new List<string>();
        for (int i = 0; i < 50; i++)
        {
            props.Add($"\"prop-{i}\": {i}");
        }
        var json = "{" + string.Join(",", props) + "}";

        // Act
        var behavior = JsonSerializer.Deserialize<PropertiesBehavior>(json);

        // Assert
        // test-architect: Per requirements, bosses may have 20-50+ properties
        Assert.NotNull(behavior.Properties);
        Assert.Equal(50, behavior.Properties.Count);
        for (int i = 0; i < 50; i++)
        {
            Assert.True(behavior.Properties.ContainsKey($"prop-{i}"));
        }
    }
}
