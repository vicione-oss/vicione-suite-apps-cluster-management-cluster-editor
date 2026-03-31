using ViciOne.Ui.Blazor.Components.Dialog.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.Dataflow.Components;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.Dataflow.Components;

public class DataflowSectionTests
{
    [Fact]
    public void Component_should_render()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.SetupDevExpressBlazor();
        ctx.SetupTreeEditorJs();
        ctx.SetupDatastore();
        ctx.SetupDiagramService();
        ctx.SetupDataflowStructureTreeAdapter();
        ctx.Services.AddDialog();

        ctx.CreateDiagramInstance();

        // Act
        var component = ctx.RenderComponent<DataflowSection>();

        // Assert
        Assert.NotNull(component);
    }
}
