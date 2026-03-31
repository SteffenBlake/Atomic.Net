using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for JSONLogic 'none' operator (no elements pass test).
/// </summary>
[Collection("NonParallel")]
public sealed class JsonExpressionNoneTests(ITestOutputHelper output) : IDisposable
{
    private readonly record struct TestInput(int Unused);

    private readonly ErrorEventLogger _errorLogger = new(output);
    private readonly FakeEventListener<ErrorEvent> _errorListener = new();

    public void Dispose()
    {
        _errorListener.Dispose();
        _errorLogger.Dispose();
    }

    [Fact]
    public void None_AllNegative_ReturnsTrue()
    {
        // Arrange
        var json = """{"none": [[-3, -2, -1], {">": [{"var": ""}, 0]}]}""";
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
    public void None_OnePositive_ReturnsFalse()
    {
        // Arrange
        var json = """{"none": [[-1, 0, 1], {">": [{"var": ""}, 0]}]}""";
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
    public void None_EmptyArray_ReturnsTrue()
    {
        // Arrange
        var json = """{"none": [[], {">": [{"var": ""}, 0]}]}""";
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
    public void None_AllOdd_ReturnsTrue()
    {
        // Arrange
        var json = """{"none": [[1, 3, 5], {"==": [{"%": [{"var": ""}, 2]}, 0]}]}""";
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
    public void None_OneEven_ReturnsFalse()
    {
        // Arrange
        var json = """{"none": [[1, 2, 5], {"==": [{"%": [{"var": ""}, 2]}, 0]}]}""";
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
    public void None_WrongOutputType_Fails()
    {
        // Arrange - none returns bool, but requesting string
        var json = """{ "none": [[1, 3, 5], {"==": [{"%": [{"var": ""}, 2]}, 0]}]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, string>(doc, out _));
    }}
