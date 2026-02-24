using System.Linq;
using ViciOne.Ui.ClusterEditor.Models.Comparer;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests;

public class AlphaNumericComparerTests
{
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
