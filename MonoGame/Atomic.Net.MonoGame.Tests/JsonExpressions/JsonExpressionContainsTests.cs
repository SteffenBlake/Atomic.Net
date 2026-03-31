using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for JSONLogic 'contains' operator for strings (substring test).
/// </summary>
[Collection("NonParallel")]
public sealed class JsonExpressionContainsTests(ITestOutputHelper output) : IDisposable
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
    public void StringContains_SubstringPresent_ReturnsTrue()
    {
        // Arrange
        var json = """{"contains": ["Spring", "Springfield"]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput("");

        // Act
        var result = func(data);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void StringContains_SubstringNotPresent_ReturnsFalse()
    {
        // Arrange
        var json = """{"contains": ["Summer", "Springfield"]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput("");

        // Act
        var result = func(data);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void StringContains_EmptySubstring_ReturnsTrue()
    {
        // Arrange
        var json = """{"contains": ["", "test"]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput("");

        // Act
        var result = func(data);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void StringContains_CaseSensitive_ReturnsFalse()
    {
        // Arrange
        var json = """{"contains": ["SPRING", "Springfield"]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput("");

        // Act
        var result = func(data);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void StringContains_WithVarData_ReturnsCorrectResult()
    {
        // Arrange
        var json = """{"contains": ["test", {"var": "Text"}]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput("This is a test string");

        // Act
        var result = func(data);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void StringContains_AtBeginning_ReturnsTrue()
    {
        // Arrange
        var json = """{"contains": ["Hello", "Hello World"]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput("");

        // Act
        var result = func(data);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void StringContains_AtEnd_ReturnsTrue()
    {
        // Arrange
        var json = """{"contains": ["World", "Hello World"]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput("");

        // Act
        var result = func(data);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void StringContains_WrongOutputType_Fails()
    {
        // Arrange - contains returns bool, but requesting int[]
        var json = """{"contains": ["test", "This is a test"]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, int[]>(doc, out _));
    }
}
