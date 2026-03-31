using System;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for JSONLogic '+' operator (addition/concatenation).
/// </summary>
public sealed class JsonExpressionAddTests : IDisposable
{
    private readonly record struct TestInput(int A, int B);

    private readonly ErrorEventLogger _errorLogger;
    private readonly FakeEventListener<ErrorEvent> _errorListener;

    public JsonExpressionAddTests(ITestOutputHelper output)
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
    public void Add_TwoIntegers_ReturnsSum()
    {
        // Arrange
        var json = """{"+": [4, 2]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, int>(doc);
        var func = expr.Compile();
        var data = new TestInput(0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(6, result);
    }

    [Fact]
    public void Add_MultipleIntegers_ReturnsSum()
    {
        // Arrange
        var json = """{"+": [2, 2, 2, 2, 2]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, int>(doc);
        var func = expr.Compile();
        var data = new TestInput(0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(10, result);
    }

    [Fact]
    public void Add_WithVarData_ReturnsSum()
    {
        // Arrange
        var json = """{"+": [{"var": "A"}, {"var": "B"}]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, int>(doc);
        var func = expr.Compile();
        var data = new TestInput(10, 32);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public void Add_NegativeNumbers_ReturnsSum()
    {
        // Arrange
        var json = """{"+": [-5, 3]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, int>(doc);
        var func = expr.Compile();
        var data = new TestInput(0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(-2, result);
    }

    [Fact]
    public void Add_Floats_ReturnsSum()
    {
        // Arrange
        var json = """{"+": [3.14, 2.86]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, double>(doc);
        var func = expr.Compile();
        var data = new TestInput(0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(6.0, result, 0.001);
    }

    [Fact]
    public void Add_UnaryString_CastsToNumber()
    {
        // Arrange
        var json = """{"+": "3.14"}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, double>(doc);
        var func = expr.Compile();
        var data = new TestInput(0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(3.14, result);
    }

    [Fact]
    public void Add_UnaryInteger_ReturnsInteger()
    {
        // Arrange
        var json = """{"+": 42}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, int>(doc);
        var func = expr.Compile();
        var data = new TestInput(0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(42, result);
    }
}
