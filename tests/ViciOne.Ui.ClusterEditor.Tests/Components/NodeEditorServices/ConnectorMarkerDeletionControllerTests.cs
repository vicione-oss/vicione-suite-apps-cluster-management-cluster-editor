using AwesomeAssertions;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.Ui.ClusterEditor.Components.NodeEditorServices;
using ViciOne.Ui.ClusterEditor.Constants;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;
using TestContext = Bunit.TestContext;

namespace ViciOne.Ui.ClusterEditor.Tests.Components.NodeEditorServices;

// NOTE: The actual link-deletion branches of DeleteSelectedConnectorMarkerLinks (un-publishing a
// connector, deleting one/many links, opening the deletion dialog) all mutate the real
// ViciOne.Cluster.Builder graph via a fully wired connector marker with links. Constructing that
// state is only meaningful with a real cluster fixture, so those side effects are covered by the
// higher-level component/integration tests rather than unit tests here. What we CAN pin down
// deterministically is the input routing: only the Delete key triggers the deletion attempt, and a
// deletion attempt with nothing selected is a no-op.
public sealed class ConnectorMarkerDeletionControllerTests
{
    private static (ConnectorMarkerDeletionController Sut, InputEventService InputEvents, LinkDestinationDialogService DialogService) CreateSut(TestContext ctx)
    {
        ctx.SetupNodeEditor();
        var sut = ctx.Services.GetRequiredService<ConnectorMarkerDeletionController>();
        var inputEvents = ctx.Services.GetRequiredService<InputEventService>();
        var dialogService = ctx.Services.GetRequiredService<LinkDestinationDialogService>();

        return (sut, inputEvents, dialogService);
    }

    [Fact]
    public void KeyDown_AfterDispose_IsNoLongerHandled()
    {
        // Arrange
        using var ctx = new TestContext();
        var (sut, inputEvents, dialogService) = CreateSut(ctx);
        sut.Initialize();
        sut.Dispose();

        // Act - once disposed the controller must have detached from KeyDown, so even a Delete key
        // press cannot flip the controller into deletion mode.
        var act = () => inputEvents.InvokeKeyDown(new KeyboardEventArgs { Code = KeyboardCodes.Delete });

        // Assert
        act.Should().NotThrow();
        dialogService.IsDeletionMode.Should().BeFalse();
    }

    [Fact]
    public void KeyDown_WithDeleteKeyAndNoSelectedConnectorMarker_DoesNothing()
    {
        // Arrange
        using var ctx = new TestContext();
        var (sut, inputEvents, dialogService) = CreateSut(ctx);
        sut.Initialize();

        // Act - reaches DeleteSelectedConnectorMarkerLinks, which returns early with no selection.
        inputEvents.InvokeKeyDown(new KeyboardEventArgs { Code = KeyboardCodes.Delete });

        // Assert - nothing selected, so the deletion dialog is never entered.
        dialogService.IsDeletionMode.Should().BeFalse();
    }

    [Fact]
    public void KeyDown_WithNonDeleteKey_DoesNothing()
    {
        // Arrange
        using var ctx = new TestContext();
        var (sut, inputEvents, dialogService) = CreateSut(ctx);
        sut.Initialize();

        // Act
        var act = () => inputEvents.InvokeKeyDown(new KeyboardEventArgs { Code = "KeyA" });

        // Assert
        act.Should().NotThrow();
        dialogService.IsDeletionMode.Should().BeFalse();
    }
}
