using System;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for JSONLogic 'in' operator for strings (substring test).
/// </summary>
public sealed class JsonExpressionInStringTests : IDisposable
{
    private readonly record struct TestInput(string Text);

    private readonly ErrorEventLogger _errorLogger;
    private readonly FakeEventListener<ErrorEvent> _errorListener;

    public JsonExpressionInStringTests(ITestOutputHelper output)
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
    public void InString_SubstringPresent_ReturnsTrue()
    {
        // Arrange
        var json = """{"in": ["Spring", "Springfield"]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, bool>(doc);
        var func = expr.Compile();
        var data = new TestInput("");

        // Act
        var result = func(data);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void InString_SubstringNotPresent_ReturnsFalse()
    {
        // Arrange
        var json = """{"in": ["Summer", "Springfield"]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, bool>(doc);
        var func = expr.Compile();
        var data = new TestInput("");

        // Act
        var result = func(data);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void InString_EmptySubstring_ReturnsTrue()
    {
        // Arrange
        var json = """{"in": ["", "test"]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, bool>(doc);
        var func = expr.Compile();
        var data = new TestInput("");

        // Act
        var result = func(data);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void InString_CaseSensitive_ReturnsFalse()
    {
        // Arrange
        var json = """{"in": ["SPRING", "Springfield"]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, bool>(doc);
        var func = expr.Compile();
        var data = new TestInput("");

        // Act
        var result = func(data);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void InString_WithVarData_ReturnsCorrectResult()
    {
        // Arrange
        var json = """{"in": ["test", {"var": "Text"}]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, bool>(doc);
        var func = expr.Compile();
        var data = new TestInput("This is a test string");

        // Act
        var result = func(data);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void InString_AtBeginning_ReturnsTrue()
    {
        // Arrange
        var json = """{"in": ["Hello", "Hello World"]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, bool>(doc);
        var func = expr.Compile();
        var data = new TestInput("");

        // Act
        var result = func(data);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void InString_AtEnd_ReturnsTrue()
    {
        // Arrange
        var json = """{"in": ["World", "Hello World"]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, bool>(doc);
        var func = expr.Compile();
        var data = new TestInput("");

        // Act
        var result = func(data);

        // Assert
        Assert.True(result);
    }
}
