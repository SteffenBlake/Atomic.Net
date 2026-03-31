using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for JSONLogic '!=' operator (inequality with type coercion).
/// </summary>
[Collection("NonParallel")]
public sealed class JsonExpressionSymbolNotEqualsTests(ITestOutputHelper output) : IDisposable
{
    private readonly record struct TestInput(int Value, string Name);

    private readonly ErrorEventLogger _errorLogger = new(output);
    private readonly FakeEventListener<ErrorEvent> _errorListener = new();

    public void Dispose()
    {
        _errorListener.Dispose();
        _errorLogger.Dispose();
    }

    [Fact]
    public void NotEquals_DifferentIntegers_ReturnsTrue()
    {
        // Arrange
        var json = """{"!=": [1, 2]}""";
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
    public void NotEquals_SameIntegers_ReturnsFalse()
    {
        // Arrange
        var json = """{"!=": [1, 1]}""";
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
    public void NotEquals_IntegerAndString_WithCoercion_ReturnsFalse()
    {
        // Arrange - comparing int and string not supported in C# semantics (no coercion)
        var json = """{"!=": [1, "1"]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert - should fail to compile
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out _));
    }

    [Fact]
    public void NotEquals_WithVarData_ReturnsTrue()
    {
        // Arrange
        var json = """{"!=": [{"var": "Value"}, 100]}""";
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
    public void NotEquals_WithVarData_ReturnsFalse()
    {
        // Arrange
        var json = """{"!=": [{"var": "Value"}, 42]}""";
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
    public void NotEquals_WrongOutputType_Fails()
    {
        // Arrange - not equals returns bool, but requesting string
        var json = """{ "!=": [1, 2]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, string>(doc, out _));
    }}
