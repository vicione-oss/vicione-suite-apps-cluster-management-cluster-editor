using System.Threading.Tasks;
using AwesomeAssertions;
using Blazor.Diagrams;
using Blazor.Diagrams.Core.Geometry;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.Ui.ClusterEditor.Components.NodeEditorServices;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components.NodeEditorServices;

public sealed class DiagramPointerInteractionControllerTests
{
    private static (DiagramPointerInteractionController Sut, BlazorDiagram Diagram, DiagramEventService DiagramEvents) CreateSut(BunitContext ctx)
    {
        ctx.SetupNodeEditor();
        var diagram = NodeEditorDiagramFactory.Create();
        // A container is required for the relative-mouse-point math in the pointer-down path.
        diagram.SetContainer(new Rectangle(0, 0, 800, 600));
        var diagramService = ctx.Services.GetRequiredService<DiagramService>();
        diagramService.Diagram = diagram;
        var diagramEvents = ctx.Services.GetRequiredService<DiagramEventService>();
        var sut = ctx.Services.GetRequiredService<DiagramPointerInteractionController>();

        return (sut, diagram, diagramEvents);
    }

    [Fact]
    public async Task ContextMenuAllowed_ByDefault_IsTrue()
    {
        // Arrange
        await using var ctx = new BunitContext();
        var (sut, diagram, _) = CreateSut(ctx);
        sut.Initialize(diagram, () => { });

        // Assert
        sut.ContextMenuAllowed().Should().BeTrue();
    }

