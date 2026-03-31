using System;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for JSONLogic '!==' operator (strict inequality).
/// </summary>
public sealed class JsonExpressionStrictNotEqualsTests : IDisposable
{
    private readonly record struct TestInput(int Value, string Name);

    private readonly ErrorEventLogger _errorLogger;
    private readonly FakeEventListener<ErrorEvent> _errorListener;

    public JsonExpressionStrictNotEqualsTests(ITestOutputHelper output)
    {
        _errorLogger = new ErrorEventLogger(output);
        _errorListener = new FakeEventListener<ErrorEvent>();
    }

    public void Dispose()
    {
        _errorListener.Dispose();
        _errorLogger.Dispose();
    }

    [Fact]
    public void StrictNotEquals_DifferentIntegers_ReturnsTrue()
    {
        // Arrange
        var json = """{"!==": [1, 2]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, bool>(doc);
        var func = expr.Compile();
        var data = new TestInput(0, "");

        // Act
        var result = func(data);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void StrictNotEquals_SameIntegers_ReturnsFalse()
    {
        // Arrange
        var json = """{"!==": [1, 1]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, bool>(doc);
        var func = expr.Compile();
        var data = new TestInput(0, "");

        // Act
        var result = func(data);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void StrictNotEquals_IntegerAndString_NoCoercion_ReturnsTrue()
    {
        // Arrange
        var json = """{"!==": [1, "1"]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, bool>(doc);
        var func = expr.Compile();
        var data = new TestInput(0, "");

        // Act
        var result = func(data);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void StrictNotEquals_SameStrings_ReturnsFalse()
    {
        // Arrange
        var json = """{"!==": ["test", "test"]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, bool>(doc);
        var func = expr.Compile();
        var data = new TestInput(0, "");

        // Act
        var result = func(data);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void StrictNotEquals_WithVarData_ReturnsTrue()
    {
        // Arrange
        var json = """{"!==": [{"var": "Value"}, 100]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, bool>(doc);
        var func = expr.Compile();
        var data = new TestInput(42, "test");

        // Act
        var result = func(data);

        // Assert
        Assert.True(result);
    }
}
