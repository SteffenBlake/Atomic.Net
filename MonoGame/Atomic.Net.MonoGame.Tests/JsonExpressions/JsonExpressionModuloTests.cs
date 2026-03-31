using System;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for JSONLogic '%' operator (modulo).
/// </summary>
[Collection("NonParallel")]
public sealed class JsonExpressionModuloTests : IDisposable
{
    private readonly record struct TestInput(int Value);

    private readonly ErrorEventLogger _errorLogger;
    private readonly FakeEventListener<ErrorEvent> _errorListener;

    public JsonExpressionModuloTests(ITestOutputHelper output)
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
    public void Modulo_Odd_ReturnsRemainder()
    {
        // Arrange
        var json = """{"%": [101, 2]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, int>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public void Modulo_Even_ReturnsZero()
    {
        // Arrange
        var json = """{"%": [100, 2]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, int>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void Modulo_WithVarData_ReturnsRemainder()
    {
        // Arrange
        var json = """{"%": [{"var": "Value"}, 10]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, int>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(42);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(2, result);
    }

    [Fact]
    public void Modulo_LargerDivisor_ReturnsOriginal()
    {
        // Arrange
        var json = """{"%": [5, 10]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, int>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(5, result);
    }

    [Fact]
    public void Modulo_NegativeDividend_ReturnsRemainder()
    {
        // Arrange
        var json = """{"%": [-7, 3]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, int>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(-1, result);
    }

    [Fact]
    public void Modulo_ByZero_ReturnsNullAndFiresErrorEvent()
    {
        // Arrange
        var json = """{"%": [42, 0]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, int?>(doc, out var expr));

        // Act
        var result = expr;

        // Assert
        Assert.Null(result);
        Assert.True(_errorListener.ReceivedEvents.Count > 0, "Should fire at least one ErrorEvent for modulo by zero");
    }
    [Fact]
    public void Modulo_WrongOutputType_Fails()
    {
        // Arrange - modulo returns int, but requesting float
        var json = """{"%": [10, 3]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out _));
    }}
