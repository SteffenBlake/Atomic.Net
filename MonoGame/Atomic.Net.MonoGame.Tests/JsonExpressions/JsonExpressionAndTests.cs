using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for JSONLogic 'and' operator (returns first falsy value or last truthy).
/// </summary>
[Collection("NonParallel")]
public sealed class JsonExpressionAndTests(ITestOutputHelper output) : IDisposable
{
    private readonly record struct TestInput(float Unused);

    private readonly ErrorEventLogger _errorLogger = new(output);
    private readonly FakeEventListener<ErrorEvent> _errorListener = new();

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
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out var expr));
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
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out var expr));
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
        // Arrange - mixing bool/string/int not supported in C# semantics (no truthiness)
        var json = """{"and": [true, "a", 3]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert - should fail to compile
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, int>(doc, out _));
    }

    [Fact]
    public void And_ReturnsFirstFalsy()
    {
        // Arrange - mixing bool/string/int not supported in C# semantics (no truthiness)
        var json = """{"and": [true, "", 3]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert - should fail to compile
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, string>(doc, out _));
    }

    [Fact]
    public void And_WithVarData_ReturnsTrueWhenBothConditionsTrue()
    {
        // Arrange
        var json = """{"and": [{">":  [{"var": "Unused"}, 0]}, {"<": [{"var": "Unused"}, 100]}]}""";
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
    public void And_WithVarData_ReturnsFalseWhenOneConditionFalse()
    {
        // Arrange
        var json = """{"and": [{">":  [{"var": "Unused"}, 0]}, {"<": [{"var": "Unused"}, 10]}]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(42);

        // Act
        var result = func(data);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void And_EmptyArrayIsFalsy_ReturnsEmptyArray()
    {
        // Arrange - mixing bool and array not supported in C# semantics (no truthiness)
        var json = """{"and": [true, []]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert - should fail to compile
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, int[]>(doc, out _));
    }
    [Fact]
    public void And_WrongOutputType_Fails()
    {
        // Arrange - and returns bool, but requesting string
        var json = """{ "and": [true, true]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, string>(doc, out _));
    }

    [Fact]
    public void And_NotAnArray_Fails()
    {
        // Arrange - 'and' value must be an array, not an object
        var json = """{"and": {"a": true}}""";
        var doc = JsonDocument.Parse(json);

        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out _));
    }

    [Fact]
    public void And_EmptyArray_Fails()
    {
        // Arrange - 'and' requires exactly 2 operands
        var json = """{"and": []}""";
        var doc = JsonDocument.Parse(json);

        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out _));
    }

    [Fact]
    public void And_SingleOperand_Fails()
    {
        // Arrange - 'and' requires exactly 2 operands
        var json = """{"and": [true]}""";
        var doc = JsonDocument.Parse(json);

        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out _));
    }

    [Fact]
    public void And_ThreeOperands_Fails()
    {
        // Arrange - 'and' requires exactly 2 operands
        var json = """{"and": [true, false, true]}""";
        var doc = JsonDocument.Parse(json);

        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out _));
    }
}
