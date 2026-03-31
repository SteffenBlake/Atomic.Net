using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for number literal expressions.
/// </summary>
[Collection("NonParallel")]
public sealed class JsonExpressionNumberLiteralTests(ITestOutputHelper output) : IDisposable
{
    private readonly record struct TestInput(float Value);

    private readonly ErrorEventLogger _errorLogger = new(output);

    public void Dispose()
    {
        _errorLogger.Dispose();
    }

    [Fact]
    public void NumberLiteral_Integer_CompilesAndReturnsValue()
    {
        // Arrange
        var json = """{"log": 42}""";
        var doc = JsonDocument.Parse(json);

        // Act
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out var expr));
        var result = expr.Compile()(new TestInput(0));

        // Assert
        Assert.Equal(42f, result);
    }

    [Fact]
    public void NumberLiteral_NegativeFloat_CompilesAndReturnsValue()
    {
        // Arrange
        var json = """{"log": -3.14}""";
        var doc = JsonDocument.Parse(json);

        // Act
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out var expr));
        var result = expr.Compile()(new TestInput(0));

        // Assert
        Assert.Equal(-3.14f, result, 0.001f);
    }

    [Fact]
    public void NumberLiteral_ArrayOutputType_Fails()
    {
        // Arrange - number literal cannot produce an array type
        var json = """{"log": 42}""";
        var doc = JsonDocument.Parse(json);

        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, float[]>(doc, out _));
    }

    [Fact]
    public void NumberLiteral_NonNumberToken_Fails()
    {
        // Arrange - 'true' is a boolean token, not a number
        var json = """{"log": true}""";
        var doc = JsonDocument.Parse(json);

        // Assert - requesting float, but parsing boolean literal should fail
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out _));
    }
}
