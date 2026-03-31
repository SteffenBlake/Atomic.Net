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
public sealed class JsonExpressionLinqAggregateTests(ITestOutputHelper output) : IDisposable
{
    private readonly record struct TestInput(float[] Numbers);

    private readonly ErrorEventLogger _errorLogger = new(output);

    public void Dispose()
    {
        _errorLogger.Dispose();
    }

    [Fact]
    public void Reduce_Sum_ReturnsTotal()
    {
        // Arrange
        var json = """{"aggregate": [{"var": "Numbers"}, {"+": [{"var": "current"}, {"var": "accumulator"}]}, 0]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput([1, 2, 3, 4, 5]);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(15f, result, 0.001f);
    }

    [Fact]
    public void Reduce_Product_ReturnsTotal()
    {
        // Arrange
        var json = """{"aggregate": [{"var": "Numbers"}, {"*": [{"var": "current"}, {"var": "accumulator"}]}, 1]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput([2, 3, 4]);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(24f, result, 0.001f);
    }

    [Fact]
    public void Reduce_EmptyArray_ReturnsInitialValue()
    {
        // Arrange
        var json = """{"aggregate": [{"var": "Numbers"}, {"+": [{"var": "current"}, {"var": "accumulator"}]}, 100]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput([]);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(100f, result, 0.001f);
    }

    [Fact]
    public void Reduce_Max_ReturnsMaximum()
    {
        // Arrange
        var json = """{"aggregate": [{"var": "Numbers"}, {"if": [{">": [{"var": "current"}, {"var": "accumulator"}]}, {"var": "current"}, {"var": "accumulator"}]}, 0]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput([5, 2, 8, 1, 9]);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(9f, result, 0.001f);
    }

    [Fact]
    public void Reduce_WithDifferentInitialValue_UsesInitial()
    {
        // Arrange
        var json = """{"aggregate": [{"var": "Numbers"}, {"+": [{"var": "current"}, {"var": "accumulator"}]}, 50]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput([1, 2, 3]);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(56f, result, 0.001f);
    }

    [Fact]
    public void Aggregate_WrongOutputType_Fails()
    {
        // Arrange - aggregate returns float, but requesting string[]
        var json = """{ "aggregate": [{"var": "Numbers"}, {"+": [{"var": "current"}, {"var": "accumulator"}]}, 0]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, string[]>(doc, out _));
    }
}
