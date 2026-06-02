using System;
using System.Linq;
using ViciOne.Ui.ClusterEditor.Models.Comparer;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests;

public class AlphaNumericComparerTests
{
    [Theory]
    [InlineData("ABC", "abc", -1)]
    [InlineData("Abc", "abc", -1)]
    [InlineData("ABc", "aBc", -1)]
    [InlineData("ABC", "ABc", -1)]
    [InlineData("abc", "ABC", 1)]
    [InlineData("abc", "Abc", 1)]
    [InlineData("aBc", "ABc", 1)]
    [InlineData("ABc", "ABC", 1)]
    [InlineData("abc", "abc", 0)]
    [InlineData("Abc", "Abc", 0)]
    [InlineData("ABc", "ABc", 0)]
    [InlineData("ABC", "ABC", 0)]
    public void CaseSensitiveComparer_DifferentCase_SortsOrdinally(string x, string y, int expected)
    {
        // Arrange & Act
        var result = Math.Sign(AlphaNumericCaseSensitiveComparer<string>.Default.Compare(x, y));

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void SortsCorrectly()
    {
        var items = new[] { "Test", "_Test", "ebb", "eCb", "con", "Dan", "daniel", "", "e1b", "d12", "Bar", "123", "d5", null, "eAb", };
        var sorted = items.Order(AlphaNumericComparer<string?>.Default);

        Assert.Collection(sorted,
            Assert.Null,
            (i) => Assert.Equal("", i),
            (i) => Assert.Equal("123", i),
            (i) => Assert.Equal("Bar", i),
            (i) => Assert.Equal("con", i),
            (i) => Assert.Equal("d5", i),
            (i) => Assert.Equal("d12", i),
            (i) => Assert.Equal("Dan", i),
            (i) => Assert.Equal("daniel", i),
            (i) => Assert.Equal("e1b", i),
            (i) => Assert.Equal("eAb", i),
            (i) => Assert.Equal("ebb", i),
            (i) => Assert.Equal("eCb", i),
            (i) => Assert.Equal("Test", i),
            (i) => Assert.Equal("_Test", i));
    }
}
