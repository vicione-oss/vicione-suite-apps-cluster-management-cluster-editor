using System.Threading.Tasks;
using AwesomeAssertions;
using Blazor.Diagrams;
using Blazor.Diagrams.Core.Geometry;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.Ui.ClusterEditor.Behaviors;
using ViciOne.Ui.ClusterEditor.Components.NodeEditorServices;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components.NodeEditorServices;

public sealed class NodeEditorBehaviorControllerTests
{
    /// <summary>
    /// Builds the controller together with a diagram whose initial pan behavior is pinned to VO
    /// (rather than the build-configuration dependent default) so tests stay deterministic.
    /// </summary>
    private static (NodeEditorBehaviorController Sut, BlazorDiagram Diagram, DiagramService DiagramService) CreateSut(BunitContext ctx)
    {
        ctx.SetupNodeEditor();
        var diagram = NodeEditorDiagramFactory.Create();
        var diagramService = ctx.Services.GetRequiredService<DiagramService>();
        diagramService.Diagram = diagram;
        diagramService.DiagramState.UsesGimpPanBehavior = false;
        var sut = ctx.Services.GetRequiredService<NodeEditorBehaviorController>();

        return (sut, diagram, diagramService);
    }

    [Fact]
    public async Task Dispose_AfterInitialize_UnregistersAllBehaviors()
    {
        // Arrange
        await using var ctx = new BunitContext();
        var (sut, diagram, _) = CreateSut(ctx);
        sut.Initialize(diagram);

        // Act
        sut.Dispose();

        // Assert - every behavior the controller registered is gone again.
        diagram.GetBehavior<VOSelectionBehavior>().Should().BeNull();
        diagram.GetBehavior<VODragMovablesBehavior>().Should().BeNull();
        diagram.GetBehavior<VODragNewLinkBehavior>().Should().BeNull();
        diagram.GetBehavior<VOZoomToFitBehavior>().Should().BeNull();
        diagram.GetBehavior<VOKeyboardBehavior>().Should().BeNull();
        diagram.GetBehavior<VOPanBehavior>().Should().BeNull();
        diagram.GetBehavior<VOZoomBehavior>().Should().BeNull();
    }

    [Fact]
    public async Task Initialize_AfterInitialize_ReRegistersBehaviorsWithoutDuplicating()
    {
        // Arrange
        await using var ctx = new BunitContext();
        var (sut, diagram, _) = CreateSut(ctx);
        sut.Initialize(diagram);
        var firstSelectionBehavior = diagram.GetBehavior<VOSelectionBehavior>();

        // Act - a second Initialize must tear the old behaviors down first, then register fresh ones.
        sut.Initialize(diagram);

        // Assert
        var secondSelectionBehavior = diagram.GetBehavior<VOSelectionBehavior>();
        secondSelectionBehavior.Should().NotBeNull();
        secondSelectionBehavior.Should().NotBeSameAs(firstSelectionBehavior);
        diagram.GetBehavior<VOPanBehavior>().Should().NotBeNull();
    }

    [Fact]
    public async Task IsMoving_AfterInitialize_IsFalse()
    {
        // Arrange
        await using var ctx = new BunitContext();
        var (sut, diagram, _) = CreateSut(ctx);

        // Act
        sut.Initialize(diagram);

        // Assert
        sut.IsMoving.Should().BeFalse();
    }

    [Fact]
    public async Task OnDiagramPointerLeave_AfterInitialize_IsForwardedWithoutThrowing()
    {
        // Arrange
        await using var ctx = new BunitContext();
        var (sut, diagram, _) = CreateSut(ctx);
        var diagramEvents = ctx.Services.GetRequiredService<DiagramEventService>();
        sut.Initialize(diagram);

        // Act - the controller subscribes to DiagramPointerLeave and stops the active pan behavior.
        var act = diagramEvents.InvokeDiagramPointerLeave;

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public async Task RequestPanBehaviorChange_SwapsGimpAndVoPanAndZoomBehaviors()
    {
        // Arrange - initialized with VO active.
        await using var ctx = new BunitContext();
        var (sut, diagram, diagramService) = CreateSut(ctx);
        sut.Initialize(diagram);

        // Act - switch to Gimp
        diagramService.RequestPanBehaviorChange(true);

        // Assert
        diagram.GetBehavior<GimpPanBehavior>().Should().NotBeNull();
        diagram.GetBehavior<GimpZoomBehavior>().Should().NotBeNull();
        diagram.GetBehavior<VOPanBehavior>().Should().BeNull();
        diagram.GetBehavior<VOZoomBehavior>().Should().BeNull();

        // Act - switch back to VO
        diagramService.RequestPanBehaviorChange(false);

        // Assert
        diagram.GetBehavior<VOPanBehavior>().Should().NotBeNull();
        diagram.GetBehavior<VOZoomBehavior>().Should().NotBeNull();
        diagram.GetBehavior<GimpPanBehavior>().Should().BeNull();
        diagram.GetBehavior<GimpZoomBehavior>().Should().BeNull();
    }

    [Fact]
    public async Task RequestPanBehaviorChange_ToSameBehaviorRepeatedly_KeepsExistingBehaviorInstances()
    {
        // Arrange - initialized with VO active.
        await using var ctx = new BunitContext();
        var (sut, diagram, diagramService) = CreateSut(ctx);
        sut.Initialize(diagram);
        var initialVoPan = diagram.GetBehavior<VOPanBehavior>();

        // Act - requesting VO again hits the VO early-return guard and keeps the same instance.
        diagramService.RequestPanBehaviorChange(false);

        // Assert
        diagram.GetBehavior<VOPanBehavior>().Should().BeSameAs(initialVoPan);

        // Act - switch to Gimp, then request Gimp again to hit the Gimp early-return guard.
        diagramService.RequestPanBehaviorChange(true);
        var gimpPan = diagram.GetBehavior<GimpPanBehavior>();
        diagramService.RequestPanBehaviorChange(true);

        // Assert
        diagram.GetBehavior<GimpPanBehavior>().Should().BeSameAs(gimpPan);
    }

    [Fact]
    public async Task SimplifiedViewChangeRequested_AfterInitialize_RefreshesNodesWithoutThrowing()
    {
        // Arrange
        await using var ctx = new BunitContext();
        var (sut, diagram, diagramService) = CreateSut(ctx);
        sut.Initialize(diagram);
        diagram.Nodes.Add(new LabelNode(new Point(0, 0)));

        // Act - the controller refreshes every node when a simplified-view change is requested.
        var act = () => diagramService.RequestSimplifiedViewChange(true);

        // Assert
        act.Should().NotThrow();
    }
}
