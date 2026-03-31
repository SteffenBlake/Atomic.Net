using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for JSONLogic '>' operator.
/// </summary>
[Collection("NonParallel")]
public sealed class JsonExpressionSymbolGreaterThanTests(ITestOutputHelper output) : IDisposable
{
    private readonly record struct TestInput(float Value);

    private readonly ErrorEventLogger _errorLogger = new(output);
    private readonly FakeEventListener<ErrorEvent> _errorListener = new();

    public void Dispose()
    {
        _errorListener.Dispose();
        _errorLogger.Dispose();
    }

    [Fact]
    public void GreaterThan_TwoGreater_ReturnsTrue()
    {
        // Arrange
        var json = """{ ">": [2, 1]}""";
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
    public void GreaterThan_TwoLess_ReturnsFalse()
    {
        // Arrange
        var json = """{">": [1, 2]}""";
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
    public void GreaterThan_Equal_ReturnsFalse()
    {
        // Arrange
        var json = """{">": [1, 1]}""";
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
    public void GreaterThan_WithVarData_ReturnsCorrectResult()
    {
        // Arrange
        var json = """{">": [{"var": "Value"}, 10]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(42);

        // Act
        var result = func(data);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GreaterThan_NegativeNumbers_ReturnsCorrectResult()
    {
        // Arrange
        var json = """{">": [-1, -5]}""";
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
    public void GreaterThan_Floats_ReturnsCorrectResult()
    {
        // Arrange
        var json = """{">": [3.14, 2.71]}""";
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
    public void GreaterThan_WrongOutputType_Fails()
    {
        // Arrange - greater than returns bool, but requesting int[]
        var json = """{ ">": [2, 1]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, int[]>(doc, out _));
    }

    [Fact]
    public void GreaterThan_NotAnArray_Fails()
    {
        // Arrange - value of '>' must be an array
        var json = """{">": "not-an-array"}""";
        var doc = JsonDocument.Parse(json);

        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out _));
    }

    [Fact]
    public void GreaterThan_TooFewArguments_Fails()
    {
        // Arrange - '>' requires exactly 2 elements; 1 is invalid
        var json = """{">": [1]}""";
        var doc = JsonDocument.Parse(json);

        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out _));
    }

    [Fact]
    public void GreaterThan_TooManyArguments_Fails()
    {
        // Arrange - '>' requires exactly 2 elements; 3 is invalid
        var json = """{">": [1, 2, 3]}""";
        var doc = JsonDocument.Parse(json);

        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out _));
    }
}
