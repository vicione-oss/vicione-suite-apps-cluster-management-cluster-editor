using ViciOne.Ui.ClusterEditor.Services.ComponentServices.PropertyValueEqualityComparer;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Services.ComponentServices.PropertyValueEqualityComparer;

public class AlphaNumericPropertyValueEqualityComparerTests
{
    private readonly AlphaNumericPropertyValueEqualityComparer _sut = new();

    [Fact]
    public void Equals_BothNull_ReturnsTrue()
    {
        // Arrange & Act
        var result = _sut.Equals(null, null);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("abc", "ABC")]
    [InlineData("abc", "abd")]
    [InlineData("d5", "d12")]
    [InlineData("", "abc")]
    public void Equals_DifferentValues_ReturnsFalse(string x, string y)
    {
        // Arrange & Act
        var result = _sut.Equals(x, y);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("abc", "abc")]
    [InlineData("ABC", "ABC")]
    [InlineData("d5", "d5")]
    [InlineData("", "")]
    public void Equals_EqualValues_ReturnsTrue(string x, string y)
    {
        // Arrange & Act
        var result = _sut.Equals(x, y);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(null, "abc")]
    [InlineData("abc", null)]
    public void Equals_OneNull_ReturnsFalse(string? x, string? y)
    {
        // Arrange & Act
        var result = _sut.Equals(x, y);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("abc", "ABC")]
    [InlineData("abc123", "Abc123")]
    public void GetHashCode_CaseSensitive_DifferentHashForDifferentCasings(string x, string y)
    {
        // Arrange & Act
        var hashX = _sut.GetHashCode(x);
        var hashY = _sut.GetHashCode(y);

        // Assert
        Assert.NotEqual(hashX, hashY);
    }
}
