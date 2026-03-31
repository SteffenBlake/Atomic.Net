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
public sealed class JsonExpressionMinTests(ITestOutputHelper output) : IDisposable
{
    private readonly record struct TestInput(float A, float B, float C);

    private readonly ErrorEventLogger _errorLogger = new ErrorEventLogger(output);
    private readonly FakeEventListener<ErrorEvent> _errorListener = new FakeEventListener<ErrorEvent>();

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
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0, 0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(1f, result);
    }

    [Fact]
    public void Min_TwoIntegers_ReturnsMinimum()
    {
        // Arrange
        var json = """{"min": [5, 2]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0, 0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(2f, result);
    }

    [Fact]
    public void Min_NegativeNumbers_ReturnsMinimum()
    {
        // Arrange
        var json = """{"min": [-5, -2, -10]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0, 0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(-10f, result);
    }

    [Fact]
    public void Min_WithVarData_ReturnsMinimum()
    {
        // Arrange
        var json = """{"min": [{"var": "A"}, {"var": "B"}, {"var": "C"}]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(10, 50, 30);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(10f, result);
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
        Assert.Equal(0.3f, result, 0.001f);
    }

    [Fact]
    public void Min_SingleValue_ReturnsThatValue()
    {
        // Arrange
        var json = """{"min": [42]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0, 0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(42f, result);
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
