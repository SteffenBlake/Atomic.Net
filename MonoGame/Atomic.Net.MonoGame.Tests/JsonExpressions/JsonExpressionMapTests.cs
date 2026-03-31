using System;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for JSONLogic 'map' operator (transform array elements).
/// </summary>
public sealed class JsonExpressionMapTests : IDisposable
{
    private readonly record struct TestInput(int[] Numbers);

    private readonly ErrorEventLogger _errorLogger;
    private readonly FakeEventListener<ErrorEvent> _errorListener;

    public JsonExpressionMapTests(ITestOutputHelper output)
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
    public void Map_MultiplyByTwo_ReturnsTransformedArray()
    {
        // Arrange
        var json = """{"map": [{"var": "Numbers"}, {"*": [{"var": ""}, 2]}]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, int[]>(doc);
        var func = expr.Compile();
        var data = new TestInput(new[] { 1, 2, 3, 4, 5 });

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(new[] { 2, 4, 6, 8, 10 }, result);
    }

    [Fact]
    public void Map_AddConstant_ReturnsTransformedArray()
    {
        // Arrange
        var json = """{"map": [{"var": "Numbers"}, {"+": [{"var": ""}, 10]}]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, int[]>(doc);
        var func = expr.Compile();
        var data = new TestInput(new[] { 1, 2, 3 });

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(new[] { 11, 12, 13 }, result);
    }

    [Fact]
    public void Map_EmptyArray_ReturnsEmptyArray()
    {
        // Arrange
        var json = """{"map": [{"var": "Numbers"}, {"*": [{"var": ""}, 2]}]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, int[]>(doc);
        var func = expr.Compile();
        var data = new TestInput(Array.Empty<int>());

        // Act
        var result = func(data);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Map_SquareNumbers_ReturnsTransformedArray()
    {
        // Arrange
        var json = """{"map": [{"var": "Numbers"}, {"*": [{"var": ""}, {"var": ""}]}]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, int[]>(doc);
        var func = expr.Compile();
        var data = new TestInput(new[] { 2, 3, 4 });

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(new[] { 4, 9, 16 }, result);
    }
}
