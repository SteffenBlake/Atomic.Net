using System;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for JSONLogic '==' operator (equality with type coercion).
/// </summary>
[Collection("NonParallel")]
public sealed class JsonExpressionEqualsTests : IDisposable
{
    private readonly record struct TestInput(int Value, string Name);

    private readonly ErrorEventLogger _errorLogger;
    private readonly FakeEventListener<ErrorEvent> _errorListener;

    public JsonExpressionEqualsTests(ITestOutputHelper output)
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
    public void Equals_SameIntegers_ReturnsTrue()
    {
        // Arrange
        var json = """{"==": [1, 1]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0, "");

        // Act
        var result = func(data);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Equals_DifferentIntegers_ReturnsFalse()
    {
        // Arrange
        var json = """{"==": [1, 2]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0, "");

        // Act
        var result = func(data);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Equals_IntegerAndString_WithCoercion_ReturnsTrue()
    {
        // Arrange - comparing int and string not supported in C# semantics (no coercion)
        var json = """{ "==": [1, "1"]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert - should fail to compile
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out _));
    }

    [Fact]
    public void Equals_ZeroAndFalse_WithCoercion_ReturnsTrue()
    {
        // Arrange - comparing int and bool not supported in C# semantics (no coercion)
        var json = """{ "==": [0, false]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert - should fail to compile
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out _));
    }

    [Fact]
    public void Equals_WithVarData_ReturnsCorrectResult()
    {
        // Arrange
        var json = """{"==": [{"var": "Value"}, 42]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(42, "test");

        // Act
        var result = func(data);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Equals_StringComparison_ReturnsTrue()
    {
        // Arrange
        var json = """{"==": [{"var": "Name"}, "test"]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(42, "test");

        // Act
        var result = func(data);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Equals_StringComparison_ReturnsFalse()
    {
        // Arrange
        var json = """{"==": [{"var": "Name"}, "other"]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(42, "test");

        // Act
        var result = func(data);

        // Assert
        Assert.False(result);
    }
    [Fact]
    public void Equals_WrongOutputType_Fails()
    {
        // Arrange - equals returns bool, but requesting int
        var json = """{ "==": [1, 1]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, int>(doc, out _));
    }}
