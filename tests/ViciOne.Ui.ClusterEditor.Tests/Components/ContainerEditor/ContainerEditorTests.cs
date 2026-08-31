using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using ViciOne.Ui.Blazor.Components.ContextMenu.Services;
using ViciOne.Ui.Blazor.Components.Dialog.Extensions;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Services;
using ViciOne.Ui.ClusterEditor.Components.ContainerEditor;
using ViciOne.Ui.ClusterEditor.Components.ContainerEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Components.ToolbarDataflow.Models;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components.ContainerEditor;

public class ContainerEditorTests
{
    [Fact]
    public void Component_should_render()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.Services.TryAddScoped(_ => Substitute.For<IContainerEditorRequest>());
        ctx.Services.TryAddScoped(_ => Substitute.For<IContextMenuSettings>());
        ctx.Services.AddDialog();
        ctx.SetupDiagramService();
        ctx.Services.AddContainerEditor();
        ctx.Services.TryAddScoped(_ => Substitute.For<IPropertyGridController<DataflowToolbarPropertyGridContext>>());
        ctx.SetupSelectionManager();

        ctx.CreateDiagramInstance();

        // Act
        var component = ctx.RenderComponent<ClusterEditor.Components.ContainerEditor.ContainerEditor>();

        // Assert
        Assert.NotNull(component);
    }
}
