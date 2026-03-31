using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for JSONLogic '/' operator (division).
/// </summary>
[Collection("NonParallel")]
public sealed class JsonExpressionSymbolDivideTests(ITestOutputHelper output) : IDisposable
{
    private readonly record struct TestInput(float A, float B);

    private readonly ErrorEventLogger _errorLogger = new(output);

    public void Dispose()
    {
        _errorLogger.Dispose();
    }

    [Fact]
    public void Divide_TwoFloats_ReturnsQuotient()
    {
        // Arrange
        var json = """{"/": [4, 2]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(2f, result, 0.001f);
    }

    [Fact]
    public void Divide_WithVarData_ReturnsQuotient()
    {
        // Arrange
        var json = """{"/": [{"var": "A"}, {"var": "B"}]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(84, 2);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(42f, result, 0.001f);
    }

    [Fact]
    public void Divide_Floats_ReturnsQuotient()
    {
        // Arrange
        var json = """{"/": [7.5, 2.5]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(3.0, result, 0.001);
    }

    [Fact]
    public void Divide_ResultFloat_ReturnsFloat()
    {
        // Arrange
        var json = """{"/": [5, 2]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(2.5, result, 0.001);
    }

    [Fact]
    public void Divide_NegativeNumbers_ReturnsQuotient()
    {
        // Arrange
        var json = """{"/": [-10, 2]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(-5f, result, 0.001f);
    }

    [Fact]
    public void Divide_ByZero_ReturnsInfinity()
    {
        // Arrange - float division by zero yields Infinity per IEEE 754, not an exception
        var json = """{"/": [42, 0]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.True(float.IsInfinity(result), $"Expected Infinity but got {result}");
    }

    [Fact]
    public void Divide_WrongOutputType_Fails()
    {
        // Arrange - divide returns int, but requesting string[]
        var json = """{"/": [10, 2]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, string[]>(doc, out _));
    }

    [Fact]
    public void Divide_BoolOutputType_Fails()
    {
        // Arrange - '/' requires numeric TOut; bool is not numeric
        var json = """{"/": [10, 2]}""";
        var doc = JsonDocument.Parse(json);

        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out _));
    }

    [Fact]
    public void Divide_StringOutputType_Fails()
    {
        // Arrange - '/' requires numeric TOut; string is not numeric
        var json = """{"/": [10, 2]}""";
        var doc = JsonDocument.Parse(json);

        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, string>(doc, out _));
    }

    [Fact]
    public void Divide_NotAnArray_Fails()
    {
        // Arrange - value of '/' must be an array
        var json = """{"/": "not-an-array"}""";
        var doc = JsonDocument.Parse(json);

        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out _));
    }

    [Fact]
    public void Divide_TooFewArguments_Fails()
    {
        // Arrange - '/' requires exactly 2 elements; 1 is invalid
        var json = """{"/": [10]}""";
        var doc = JsonDocument.Parse(json);

        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out _));
    }

    [Fact]
    public void Divide_TooManyArguments_Fails()
    {
        // Arrange - '/' requires exactly 2 elements; 3 is invalid
        var json = """{"/": [10, 2, 5]}""";
        var doc = JsonDocument.Parse(json);

        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out _));
    }
}
