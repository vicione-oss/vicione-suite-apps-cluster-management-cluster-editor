using AwesomeAssertions;
using NSubstitute;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Models;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.PublishedConnectors.Models;

public class PublishedConnectorDirectionFilterTests
{
    [Fact]
    public void Matches_keeps_the_inputs_when_filtering_for_inputs()
    {
        // Arrange
        var sut = new PublishedConnectorDirectionFilter(ConnectorDirection.Input);
        var kept = Wrapper(isInput: true);
        var dropped = Wrapper(isInput: false);

        // Act
        var keptMatches = sut.Matches(kept);
        var droppedMatches = sut.Matches(dropped);

        // Assert
        keptMatches.Should().BeTrue();
        droppedMatches.Should().BeFalse();
    }

    [Fact]
    public void Matches_keeps_the_outputs_when_filtering_for_outputs()
    {
        // Arrange
        var sut = new PublishedConnectorDirectionFilter(ConnectorDirection.Output);
        var kept = Wrapper(isInput: false);
        var dropped = Wrapper(isInput: true);

        // Act
        var keptMatches = sut.Matches(kept);
        var droppedMatches = sut.Matches(dropped);

        // Assert
        keptMatches.Should().BeTrue();
        droppedMatches.Should().BeFalse();
    }

    // ConnectorDesign is abstract with an inaccessible member and no public implementation outside its own
    // assembly. The wrapper reads its direction off the connector, so neither design is read.
    private static DataGridConnectorWrapper Wrapper(bool isInput)
    {
        var connector = isInput
            ? (IConnector)Substitute.For<IConnectorInput>()
            : Substitute.For<IConnectorOutput>();

        return new(connector, null!, null!);
    }
}
