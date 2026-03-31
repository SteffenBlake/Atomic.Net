using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for JSONLogic '%' operator (modulo).
/// </summary>
[Collection("NonParallel")]
public sealed class JsonExpressionSymbolModuloTests(ITestOutputHelper output) : IDisposable
{
    private readonly record struct TestInput(float Value);

    private readonly ErrorEventLogger _errorLogger = new(output);

    public void Dispose()
    {
        _errorLogger.Dispose();
    }

    [Fact]
    public void Modulo_Odd_ReturnsRemainder()
    {
        // Arrange
        var json = """{"%": [101, 2]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(1f, result, 0.001f);
    }

    [Fact]
    public void Modulo_Even_ReturnsZero()
    {
        // Arrange
        var json = """{"%": [100, 2]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(0f, result, 0.001f);
    }

    [Fact]
    public void Modulo_WithVarData_ReturnsRemainder()
    {
        // Arrange
        var json = """{"%": [{"var": "Value"}, 10]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(42);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(2f, result, 0.001f);
    }

    [Fact]
    public void Modulo_LargerDivisor_ReturnsOriginal()
    {
        // Arrange
        var json = """{"%": [5, 10]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(5f, result, 0.001f);
    }

    [Fact]
    public void Modulo_NegativeDividend_ReturnsRemainder()
    {
        // Arrange
        var json = """{"%": [-7, 3]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(-1f, result, 0.001f);
    }

    [Fact]
    public void Modulo_ByZero_ReturnsNaN()
    {
        // Arrange - float modulo by zero yields NaN per IEEE 754, not an exception
        var json = """{"%": [42, 0]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.True(float.IsNaN(result), $"Expected NaN but got {result}");
    }

    [Fact]
    public void Modulo_WrongOutputType_Fails()
    {
        // Arrange - modulo returns float, but requesting string[]
        var json = """{"%": [10, 3]}""";
        var doc = JsonDocument.Parse(json);

        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, string[]>(doc, out _));
    }

    [Fact]
    public void Modulo_BoolOutputType_Fails()
    {
        var json = """{"%": [10, 3]}""";
        var doc = JsonDocument.Parse(json);
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out _));
    }

    [Fact]
    public void Modulo_StringOutputType_Fails()
    {
        var json = """{"%": [10, 3]}""";
        var doc = JsonDocument.Parse(json);
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, string>(doc, out _));
    }

    [Fact]
    public void Modulo_NotAnArray_Fails()
    {
        var json = """{"%": "not-an-array"}""";
        var doc = JsonDocument.Parse(json);
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out _));
    }

    [Fact]
    public void Modulo_TooFewArguments_Fails()
    {
        var json = """{"%": [10]}""";
        var doc = JsonDocument.Parse(json);
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out _));
    }

    [Fact]
    public void Modulo_TooManyArguments_Fails()
    {
        var json = """{"%": [10, 3, 2]}""";
        var doc = JsonDocument.Parse(json);
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out _));
    }
}
