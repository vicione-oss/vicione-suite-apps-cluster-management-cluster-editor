using System.Threading.Tasks;
using AwesomeAssertions;
using Blazor.Diagrams;
using Blazor.Diagrams.Core.Geometry;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.Ui.ClusterEditor.Components.NodeEditorServices;
using ViciOne.Ui.ClusterEditor.Constants;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components.NodeEditorServices;

public sealed class DiagramModelSyncControllerTests
{
    private static (DiagramModelSyncController Sut, BlazorDiagram Diagram, DiagramService DiagramService) CreateSut(BunitContext ctx)
    {
        ctx.SetupNodeEditor();
        var diagram = NodeEditorDiagramFactory.Create();
        var diagramService = ctx.Services.GetRequiredService<DiagramService>();
        diagramService.Diagram = diagram;
        var sut = ctx.Services.GetRequiredService<DiagramModelSyncController>();

        return (sut, diagram, diagramService);
    }

    [Fact]
    public async Task Dispose_AfterInitialize_StopsForwardingLabelEditModeForNewNodes()
    {
        // Arrange
        await using var ctx = new BunitContext();
        var (sut, diagram, _) = CreateSut(ctx);
        sut.Initialize(diagram);

        var forwardCount = 0;
        sut.LabelEditModeStarted += _ => { forwardCount++; return Task.CompletedTask; };

        // Act - after Dispose the Nodes.Added subscription is gone, so a newly added label
        // is never wired up and its edit-mode event must not be forwarded.
        sut.Dispose();
        var labelNode = new LabelNode(new Point(0, 0));
        diagram.Nodes.Add(labelNode);
        await labelNode.ProcessTextEditStarted();

        // Assert
        forwardCount.Should().Be(0);
    }

    [Fact]
    public async Task LabelEditModeStarted_WhenAddedLabelNodeStartsEditing_ForwardsTheEvent()
    {
        // Arrange
        await using var ctx = new BunitContext();
        var (sut, diagram, _) = CreateSut(ctx);
        sut.Initialize(diagram);

        LabelNode? forwarded = null;
        sut.LabelEditModeStarted += node => { forwarded = node; return Task.CompletedTask; };

        var labelNode = new LabelNode(new Point(0, 0));
        diagram.Nodes.Add(labelNode);

        // Act
        await labelNode.ProcessTextEditStarted();

        // Assert
        forwarded.Should().BeSameAs(labelNode);
    }

    [Fact]
    public async Task OnDiagramStateZoomChanged_WithOutOfRangeZoom_IsIgnored()
    {
        // Arrange
        await using var ctx = new BunitContext();
        var (sut, diagram, _) = CreateSut(ctx);
        diagram.SetContainer(new Rectangle(0, 0, 800, 600));
        sut.Initialize(diagram);
        var originalZoom = diagram.Zoom;

        // Act - a zoom beyond the allowed range must be rejected without touching the diagram.
        ctx.Services.GetRequiredService<DiagramEventService>().InvokeZoomChanged(DiagramSettings.ZoomMaximum + 1);

        // Assert
        diagram.Zoom.Should().Be(originalZoom);
    }

    [Fact]
    public async Task OnDiagramStateZoomChanged_WithValidZoom_RecentersAndAppliesZoomToDiagram()
    {
        // Arrange - the recenter math needs a measured container.
        await using var ctx = new BunitContext();
        var (sut, diagram, diagramService) = CreateSut(ctx);
        diagram.SetContainer(new Rectangle(0, 0, 800, 600));
        sut.Initialize(diagram);

        const double RequestedZoom = 1.2;
        RequestedZoom.Should().NotBe(diagram.Zoom);
        RequestedZoom.Should().BeInRange(DiagramSettings.ZoomMinimum, DiagramSettings.ZoomMaximum);

        // Act - a zoom request coming from the diagram state (e.g. the zoom toolbar) is applied
        // to the diagram while keeping the viewport centered.
        diagramService.DiagramState.Zoom = RequestedZoom;
        ctx.Services.GetRequiredService<DiagramEventService>().InvokeZoomChanged(RequestedZoom);

        // Assert
        diagram.Zoom.Should().Be(RequestedZoom);
    }

    [Fact]
    public async Task OnDiagramZoomChanged_WhenDiagramZoomChanges_SyncsZoomIntoDiagramState()
    {
        // Arrange
        await using var ctx = new BunitContext();
        var (sut, diagram, diagramService) = CreateSut(ctx);
        sut.Initialize(diagram);

        const double NewZoom = 1.5;
        NewZoom.Should().NotBe(diagramService.DiagramState.Zoom);

        // Act - the diagram raising ZoomChanged must flow back into the shared diagram state.
        diagram.SetZoom(NewZoom);

        // Assert
        diagramService.DiagramState.Zoom.Should().Be(NewZoom);
        NewZoom.Should().BeInRange(DiagramSettings.ZoomMinimum, DiagramSettings.ZoomMaximum);
    }
}
