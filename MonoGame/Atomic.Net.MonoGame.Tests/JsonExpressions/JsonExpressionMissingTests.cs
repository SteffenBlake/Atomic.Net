using System;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for JSONLogic 'missing' operator (returns array of missing keys).
/// </summary>
public sealed class JsonExpressionMissingTests : IDisposable
{
    private readonly record struct TestInput(int A, string B);

    private readonly ErrorEventLogger _errorLogger;
    private readonly FakeEventListener<ErrorEvent> _errorListener;

    public JsonExpressionMissingTests(ITestOutputHelper output)
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
    public void Missing_AllPresent_ReturnsEmptyArray()
    {
        // Arrange
        var json = """{"missing": ["A", "B"]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, string[]>(doc);
        var func = expr.Compile();
        var data = new TestInput(42, "value");

        // Act
        var result = func(data);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Missing_SomeMissing_ReturnsMissingKeys()
    {
        // Arrange
        var json = """{"missing": ["A", "B", "C"]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, string[]>(doc);
        var func = expr.Compile();
        var data = new TestInput(42, "value");

        // Act
        var result = func(data);

        // Assert
        Assert.Single(result);
        Assert.Equal("C", result[0]);
    }

    [Fact]
    public void Missing_AllMissing_ReturnsAllKeys()
    {
        // Arrange
        var json = """{"missing": ["X", "Y", "Z"]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, string[]>(doc);
        var func = expr.Compile();
        var data = new TestInput(42, "value");

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal(new[] { "X", "Y", "Z" }, result);
    }

    [Fact]
    public void Missing_EmptyArray_ReturnsEmptyArray()
    {
        // Arrange
        var json = """{"missing": []}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, string[]>(doc);
        var func = expr.Compile();
        var data = new TestInput(42, "value");

        // Act
        var result = func(data);

        // Assert
        Assert.Empty(result);
    }
}
