using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for JSONLogic 'max' operator.
/// </summary>
[Collection("NonParallel")]
public sealed class JsonExpressionMaxTests(ITestOutputHelper output) : IDisposable
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
    public void Max_ThreeIntegers_ReturnsMaximum()
    {
        // Arrange
        var json = """{"max": [1, 2, 3]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0, 0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(3f, result);
    }

    [Fact]
    public void Max_TwoIntegers_ReturnsMaximum()
    {
        // Arrange
        var json = """{"max": [5, 2]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0, 0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(5f, result);
    }

    [Fact]
    public void Max_NegativeNumbers_ReturnsMaximum()
    {
        // Arrange
        var json = """{"max": [-5, -2, -10]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0, 0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(-2f, result);
    }

    [Fact]
    public void Max_WithVarData_ReturnsMaximum()
    {
        // Arrange
        var json = """{"max": [{"var": "A"}, {"var": "B"}, {"var": "C"}]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(10, 50, 30);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(50f, result);
    }

    [Fact]
    public void Max_Floats_ReturnsMaximum()
    {
        // Arrange
        var json = """{"max": [1.5, 2.7, 0.3]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0, 0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(2.7f, result, 0.001f);
    }

    [Fact]
    public void Max_SingleValue_ReturnsThatValue()
    {
        // Arrange
        var json = """{"max": [42]}""";
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
    public void Max_WrongOutputType_Fails()
    {
        // Arrange - max returns int, but requesting string[]
        var json = """{ "max": [1, 2, 3]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, string[]>(doc, out _));
    }}
