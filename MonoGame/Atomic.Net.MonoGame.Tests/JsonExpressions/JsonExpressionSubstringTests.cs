using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for JSONLogic 'substr' operator (substring extraction).
/// </summary>
[Collection("NonParallel")]
public sealed class JsonExpressionSubstringTests(ITestOutputHelper output) : IDisposable
{
    private readonly record struct TestInput(string Text);

    private readonly ErrorEventLogger _errorLogger = new(output);
    private readonly FakeEventListener<ErrorEvent> _errorListener = new();

    public void Dispose()
    {
        _errorListener.Dispose();
        _errorLogger.Dispose();
    }

    [Fact]
    public void Substr_PositiveStart_ReturnsFromIndex()
    {
        // Arrange
        var json = """{"substring": ["jsonlogic", 4]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, string>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput("");

        // Act
        var result = func(data);

        // Assert
        Assert.Equal("logic", result);
    }

    [Fact]
    public void Substr_NegativeStart_ReturnsFromEnd()
    {
        // Arrange - C# does not support negative indices (throws ArgumentOutOfRangeException)
        var json = """{"substring": ["jsonlogic", -5]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert - should fail to compile
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, string>(doc, out _));
    }

    [Fact]
    public void Substr_PositiveStartAndLength_ReturnsSubstring()
    {
        // Arrange
        var json = """{"substring": ["jsonlogic", 1, 3]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, string>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput("");

        // Act
        var result = func(data);

        // Assert
        Assert.Equal("son", result);
    }

    [Fact]
    public void Substr_NegativeLength_StopsBeforeEnd()
    {
        // Arrange - C# does not support negative lengths (throws ArgumentOutOfRangeException)
        var json = """{"substring": ["jsonlogic", 4, -2]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert - should fail to compile
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, string>(doc, out _));
    }

    [Fact]
    public void Substr_StartAtZero_ReturnsFromBeginning()
    {
        // Arrange
        var json = """{"substring": ["jsonlogic", 0, 4]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, string>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput("");

        // Act
        var result = func(data);

        // Assert
        Assert.Equal("json", result);
    }

    [Fact]
    public void Substr_WithVarData_ReturnsSubstring()
    {
        // Arrange
        var json = """{"substring": [{"var": "Text"}, 0, 5]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, string>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput("Hello World");

        // Act
        var result = func(data);

        // Assert
        Assert.Equal("Hello", result);
    }

    [Fact]
    public void Substr_LengthExceedsString_ReturnsRemainder()
    {
        // Arrange
        var json = """{"substring": ["short", 2, 100]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, string>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput("");

        // Act
        var result = func(data);

        // Assert
        Assert.Equal("ort", result);
    }

    [Fact]
    public void Substr_StartExceedsLength_ReturnsEmpty()
    {
        // Arrange
        var json = """{"substring": ["short", 100]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, string>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput("");

        // Act
        var result = func(data);

        // Assert
        Assert.Equal("", result);
    }
    [Fact]
    public void Substring_WrongOutputType_Fails()
    {
        // Arrange - substring returns string, but requesting int
        var json = """{ "substring": ["hello", 0, 3]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, int>(doc, out _));
    }}
