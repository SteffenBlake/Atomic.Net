using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for 'addRange' operator (concatenate arrays).
/// </summary>
[Collection("NonParallel")]
public sealed class JsonExpressionLinqAddRangeTests(ITestOutputHelper output) : IDisposable
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
    public void Merge_TwoArrays_ReturnsCombined()
    {
        // Arrange
        var json = """{"addRange": [[1, 2], [3, 4]]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, int[]>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal([1, 2, 3, 4], result);
    }

    [Fact]
    public void Merge_ThreeArrays_ReturnsCombined()
    {
        // Arrange - addRange takes exactly 2 arrays
        var json = """{"addRange": [[1], [2, 3], [4, 5, 6]]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert - should fail to compile (takes only 2 arrays, not 3)
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, int[]>(doc, out _));
    }

    [Fact]
    public void Merge_NonArraysGetCast_ReturnsCombined()
    {
        // Arrange - C# does not auto-wrap scalars as arrays
        var json = """{"addRange": [1, 2, [3, 4]]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert - should fail to compile
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, int[]>(doc, out _));
    }

    [Fact]
    public void Merge_EmptyArrays_ReturnsEmpty()
    {
        // Arrange
        var json = """{"addRange": [[], []]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, int[]>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Merge_SingleArray_ReturnsSame()
    {
        // Arrange - addRange requires 2 arrays
        var json = """{"addRange": [[1, 2, 3]]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert - should fail to compile (needs 2 arrays)
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, int[]>(doc, out _));
    }
    [Fact]
    public void Append_WrongOutputType_Fails()
    {
        // Arrange - addRange returns int[], but requesting bool
        var json = """{ "addRange": [[1, 2], [3, 4]]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, bool>(doc, out _));
    }}
