using System.Threading.Tasks;
using AwesomeAssertions;
using Bunit;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Models.DiagramModels;

public class BlockNodeConnectorTests
{
    [Fact]
    public async Task Markers_are_created_with_their_matching_type()
    {
        // Arrange
        await using var ctx = new BunitContext();
        ctx.SetupDiagramService();
        ctx.CreateDiagramInstance();

        // Act
        using var connector = ctx.CreateBlockNodeConnector();

        // Assert
        connector.DataPortConnectorMarker.Type.Should().Be(ConnectorMarkerType.DataPort);
        connector.PublishedConnectorMarker.Type.Should().Be(ConnectorMarkerType.Published);
    }
}
