using System;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for JSONLogic 'reduce' operator (aggregate array elements).
/// </summary>
[Collection("NonParallel")]
public sealed class JsonExpressionAggregateTests : IDisposable
{
    private readonly record struct TestInput(int[] Numbers);

    private readonly ErrorEventLogger _errorLogger;
    private readonly FakeEventListener<ErrorEvent> _errorListener;

    public JsonExpressionAggregateTests(ITestOutputHelper output)
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
    public void Reduce_Sum_ReturnsTotal()
    {
        // Arrange
        var json = """{"aggregate": [{"var": "Numbers"}, {"+": [{"var": "current"}, {"var": "accumulator"}]}, 0]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, int>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(new[] { 1, 2, 3, 4, 5 });

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(15, result);
    }

    [Fact]
    public void Reduce_Product_ReturnsTotal()
    {
        // Arrange
        var json = """{"aggregate": [{"var": "Numbers"}, {"*": [{"var": "current"}, {"var": "accumulator"}]}, 1]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, int>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(new[] { 2, 3, 4 });

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(24, result);
    }

    [Fact]
    public void Reduce_EmptyArray_ReturnsInitialValue()
    {
        // Arrange
        var json = """{"aggregate": [{"var": "Numbers"}, {"+": [{"var": "current"}, {"var": "accumulator"}]}, 100]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, int>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(Array.Empty<int>());

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(100, result);
    }

    [Fact]
    public void Reduce_Max_ReturnsMaximum()
    {
        // Arrange
        var json = """{"aggregate": [{"var": "Numbers"}, {"if": [{">": [{"var": "current"}, {"var": "accumulator"}]}, {"var": "current"}, {"var": "accumulator"}]}, 0]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, int>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(new[] { 5, 2, 8, 1, 9 });

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(9, result);
    }

    [Fact]
    public void Reduce_WithDifferentInitialValue_UsesInitial()
    {
        // Arrange
        var json = """{"aggregate": [{"var": "Numbers"}, {"+": [{"var": "current"}, {"var": "accumulator"}]}, 50]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, int>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(new[] { 1, 2, 3 });

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(56, result);
    }
    [Fact]
    public void Aggregate_WrongOutputType_Fails()
    {
        // Arrange - aggregate returns int, but requesting string[]
        var json = """{ "aggregate": [{"var": "Numbers"}, {"+": [{"var": "current"}, {"var": "accumulator"}]}, 0]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, string[]>(doc, out _));
    }}
