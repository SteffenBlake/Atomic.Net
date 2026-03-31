using System;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for JSONLogic 'missing_some' operator (requires minimum N keys present).
/// </summary>
public sealed class JsonExpressionMissingSomeTests : IDisposable
{
    private readonly record struct TestInput(int A, string B);

    private readonly ErrorEventLogger _errorLogger;
    private readonly FakeEventListener<ErrorEvent> _errorListener;

    public JsonExpressionMissingSomeTests(ITestOutputHelper output)
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
    public void MissingSome_MinimumMet_ReturnsEmptyArray()
    {
        // Arrange
        var json = """{"missing_some": [1, ["A", "B", "C"]]}""";
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
    public void MissingSome_MinimumNotMet_ReturnsMissingKeys()
    {
        // Arrange
        var json = """{"missing_some": [2, ["A", "B", "C"]]}""";
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
    public void MissingSome_AllPresent_ReturnsEmptyArray()
    {
        // Arrange
        var json = """{"missing_some": [2, ["A", "B"]]}""";
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
    public void MissingSome_MinimumZero_ReturnsEmptyArray()
    {
        // Arrange
        var json = """{"missing_some": [0, ["X", "Y", "Z"]]}""";
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
    public void MissingSome_RequireAll_ReturnsMissingKeys()
    {
        // Arrange
        var json = """{"missing_some": [3, ["A", "B", "C"]]}""";
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
}
