using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for 'unshift' operator (prepends a single value to the start of an array).
/// </summary>
[Collection("NonParallel")]
public sealed class JsonExpressionLinqUnshiftTests(ITestOutputHelper output) : IDisposable
{
    private readonly record struct TestInput(float Value);

    private readonly ErrorEventLogger _errorLogger = new(output);

    public void Dispose()
    {
        _errorLogger.Dispose();
    }

    [Fact]
    public void Unshift_ValueToArray_ReturnsArrayWithValueAtStart()
    {
        // Arrange
        var json = """{"unshift": [5, [1, 2, 3, 4]]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, float[]>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal([5f, 1f, 2f, 3f, 4f], result);
    }

    [Fact]
    public void Unshift_ValueToEmptyArray_ReturnsArrayWithSingleValue()
    {
        // Arrange
        var json = """{"unshift": [42, []]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, float[]>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal([42f], result);
    }

    [Fact]
    public void Unshift_ValueWithVarData_ReturnsArrayWithValue()
    {
        // Arrange
        var json = """{"unshift": [{"var": "Value"}, [10, 20, 30]]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, float[]>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(99);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal([99f, 10f, 20f, 30f], result);
    }

    [Fact]
    public void Unshift_StringToStringArray_ReturnsArrayWithString()
    {
        // Arrange
        var json = """{"unshift": ["Hello", ["World"]]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, string[]>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(["Hello", "World"], result);
    }

    [Fact]
    public void Unshift_CompareWithPush_ReturnsCorrectOrder()
    {
        // Arrange - push puts item at end, unshift at start
        var pushJson = """{"push": [5, [1, 2, 3]]}""";
        var unshiftJson = """{"unshift": [5, [1, 2, 3]]}""";
        
        var pushDoc = JsonDocument.Parse(pushJson);
        var unshiftDoc = JsonDocument.Parse(unshiftJson);
        
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, float[]>(pushDoc, out var pushExpr));
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, float[]>(unshiftDoc, out var unshiftExpr));
        
        var pushFunc = pushExpr.Compile();
        var unshiftFunc = unshiftExpr.Compile();
        var data = new TestInput(0);

        // Act
        var pushResult = pushFunc(data);
        var unshiftResult = unshiftFunc(data);

        // Assert
        Assert.Equal([1f, 2f, 3f, 5f], pushResult);  // push adds to end
        Assert.Equal([5f, 1f, 2f, 3f], unshiftResult);  // unshift adds to start
    }

    [Fact]
    public void Unshift_TwoNonArrayValues_Fails()
    {
        // Arrange - unshift requires [value, array], not [value, value]
        var json = """{"unshift": [1, 2]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert - should fail to compile
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, int[]>(doc, out _));
    }

    [Fact]
    public void Unshift_TwoArrays_Fails()
    {
        // Arrange - unshift requires [value, array], not [array, array] (use addRange for that)
        var json = """{"unshift": [[1, 2], [3, 4]]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert - should fail to compile
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, int[]>(doc, out _));
    }

    [Fact]
    public void Unshift_ThreeArguments_Fails()
    {
        // Arrange - unshift takes exactly 2 arguments
        var json = """{"unshift": [1, [2, 3], [4, 5]]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert - should fail to compile
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, int[]>(doc, out _));
    }

    [Fact]
    public void Unshift_SingleArgument_Fails()
    {
        // Arrange - unshift requires 2 arguments
        var json = """{"unshift": [[1, 2, 3]]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert - should fail to compile
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, int[]>(doc, out _));
    }

    [Fact]
    public void Unshift_WrongOrder_ArrayThenValue_Fails()
    {
        // Arrange - unshift expects [value, array], not [array, value]
        var json = """{"unshift": [[1, 2, 3], 4]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert - should fail to compile (first arg must be scalar, second must be array)
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, int[]>(doc, out _));
    }
    
    [Fact]
    public void LinqUnshift_WrongOutputType_Fails()
    {
        // Arrange - unshift returns int[], but requesting string
        var json = """{"unshift": [5, [1, 2, 3]]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, string>(doc, out _));
    }

    [Fact]
    public void LinqUnshift_NotAnArray_Fails()
    {
        // Arrange - value of 'unshift' must be an array, not a string
        var json = """{"unshift": "not-an-array"}""";
        var doc = JsonDocument.Parse(json);

        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, float[]>(doc, out _));
    }
}
