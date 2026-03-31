using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for JSONLogic 'all' operator (all elements pass test).
/// </summary>
[Collection("NonParallel")]
public sealed class JsonExpressionLinqAllTests(ITestOutputHelper output) : IDisposable
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
    public void All_AllPositive_ReturnsTrue()
    {
        // Arrange
        var json = """{"all": [[1, 2, 3], {">": [{"var": ""}, 0]}]}""";
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
    public void All_OneNegative_ReturnsFalse()
    {
        // Arrange
        var json = """{"all": [[-1, 2, 3], {">": [{"var": ""}, 0]}]}""";
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
    public void All_EmptyArray_ReturnsFalse()
    {
        // Arrange
        var json = """{"all": [[], {">": [{"var": ""}, 0]}]}""";
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
    public void All_ComplexCondition_ReturnsCorrectResult()
    {
        // Arrange
        var json = """{"all": [[10, 20, 30], {"and": [{">": [{"var": ""}, 0]}, {"<": [{"var": ""}, 100]}]}]}""";
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
    public void All_AllEven_ReturnsTrue()
    {
        // Arrange
        var json = """{"all": [[2, 4, 6], {"==": [{"%": [{"var": ""}, 2]}, 0]}]}""";
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
    public void All_WrongOutputType_Fails()
    {
        // Arrange - all returns bool, but requesting int
        var json = """{ "all": [[1, 2, 3], {">": [{"var": ""}, 0]}]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, int>(doc, out _));
    }

    [Fact]
    public void All_NotAnArray_Fails()
    {
        // Arrange - value of 'all' must be an array, not a string
        var json = """{"all": "not-an-array"}""";
        var doc = JsonDocument.Parse(json);

        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out _));
    }

    [Fact]
    public void All_TooFewArguments_Fails()
    {
        // Arrange - all requires exactly 2 elements; 1 is invalid
        var json = """{"all": [[1, 2, 3]]}""";
        var doc = JsonDocument.Parse(json);

        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out _));
    }

    [Fact]
    public void All_TooManyArguments_Fails()
    {
        // Arrange - all requires exactly 2 elements; 3 is invalid
        var json = """{"all": [[1, 2, 3], {">": [{"var": ""}, 0]}, "extra"]}""";
        var doc = JsonDocument.Parse(json);

        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out _));
    }

    [Fact]
    public void All_SourceNotArrayType_Fails()
    {
        // Arrange - source (var Unused) resolves to float, not an array type
        var json = """{"all": [{"var": "Unused"}, {">": [{"var": ""}, 0]}]}""";
        var doc = JsonDocument.Parse(json);

        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out _));
    }
}
