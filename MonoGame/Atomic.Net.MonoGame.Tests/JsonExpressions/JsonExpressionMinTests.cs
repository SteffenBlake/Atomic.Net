using System;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for JSONLogic 'min' operator.
/// </summary>
[Collection("NonParallel")]
public sealed class JsonExpressionMinTests : IDisposable
{
    private readonly record struct TestInput(int A, int B, int C);

    private readonly ErrorEventLogger _errorLogger;
    private readonly FakeEventListener<ErrorEvent> _errorListener;

    public JsonExpressionMinTests(ITestOutputHelper output)
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
    public void Min_ThreeIntegers_ReturnsMinimum()
    {
        // Arrange
        var json = """{"min": [1, 2, 3]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, int>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0, 0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public void Min_TwoIntegers_ReturnsMinimum()
    {
        // Arrange
        var json = """{"min": [5, 2]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, int>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0, 0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(2, result);
    }

    [Fact]
    public void Min_NegativeNumbers_ReturnsMinimum()
    {
        // Arrange
        var json = """{"min": [-5, -2, -10]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, int>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0, 0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(-10, result);
    }

    [Fact]
    public void Min_WithVarData_ReturnsMinimum()
    {
        // Arrange
        var json = """{"min": [{"var": "A"}, {"var": "B"}, {"var": "C"}]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, int>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(10, 50, 30);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(10, result);
    }

    [Fact]
    public void Min_Floats_ReturnsMinimum()
    {
        // Arrange
        var json = """{"min": [1.5, 2.7, 0.3]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0, 0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(0.3, result);
    }

    [Fact]
    public void Min_SingleValue_ReturnsThatValue()
    {
        // Arrange
        var json = """{"min": [42]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, int>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0, 0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(42, result);
    }
    [Fact]
    public void Min_WrongOutputType_Fails()
    {
        // Arrange - min returns int, but requesting bool
        var json = """{ "min": [1, 2, 3]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out _));
    }}
