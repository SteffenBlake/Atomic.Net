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
public sealed class JsonExpressionVarTests : IDisposable
{
    private readonly record struct TestInput(int A, int B, string Name);
    private readonly record struct NestedInput(string Name, ChildData Child);
    private readonly record struct ChildData(int Value, string Label);

    private readonly ErrorEventLogger _errorLogger;
    private readonly FakeEventListener<ErrorEvent> _errorListener;

    public JsonExpressionVarTests(ITestOutputHelper output)
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
    public void Var_SimpleProperty_ReturnsValue()
    {
        // Arrange
        var json = """{"var": "A"}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpression.TryCompile<TestInput, int>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(42, 100, "test");

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public void Var_PropertyWithArray_ReturnsValue()
    {
        // Arrange
        var json = """{"var": ["B"]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpression.TryCompile<TestInput, int>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(42, 100, "test");

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(100, result);
    }

    [Fact]
    public void Var_WithDefault_MissingProperty_ReturnsDefault()
    {
        // Arrange
        var json = """{"var": ["Z", 999]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpression.TryCompile<TestInput, int>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(42, 100, "test");

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(999, result);
    }

    [Fact]
    public void Var_DotNotation_ReturnsNestedValue()
    {
        // Arrange
        var json = """{"var": "Child.Value"}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpression.TryCompile<NestedInput, int>(doc, out var expr));
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
        Assert.True(JsonExpression.TryCompile<TestInput, TestInput>(doc, out var expr));
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
        Assert.True(JsonExpression.TryCompile<int[], int>(doc, out var expr));
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
        Assert.True(JsonExpression.TryCompile<TestInput, int?>(doc, out var expr));

        // Act
        var result = expr;

        // Assert
        Assert.Null(result);
        Assert.True(_errorListener.ReceivedEvents.Count > 0, "Should fire at least one ErrorEvent for invalid property");
    }
    [Fact]
    public void Var_WrongOutputType_Fails()
    {
        // Arrange - var returns int, but requesting string
        var json = """{ "var": "A"}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert
        Assert.False(JsonExpression.TryCompile<TestInput, string>(doc, out _));
    }}