    [Fact]
    public async Task EdgeDraggingPointerUp_AfterDispose_DoesNotThrow()
    {
        // Arrange
        await using var ctx = new BunitContext();
        var (sut, diagram, diagramEvents) = CreateSut(ctx);
        sut.Initialize(diagram, () => { });
        sut.Dispose();

        // Act
        var act = () => diagramEvents.InvokeEdgeDraggingPointerUp(new PointerEventArgs());

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public async Task Initialize_CalledTwice_TearsDownBeforeReinitializingWithoutThrowing()
    {
        // Arrange
        await using var ctx = new BunitContext();
        var (sut, diagram, diagramEvents) = CreateSut(ctx);
        sut.Initialize(diagram, () => { });

        // Act - a second Initialize must run Teardown first, then re-wire the diagram cleanly.
        sut.Initialize(diagram, () => { });

        // Assert - the re-wired controller still behaves and only reacts once (no duplicate handlers).
        bool? edgeDraggingVisible = null;
        var invocations = 0;
        diagramEvents.EdgeDraggingVisibilityChangeRequested += visible =>
        {
            edgeDraggingVisible = visible;
            invocations++;
        };
        sut.OnContainerPointerDown(new PointerEventArgs { Button = 0, ClientX = 10, ClientY = 10 });

        edgeDraggingVisible.Should().BeTrue();
        invocations.Should().Be(1);
    }

    [Fact]
    public async Task OnContainerPointerCancel_DuringSelectBoxDrag_ResetsInteractionState()
    {
        // Arrange
        await using var ctx = new BunitContext();
        var (sut, diagram, diagramEvents) = CreateSut(ctx);
        var renderRequested = false;
        sut.Initialize(diagram, () => renderRequested = true);

        bool? edgeDraggingVisible = null;
        diagramEvents.EdgeDraggingVisibilityChangeRequested += visible => edgeDraggingVisible = visible;

        sut.OnContainerPointerDown(new PointerEventArgs { Button = 0, ClientX = 10, ClientY = 10 });
        diagramEvents.InvokeEdgeDraggingPointerMove(new PointerEventArgs { Buttons = 1, ClientX = 100, ClientY = 100 });
        sut.ViewRectangle.Should().NotBeNull();
        renderRequested = false;

        // Act - the browser cancels the pointer (e.g. native drag start), no pointer up follows.
        sut.OnContainerPointerCancel(new PointerEventArgs());

        // Assert
        sut.ContextMenuAllowed().Should().BeTrue();
        sut.ViewRectangle.Should().BeNull();
        edgeDraggingVisible.Should().BeFalse();
        renderRequested.Should().BeTrue();
    }

    [Fact]
    public async Task OnContainerPointerCancel_DuringSelectBoxDrag_DoesNotApplyDiscardedSelectBoxOnNextClick()
    {
        // Arrange
        await using var ctx = new BunitContext();
        var (sut, diagram, diagramEvents) = CreateSut(ctx);
        sut.Initialize(diagram, () => { });
        var initialZoom = diagram.Zoom;
        var initialPanX = diagram.Pan.X;
        var initialPanY = diagram.Pan.Y;

        sut.OnContainerPointerDown(new PointerEventArgs { Button = 0, ClientX = 10, ClientY = 10 });
        diagramEvents.InvokeEdgeDraggingPointerMove(new PointerEventArgs { Buttons = 1, ClientX = 100, ClientY = 100 });
        sut.OnContainerPointerCancel(new PointerEventArgs());

        // Act - a click with Shift would zoom to the discarded select box if it was still stored.
        sut.OnContainerPointerDown(new PointerEventArgs { Button = 0, ClientX = 10, ClientY = 10, ShiftKey = true });
        diagramEvents.InvokeEdgeDraggingPointerUp(new PointerEventArgs());

        // Assert
        diagram.Zoom.Should().Be(initialZoom);
        diagram.Pan.X.Should().Be(initialPanX);
        diagram.Pan.Y.Should().Be(initialPanY);
    }

    [Fact]
    public async Task OnContainerPointerCancel_DuringSelectBoxDrag_IgnoresSubsequentPointerMove()
    {
        // Arrange
        await using var ctx = new BunitContext();
        var (sut, diagram, diagramEvents) = CreateSut(ctx);
        sut.Initialize(diagram, () => { });

        sut.OnContainerPointerDown(new PointerEventArgs { Button = 0, ClientX = 10, ClientY = 10 });
        diagramEvents.InvokeEdgeDraggingPointerMove(new PointerEventArgs { Buttons = 1, ClientX = 100, ClientY = 100 });
        sut.OnContainerPointerCancel(new PointerEventArgs());

        // Act - a pointer move with the button still pressed must not resurrect the discarded select box.
        diagramEvents.InvokeEdgeDraggingPointerMove(new PointerEventArgs { Buttons = 1, ClientX = 200, ClientY = 200 });

        // Assert
        sut.ViewRectangle.Should().BeNull();
    }

    [Fact]
    public async Task OnContainerPointerDown_WhenContainerNotInitialized_IsIgnored()
    {
        // Arrange - a diagram whose container has not been measured yet (as during initial load).
        await using var ctx = new BunitContext();
        ctx.SetupNodeEditor();
        var diagram = NodeEditorDiagramFactory.Create();
        var diagramService = ctx.Services.GetRequiredService<DiagramService>();
        diagramService.Diagram = diagram;
        var diagramEvents = ctx.Services.GetRequiredService<DiagramEventService>();
        var sut = ctx.Services.GetRequiredService<DiagramPointerInteractionController>();
        sut.Initialize(diagram, () => { });

        var edgeDraggingRequested = false;
        diagramEvents.EdgeDraggingVisibilityChangeRequested += _ => edgeDraggingRequested = true;

        // Act - without a container the primary-button handler must bail out early.
        sut.OnContainerPointerDown(new PointerEventArgs { Button = 0, ClientX = 10, ClientY = 10 });

        // Assert
        sut.ContextMenuAllowed().Should().BeTrue();
        edgeDraggingRequested.Should().BeFalse();
    }

    [Fact]
    public async Task OnContainerPointerDown_WithLeftButton_SuppressesContextMenuAndRequestsEdgeDraggingVisibility()
    {
        // Arrange
        await using var ctx = new BunitContext();
        var (sut, diagram, diagramEvents) = CreateSut(ctx);
        sut.Initialize(diagram, () => { });

        bool? edgeDraggingVisible = null;
        diagramEvents.EdgeDraggingVisibilityChangeRequested += visible => edgeDraggingVisible = visible;

        // Act - the primary button starting a drag on the canvas begins a rubber-band selection.
        sut.OnContainerPointerDown(new PointerEventArgs { Button = 0, ClientX = 10, ClientY = 10 });

        // Assert
        sut.ContextMenuAllowed().Should().BeFalse();
        edgeDraggingVisible.Should().BeTrue();
    }

    [Fact]
    public async Task OnContainerPointerDown_WithNonLeftButton_IsIgnored()
    {
        // Arrange
        await using var ctx = new BunitContext();
        var (sut, diagram, diagramEvents) = CreateSut(ctx);
        sut.Initialize(diagram, () => { });

        var edgeDraggingRequested = false;
        diagramEvents.EdgeDraggingVisibilityChangeRequested += _ => edgeDraggingRequested = true;

        // Act - a non-primary button (e.g. right click) must not start a selection drag.
        sut.OnContainerPointerDown(new PointerEventArgs { Button = 2, ClientX = 10, ClientY = 10 });

        // Assert
        sut.ContextMenuAllowed().Should().BeTrue();
        edgeDraggingRequested.Should().BeFalse();
    }

    [Fact]
    public async Task ViewRectangle_BeforeAnyInteraction_IsNull()
    {
        // Arrange
        await using var ctx = new BunitContext();
        var (sut, _, _) = CreateSut(ctx);

        // Assert
        sut.ViewRectangle.Should().BeNull();
    }
}
