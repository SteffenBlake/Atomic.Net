using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for JSONLogic 'in' operator for arrays (membership test).
/// </summary>
[Collection("NonParallel")]
public sealed class JsonExpressionInTests(ITestOutputHelper output) : IDisposable
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
    public void InArray_ValuePresent_ReturnsTrue()
    {
        // Arrange
        var json = """{"in": ["Ringo", ["John", "Paul", "George", "Ringo"]]}""";
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
    public void InArray_ValueNotPresent_ReturnsFalse()
    {
        // Arrange
        var json = """{"in": ["Pete", ["John", "Paul", "George", "Ringo"]]}""";
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
    public void InArray_NumberPresent_ReturnsTrue()
    {
        // Arrange
        var json = """{"in": [3, [1, 2, 3, 4, 5]]}""";
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
    public void InArray_NumberNotPresent_ReturnsFalse()
    {
        // Arrange
        var json = """{"in": [6, [1, 2, 3, 4, 5]]}""";
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
    public void InArray_EmptyArray_ReturnsFalse()
    {
        // Arrange
        var json = """{"in": [1, []]}""";
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
    public void In_WrongOutputType_Fails()
    {
        // Arrange - 'in' returns bool, but requesting string
        var json = """{ "in": [3, [1, 2, 3, 4, 5]]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, string>(doc, out _));
    }}
