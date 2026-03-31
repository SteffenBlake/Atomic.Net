using System;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for 'add' operator (adds a single value to an array).
/// </summary>
[Collection("NonParallel")]
public sealed class JsonExpressionLinqAddTests : IDisposable
{
    private readonly record struct TestInput(int Value);

    private readonly ErrorEventLogger _errorLogger;
    private readonly FakeEventListener<ErrorEvent> _errorListener;

    public JsonExpressionLinqAddTests(ITestOutputHelper output)
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
    public void Add_ValueToArray_ReturnsArrayWithValue()
    {
        // Arrange
       var json = """{"add": [5, [1, 2, 3, 4]]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpression.TryCompile<TestInput, int[]>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, result);
    }

    [Fact]
    public void Add_ValueToEmptyArray_ReturnsArrayWithSingleValue()
    {
        // Arrange
        var json = """{"add": [42, []]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpression.TryCompile<TestInput, int[]>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(new[] { 42 }, result);
    }

    [Fact]
    public void Add_ValueWithVarData_ReturnsArrayWithValue()
    {
        // Arrange
        var json = """{"add": [{"var": "Value"}, [10, 20, 30]]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpression.TryCompile<TestInput, int[]>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(99);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(new[] { 10, 20, 30, 99 }, result);
    }

    [Fact]
    public void Add_StringToStringArray_ReturnsArrayWithString()
    {
        // Arrange
        var json = """{"add": ["World", ["Hello"]]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpression.TryCompile<TestInput, string[]>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(new[] { "Hello", "World" }, result);
    }

    [Fact]
    public void Add_TwoNonArrayValues_Fails()
    {
        // Arrange - add requires [value, array], not [value, value]
        var json = """{"add": [1, 2]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert - should fail to compile
        Assert.False(JsonExpression.TryCompile<TestInput, int[]>(doc, out _));
    }

    [Fact]
    public void Add_TwoArrays_Fails()
    {
        // Arrange - add requires [value, array], not [array, array] (use addRange for that)
        var json = """{"add": [[1, 2], [3, 4]]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert - should fail to compile
        Assert.False(JsonExpression.TryCompile<TestInput, int[]>(doc, out _));
    }

    [Fact]
    public void Add_ThreeArguments_Fails()
    {
        // Arrange - add takes exactly 2 arguments
        var json = """{"add": [1, [2, 3], [4, 5]]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert - should fail to compile
        Assert.False(JsonExpression.TryCompile<TestInput, int[]>(doc, out _));
    }

    [Fact]
    public void Add_SingleArgument_Fails()
    {
        // Arrange - add requires 2 arguments
        var json = """{"add": [[1, 2, 3]]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert - should fail to compile
        Assert.False(JsonExpression.TryCompile<TestInput, int[]>(doc, out _));
    }

    [Fact]
    public void Add_WrongOrder_ArrayThenValue_Fails()
    {
        // Arrange - add expects [value, array], not [array, value]
        var json = """{"add": [[1, 2, 3], 4]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert - should fail to compile (first arg must be scalar, second must be array)
        Assert.False(JsonExpression.TryCompile<TestInput, int[]>(doc, out _));
    }
    [Fact]
    public void LinqAdd_WrongOutputType_Fails()
    {
        // Arrange - add returns int[], but requesting string
        var json = """{ "add": [5, [1, 2, 3]]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert
        Assert.False(JsonExpression.TryCompile<TestInput, string>(doc, out _));
    }}
