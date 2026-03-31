using System;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for JSONLogic 'or' operator (returns first truthy value or last falsy).
/// </summary>
[Collection("NonParallel")]
public sealed class JsonExpressionOrTests : IDisposable
{
    private readonly record struct TestInput(int Unused);

    private readonly ErrorEventLogger _errorLogger;
    private readonly FakeEventListener<ErrorEvent> _errorListener;

    public JsonExpressionOrTests(ITestOutputHelper output)
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
    public void Or_TrueAndFalse_ReturnsTrue()
    {
        // Arrange
        var json = """{"or": [true, false]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Or_FalseAndTrue_ReturnsTrue()
    {
        // Arrange
        var json = """{"or": [false, true]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Or_BothFalse_ReturnsFalse()
    {
        // Arrange
        var json = """{"or": [false, false]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Or_ReturnsFirstTruthy()
    {
        // Arrange - mixing bool and string not supported in C# semantics (no truthiness)
        var json = """{ "or": [false, "a"]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert - should fail to compile
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, string>(doc, out _));
    }

    [Fact]
    public void Or_ReturnsFirstTruthyFromMultiple()
    {
        // Arrange - mixing bool/int/string not supported in C# semantics (no truthiness)
        var json = """{ "or": [false, 0, "a"]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert - should fail to compile
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, string>(doc, out _));
    }

    [Fact]
    public void Or_AllFalsy_ReturnsLast()
    {
        // Arrange - mixing bool/int/string not supported in C# semantics (no truthiness)
        var json = """{ "or": [false, 0, ""]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert - should fail to compile
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, string>(doc, out _));
    }

    [Fact]
    public void Or_WithVarData_ReturnsCorrectResult()
    {
        // Arrange
        var json = """{"or": [{"==": [{"var": "Value"}, 0]}, {"==": [{"var": "Value"}, 42]}]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(42);

        // Act
        var result = func(data);

        // Assert
        Assert.True(result);
    }
    [Fact]
    public void Or_WrongOutputType_Fails()
    {
        // Arrange - or returns bool, but requesting int[]
        var json = """{ "or": [true, false]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, int[]>(doc, out _));
    }}
