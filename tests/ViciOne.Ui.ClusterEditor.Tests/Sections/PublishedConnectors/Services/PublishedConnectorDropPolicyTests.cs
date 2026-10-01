using System.Collections.Generic;
using System.Threading.Tasks;
using AwesomeAssertions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.Draggable.Components;
using ViciOne.Ui.Blazor.Components.Tables.Shared.Models;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Services;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.PublishedConnectors.Services;

public class PublishedConnectorDropPolicyTests
{
    [Fact]
    public async Task Accepts_rejects_a_draggable_that_is_not_a_published_connector_row_set()
    {
        // Arrange
        await using var ctx = CreateContext();
        using var connector = ctx.CreateBlockNodeConnector();
        await using var datastore = Substitute.For<IDatastore>();
        using var service = CreateService(ctx, datastore);
        var sut = new PublishedConnectorDropPolicy(service);

        // Act
        var accepted = sut.Accepts(Substitute.For<IDraggable>(), connector);

        // Assert
        accepted.Should().BeFalse();
    }

    [Fact]
    public async Task Accepts_admits_only_the_connectors_the_service_resolved_as_valid_targets()
    {
        // Arrange
        await using var ctx = CreateContext();
        using var validTarget = ctx.CreateBlockNodeConnector();
        using var otherConnector = ctx.CreateBlockNodeConnector();
        await using var datastore = Substitute.For<IDatastore>();
        using var service = CreateService(ctx, datastore);
        ReturnsValidTargets(datastore, validTarget);

        var sut = new PublishedConnectorDropPolicy(service);
        var draggable = CreateRowSet();

        // Act
        var validTargetAccepted = sut.Accepts(draggable, validTarget);
        var otherConnectorAccepted = sut.Accepts(draggable, otherConnector);

        // Assert
        validTargetAccepted.Should().BeTrue();
        otherConnectorAccepted.Should().BeFalse();
    }

    // Every candidate connector asks separately, so the valid-target set is resolved once per payload and
    // reused.
    [Fact]
    public async Task Accepts_resolves_the_valid_targets_once_per_payload()
    {
        // Arrange
        await using var ctx = CreateContext();
        using var target = ctx.CreateBlockNodeConnector();
        await using var datastore = Substitute.For<IDatastore>();
        using var service = CreateService(ctx, datastore);
        ReturnsValidTargets(datastore, target);

        var sut = new PublishedConnectorDropPolicy(service);
        var drag = CreateRowSet();

        // Act
        var firstAccepted = sut.Accepts(drag, target);
        var secondAccepted = sut.Accepts(drag, target);

        // Assert
        firstAccepted.Should().BeTrue();
        secondAccepted.Should().BeTrue();
        datastore.Received(1).GetValidTargetConnectors(Arg.Any<IEnumerable<Connector>>(), false);
    }

    // A cache that never invalidated would answer the next drag out of the previous drag's targets.
    [Fact]
    public async Task Accepts_resolves_again_when_a_later_drag_carries_a_different_payload()
    {
        // Arrange
        await using var ctx = CreateContext();
        using var firstTarget = ctx.CreateBlockNodeConnector();
        using var secondTarget = ctx.CreateBlockNodeConnector();
        await using var datastore = Substitute.For<IDatastore>();
        using var service = CreateService(ctx, datastore);

        var sut = new PublishedConnectorDropPolicy(service);

        ReturnsValidTargets(datastore, firstTarget);
        sut.Accepts(CreateRowSet(), firstTarget);

        ReturnsValidTargets(datastore, secondTarget);
        var secondDrag = CreateRowSet();

        // Act
        var secondTargetAccepted = sut.Accepts(secondDrag, secondTarget);
        var firstTargetAccepted = sut.Accepts(secondDrag, firstTarget);

        // Assert
        secondTargetAccepted.Should().BeTrue();
        firstTargetAccepted.Should().BeFalse();
    }

    // A substituted datastore cannot mint BlockNodeConnectors, so only the service is substituted.
    private static PublishedConnectorsService CreateService(BunitContext ctx, IDatastore datastore)
        => new(ctx.Services.GetRequiredService<ClusterBuilderEventBuffer>(), datastore);

    private static IDraggable CreateRowSet()
    {
        var draggable = Substitute.For<IDraggable, IDraggableRowSet<DataGridConnectorWrapper>>();
        ((IDraggableRowSet<DataGridConnectorWrapper>)draggable).Items
            .Returns(new List<DataGridConnectorWrapper>());

        return draggable;
    }

    private static void ReturnsValidTargets(IDatastore datastore, params BlockNodeConnector[] targets)
        => datastore.GetValidTargetConnectors(Arg.Any<IEnumerable<Connector>>(), false).Returns(targets);

    private static BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.SetupDiagramService();
        ctx.SetupPublishedConnectorsService();

        return ctx;
    }
}
