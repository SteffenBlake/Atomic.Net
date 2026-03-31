using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for JSONLogic '-' operator (subtraction/negation).
/// </summary>
[Collection("NonParallel")]
public sealed class JsonExpressionSymbolSubtractTests(ITestOutputHelper output) : IDisposable
{
    private readonly record struct TestInput(float A, float B);

    private readonly ErrorEventLogger _errorLogger = new(output);
    private readonly FakeEventListener<ErrorEvent> _errorListener = new();

    public void Dispose()
    {
        _errorListener.Dispose();
        _errorLogger.Dispose();
    }

    [Fact]
    public void Subtract_TwoIntegers_ReturnsDifference()
    {
        // Arrange
        var json = """{"-": [4, 2]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, int>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(2, result);
    }

    [Fact]
    public void Subtract_WithVarData_ReturnsDifference()
    {
        // Arrange
        var json = """{"-": [{"var": "A"}, {"var": "B"}]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(50, 8);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(42f, result);
    }

    [Fact]
    public void Subtract_UnaryPositive_ReturnsNegative()
    {
        // Arrange
        var json = """{"-": 2}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(-2f, result);
    }

    [Fact]
    public void Subtract_UnaryNegative_ReturnsPositive()
    {
        // Arrange
        var json = """{"-": -2}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(2f, result);
    }

    [Fact]
    public void Subtract_ResultNegative_ReturnsNegative()
    {
        // Arrange
        var json = """{"-": [2, 5]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, int>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(-3, result);
    }

    [Fact]
    public void Subtract_Floats_ReturnsDifference()
    {
        // Arrange
        var json = """{"-": [5.5, 3.2]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(2.3, result, 0.001);
    }
    [Fact]
    public void Subtract_WrongOutputType_Fails()
    {
        // Arrange - subtract returns int, but requesting string
        var json = """{"-": [10, 5]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, string>(doc, out _));
    }

    [Fact]
    public void Subtract_BoolOutputType_Fails()
    {
        var json = """{"-": [10, 5]}""";
        var doc = JsonDocument.Parse(json);
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out _));
    }

    [Fact]
    public void Subtract_ArrayOutputType_Fails()
    {
        var json = """{"-": [10, 5]}""";
        var doc = JsonDocument.Parse(json);
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, float[]>(doc, out _));
    }

    [Fact]
    public void Subtract_NotAnArray_Fails()
    {
        // String value is neither a number (unary path) nor an array
        var json = """{"-": "not-an-array"}""";
        var doc = JsonDocument.Parse(json);
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out _));
    }

    [Fact]
    public void Subtract_TooFewArguments_Fails()
    {
        var json = """{"-": [10]}""";
        var doc = JsonDocument.Parse(json);
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out _));
    }

    [Fact]
    public void Subtract_TooManyArguments_Fails()
    {
        var json = """{"-": [10, 5, 2]}""";
        var doc = JsonDocument.Parse(json);
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out _));
    }
}
