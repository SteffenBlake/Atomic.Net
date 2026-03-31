using System;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for JSONLogic 'merge' operator (flatten arrays).
/// </summary>
public sealed class JsonExpressionMergeTests : IDisposable
{
    private readonly record struct TestInput(int Unused);

    private readonly ErrorEventLogger _errorLogger;
    private readonly FakeEventListener<ErrorEvent> _errorListener;

    public JsonExpressionMergeTests(ITestOutputHelper output)
    {
        _errorLogger = new ErrorEventLogger(output);
        _errorListener = new FakeEventListener<ErrorEvent>();
    }

    public void Dispose()
    {
        _errorListener.Dispose();
        _errorLogger.Dispose();
    }

    [Fact]
    public void Merge_TwoArrays_ReturnsCombined()
    {
        // Arrange
        var json = """{"merge": [[1, 2], [3, 4]]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, int[]>(doc);
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(new[] { 1, 2, 3, 4 }, result);
    }

    [Fact]
    public void Merge_ThreeArrays_ReturnsCombined()
    {
        // Arrange
        var json = """{"merge": [[1], [2, 3], [4, 5, 6]]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, int[]>(doc);
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6 }, result);
    }

    [Fact]
    public void Merge_NonArraysGetCast_ReturnsCombined()
    {
        // Arrange
        var json = """{"merge": [1, 2, [3, 4]]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, int[]>(doc);
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(new[] { 1, 2, 3, 4 }, result);
    }

    [Fact]
    public void Merge_EmptyArrays_ReturnsEmpty()
    {
        // Arrange
        var json = """{"merge": [[], []]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, int[]>(doc);
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
        // Arrange
        var json = """{"merge": [[1, 2, 3]]}""";
        var doc = JsonDocument.Parse(json);
        var expr = JsonExpression.Compile<TestInput, int[]>(doc);
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(new[] { 1, 2, 3 }, result);
    }
}
