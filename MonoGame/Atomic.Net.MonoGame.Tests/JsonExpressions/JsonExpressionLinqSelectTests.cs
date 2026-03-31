using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for JSONLogic 'map' operator (transform array elements).
/// </summary>
[Collection("NonParallel")]
public sealed class JsonExpressionLinqSelectTests(ITestOutputHelper output) : IDisposable
{
    private readonly record struct TestInput(float[] Numbers);

    private readonly ErrorEventLogger _errorLogger = new(output);
    private readonly FakeEventListener<ErrorEvent> _errorListener = new();

    public void Dispose()
    {
        _errorListener.Dispose();
        _errorLogger.Dispose();
    }

    [Fact]
    public void Map_MultiplyByTwo_ReturnsTransformedArray()
    {
        // Arrange
        var json = """{"select": [{"var": "Numbers"}, {"*": [{"var": ""}, 2]}]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, int[]>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput([1, 2, 3, 4, 5]);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal([2, 4, 6, 8, 10], result);
    }

    [Fact]
    public void Map_AddConstant_ReturnsTransformedArray()
    {
        // Arrange
        var json = """{"select": [{"var": "Numbers"}, {"+": [{"var": ""}, 10]}]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, int[]>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput([1, 2, 3]);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal([11, 12, 13], result);
    }

    [Fact]
    public void Map_EmptyArray_ReturnsEmptyArray()
    {
        // Arrange
        var json = """{"select": [{"var": "Numbers"}, {"*": [{"var": ""}, 2]}]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, int[]>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput([]);

        // Act
        var result = func(data);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Map_SquareNumbers_ReturnsTransformedArray()
    {
        // Arrange
        var json = """{"select": [{"var": "Numbers"}, {"*": [{"var": ""}, {"var": ""}]}]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, int[]>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput([2, 3, 4]);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal([4, 9, 16], result);
    }
    [Fact]
    public void Select_WrongOutputType_Fails()
    {
        // Arrange - select returns int[], but requesting string
        var json = """{ "select": [{"var": "Numbers"}, {"*": [{"var": ""}, 2]}]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, string>(doc, out _));
    }}
