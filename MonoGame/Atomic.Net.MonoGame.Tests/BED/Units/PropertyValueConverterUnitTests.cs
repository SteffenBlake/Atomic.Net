using System.Text.Json;
using Xunit;
using Atomic.Net.MonoGame.Properties;

namespace Atomic.Net.MonoGame.Tests.BED.Units;

/// <summary>
/// Unit tests for PropertyValueConverter JSON deserialization.
/// Tests isolated JSON parsing behavior for PropertyValue types.
/// </summary>
[Collection("NonParallel")]
[Trait("Category", "Unit")]
public sealed class PropertyValueConverterUnitTests
{    [Fact]
    public void Deserialize_JsonString_CreatesStringPropertyValue()
    {
        // Arrange
        var json = "\"test-value\"";

        // Act
        var value = JsonSerializer.Deserialize<PropertyValue>(json);

        // Assert
        Assert.True(value.TryMatch(out string? result));
        Assert.Equal("test-value", result);
    }

    [Fact]
    public void Deserialize_JsonNumber_CreatesFloatPropertyValue()
    {
        // Arrange
        var json = "123.45";

        // Act
        var value = JsonSerializer.Deserialize<PropertyValue>(json);

        // Assert
        Assert.True(value.TryMatch(out float result));
        Assert.Equal(123.45f, result);
    }

    [Fact]
    public void Deserialize_JsonInteger_CreatesFloatPropertyValue()
    {
        // Arrange
        var json = "100";

        // Act
        var value = JsonSerializer.Deserialize<PropertyValue>(json);

        // Assert
        Assert.True(value.TryMatch(out float result));
        Assert.Equal(100f, result);
    }

    [Fact]
    public void Deserialize_JsonTrue_CreatesBoolPropertyValue()
    {
        // Arrange
        var json = "true";

        // Act
        var value = JsonSerializer.Deserialize<PropertyValue>(json);

        // Assert
        Assert.True(value.TryMatch(out bool result));
        Assert.True(result);
    }

    [Fact]
    public void Deserialize_JsonFalse_CreatesBoolPropertyValue()
    {
        // Arrange
        var json = "false";

        // Act
        var value = JsonSerializer.Deserialize<PropertyValue>(json);

        // Assert
        Assert.True(value.TryMatch(out bool result));
        Assert.False(result);
    }

    [Fact]
    public void Deserialize_JsonNull_ReturnsDefault()
    {
        // Arrange
        var json = "null";

        // Act
        var value = JsonSerializer.Deserialize<PropertyValue>(json);

        // Assert
        // test-architect: Per requirements, null should be skipped (returns default/empty)
        Assert.False(value.TryMatch(out string? _));
        Assert.False(value.TryMatch(out float _));
        Assert.False(value.TryMatch(out bool _));
    }

    [Fact]
    public void Deserialize_JsonArray_ReturnsDefault()
    {
        // Arrange
        var json = "[1, 2, 3]";

        // Act
        var value = JsonSerializer.Deserialize<PropertyValue>(json);

        // Assert
        // test-architect: Per requirements, arrays are not supported and should return default
        Assert.False(value.TryMatch(out string? _));
        Assert.False(value.TryMatch(out float _));
        Assert.False(value.TryMatch(out bool _));
    }

    [Fact]
    public void Deserialize_JsonObject_ReturnsDefault()
    {
        // Arrange
        var json = "{\"nested\": \"value\"}";

        // Act
        var value = JsonSerializer.Deserialize<PropertyValue>(json);

        // Assert
        // test-architect: Per requirements, objects are not supported and should return default
        Assert.False(value.TryMatch(out string? _));
        Assert.False(value.TryMatch(out float _));
        Assert.False(value.TryMatch(out bool _));
    }

    [Fact]
    public void Deserialize_EmptyString_CreatesStringPropertyValue()
    {
        // Arrange
        var json = "\"\"";

        // Act
        var value = JsonSerializer.Deserialize<PropertyValue>(json);

        // Assert
        // test-architect: Per requirements, empty string is a valid value (different from null)
        Assert.True(value.TryMatch(out string? result));
        Assert.Equal("", result);
    }

    [Fact]
    public void Deserialize_ZeroNumber_CreatesFloatPropertyValue()
    {
        // Arrange
        var json = "0";

        // Act
        var value = JsonSerializer.Deserialize<PropertyValue>(json);

        // Assert
        // test-architect: Per requirements, zero is a valid value (different from null)
        Assert.True(value.TryMatch(out float result));
        Assert.Equal(0f, result);
    }

    [Fact]
    public void Deserialize_NegativeNumber_CreatesFloatPropertyValue()
    {
        // Arrange
        var json = "-42.5";

        // Act
        var value = JsonSerializer.Deserialize<PropertyValue>(json);

        // Assert
        Assert.True(value.TryMatch(out float result));
        Assert.Equal(-42.5f, result);
    }

    [Fact]
    public void Deserialize_StringWithSpecialCharacters_PreservesValue()
    {
        // Arrange
        var json = "\"enemy-type_v2.0\"";

        // Act
        var value = JsonSerializer.Deserialize<PropertyValue>(json);

        // Assert
        Assert.True(value.TryMatch(out string? result));
        Assert.Equal("enemy-type_v2.0", result);
    }

    [Fact]
    public void Deserialize_StringWithUnicode_PreservesValue()
    {
        // Arrange
        var json = "\"日本語\"";

        // Act
        var value = JsonSerializer.Deserialize<PropertyValue>(json);

        // Assert
        Assert.True(value.TryMatch(out string? result));
        Assert.Equal("日本語", result);
    }
}
