using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for JSONLogic '<=' operator.
/// </summary>
[Collection("NonParallel")]
public sealed class JsonExpressionSymbolLessThanOrEqualTests(ITestOutputHelper output) : IDisposable
{
    private readonly record struct TestInput(int Value);

    private readonly ErrorEventLogger _errorLogger = new(output);
    private readonly FakeEventListener<ErrorEvent> _errorListener = new();

    public void Dispose()
    {
        _errorListener.Dispose();
        _errorLogger.Dispose();
    }

    [Fact]
    public void LessThanOrEqual_Less_ReturnsTrue()
    {
        // Arrange
        var json = """{"<=": [1, 2]}""";
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
    public void LessThanOrEqual_Equal_ReturnsTrue()
    {
        // Arrange
        var json = """{"<=": [1, 1]}""";
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
    public void LessThanOrEqual_Greater_ReturnsFalse()
    {
        // Arrange
        var json = """{"<=": [2, 1]}""";
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
    public void LessThanOrEqual_BetweenInclusive_ReturnsTrue()
    {
        // Arrange - C# does not support chained comparisons (must use && for multiple checks)
        var json = """{"<=": [1, 2, 3]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert - should fail to compile
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out _));
    }

    [Fact]
    public void LessThanOrEqual_BetweenInclusive_EqualToFirst_ReturnsTrue()
    {
        // Arrange - C# does not support chained comparisons (must use && for multiple checks)
        var json = """{"<=": [1, 1, 3]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert - should fail to compile
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out _));
    }

    [Fact]
    public void LessThanOrEqual_BetweenInclusive_EqualToLast_ReturnsTrue()
    {
        // Arrange - C# does not support chained comparisons (must use && for multiple checks)
        var json = """{"<=": [1, 3, 3]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert - should fail to compile
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out _));
    }

    [Fact]
    public void LessThanOrEqual_BetweenInclusive_OutOfRange_ReturnsFalse()
    {
        // Arrange - C# does not support chained comparisons (must use && for multiple checks)
        var json = """{"<=": [1, 4, 3]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert - should fail to compile
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out _));
    }

    [Fact]
    public void LessThanOrEqual_WithVarData_ReturnsCorrectResult()
    {
        // Arrange
        var json = """{"<=": [{"var": "Value"}, 42]}""";
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
    public void LessThanOrEqual_WrongOutputType_Fails()
    {
        // Arrange - <= returns bool, but requesting int
        var json = """{ "<=": [1, 2]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, int>(doc, out _));
    }}
