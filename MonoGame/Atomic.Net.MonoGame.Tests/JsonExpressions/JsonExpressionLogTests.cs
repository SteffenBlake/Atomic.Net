using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for JSONLogic 'log' operator (pass-through with side effect).
/// </summary>
[Collection("NonParallel")]
public sealed class JsonExpressionLogTests(ITestOutputHelper output) : IDisposable
{
    private readonly record struct TestInput(int Value);

    private readonly ErrorEventLogger _errorLogger = new(output);
    private readonly FakeEventListener<ErrorEvent> _errorListener = new();
    private readonly FakeEventListener<LogEvent> _logListener = new();

    public void Dispose()
    {
        _logListener.Dispose();
        _errorListener.Dispose();
        _errorLogger.Dispose();
    }

    [Fact]
    public void Log_String_ReturnsValueAndLogs()
    {
        // Arrange
        var json = """{"log": "apple"}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, string>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal("apple", result);
        Assert.True(_logListener.ReceivedEvents.Count > 0);
        Assert.Contains(_logListener.ReceivedEvents, e => e.Message.Contains("apple"));
    }

    [Fact]
    public void Log_Number_ReturnsNumberAndLogs()
    {
        // Arrange
        var json = """{"log": 42}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, int>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(42, result);
        Assert.True(_logListener.ReceivedEvents.Count > 0);
        Assert.Contains(_logListener.ReceivedEvents, e => e.Message.Contains("42"));
    }

    [Fact]
    public void Log_WithVarData_ReturnsValueAndLogs()
    {
        // Arrange
        var json = """{"log": {"var": "Value"}}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, int>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(100);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(100, result);
        Assert.True(_logListener.ReceivedEvents.Count > 0);
        Assert.Contains(_logListener.ReceivedEvents, e => e.Message.Contains("100"));
    }

    [Fact]
    public void Log_Boolean_ReturnsBooleanAndLogs()
    {
        // Arrange
        var json = """{"log": true}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.True(result);
        Assert.True(_logListener.ReceivedEvents.Count > 0);
        Assert.Contains(_logListener.ReceivedEvents, e => e.Message.Contains("True", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Log_NestedExpression_ReturnsResultAndLogs()
    {
        // Arrange
        var json = """{"log": {"+": [1, 2]}}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, int>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(3, result);
        Assert.True(_logListener.ReceivedEvents.Count > 0);
        Assert.Contains(_logListener.ReceivedEvents, e => e.Message.Contains('3'));
    }
    [Fact]
    public void Log_WrongOutputType_Fails()
    {
        // Arrange - log returns int, but requesting bool
        var json = """{ "log": 42}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out _));
    }}
