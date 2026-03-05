using Blazor.Diagrams.Core.Geometry;
using Bunit;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using ViciOne.Ui.Blazor.Components.ContextMenu.Services;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Services;
using ViciOne.Ui.ClusterEditor.Components;
using ViciOne.Ui.ClusterEditor.Components.ContainerEditor;
using ViciOne.Ui.ClusterEditor.Components.ContainerEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Components.FbSettingsEditor;
using ViciOne.Ui.ClusterEditor.Components.ToolbarDataflow.Models;
using ViciOne.Ui.ClusterEditor.Models.ContextMenu.Specialized;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components;

public class NodeEditorTests
{
    [Fact]
    public void Component_should_render()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.SetupDatastore();
        ctx.SetupDevExpressBlazor();
        ctx.SetupDragService();
        ctx.SetupConnectorService();
        ctx.Services.TryAddScoped(_ => Substitute.For<IContextMenuRequest<NodeEditorContextMenuContext>>());
        ctx.Services.TryAddScoped(_ => Substitute.For<IContextMenuSettings>());
        ctx.SetupLibraryService();
        ctx.SetupResizeObserver();
        ctx.SetupConnectorSelectionDialogService();
        ctx.SetupLinkDestinationDialogService();
        ctx.Services.TryAddScoped(_ => Substitute.For<IContainerEditorRequest>());
        ctx.Services.TryAddScoped(_ => Substitute.For<IFbSettingsEditorRequest>());
        ctx.Services.TryAddScoped<FullscreenService>();
        ctx.Services.TryAddScoped<TooltipService>();
        ctx.Services.AddContainerEditor();
        ctx.Services.TryAddScoped(_ => Substitute.For<IPropertyGridController<DataflowToolbarPropertyGridContext>>());

        ctx.JSInterop.Setup<Rectangle>("ZBlazorDiagrams.getBoundingClientRect", _ => true);

        // Act
        var component = ctx.RenderComponent<NodeEditor>();

        // Assert
        Assert.NotNull(component);
    }
}
