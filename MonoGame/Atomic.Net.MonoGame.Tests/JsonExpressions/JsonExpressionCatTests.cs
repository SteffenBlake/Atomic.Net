using System;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for JSONLogic 'cat' operator (string concatenation).
/// </summary>
public sealed class JsonExpressionCatTests : IDisposable
{
    private readonly record struct TestInput(string Name, int Value);

    private readonly ErrorEventLogger _errorLogger;
    private readonly FakeEventListener<ErrorEvent> _errorListener;

    public JsonExpressionCatTests(ITestOutputHelper output)
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
    public void Cat_TwoStrings_ReturnsConcatenated()
    {
        // Arrange
        var json = """{"cat": ["I love", " pie"]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, string>(doc);
        var func = expr.Compile();
        var data = new TestInput("", 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal("I love pie", result);
    }

    [Fact]
    public void Cat_ThreeStrings_ReturnsConcatenated()
    {
        // Arrange
        var json = """{"cat": ["Hello", " ", "World"]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, string>(doc);
        var func = expr.Compile();
        var data = new TestInput("", 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void Cat_WithVarData_ReturnsConcatenated()
    {
        // Arrange
        var json = """{"cat": ["I love ", {"var": "Name"}, " pie"]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, string>(doc);
        var func = expr.Compile();
        var data = new TestInput("apple", 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal("I love apple pie", result);
    }

    [Fact]
    public void Cat_EmptyStrings_ReturnsEmpty()
    {
        // Arrange
        var json = """{"cat": ["", "", ""]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, string>(doc);
        var func = expr.Compile();
        var data = new TestInput("", 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void Cat_SingleString_ReturnsSame()
    {
        // Arrange
        var json = """{"cat": ["Hello"]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, string>(doc);
        var func = expr.Compile();
        var data = new TestInput("", 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal("Hello", result);
    }

    [Fact]
    public void Cat_WithNumber_ConvertsToString()
    {
        // Arrange
        var json = """{"cat": ["Value: ", {"var": "Value"}]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, string>(doc);
        var func = expr.Compile();
        var data = new TestInput("", 42);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal("Value: 42", result);
    }

    [Fact]
    public void Cat_ManyStrings_ReturnsConcatenated()
    {
        // Arrange
        var json = """{"cat": ["a", "b", "c", "d", "e"]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, string>(doc);
        var func = expr.Compile();
        var data = new TestInput("", 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal("abcde", result);
    }
}
