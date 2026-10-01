using AwesomeAssertions;
using ViciOne.Ui.ClusterEditor.Models;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Models;

public class LinkDestinationSearchFilterTests
{
    /// <summary>
    /// A row that is neither a connector nor a data port never matches.
    /// </summary>
    [Fact]
    public void Matches_rejects_a_row_type_it_does_not_recognise()
    {
        // Arrange
        var filter = new LinkDestinationSearchFilter("anything");

        // Act
        var matches = filter.Matches(new object());

        // Assert
        matches.Should().BeFalse();
    }
}
