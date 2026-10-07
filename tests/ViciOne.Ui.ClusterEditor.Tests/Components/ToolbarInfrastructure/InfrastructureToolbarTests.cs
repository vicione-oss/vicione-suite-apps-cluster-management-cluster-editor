using System.Threading.Tasks;
using Bunit;
using ViciOne.Ui.Blazor.Components.Dialog.Extensions;
using ViciOne.Ui.Blazor.Components.ExpandableMenu.Extensions;
using ViciOne.Ui.Blazor.Components.Sidebar.Extensions;
using ViciOne.Ui.Blazor.Components.TestingHelpers.SearchBox.Extensions;
using ViciOne.Ui.Blazor.Components.TestingHelpers.Sidebar.Extensions;
using ViciOne.Ui.ClusterEditor.Components.ToolbarInfrastructure;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components.ToolbarInfrastructure;

public class InfrastructureToolbarTests
{
    [Fact]
    public async Task Component_should_render()
    {
        // Arrange
        await using var ctx = new BunitContext();
        ctx.SetupTreeEditorJs();
        ctx.Services.AddExpandableMenu();
        ctx.Services.AddDialog();
        ctx.Services.AddSidebar();
        ctx.JSInterop.SetupForSidebar();
        ctx.SetupClusterEditorManagement();
        ctx.SetupDataflowStructureTreeAdapter();
        ctx.SetupStatisticService();
        ctx.SetupDatastore();
        ctx.JSInterop.SetupForSearchBox();

        ctx.CreateDiagramInstance();

        // Act
        var component = ctx.Render<InfrastructureToolbar>();

        // Assert
        Assert.NotNull(component);
    }
}
