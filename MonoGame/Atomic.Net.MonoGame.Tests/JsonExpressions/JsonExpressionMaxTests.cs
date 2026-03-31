using System;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for JSONLogic 'max' operator.
/// </summary>
public sealed class JsonExpressionMaxTests : IDisposable
{
    private readonly record struct TestInput(int A, int B, int C);

    private readonly ErrorEventLogger _errorLogger;
    private readonly FakeEventListener<ErrorEvent> _errorListener;

    public JsonExpressionMaxTests(ITestOutputHelper output)
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
    public void Max_ThreeIntegers_ReturnsMaximum()
    {
        // Arrange
        var json = """{"max": [1, 2, 3]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, int>(doc);
        var func = expr.Compile();
        var data = new TestInput(0, 0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(3, result);
    }

    [Fact]
    public void Max_TwoIntegers_ReturnsMaximum()
    {
        // Arrange
        var json = """{"max": [5, 2]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, int>(doc);
        var func = expr.Compile();
        var data = new TestInput(0, 0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(5, result);
    }

    [Fact]
    public void Max_NegativeNumbers_ReturnsMaximum()
    {
        // Arrange
        var json = """{"max": [-5, -2, -10]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, int>(doc);
        var func = expr.Compile();
        var data = new TestInput(0, 0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(-2, result);
    }

    [Fact]
    public void Max_WithVarData_ReturnsMaximum()
    {
        // Arrange
        var json = """{"max": [{"var": "A"}, {"var": "B"}, {"var": "C"}]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, int>(doc);
        var func = expr.Compile();
        var data = new TestInput(10, 50, 30);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(50, result);
    }

    [Fact]
    public void Max_Floats_ReturnsMaximum()
    {
        // Arrange
        var json = """{"max": [1.5, 2.7, 0.3]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, double>(doc);
        var func = expr.Compile();
        var data = new TestInput(0, 0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(2.7, result);
    }

    [Fact]
    public void Max_SingleValue_ReturnsThatValue()
    {
        // Arrange
        var json = """{"max": [42]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, int>(doc);
        var func = expr.Compile();
        var data = new TestInput(0, 0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(42, result);
    }
}
