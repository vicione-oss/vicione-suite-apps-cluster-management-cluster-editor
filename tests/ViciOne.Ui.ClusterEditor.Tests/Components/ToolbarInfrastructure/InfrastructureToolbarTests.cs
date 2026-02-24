using ViciOne.Ui.Blazor.Components.ExpandableMenu.Extensions;
using ViciOne.Ui.ClusterEditor.Components.ToolbarInfrastructure;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components.ToolbarInfrastructure;

public class InfrastructureToolbarTests
{
    [Fact]
    public void Component_should_render()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.SetupDevExpressBlazor();
        ctx.SetupTreeEditorJs();
        ctx.Services.AddExpandableMenu();
        ctx.SetupDataManagementService();
        ctx.SetupDataflowStructureTreeAdapter();
        ctx.SetupStatisticService();
        ctx.SetupDatastore();

        ctx.CreateDiagramInstance();

        // Act
        var component = ctx.RenderComponent<InfrastructureToolbar>();

        // Assert
        Assert.NotNull(component);
    }
}
