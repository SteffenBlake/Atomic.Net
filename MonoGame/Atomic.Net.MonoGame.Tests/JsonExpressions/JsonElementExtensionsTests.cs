using System;
using System.Text.Json;
using Xunit;
using Atomic.Net.MonoGame.JsonExpressions;

namespace Atomic.Net.MonoGame.Tests.JsonExpressions;

/// <summary>
/// Tests for JsonElement type inference extension methods.
/// </summary>
public sealed class JsonElementExtensionsTests
{
    [Fact]
    public void InferType_NumberLiteral_ReturnsFloat()
    {
        // Arrange
        var json = "42";
        var doc = JsonDocument.Parse(json);

        // Act
        var type = doc.RootElement.InferJsonExpressionType<object>();

        // Assert
        Assert.Equal(typeof(float), type);
    }

    [Fact]
    public void InferType_StringLiteral_ReturnsString()
    {
        // Arrange
        var json = "\"hello\"";
        var doc = JsonDocument.Parse(json);

        // Act
        var type = doc.RootElement.InferJsonExpressionType<object>();

        // Assert
        Assert.Equal(typeof(string), type);
    }

    [Fact]
    public void InferType_BoolLiteralTrue_ReturnsBool()
    {
        // Arrange
        var json = "true";
        var doc = JsonDocument.Parse(json);

        // Act
        var type = doc.RootElement.InferJsonExpressionType<object>();

        // Assert
        Assert.Equal(typeof(bool), type);
    }

    [Fact]
    public void InferType_BoolLiteralFalse_ReturnsBool()
    {
        // Arrange
        var json = "false";
        var doc = JsonDocument.Parse(json);

        // Act
        var type = doc.RootElement.InferJsonExpressionType<object>();

        // Assert
        Assert.Equal(typeof(bool), type);
    }

    [Fact]
    public void InferType_ArrayOfNumbers_ReturnsFloatArray()
    {
        // Arrange
        var json = "[1, 2, 3]";
        var doc = JsonDocument.Parse(json);

        // Act
        var type = doc.RootElement.InferJsonExpressionType<object>();

        // Assert
        Assert.Equal(typeof(float[]), type);
    }

    [Fact]
    public void InferType_ArrayOfStrings_ReturnsStringArray()
    {
        // Arrange
        var json = "[\"a\", \"b\", \"c\"]";
        var doc = JsonDocument.Parse(json);

        // Act
        var type = doc.RootElement.InferJsonExpressionType<object>();

        // Assert
        Assert.Equal(typeof(string[]), type);
    }

    [Fact]
    public void InferType_ArrayOfBools_ReturnsBoolArray()
    {
        // Arrange
        var json = "[true, false, true]";
        var doc = JsonDocument.Parse(json);

        // Act
        var type = doc.RootElement.InferJsonExpressionType<object>();

        // Assert
        Assert.Equal(typeof(bool[]), type);
    }

