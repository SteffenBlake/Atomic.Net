using System;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for JSONLogic 'if' operator (conditional).
/// </summary>
[Collection("NonParallel")]
public sealed class JsonExpressionIfTests : IDisposable
{
    private readonly record struct TestInput(int Value, string Name);

    private readonly ErrorEventLogger _errorLogger;
    private readonly FakeEventListener<ErrorEvent> _errorListener;

    public JsonExpressionIfTests(ITestOutputHelper output)
    {
        _errorLogger = new ErrorEventLogger(output);
        _errorListener = new FakeEventListener<ErrorEvent>();
    }

    public void Dispose()
    {
        _errorListener.Dispose();
        _errorLogger.Dispose();
    }

    [Fact]
    public void If_ConditionTrue_ReturnsThenBranch()
    {
        // Arrange
        var json = """{"if": [true, "yes", "no"]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, string>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(42, "test");

        // Act
        var result = func(data);

        // Assert
        Assert.Equal("yes", result);
    }

    [Fact]
    public void If_ConditionFalse_ReturnsElseBranch()
    {
        // Arrange
        var json = """{"if": [false, "yes", "no"]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, string>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(42, "test");

        // Act
        var result = func(data);

        // Assert
        Assert.Equal("no", result);
    }

    [Fact]
    public void If_WithDataCondition_EvaluatesCorrectly()
    {
        // Arrange
        var json = """{"if": [{"==": [{"var": "Value"}, 42]}, "found", "not found"]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, string>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(42, "test");

        // Act
        var result = func(data);

        // Assert
        Assert.Equal("found", result);
    }

    [Fact]
    public void If_MultipleElseIf_ReturnCorrectBranch()
    {
        // Arrange
        var json = """{"if": [{"<": [{"var": "Value"}, 0]}, "negative", {"<": [{"var": "Value"}, 100]}, "small", "large"]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, string>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(50, "test");

        // Act
        var result = func(data);

        // Assert
        Assert.Equal("small", result);
    }

    [Fact]
    public void If_MultipleElseIf_FallsToFinalElse()
    {
        // Arrange
        var json = """{"if": [{"<": [{"var": "Value"}, 0]}, "negative", {"<": [{"var": "Value"}, 10]}, "small", "large"]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, string>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(100, "test");

        // Act
        var result = func(data);

        // Assert
        Assert.Equal("large", result);
    }

    [Fact]
    public void If_TruthyValue_ReturnsThenBranch()
    {
        // Arrange - if condition must be bool in C# semantics (no truthiness)
        var json = """{ "if": [1, "truthy", "falsy"]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert - should fail to compile
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, string>(doc, out _));
    }

    [Fact]
    public void If_FalsyValue_ReturnsElseBranch()
    {
        // Arrange - if condition must be bool in C# semantics (no truthiness)
        var json = """{ "if": [0, "truthy", "falsy"]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert - should fail to compile
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, string>(doc, out _));
    }

    [Fact]
    public void If_WrongOutputType_Fails()
    {
        // Arrange - if returns string, but requesting int
        var json = """{ "if": [true, "yes", "no"]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, int>(doc, out _));
    }
}
