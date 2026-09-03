using AwesomeAssertions;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Models.DiagramModels;

public class BlockNodeConnectorTests
{
    [Fact]
    public void Markers_are_created_with_their_matching_type()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.SetupDiagramService();
        ctx.CreateDiagramInstance();

        // Act
        using var connector = ctx.CreateBlockNodeConnector();

        // Assert
        connector.DataPortConnectorMarker.Type.Should().Be(ConnectorMarkerType.DataPort);
        connector.PublishedConnectorMarker.Type.Should().Be(ConnectorMarkerType.Published);
    }
}
