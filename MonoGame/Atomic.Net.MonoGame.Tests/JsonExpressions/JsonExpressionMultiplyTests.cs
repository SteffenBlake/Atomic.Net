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
[Collection("NonParallel")]
public sealed class JsonExpressionMultiplyTests(ITestOutputHelper output) : IDisposable
{
    private readonly record struct TestInput(int A, int B);

    private readonly ErrorEventLogger _errorLogger = new ErrorEventLogger(output);
    private readonly FakeEventListener<ErrorEvent> _errorListener = new FakeEventListener<ErrorEvent>();

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
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, int>(doc, out var expr));
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
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, int>(doc, out var expr));
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
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, int>(doc, out var expr));
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
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, int>(doc, out var expr));
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
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, int>(doc, out var expr));
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
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(7.0, result, 0.001);
    }
    [Fact]
    public void Multiply_WrongOutputType_Fails()
    {
        // Arrange - multiply returns int, but requesting bool
        var json = """{ "*": [2, 3]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out _));
    }}
