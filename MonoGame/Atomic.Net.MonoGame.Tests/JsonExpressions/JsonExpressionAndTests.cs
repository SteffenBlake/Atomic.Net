using System;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for JSONLogic 'and' operator (returns first falsy value or last truthy).
/// </summary>
public sealed class JsonExpressionAndTests : IDisposable
{
    private readonly record struct TestInput(int Unused);

    private readonly ErrorEventLogger _errorLogger;
    private readonly FakeEventListener<ErrorEvent> _errorListener;

    public JsonExpressionAndTests(ITestOutputHelper output)
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
    public void And_BothTrue_ReturnsTrue()
    {
        // Arrange
        var json = """{"and": [true, true]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, bool>(doc);
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void And_TrueAndFalse_ReturnsFalse()
    {
        // Arrange
        var json = """{"and": [true, false]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, bool>(doc);
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void And_ReturnsLastWhenAllTruthy()
    {
        // Arrange
        var json = """{"and": [true, "a", 3]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, int>(doc);
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(3, result);
    }

    [Fact]
    public void And_ReturnsFirstFalsy()
    {
        // Arrange
        var json = """{"and": [true, "", 3]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, string>(doc);
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void And_WithVarData_ReturnsTrueWhenBothConditionsTrue()
    {
        // Arrange
        var json = """{"and": [{">": [{"var": "Value"}, 0]}, {"<": [{"var": "Value"}, 100]}]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, bool>(doc);
        var func = expr.Compile();
        var data = new TestInput(42);

        // Act
        var result = func(data);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void And_WithVarData_ReturnsFalseWhenOneConditionFalse()
    {
        // Arrange
        var json = """{"and": [{">": [{"var": "Value"}, 0]}, {"<": [{"var": "Value"}, 10]}]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, bool>(doc);
        var func = expr.Compile();
        var data = new TestInput(42);

        // Act
        var result = func(data);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void And_SingleArgument_ReturnsThatArg()
    {
        // Arrange
        var json = """{"and": [true]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, bool>(doc);
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void And_EmptyArrayIsFalsy_ReturnsEmptyArray()
    {
        // Arrange
        var json = """{"and": [true, []]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, int[]>(doc);
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.Empty(result);
    }
}
