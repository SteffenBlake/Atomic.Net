using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for JSONLogic 'some' operator (at least one element passes test).
/// </summary>
[Collection("NonParallel")]
public sealed class JsonExpressionLinqAnyTests(ITestOutputHelper output) : IDisposable
{
    private readonly record struct TestInput(float Unused);

    private readonly ErrorEventLogger _errorLogger = new(output);
    private readonly FakeEventListener<ErrorEvent> _errorListener = new();

    public void Dispose()
    {
        _errorListener.Dispose();
        _errorLogger.Dispose();
    }

    [Fact]
    public void Some_OnePositive_ReturnsTrue()
    {
        // Arrange
        var json = """{"any": [[-1, 0, 1], {">": [{"var": ""}, 0]}]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Some_AllNegative_ReturnsFalse()
    {
        // Arrange
        var json = """{"any": [[-3, -2, -1], {">": [{"var": ""}, 0]}]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Some_EmptyArray_ReturnsFalse()
    {
        // Arrange
        var json = """{"any": [[], {">": [{"var": ""}, 0]}]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Some_AllMatch_ReturnsTrue()
    {
        // Arrange
        var json = """{"any": [[1, 2, 3], {">": [{"var": ""}, 0]}]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Some_OneEven_ReturnsTrue()
    {
        // Arrange
        var json = """{"any": [[1, 2, 5], {"==": [{"%": [{"var": ""}, 2]}, 0]}]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.True(result);
    }
    [Fact]
    public void Any_WrongOutputType_Fails()
    {
        // Arrange - any returns bool, but requesting int[]
        var json = """{ "any": [[1, 2, 3], {">": [{"var": ""}, 0]}]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, int[]>(doc, out _));
    }}
