using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for JSONLogic '!' operator (logical NOT).
/// </summary>
[Collection("NonParallel")]
public sealed class JsonExpressionSymbolNotTests(ITestOutputHelper output) : IDisposable
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
    public void Not_True_ReturnsFalse()
    {
        // Arrange
        var json = """{"!": [true]}""";
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
    public void Not_False_ReturnsTrue()
    {
        // Arrange
        var json = """{"!": [false]}""";
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
    public void Not_UnarySyntax_Works()
    {
        // Arrange
        var json = """{"!": true}""";
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
    public void Not_TruthyValue_ReturnsFalse()
    {
        // Arrange - ! operator only works on bool in C# semantics (no truthiness)
        var json = """{ "!": 1}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert - should fail to compile
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out _));
    }

    [Fact]
    public void Not_FalsyValue_ReturnsTrue()
    {
        // Arrange - ! operator only works on bool in C# semantics (no truthiness)
        var json = """{ "!": 0}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert - should fail to compile
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out _));
    }

    [Fact]
    public void Not_EmptyArray_ReturnsTrue()
    {
        // Arrange - ! operator only works on bool in C# semantics (no truthiness)
        var json = """{ "!": []}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert - should fail to compile
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out _));
    }

    [Fact]
    public void Not_NonEmptyArray_ReturnsFalse()
    {
        // Arrange - ! operator only works on bool in C# semantics (no truthiness)
        var json = """{ "!": [[1]]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert - should fail to compile
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out _));
    }

    [Fact]
    public void Not_WithVarData_ReturnsCorrectResult()
    {
        // Arrange
        var json = """{"!": {"==": [{"var": "Unused"}, 0]}}""";
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
    public void Not_WrongOutputType_Fails()
    {
        // Arrange - not returns bool, but requesting float
        var json = """{ "!": [true]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out _));
    }}
