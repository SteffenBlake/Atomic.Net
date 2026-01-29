using Xunit;
using Atomic.Net.MonoGame.Properties;

namespace Atomic.Net.MonoGame.Tests.BED.Units;

/// <summary>
/// Unit tests for PropertyValue union type.
/// Tests isolated PropertyValue behavior (type conversions, equality, pattern matching).
/// </summary>
[Collection("NonParallel")]
[Trait("Category", "Unit")]
public sealed class PropertyValueUnitTests
{    [Fact]
    public void ImplicitConversion_FromString_CreatesStringVariant()
    {
        // Arrange
        string testValue = "test-string";

        // Act
        PropertyValue value = testValue;

        // Assert
        Assert.True(value.TryMatch(out string? result));
        Assert.Equal("test-string", result);
    }

    [Fact]
    public void ImplicitConversion_FromFloat_CreatesFloatVariant()
    {
        // Arrange
        float testValue = 123.45f;

        // Act
        PropertyValue value = testValue;

        // Assert
        Assert.True(value.TryMatch(out float result));
        Assert.Equal(123.45f, result);
    }

    [Fact]
    public void ImplicitConversion_FromBool_CreatesBoolVariant()
    {
        // Arrange
        bool testValue = true;

        // Act
        PropertyValue value = testValue;

        // Assert
        Assert.True(value.TryMatch(out bool result));
        Assert.True(result);
    }

    [Fact]
    public void Default_ReturnsEmptyVariant()
    {
        // Arrange & Act
        PropertyValue value = default;

        // Assert
        Assert.False(value.TryMatch(out string? _));
        Assert.False(value.TryMatch(out float _));
        Assert.False(value.TryMatch(out bool _));
    }

    [Fact]
    public void TryMatch_WrongType_ReturnsFalse()
    {
        // Arrange
        PropertyValue stringValue = "test";

        // Act & Assert
        Assert.False(stringValue.TryMatch(out float _));
        Assert.False(stringValue.TryMatch(out bool _));
    }

    [Fact]
    public void Equality_SameStringValues_AreEqual()
    {
        // Arrange
        PropertyValue value1 = "test";
        PropertyValue value2 = "test";

        // Act & Assert
        Assert.Equal(value1, value2);
    }

    [Fact]
    public void Equality_DifferentStringValues_AreNotEqual()
    {
        // Arrange
        PropertyValue value1 = "test1";
        PropertyValue value2 = "test2";

        // Act & Assert
        Assert.NotEqual(value1, value2);
    }

    [Fact]
    public void Equality_SameFloatValues_AreEqual()
    {
        // Arrange
        PropertyValue value1 = 123.45f;
        PropertyValue value2 = 123.45f;

        // Act & Assert
        Assert.Equal(value1, value2);
    }

    [Fact]
    public void Equality_DifferentFloatValues_AreNotEqual()
    {
        // Arrange
        PropertyValue value1 = 123.45f;
        PropertyValue value2 = 678.90f;

        // Act & Assert
        Assert.NotEqual(value1, value2);
    }

    [Fact]
    public void Equality_SameBoolValues_AreEqual()
    {
        // Arrange
        PropertyValue value1 = true;
        PropertyValue value2 = true;

        // Act & Assert
        Assert.Equal(value1, value2);
    }

    [Fact]
    public void Equality_DifferentBoolValues_AreNotEqual()
    {
        // Arrange
        PropertyValue value1 = true;
        PropertyValue value2 = false;

        // Act & Assert
        Assert.NotEqual(value1, value2);
    }

    [Fact]
    public void Equality_DifferentTypes_AreNotEqual()
    {
        // Arrange
        PropertyValue stringValue = "123";
        PropertyValue floatValue = 123f;
        PropertyValue boolValue = true;

        // Act & Assert
        Assert.NotEqual(stringValue, floatValue);
        Assert.NotEqual(stringValue, boolValue);
        Assert.NotEqual(floatValue, boolValue);
    }

    [Fact]
    public void EmptyString_IsValidStringValue()
    {
        // Arrange
        PropertyValue value = "";

        // Act
        Assert.True(value.TryMatch(out string? result));

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void ZeroFloat_IsValidFloatValue()
    {
        // Arrange
        PropertyValue value = 0f;

        // Act
        Assert.True(value.TryMatch(out float result));

        // Assert
        Assert.Equal(0f, result);
    }

    [Fact]
    public void False_IsValidBoolValue()
    {
        // Arrange
        PropertyValue value = false;

        // Act
        Assert.True(value.TryMatch(out bool result));

        // Assert
        Assert.False(result);
    }


    [Fact]
    public void GetHashCode_IsConsistent()
    {
        // Arrange
        PropertyValue stringValueA = "123";
        PropertyValue floatValueA = 123f;
        PropertyValue boolValueA = true;

        PropertyValue stringValueB = "123";
        PropertyValue floatValueB = 123f;
        PropertyValue boolValueB = true;

        HashSet<PropertyValue> hashSet = [stringValueA, floatValueA, boolValueA];

        // Act & Assert
        Assert.Equal(stringValueA.GetHashCode(), stringValueB.GetHashCode());
        Assert.Equal(floatValueA.GetHashCode(), floatValueB.GetHashCode());
        Assert.Equal(boolValueA.GetHashCode(), boolValueB.GetHashCode());

        Assert.Contains(stringValueB, hashSet);
        Assert.Contains(floatValueB, hashSet);
        Assert.Contains(boolValueB, hashSet);
    }
}
