using AwesomeAssertions;
using Blazor.Diagrams;
using Blazor.Diagrams.Core.Geometry;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.Ui.ClusterEditor.Components.NodeEditorServices;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;
using TestContext = Bunit.TestContext;

namespace ViciOne.Ui.ClusterEditor.Tests.Components.NodeEditorServices;

public sealed class DiagramPointerInteractionControllerTests
{
    private static (DiagramPointerInteractionController Sut, BlazorDiagram Diagram, DiagramEventService DiagramEvents) CreateSut(TestContext ctx)
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
    public void ContextMenuAllowed_ByDefault_IsTrue()
    {
        // Arrange
        using var ctx = new TestContext();
        var (sut, diagram, _) = CreateSut(ctx);
        sut.Initialize(diagram, () => { });

        // Assert
        sut.ContextMenuAllowed().Should().BeTrue();
    }

    [Fact]
    public void EdgeDraggingPointerUp_AfterDispose_DoesNotThrow()
    {
        // Arrange
        using var ctx = new TestContext();
        var (sut, diagram, diagramEvents) = CreateSut(ctx);
        sut.Initialize(diagram, () => { });
        sut.Dispose();

        // Act
        var act = () => diagramEvents.InvokeEdgeDraggingPointerUp(new PointerEventArgs());

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Initialize_CalledTwice_TearsDownBeforeReinitializingWithoutThrowing()
    {
        // Arrange
        using var ctx = new TestContext();
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
    public void OnContainerPointerDown_WhenContainerNotInitialized_IsIgnored()
    {
        // Arrange - a diagram whose container has not been measured yet (as during initial load).
        using var ctx = new TestContext();
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
    public void OnContainerPointerDown_WithLeftButton_SuppressesContextMenuAndRequestsEdgeDraggingVisibility()
    {
        // Arrange
        using var ctx = new TestContext();
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
    public void OnContainerPointerDown_WithNonLeftButton_IsIgnored()
    {
        // Arrange
        using var ctx = new TestContext();
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
    public void ViewRectangle_BeforeAnyInteraction_IsNull()
    {
        // Arrange
        using var ctx = new TestContext();
        var (sut, _, _) = CreateSut(ctx);

        // Assert
        sut.ViewRectangle.Should().BeNull();
    }
}
