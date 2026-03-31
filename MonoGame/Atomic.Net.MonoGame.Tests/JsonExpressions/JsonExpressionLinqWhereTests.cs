using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for JSONLogic 'filter' operator (filter array elements).
/// </summary>
[Collection("NonParallel")]
public sealed class JsonExpressionLinqWhereTests(ITestOutputHelper output) : IDisposable
{
    private readonly record struct TestInput(int[] Numbers);

    private readonly ErrorEventLogger _errorLogger = new(output);
    private readonly FakeEventListener<ErrorEvent> _errorListener = new();

    public void Dispose()
    {
        _errorListener.Dispose();
        _errorLogger.Dispose();
    }

    [Fact]
    public void Filter_OddNumbers_ReturnsOddOnly()
    {
        // Arrange - where predicate must return bool (modulo returns int, so wrap in equality check)
        var json = """{"where": [{"var": "Numbers"}, {"!=": [{"%": [{"var": ""}, 2]}, 0]}]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, int[]>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput([1, 2, 3, 4, 5]);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal([1, 3, 5], result);
    }

    [Fact]
    public void Filter_GreaterThanZero_ReturnsPositives()
    {
        // Arrange
        var json = """{"where": [{"var": "Numbers"}, {">": [{"var": ""}, 0]}]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, int[]>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput([-2, -1, 0, 1, 2]);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal([1, 2], result);
    }

    [Fact]
    public void Filter_NoMatches_ReturnsEmpty()
    {
        // Arrange
        var json = """{"where": [{"var": "Numbers"}, {">": [{"var": ""}, 100]}]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, int[]>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput([1, 2, 3, 4, 5]);

        // Act
        var result = func(data);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Filter_AllMatch_ReturnsAll()
    {
        // Arrange
        var json = """{"where": [{"var": "Numbers"}, {">": [{"var": ""}, 0]}]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, int[]>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput([1, 2, 3, 4, 5]);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal([1, 2, 3, 4, 5], result);
    }

    [Fact]
    public void Filter_EmptyArray_ReturnsEmpty()
    {
        // Arrange
        var json = """{"where": [{"var": "Numbers"}, {">": [{"var": ""}, 0]}]}""";
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
    public void Where_WrongOutputType_Fails()
    {
        // Arrange - where returns int[], but requesting bool
        var json = """{ "where": [{"var": "Numbers"}, {">": [{"var": ""}, 0]}]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out _));
    }}
