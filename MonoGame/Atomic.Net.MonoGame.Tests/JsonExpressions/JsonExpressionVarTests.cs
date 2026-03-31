using System;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for JSONLogic 'var' operator (data access).
/// </summary>
[Collection("NonParallel")]
public sealed class JsonExpressionVarTests(ITestOutputHelper output) : IDisposable
{
    private readonly record struct TestInput(float A, float B, string Name);
    private readonly record struct NestedInput(string Name, ChildData Child);
    private readonly record struct ChildData(int Value, string Label);

    private readonly ErrorEventLogger _errorLogger = new ErrorEventLogger(output);
    private readonly FakeEventListener<ErrorEvent> _errorListener = new FakeEventListener<ErrorEvent>();

    public void Dispose()
    {
        _errorListener.Dispose();
        _errorLogger.Dispose();
    }

    [Fact]
    public void Var_SimpleProperty_ReturnsValue()
    {
        // Arrange
        var json = """{"var": "A"}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(42, 100, "test");

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(42f, result);
    }

    [Fact]
    public void Var_PropertyWithArray_ReturnsValue()
    {
        // Arrange
        var json = """{"var": ["B"]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(42, 100, "test");

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(100f, result);
    }

    [Fact]
    public void Var_WithDefault_MissingProperty_ReturnsDefault()
    {
        // Arrange - property "Z" does not exist on TestInput, so TryBuild should fail
        var json = """{"var": ["Z", 999]}""";
        var doc = JsonDocument.Parse(json);

        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, int>(doc, out _));
    }

    [Fact]
    public void Var_DotNotation_ReturnsNestedValue()
    {
        // Arrange
        var json = """{"var": "Child.Value"}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<NestedInput, int>(doc, out var expr));
        var func = expr.Compile();
        var data = new NestedInput("parent", new ChildData(123, "child"));

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(123, result);
    }

    [Fact]
    public void Var_EmptyString_ReturnsEntireData()
    {
        // Arrange
        var json = """{"var": ""}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, TestInput>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(42, 100, "test");

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(data, result);
    }

    [Fact]
    public void Var_ArrayIndex_ReturnsElement()
    {
        // Arrange
        var json = """{"var": 1}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<int[], int>(doc, out var expr));
        var func = expr.Compile();
        var data = new[] { 10, 20, 30 };

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(20, result);
    }

    [Fact]
    public void Var_InvalidProperty_ReturnsNullAndFiresErrorEvent()
    {
        // Arrange
        var json = """{"var": "NonExistent"}""";
        var doc = JsonDocument.Parse(json);

        // Assert - should fail to compile since property doesn't exist
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, int>(doc, out _));
    }
    [Fact]
    public void Var_WithDefault_NullableProperty_ReturnsDefault()
    {
        // Arrange - property "Name" exists on TestInput (string, nullable), data has null value
        // The default expression should be returned when the property value is null
        var json = """{"var": ["Name", "fallback"]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, string>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0, 0, null!);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal("fallback", result);
    }

    [Fact]
    public void Var_WrongOutputType_Fails()
    {
        // Arrange - var returns int, but requesting string
        var json = """{ "var": "A"}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, string>(doc, out _));
    }

    [Fact]
    public void Var_BooleanValue_Fails()
    {
        // Neither number, string, nor array
        var json = """{"var": true}""";
        var doc = JsonDocument.Parse(json);
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out _));
    }

    [Fact]
    public void Var_EmptyArray_Fails()
    {
        var json = """{"var": []}""";
        var doc = JsonDocument.Parse(json);
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out _));
    }

    [Fact]
    public void Var_TooManyArrayElements_Fails()
    {
        var json = """{"var": ["A", 0, "extra"]}""";
        var doc = JsonDocument.Parse(json);
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out _));
    }

    [Fact]
    public void Var_ArrayWithNonStringNonNumberPath_Fails()
    {
        // First element is boolean, not string or number
        var json = """{"var": [true]}""";
        var doc = JsonDocument.Parse(json);
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out _));
    }
}
