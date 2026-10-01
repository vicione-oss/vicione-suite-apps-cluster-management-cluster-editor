using System.Threading.Tasks;
using AwesomeAssertions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using ViciOne.Ui.Blazor.Components.Draggable.Components;
using ViciOne.Ui.Blazor.Components.Draggable.Services;
using ViciOne.Ui.ClusterEditor.Components.DiagramComponents;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components.DiagramComponents;

public class ConnectorDropzoneTests
{
    [Fact]
    public async Task A_drag_the_policy_admits_joins_the_drag_and_marks_the_connector()
    {
        // Arrange
        var dragInteraction = Substitute.For<IDragInteraction>();
        var policy = Substitute.For<IDropPolicy<BlockNodeConnector>>();
        await using var ctx = CreateContext(dragInteraction, policy, Substitute.For<IDropHandler<BlockNodeConnector>>());
        using var connector = ctx.CreateBlockNodeConnector();
        policy.Accepts(Arg.Any<IDraggable>(), connector).Returns(true);
        var component = RenderDropzone(ctx, connector);

        // Act
        var args = RaiseDragStart(dragInteraction);

        // Assert
        args.Dropzones.Should().Contain(component.Instance);
        connector.IsValidDropTarget.Should().BeTrue();
    }

    [Fact]
    public async Task A_drag_the_policy_rejects_leaves_the_connector_alone()
    {
        // Arrange
        var dragInteraction = Substitute.For<IDragInteraction>();
        var policy = Substitute.For<IDropPolicy<BlockNodeConnector>>();
        await using var ctx = CreateContext(dragInteraction, policy, Substitute.For<IDropHandler<BlockNodeConnector>>());
        using var connector = ctx.CreateBlockNodeConnector();
        policy.Accepts(Arg.Any<IDraggable>(), connector).Returns(false);
        var component = RenderDropzone(ctx, connector);

        // Act
        var args = RaiseDragStart(dragInteraction);

        // Assert
        args.Dropzones.Should().NotContain(component.Instance);
        connector.IsValidDropTarget.Should().BeFalse();
    }

    // A marked input port is an unlocked one, and nothing else clears the mark once the dropzone is gone.
    [Fact]
    public async Task Disposing_mid_drag_clears_the_mark_it_set()
    {
        // Arrange
        var dragInteraction = Substitute.For<IDragInteraction>();
        var policy = Substitute.For<IDropPolicy<BlockNodeConnector>>();
        await using var ctx = CreateContext(dragInteraction, policy, Substitute.For<IDropHandler<BlockNodeConnector>>());
        using var connector = ctx.CreateBlockNodeConnector();
        policy.Accepts(Arg.Any<IDraggable>(), connector).Returns(true);
        RenderDropzone(ctx, connector);
        RaiseDragStart(dragInteraction);

        // Act
        await ctx.DisposeComponentsAsync();

        // Assert
        connector.IsValidDropTarget.Should().BeFalse();
    }

    // Every block scans its connectors on each update, so an update per marked connector would scale with the
    // number of blocks times the number of targets.
    [Fact]
    public async Task A_drag_start_updates_the_blocks_once_however_many_connectors_it_marks()
    {
        // Arrange
        var dragInteraction = Substitute.For<IDragInteraction>();
        var policy = Substitute.For<IDropPolicy<BlockNodeConnector>>();
        await using var ctx = CreateContext(dragInteraction, policy, Substitute.For<IDropHandler<BlockNodeConnector>>());
        using var firstConnector = ctx.CreateBlockNodeConnector();
        using var secondConnector = ctx.CreateBlockNodeConnector();
        policy.Accepts(Arg.Any<IDraggable>(), Arg.Any<BlockNodeConnector>()).Returns(true);
        RenderDropzone(ctx, firstConnector);
        RenderDropzone(ctx, secondConnector);

        var updates = 0;
        ctx.Services.GetRequiredService<DiagramEventService>().BlockNodesUpdateRequested += () => updates++;

        // Act
        RaiseDragStart(dragInteraction);

        // Assert
        firstConnector.IsValidDropTarget.Should().BeTrue();
        secondConnector.IsValidDropTarget.Should().BeTrue();
        updates.Should().Be(1);
    }

    // The drag interaction ends the drag on each of its dropzones.
    [Fact]
    public async Task A_drag_end_updates_the_blocks_once_however_many_dropzones_it_reaches()
    {
        // Arrange
        var dragInteraction = Substitute.For<IDragInteraction>();
        var policy = Substitute.For<IDropPolicy<BlockNodeConnector>>();
        await using var ctx = CreateContext(dragInteraction, policy, Substitute.For<IDropHandler<BlockNodeConnector>>());
        using var firstConnector = ctx.CreateBlockNodeConnector();
        using var secondConnector = ctx.CreateBlockNodeConnector();
        policy.Accepts(Arg.Any<IDraggable>(), Arg.Any<BlockNodeConnector>()).Returns(true);
        RenderDropzone(ctx, firstConnector);
        RenderDropzone(ctx, secondConnector);
        var args = RaiseDragStart(dragInteraction);

        var updates = 0;
        ctx.Services.GetRequiredService<DiagramEventService>().BlockNodesUpdateRequested += () => updates++;

        // Act
        foreach (var dropzone in args.Dropzones)
            await dropzone.DragEndAsync(args.Draggable, 0, 0);

        // Assert
        firstConnector.IsValidDropTarget.Should().BeFalse();
        secondConnector.IsValidDropTarget.Should().BeFalse();
        updates.Should().Be(1);
    }

    [Fact]
    public async Task A_drop_hands_the_dragged_payload_and_this_connector_to_the_handler()
    {
        // Arrange
        var handler = Substitute.For<IDropHandler<BlockNodeConnector>>();
        await using var ctx = CreateContext(Substitute.For<IDragInteraction>(),
            Substitute.For<IDropPolicy<BlockNodeConnector>>(), handler);
        using var connector = ctx.CreateBlockNodeConnector();
        var component = RenderDropzone(ctx, connector);
        var draggable = Substitute.For<IDraggable>();

        // Act
        await component.Instance.DragDroppedAsync(draggable, 12, 34);

        // Assert
        await handler.Received(1).DragDroppedAsync(draggable, 12, 34, connector);
    }

    private static DragStartEventArgs RaiseDragStart(IDragInteraction dragInteraction)
    {
        var args = new DragStartEventArgs { Draggable = Substitute.For<IDraggable>() };
        dragInteraction.DragStart += Raise.EventWith(dragInteraction, args);

        return args;
    }

    private static IRenderedComponent<ConnectorDropzone> RenderDropzone(BunitContext ctx, BlockNodeConnector connector)
        => ctx.Render<ConnectorDropzone>(parameters => parameters
            .Add(p => p.Connector, connector));

    private static BunitContext CreateContext(IDragInteraction dragInteraction,
        IDropPolicy<BlockNodeConnector> policy, IDropHandler<BlockNodeConnector> handler)
    {
        var ctx = new BunitContext();
        ctx.SetupDiagramService();

        ctx.Services.AddScoped(_ => dragInteraction);
        ctx.Services.AddScoped(_ => policy);
        ctx.Services.AddScoped(_ => handler);
        ctx.Services.AddScoped<ConnectorDropTargets>();

        return ctx;
    }
}
