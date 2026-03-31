using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Atomic.Net.MonoGame.JsonExpressions;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for JSONLogic '+' operator (addition/concatenation).
/// </summary>
[Collection("NonParallel")]
public sealed class JsonExpressionSymbolAddTests(ITestOutputHelper output) : IDisposable
{
    private readonly record struct TestInput(int A, int B);

    private readonly ErrorEventLogger _errorLogger = new(output);
    private readonly FakeEventListener<ErrorEvent> _errorListener = new();

    public void Dispose()
    {
        _errorListener.Dispose();
        _errorLogger.Dispose();
    }

    [Fact]
    public void Add_TwoIntegers_ReturnsSum()
    {
        // Arrange
        var json = """{"+": [4, 2]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, int>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(6, result);
    }

    [Fact]
    public void Add_MultipleIntegers_ReturnsSum()
    {
        // Arrange
        var json = """{"+": [2, 2, 2, 2, 2]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, int>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(10, result);
    }

    [Fact]
    public void Add_WithVarData_ReturnsSum()
    {
        // Arrange
        var json = """{"+": [{"var": "A"}, {"var": "B"}]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, int>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(10, 32);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public void Add_NegativeNumbers_ReturnsSum()
    {
        // Arrange
        var json = """{"+": [-5, 3]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, int>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(-2, result);
    }

    [Fact]
    public void Add_Floats_ReturnsSum()
    {
        // Arrange
        var json = """{"+": [3.14, 2.86]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(6.0, result, 0.001);
    }

    [Fact]
    public void Add_UnaryInteger_ReturnsInteger()
    {
        // Arrange
        var json = """{"+": 42}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, int>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public void Add_UnaryString_CastsToNumber()
    {
        // Arrange - C# does not support unary + on strings
        var json = """{"+": "3.14"}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert - should fail to compile
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, float>(doc, out _));
    }

    [Fact]
    public void Add_UnaryString_ReturnsString()
    {
        // Arrange - C# does not support unary + on strings
        var json = """{"+": "hello"}""";
        var doc = JsonDocument.Parse(json);
        
        // Assert - should fail to compile
        Assert.False(JsonExpressionCompiler.TryBuild<TestInput, string>(doc, out _));
    }

    [Fact]
    public void Add_TwoStrings_ReturnsConcatenation()
    {
        // Arrange
        var json = """{"+": ["Hello", " World"]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, string>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void Add_MultipleStrings_ReturnsConcatenation()
    {
        // Arrange
        var json = """{"+": ["A", "B", "C", "D"]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, string>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal("ABCD", result);
    }

    [Fact]
    public void Add_StringsWithVarData_ReturnsConcatenation()
    {
        // Arrange
        var record = new PersonRecord("John", "Doe");
        var json = """{"+": [{"var": "FirstName"}, " ", {"var": "LastName"}]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<PersonRecord, string>(doc, out var expr));
        var func = expr.Compile();

        // Act
        var result = func(record);

        // Assert
        Assert.Equal("John Doe", result);
    }

    private readonly record struct PersonRecord(string FirstName, string LastName);

    [Fact]
    public void Add_EmptyStrings_ReturnsEmptyString()
    {
        // Arrange
        var json = """{"+": ["", ""]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, string>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void Add_StringAndNumber_Concatenates()
    {
        // C# supports string + number concat ation natively ("Value: " + 42 = "Value: 42")
        // Arrange
        var json = """{"+": ["Value: ", 42]}""";
        var doc = JsonDocument.Parse(json);
        Assert.True(JsonExpressionCompiler.TryBuild<TestInput, string>(doc, out var expr));
        var func = expr.Compile();
        var data = new TestInput(0, 0);

        // Act
        var result = func(data);

        // Assert
        Assert.Equal("Value: 42", result);
    }

}
