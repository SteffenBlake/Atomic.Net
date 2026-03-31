using System;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for JSONLogic 'substr' operator (substring extraction).
/// </summary>
public sealed class JsonExpressionSubstrTests : IDisposable
{
    private readonly record struct TestInput(string Text);

    private readonly ErrorEventLogger _errorLogger;
    private readonly FakeEventListener<ErrorEvent> _errorListener;

    public JsonExpressionSubstrTests(ITestOutputHelper output)
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
    public void Substr_PositiveStart_ReturnsFromIndex()
    {
        // Arrange
        var json = """{"substr": ["jsonlogic", 4]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, string>(doc);
        var func = expr.Compile();
        var data = new TestInput("");

        // Act
        var result = func(data);

        // Assert
        Assert.Equal("logic", result);
    }

    [Fact]
    public void Substr_NegativeStart_ReturnsFromEnd()
    {
        // Arrange
        var json = """{"substr": ["jsonlogic", -5]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, string>(doc);
        var func = expr.Compile();
        var data = new TestInput("");

        // Act
        var result = func(data);

        // Assert
        Assert.Equal("logic", result);
    }

    [Fact]
    public void Substr_PositiveStartAndLength_ReturnsSubstring()
    {
        // Arrange
        var json = """{"substr": ["jsonlogic", 1, 3]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, string>(doc);
        var func = expr.Compile();
        var data = new TestInput("");

        // Act
        var result = func(data);

        // Assert
        Assert.Equal("son", result);
    }

    [Fact]
    public void Substr_NegativeLength_StopsBeforeEnd()
    {
        // Arrange
        var json = """{"substr": ["jsonlogic", 4, -2]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, string>(doc);
        var func = expr.Compile();
        var data = new TestInput("");

        // Act
        var result = func(data);

        // Assert
        Assert.Equal("log", result);
    }

    [Fact]
    public void Substr_StartAtZero_ReturnsFromBeginning()
    {
        // Arrange
        var json = """{"substr": ["jsonlogic", 0, 4]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, string>(doc);
        var func = expr.Compile();
        var data = new TestInput("");

        // Act
        var result = func(data);

        // Assert
        Assert.Equal("json", result);
    }

    [Fact]
    public void Substr_WithVarData_ReturnsSubstring()
    {
        // Arrange
        var json = """{"substr": [{"var": "Text"}, 0, 5]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, string>(doc);
        var func = expr.Compile();
        var data = new TestInput("Hello World");

        // Act
        var result = func(data);

        // Assert
        Assert.Equal("Hello", result);
    }

    [Fact]
    public void Substr_LengthExceedsString_ReturnsRemainder()
    {
        // Arrange
        var json = """{"substr": ["short", 2, 100]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, string>(doc);
        var func = expr.Compile();
        var data = new TestInput("");

        // Act
        var result = func(data);

        // Assert
        Assert.Equal("ort", result);
    }

    [Fact]
    public void Substr_StartExceedsLength_ReturnsEmpty()
    {
        // Arrange
        var json = """{"substr": ["short", 100]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, string>(doc);
        var func = expr.Compile();
        var data = new TestInput("");

        // Act
        var result = func(data);

        // Assert
        Assert.Equal("", result);
    }
}
