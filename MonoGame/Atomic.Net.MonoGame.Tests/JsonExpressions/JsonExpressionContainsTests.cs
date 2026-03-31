using System;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for JSONLogic 'in' operator for arrays (membership test).
/// </summary>
[Collection("NonParallel")]
public sealed class JsonExpressionContainsTests : IDisposable
{
    private readonly record struct TestInput(int Unused);

    private readonly ErrorEventLogger _errorLogger;
    private readonly FakeEventListener<ErrorEvent> _errorListener;

    public JsonExpressionContainsTests(ITestOutputHelper output)
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
    public void InArray_ValuePresent_ReturnsTrue()
    {
        // Arrange
        var json = """{"contains": ["Ringo", ["John", "Paul", "George", "Ringo"]]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpression.TryCompile<TestInput, bool>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void InArray_ValueNotPresent_ReturnsFalse()
    {
        // Arrange
        var json = """{"contains": ["Pete", ["John", "Paul", "George", "Ringo"]]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpression.TryCompile<TestInput, bool>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void InArray_NumberPresent_ReturnsTrue()
    {
        // Arrange
        var json = """{"contains": [3, [1, 2, 3, 4, 5]]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpression.TryCompile<TestInput, bool>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void InArray_NumberNotPresent_ReturnsFalse()
    {
        // Arrange
        var json = """{"contains": [6, [1, 2, 3, 4, 5]]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpression.TryCompile<TestInput, bool>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void InArray_EmptyArray_ReturnsFalse()
    {
        // Arrange
        var json = """{"contains": [1, []]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpression.TryCompile<TestInput, bool>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.False(result);
    }
    [Fact]
    public void Contains_WrongOutputType_Fails()
    {
        // Arrange - contains returns bool, but requesting string
        var json = """{ "contains": [3, [1, 2, 3, 4, 5]]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert
        Assert.False(JsonExpression.TryCompile<TestInput, string>(doc, out _));
    }}
