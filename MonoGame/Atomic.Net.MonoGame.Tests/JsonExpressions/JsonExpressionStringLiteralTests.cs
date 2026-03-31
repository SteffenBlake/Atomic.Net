using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for string literal expressions.
/// </summary>
[Collection("NonParallel")]
public sealed class JsonExpressionStringLiteralTests(ITestOutputHelper output) : IDisposable
{
    private readonly record struct TestInput(float Value);

    private readonly ErrorEventLogger _errorLogger = new(output);

    public void Dispose()
    {
        _errorLogger.Dispose();
    }

    [Fact]
    public void StringLiteral_SimpleString_CompilesAndReturnsValue()
    {
        // Arrange
        var json = """{"log": "hello"}""";
        var doc = JsonDocument.Parse(json);

        // Act
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, string>(doc, out var expr));
        var result = expr.Compile()(new TestInput(0));

        // Assert
        Assert.Equal("hello", result);
    }

    [Fact]
    public void StringLiteral_EmptyString_CompilesAndReturnsEmpty()
    {
        // Arrange
        var json = """{"log": ""}""";
        var doc = JsonDocument.Parse(json);

        // Act
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, string>(doc, out var expr));
        var result = expr.Compile()(new TestInput(0));

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void StringLiteral_WrongOutputType_Fails()
    {
        // Arrange - string literal requires TOut=string; float is not string
        var json = """{"log": "hello"}""";
        var doc = JsonDocument.Parse(json);

        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out _));
    }
}
