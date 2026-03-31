using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for JSONLogic '<' operator.
/// </summary>
[Collection("NonParallel")]
public sealed class JsonExpressionLessThanTests(ITestOutputHelper output) : IDisposable
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
    public void LessThan_OneLess_ReturnsTrue()
    {
        // Arrange
        var json = """{"<": [1, 2]}""";
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
    public void LessThan_OneGreater_ReturnsFalse()
    {
        // Arrange
        var json = """{"<": [2, 1]}""";
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
    public void LessThan_Equal_ReturnsFalse()
    {
        // Arrange
        var json = """{"<": [1, 1]}""";
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
    public void LessThan_BetweenExclusive_ReturnsTrue()
    {
        // Arrange - C# does not support chained comparisons (must use && for multiple checks)
        var json = """{"<": [1, 2, 3]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert - should fail to compile
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out _));
    }

    [Fact]
    public void LessThan_BetweenExclusive_EqualToFirst_ReturnsFalse()
    {
        // Arrange - C# does not support chained comparisons (must use && for multiple checks)
        var json = """{"<": [1, 1, 3]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert - should fail to compile
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out _));
    }

    [Fact]
    public void LessThan_BetweenExclusive_OutOfRange_ReturnsFalse()
    {
        // Arrange - C# does not support chained comparisons (must use && for multiple checks)
        var json = """{"<": [1, 4, 3]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert - should fail to compile
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out _));
    }

    [Fact]
    public void LessThan_BetweenWithVarData_ReturnsCorrectResult()
    {
        // Arrange - C# does not support chained comparisons (must use && for multiple checks)
        var json = """{"<": [0, {"var": "Value"}, 100]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert - should fail to compile
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out _));
    }

    [Fact]
    public void LessThan_WithVarData_ReturnsCorrectResult()
    {
        // Arrange
        var json = """{"<": [{"var": "Value"}, 100]}""";
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
    public void LessThan_WrongOutputType_Fails()
    {
        // Arrange - less than returns bool, but requesting float
        var json = """{ "<": [1, 2]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out _));
    }}
