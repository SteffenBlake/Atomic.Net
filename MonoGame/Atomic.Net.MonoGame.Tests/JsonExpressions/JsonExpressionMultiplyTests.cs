using System;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for JSONLogic '*' operator (multiplication).
/// </summary>
public sealed class JsonExpressionMultiplyTests : IDisposable
{
    private readonly record struct TestInput(int A, int B);

    private readonly ErrorEventLogger _errorLogger;
    private readonly FakeEventListener<ErrorEvent> _errorListener;

    public JsonExpressionMultiplyTests(ITestOutputHelper output)
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
    public void Multiply_TwoIntegers_ReturnsProduct()
    {
        // Arrange
        var json = """{"*": [4, 2]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, int>(doc);
        var func = expr.Compile();
        var data = new TestInput(0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(8, result);
    }

    [Fact]
    public void Multiply_MultipleIntegers_ReturnsProduct()
    {
        // Arrange
        var json = """{"*": [2, 2, 2, 2, 2]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, int>(doc);
        var func = expr.Compile();
        var data = new TestInput(0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(32, result);
    }

    [Fact]
    public void Multiply_WithVarData_ReturnsProduct()
    {
        // Arrange
        var json = """{"*": [{"var": "A"}, {"var": "B"}]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, int>(doc);
        var func = expr.Compile();
        var data = new TestInput(6, 7);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public void Multiply_ByZero_ReturnsZero()
    {
        // Arrange
        var json = """{"*": [42, 0]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, int>(doc);
        var func = expr.Compile();
        var data = new TestInput(0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void Multiply_NegativeNumbers_ReturnsProduct()
    {
        // Arrange
        var json = """{"*": [-3, 4]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, int>(doc);
        var func = expr.Compile();
        var data = new TestInput(0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(-12, result);
    }

    [Fact]
    public void Multiply_Floats_ReturnsProduct()
    {
        // Arrange
        var json = """{"*": [3.5, 2.0]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, double>(doc);
        var func = expr.Compile();
        var data = new TestInput(0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(7.0, result, 0.001);
    }
}
