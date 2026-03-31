using System;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for JSONLogic '/' operator (division).
/// </summary>
public sealed class JsonExpressionDivideTests : IDisposable
{
    private readonly record struct TestInput(int A, int B);

    private readonly ErrorEventLogger _errorLogger;
    private readonly FakeEventListener<ErrorEvent> _errorListener;

    public JsonExpressionDivideTests(ITestOutputHelper output)
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
    public void Divide_TwoIntegers_ReturnsQuotient()
    {
        // Arrange
        var json = """{"/": [4, 2]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, int>(doc);
        var func = expr.Compile();
        var data = new TestInput(0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(2, result);
    }

    [Fact]
    public void Divide_WithVarData_ReturnsQuotient()
    {
        // Arrange
        var json = """{"/": [{"var": "A"}, {"var": "B"}]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, int>(doc);
        var func = expr.Compile();
        var data = new TestInput(84, 2);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public void Divide_Floats_ReturnsQuotient()
    {
        // Arrange
        var json = """{"/": [7.5, 2.5]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, double>(doc);
        var func = expr.Compile();
        var data = new TestInput(0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(3.0, result, 0.001);
    }

    [Fact]
    public void Divide_ResultFloat_ReturnsFloat()
    {
        // Arrange
        var json = """{"/": [5, 2]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, double>(doc);
        var func = expr.Compile();
        var data = new TestInput(0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(2.5, result, 0.001);
    }

    [Fact]
    public void Divide_NegativeNumbers_ReturnsQuotient()
    {
        // Arrange
        var json = """{"/": [-10, 2]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, int>(doc);
        var func = expr.Compile();
        var data = new TestInput(0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(-5, result);
    }

    [Fact]
    public void Divide_ByZero_ReturnsNullAndFiresErrorEvent()
    {
        // Arrange
        var json = """{"/": [42, 0]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, double?>(doc);

        // Act
        var result = expr;

        // Assert
        Assert.Null(result);
        Assert.True(_errorListener.ReceivedEvents.Count > 0, "Should fire at least one ErrorEvent for division by zero");
    }
}
