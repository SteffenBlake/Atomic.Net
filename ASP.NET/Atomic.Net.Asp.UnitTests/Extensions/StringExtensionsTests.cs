using Atomic.Net.Asp.Domain.Extensions;

namespace Atomic.Net.Asp.UnitTests.Extensions;

public class StringExtensionsTests
{
    [Fact]
    public void Sanitize_EmptyString_DoesNothing()
    {
        // Arrange
        var input = "";

        // Act
        var result = input.Sanitize();

        // Assert
        Assert.Equal("", result);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("12")]
    [InlineData("123")]
    [InlineData("1234")]
    public void Sanitize_SmallString_ReturnsAllCleaned(string input)
    {
        // Act
        var result = input.Sanitize();

        // Assert
        Assert.Equal("****", result);
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("1223445")]
    [InlineData("1234445")]
    [InlineData("12344445")]
    public void Sanitize_LargeString_CleansMiddle(string input)
    {
        // Act
        var result = input.Sanitize();

        // Assert
        Assert.Equal("1****5", result);
    }
}
