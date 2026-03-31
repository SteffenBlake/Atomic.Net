using System;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for 'selectMany' operator (projects and flattens arrays).
/// </summary>
[Collection("NonParallel")]
public sealed class JsonExpressionSelectManyTests : IDisposable
{
    private readonly record struct Team(string Name, string[] Members);
    private readonly record struct TestData(Team[] Teams);
    
    private readonly record struct Person(string Name, int[] Scores);
    private readonly record struct ScoreData(Person[] People);

    private readonly ErrorEventLogger _errorLogger;
    private readonly FakeEventListener<ErrorEvent> _errorListener;

    public JsonExpressionSelectManyTests(ITestOutputHelper output)
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
    public void SelectMany_FlattenNestedArrays_ReturnsFlattened()
    {
        // Arrange
        var json = """{"selectMany": [{"var": "Teams"}, {"var": "Members"}]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpression.TryCompile<TestData, string[]>(doc, out var expr));
        var func = expr.Compile();
        
        var data = new TestData(new[]
        {
            new Team("Alpha", new[] { "Alice", "Bob" }),
            new Team("Beta", new[] { "Charlie", "Dana" }),
            new Team("Gamma", new[] { "Eve" })
        });

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(new[] { "Alice", "Bob", "Charlie", "Dana", "Eve" }, result);
    }

    [Fact]
    public void SelectMany_FlattenIntegers_ReturnsFlattened()
    {
        // Arrange
        var json = """{"selectMany": [{"var": "People"}, {"var": "Scores"}]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpression.TryCompile<ScoreData, int[]>(doc, out var expr));
        var func = expr.Compile();
        
        var data = new ScoreData(new[]
        {
            new Person("Alice", new[] { 90, 85, 92 }),
            new Person("Bob", new[] { 78, 88 }),
            new Person("Charlie", new[] { 95 })
        });

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(new[] { 90, 85, 92, 78, 88, 95 }, result);
    }

    [Fact]
    public void SelectMany_EmptySource_ReturnsEmpty()
    {
        // Arrange
        var json = """{"selectMany": [{"var": "Teams"}, {"var": "Members"}]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpression.TryCompile<TestData, string[]>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestData(Array.Empty<Team>());

        // Act
        var result = func(data);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void SelectMany_SomeEmptyArrays_SkipsThem()
    {
        // Arrange
        var json = """{"selectMany": [{"var": "Teams"}, {"var": "Members"}]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpression.TryCompile<TestData, string[]>(doc, out var expr));
        var func = expr.Compile();
        
        var data = new TestData(new[]
        {
            new Team("Alpha", new[] { "Alice" }),
            new Team("Beta", Array.Empty<string>()),
            new Team("Gamma", new[] { "Bob", "Charlie" })
        });

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(new[] { "Alice", "Bob", "Charlie" }, result);
    }

    [Fact]
    public void SelectMany_AllEmptyArrays_ReturnsEmpty()
    {
        // Arrange
        var json = """{"selectMany": [{"var": "Teams"}, {"var": "Members"}]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpression.TryCompile<TestData, string[]>(doc, out var expr));
        var func = expr.Compile();
        
        var data = new TestData(new[]
        {
            new Team("Alpha", Array.Empty<string>()),
            new Team("Beta", Array.Empty<string>())
        });

        // Act
        var result = func(data);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void SelectMany_WithTransform_AppliesBeforeFlattening()
    {
        // Arrange - multiply each score by 2, then flatten
        var json = """{"selectMany": [{"var": "People"}, {"map": [{"var": "Scores"}, {"*": [{"var": ""}, 2]}]}]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpression.TryCompile<ScoreData, int[]>(doc, out var expr));
        var func = expr.Compile();
        
        var data = new ScoreData(new[]
        {
            new Person("Alice", new[] { 10, 20 }),
            new Person("Bob", new[] { 30 })
        });

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(new[] { 20, 40, 60 }, result);
    }

    [Fact]
    public void SelectMany_SingleElementArrays_ReturnsFlattened()
    {
        // Arrange
        var json = """{"selectMany": [{"var": "Teams"}, {"var": "Members"}]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpression.TryCompile<TestData, string[]>(doc, out var expr));
        var func = expr.Compile();
        
        var data = new TestData(new[]
        {
            new Team("Alpha", new[] { "Alice" }),
            new Team("Beta", new[] { "Bob" }),
            new Team("Gamma", new[] { "Charlie" })
        });

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(new[] { "Alice", "Bob", "Charlie" }, result);
    }

    // Bad path tests

    [Fact]
    public void SelectMany_MissingArguments_Fails()
    {
        // Arrange - selectMany requires 2 arguments
        var json = """{"selectMany": [{"var": "Teams"}]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert
        Assert.False(JsonExpression.TryCompile<TestData, string[]>(doc, out _));
    }

    [Fact]
    public void SelectMany_NoArguments_Fails()
    {
        // Arrange
        var json = """{"selectMany": []}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert
        Assert.False(JsonExpression.TryCompile<TestData, string[]>(doc, out _));
    }

    [Fact]
    public void SelectMany_TooManyArguments_Fails()
    {
        // Arrange - selectMany takes exactly 2 arguments
        var json = """{"selectMany": [{"var": "Teams"}, {"var": "Members"}, {"var": "Extra"}]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert
        Assert.False(JsonExpression.TryCompile<TestData, string[]>(doc, out _));
    }

    [Fact]
    public void SelectMany_FirstArgNotArray_Fails()
    {
        // Arrange
        var json = """{"selectMany": ["not-an-array", {"var": ""}]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert
        Assert.False(JsonExpression.TryCompile<TestData, string[]>(doc, out _));
    }

    [Fact]
    public void SelectMany_SelectorNotReturningArray_Fails()
    {
        // Arrange - Name is string, not array
        var json = """{"selectMany": [{"var": "Teams"}, {"var": "Name"}]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert - should fail because selector must return array type
        Assert.False(JsonExpression.TryCompile<TestData, string[]>(doc, out _));
    }

    [Fact]
    public void SelectMany_WrongOutputType_Fails()
    {
        // Arrange - trying to compile as int[] but Members is string[]
        var json = """{"selectMany": [{"var": "Teams"}, {"var": "Members"}]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert
        Assert.False(JsonExpression.TryCompile<TestData, int[]>(doc, out _));
    }

    [Fact]
    public void SelectMany_InvalidSelectorExpression_Fails()
    {
        // Arrange - var expects string path, not object
        var json = """{"selectMany": [{"var": "Teams"}, {"var": {}}]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert
        Assert.False(JsonExpression.TryCompile<TestData, string[]>(doc, out _));
    }

    [Fact]
    public void SelectMany_NonExistentProperty_Fails()
    {
        // Arrange - "DoesNotExist" property doesn't exist
        var json = """{"selectMany": [{"var": "Teams"}, {"var": "DoesNotExist"}]}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert
        Assert.False(JsonExpression.TryCompile<TestData, string[]>(doc, out _));
    }
}
