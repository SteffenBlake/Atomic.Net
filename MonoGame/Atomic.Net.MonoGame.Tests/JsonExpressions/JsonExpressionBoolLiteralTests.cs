using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for boolean literal expressions.
/// </summary>
[Collection("NonParallel")]
public sealed class JsonExpressionBoolLiteralTests(ITestOutputHelper output) : IDisposable
{
    private readonly record struct TestInput(float Value);

    private readonly ErrorEventLogger _errorLogger = new(output);

    public void Dispose()
    {
        _errorLogger.Dispose();
    }

    [Fact]
    public void BoolLiteral_True_Compiles()
    {
        // Arrange
        var json = "true";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out var expr));
        var func = expr.Compile();

        // Act
        var result = func(new TestInput(0));

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void BoolLiteral_False_Compiles()
    {
        // Arrange
        var json = "false";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out var expr));
        var func = expr.Compile();

        // Act
        var result = func(new TestInput(0));

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void BoolLiteral_NonBoolTOut_Fails()
    {
        // Arrange - bool literal with TOut=float should fail
        var json = "true";
        var doc = JsonDocument.Parse(json);

        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out _));
    }

    [Fact]
    public void BoolLiteral_NonBoolTOutString_Fails()
    {
        // Arrange - bool literal with TOut=string should fail
        var json = "false";
        var doc = JsonDocument.Parse(json);

        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, string>(doc, out _));
    }
}
