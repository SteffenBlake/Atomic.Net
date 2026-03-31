using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for JSON array literal expressions.
/// </summary>
[Collection("NonParallel")]
public sealed class JsonExpressionArrayLiteralTests(ITestOutputHelper output) : IDisposable
{
    private readonly record struct TestInput(float Value);

    private readonly ErrorEventLogger _errorLogger = new(output);

    public void Dispose()
    {
        _errorLogger.Dispose();
    }

    [Fact]
    public void ArrayLiteral_FloatArray_Compiles()
    {
        // Arrange
        var json = "[1, 2, 3]";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, float[]>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(new float[] { 1f, 2f, 3f }, result);
    }

    [Fact]
    public void ArrayLiteral_StringArray_Compiles()
    {
        // Arrange
        var json = """["a", "b", "c"]""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, string[]>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(new string[] { "a", "b", "c" }, result);
    }

    [Fact]
    public void ArrayLiteral_BoolArray_Compiles()
    {
        // Arrange
        var json = "[true, false, true]";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, bool[]>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(new bool[] { true, false, true }, result);
    }

    [Fact]
    public void ArrayLiteral_NonArrayTOut_Fails()
    {
        // Arrange - TOut=float but JSON is an array literal
        var json = "[1, 2, 3]";
        var doc = JsonDocument.Parse(json);

        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out _));
    }
}