    [Fact]
    public void InferType_EmptyArray_ThrowsException()
    {
        // Arrange
        var json = "[]";
        var doc = JsonDocument.Parse(json);

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => doc.RootElement.InferJsonExpressionType<object>());
        Assert.Contains("empty array", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InferType_ComparisonGreaterThan_ReturnsBool()
    {
        // Arrange
        var json = """{">" : [5, 3]}""";
        var doc = JsonDocument.Parse(json);

        // Act
        var type = doc.RootElement.InferJsonExpressionType<object>();

        // Assert
        Assert.Equal(typeof(bool), type);
    }

    [Fact]
    public void InferType_ComparisonLessThan_ReturnsBool()
    {
        // Arrange
        var json = """{"<" : [3, 5]}""";
        var doc = JsonDocument.Parse(json);

        // Act
        var type = doc.RootElement.InferJsonExpressionType<object>();

        // Assert
        Assert.Equal(typeof(bool), type);
    }

    [Fact]
    public void InferType_ComparisonEquals_ReturnsBool()
    {
        // Arrange
        var json = """{"==" : [5, 5]}""";
        var doc = JsonDocument.Parse(json);

        // Act
        var type = doc.RootElement.InferJsonExpressionType<object>();

        // Assert
        Assert.Equal(typeof(bool), type);
    }

    [Fact]
    public void InferType_ComparisonNotEquals_ReturnsBool()
    {
        // Arrange
        var json = """{"!=" : [5, 3]}""";
        var doc = JsonDocument.Parse(json);

        // Act
        var type = doc.RootElement.InferJsonExpressionType<object>();

        // Assert
        Assert.Equal(typeof(bool), type);
    }

    [Fact]
    public void InferType_LogicalAnd_ReturnsBool()
    {
        // Arrange
        var json = """{"and" : [true, false]}""";
        var doc = JsonDocument.Parse(json);

        // Act
        var type = doc.RootElement.InferJsonExpressionType<object>();

        // Assert
        Assert.Equal(typeof(bool), type);
    }

    [Fact]
    public void InferType_LogicalOr_ReturnsBool()
    {
        // Arrange
        var json = """{"or" : [true, false]}""";
        var doc = JsonDocument.Parse(json);

        // Act
        var type = doc.RootElement.InferJsonExpressionType<object>();

        // Assert
        Assert.Equal(typeof(bool), type);
    }

    [Fact]
    public void InferType_LogicalNot_ReturnsBool()
    {
        // Arrange
        var json = """{"!" : [true]}""";
        var doc = JsonDocument.Parse(json);

        // Act
        var type = doc.RootElement.InferJsonExpressionType<object>();

        // Assert
        Assert.Equal(typeof(bool), type);
    }

    [Fact]
    public void InferType_MathAdd_InfersFromFirstOperand()
    {
        // Arrange - numbers
        var json = """{"+" : [1, 2, 3]}""";
        var doc = JsonDocument.Parse(json);

        // Act
        var type = doc.RootElement.InferJsonExpressionType<object>();

        // Assert
        Assert.Equal(typeof(float), type);
    }

    [Fact]
    public void InferType_MathAddStrings_InfersString()
    {
        // Arrange - string concatenation
        var json = """{"+" : ["hello", " ", "world"]}""";
        var doc = JsonDocument.Parse(json);

        // Act
        var type = doc.RootElement.InferJsonExpressionType<object>();

        // Assert
        Assert.Equal(typeof(string), type);
    }

    [Fact]
    public void InferType_MathSubtract_InfersFromFirstOperand()
    {
        // Arrange
        var json = """{"-" : [10, 3]}""";
        var doc = JsonDocument.Parse(json);

        // Act
        var type = doc.RootElement.InferJsonExpressionType<object>();

        // Assert
        Assert.Equal(typeof(float), type);
    }

    [Fact]
    public void InferType_MathMultiply_InfersFromFirstOperand()
    {
        // Arrange
        var json = """{"*" : [5, 3]}""";
        var doc = JsonDocument.Parse(json);

        // Act
        var type = doc.RootElement.InferJsonExpressionType<object>();

        // Assert
        Assert.Equal(typeof(float), type);
    }

    [Fact]
    public void InferType_MathDivide_InfersFromFirstOperand()
    {
        // Arrange
        var json = """{"/" : [10, 2]}""";
        var doc = JsonDocument.Parse(json);

        // Act
        var type = doc.RootElement.InferJsonExpressionType<object>();

        // Assert
        Assert.Equal(typeof(float), type);
    }

    [Fact]
    public void InferType_MathModulo_InfersFromFirstOperand()
    {
        // Arrange
        var json = """{"%" : [10, 3]}""";
        var doc = JsonDocument.Parse(json);

        // Act
        var type = doc.RootElement.InferJsonExpressionType<object>();

        // Assert
        Assert.Equal(typeof(float), type);
    }

    [Fact]
    public void InferType_Max_InfersFromFirstOperand()
    {
        // Arrange
        var json = """{"max" : [5, 10, 3]}""";
        var doc = JsonDocument.Parse(json);

        // Act
        var type = doc.RootElement.InferJsonExpressionType<object>();

        // Assert
        Assert.Equal(typeof(float), type);
    }

    [Fact]
    public void InferType_Min_InfersFromFirstOperand()
    {
        // Arrange
        var json = """{"min" : [5, 10, 3]}""";
        var doc = JsonDocument.Parse(json);

        // Act
        var type = doc.RootElement.InferJsonExpressionType<object>();

        // Assert
        Assert.Equal(typeof(float), type);
    }

    [Fact]
    public void InferType_IfOperator_InfersFromThenBranch()
    {
        // Arrange
        var json = """{"if" : [true, "yes", "no"]}""";
        var doc = JsonDocument.Parse(json);

        // Act
        var type = doc.RootElement.InferJsonExpressionType<object>();

        // Assert
        Assert.Equal(typeof(string), type);
    }

    [Fact]
    public void InferType_IfOperatorWithNumbers_InfersFloat()
    {
        // Arrange
        var json = """{"if" : [true, 42, 0]}""";
        var doc = JsonDocument.Parse(json);

        // Act
        var type = doc.RootElement.InferJsonExpressionType<object>();

        // Assert
        Assert.Equal(typeof(float), type);
    }

    [Fact]
    public void InferType_VarOperator_SimpleProperty_ThrowsForUnsupportedType()
    {
        // Arrange - string.Length returns int, which is not a supported type
        var json = """{"var" : "Length"}""";
        var doc = JsonDocument.Parse(json);

        // Act & Assert - should throw because int is not supported
        var ex = Assert.Throws<JsonException>(() => doc.RootElement.InferJsonExpressionType<string>());
        Assert.Contains("Int32", ex.Message);
        Assert.Contains("not supported", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InferType_VarOperator_EmptyString_ReturnsInputType()
    {
        // Arrange - empty string returns entire input
        var json = """{"var" : ""}""";
        var doc = JsonDocument.Parse(json);

        // Act
        var type = doc.RootElement.InferJsonExpressionType<string>();

        // Assert
        Assert.Equal(typeof(string), type);
    }

    [Fact]
    public void InferType_VarOperator_InvalidProperty_ThrowsException()
    {
        // Arrange
        var json = """{"var" : "NonExistentProperty"}""";
        var doc = JsonDocument.Parse(json);

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => doc.RootElement.InferJsonExpressionType<string>());
        Assert.Contains("Property", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("NonExistentProperty", ex.Message);
    }

    [Fact]
    public void InferType_VarOperator_ArrayIndex_ReturnsElementType()
    {
        // Arrange - numeric index for arrays with supported element type
        var json = """{"var" : 0}""";
        var doc = JsonDocument.Parse(json);

        // Act - float[] has float elements (supported type)
        var type = doc.RootElement.InferJsonExpressionType<float[]>();

        // Assert
        Assert.Equal(typeof(float), type);
    }

    [Fact]
    public void InferType_SelectOperator_ReturnsArrayOfSelectorType()
    {
        // Arrange - select returns numbers (multiply literal by 2)
        var json = """{"select" : [[1, 2, 3], {"*" : [5, 2]}]}""";
        var doc = JsonDocument.Parse(json);

        // Act
        var type = doc.RootElement.InferJsonExpressionType<object>();

        // Assert
        Assert.Equal(typeof(float[]), type);
    }

    [Fact]
    public void InferType_SelectOperatorWithVar_ReturnsArrayOfSelectorType()
    {
        // Arrange - select with var operator, needs inputType
        var json = """{"select" : [[1, 2, 3], {"*" : [{"var" : ""}, 2]}]}""";
        var doc = JsonDocument.Parse(json);

        // Act - pass float as inputType for var
        var type = doc.RootElement.InferJsonExpressionType<float>();

        // Assert
        Assert.Equal(typeof(float[]), type);
    }

    [Fact]
    public void InferType_WhereOperator_ReturnsSourceArrayType()
    {
        // Arrange
        var json = """{"where" : [[1, 2, 3], {">" : [{"var" : ""}, 1]}]}""";
        var doc = JsonDocument.Parse(json);

        // Act
        var type = doc.RootElement.InferJsonExpressionType<float>();

        // Assert
        Assert.Equal(typeof(float[]), type);
    }

    [Fact]
    public void InferType_AllOperator_ReturnsBool()
    {
        // Arrange
        var json = """{"all" : [[1, 2, 3], {">" : [{"var" : ""}, 0]}]}""";
        var doc = JsonDocument.Parse(json);

        // Act
        var type = doc.RootElement.InferJsonExpressionType<object>();

        // Assert
        Assert.Equal(typeof(bool), type);
    }

    [Fact]
    public void InferType_AnyOperator_ReturnsBool()
    {
        // Arrange
        var json = """{"any" : [[1, 2, 3], {">" : [{"var" : ""}, 5]}]}""";
        var doc = JsonDocument.Parse(json);

        // Act
        var type = doc.RootElement.InferJsonExpressionType<object>();

        // Assert
        Assert.Equal(typeof(bool), type);
    }

    [Fact]
    public void InferType_NoneOperator_ReturnsBool()
    {
        // Arrange
        var json = """{"none" : [[1, 2, 3], {"<" : [{"var" : ""}, 0]}]}""";
        var doc = JsonDocument.Parse(json);

        // Act
        var type = doc.RootElement.InferJsonExpressionType<object>();

        // Assert
        Assert.Equal(typeof(bool), type);
    }

    [Fact]
    public void InferType_ContainsOperator_ReturnsBool()
    {
        // Arrange
        var json = """{"contains" : [[1, 2, 3], 2]}""";
        var doc = JsonDocument.Parse(json);

        // Act
        var type = doc.RootElement.InferJsonExpressionType<object>();

        // Assert
        Assert.Equal(typeof(bool), type);
    }

    [Fact]
    public void InferType_StringContainsOperator_ReturnsBool()
    {
        // Arrange
        var json = """{"stringContains" : ["hello world", "world"]}""";
        var doc = JsonDocument.Parse(json);

        // Act
        var type = doc.RootElement.InferJsonExpressionType<object>();

        // Assert
        Assert.Equal(typeof(bool), type);
    }

    [Fact]
    public void InferType_SubstringOperator_ReturnsString()
    {
        // Arrange
        var json = """{"substring" : ["hello world", 0, 5]}""";
        var doc = JsonDocument.Parse(json);

        // Act
        var type = doc.RootElement.InferJsonExpressionType<object>();

        // Assert
        Assert.Equal(typeof(string), type);
    }

    [Fact]
    public void InferType_AppendOperator_ReturnsArrayType()
    {
        // Arrange
        var json = """{"append" : [[1, 2], [3, 4]]}""";
        var doc = JsonDocument.Parse(json);

        // Act
        var type = doc.RootElement.InferJsonExpressionType<object>();

        // Assert
        Assert.Equal(typeof(float[]), type);
    }

    [Fact]
    public void InferType_AggregateOperator_InfersFromInitialValue()
    {
        // Arrange - initial value is 0 (number)
        var json = """{"aggregate" : [[1, 2, 3], {"+" : [{"var" : ""}, {"var" : ""}]}, 0]}""";
        var doc = JsonDocument.Parse(json);

        // Act
        var type = doc.RootElement.InferJsonExpressionType<object>();

        // Assert
        Assert.Equal(typeof(float), type);
    }

    [Fact]
    public void InferType_LogOperator_PassesThroughValueType()
    {
        // Arrange
        var json = """{"log" : "hello"}""";
        var doc = JsonDocument.Parse(json);

        // Act
        var type = doc.RootElement.InferJsonExpressionType<object>();

        // Assert
        Assert.Equal(typeof(string), type);
    }

    [Fact]
    public void InferType_NestedExpression_InfersCorrectly()
    {
        // Arrange - nested: if(true, "yes", "no") where then branch is string
        var json = """{"if" : [{">" : [5, 3]}, "yes", "no"]}""";
        var doc = JsonDocument.Parse(json);

        // Act
        var type = doc.RootElement.InferJsonExpressionType<object>();

        // Assert
        Assert.Equal(typeof(string), type);
    }

    [Fact]
    public void InferType_UnknownOperator_ThrowsException()
    {
        // Arrange
        var json = """{"unknownOp" : [1, 2]}""";
        var doc = JsonDocument.Parse(json);

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => doc.RootElement.InferJsonExpressionType<object>());
        Assert.Contains("unknown", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("unknownOp", ex.Message);
    }
}
