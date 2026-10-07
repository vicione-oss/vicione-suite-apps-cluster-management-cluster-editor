using System.Threading.Tasks;
using Bunit;
using ViciOne.Ui.Blazor.Components.Dialog.Extensions;
using ViciOne.Ui.Blazor.Components.TestingHelpers.SearchBox.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.Dataflow.Components;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.Dataflow.Components;

public class DataflowSectionTests
{
    [Fact]
    public async Task Component_should_render()
    {
        // Arrange
        await using var ctx = new BunitContext();
        ctx.SetupTreeEditorJs();
        ctx.SetupDatastore();
        ctx.SetupDiagramService();
        ctx.SetupDataflowStructureTreeAdapter();
        ctx.Services.AddDialog();
        ctx.JSInterop.SetupForSearchBox();

        ctx.CreateDiagramInstance();

        // Act
        var component = ctx.Render<DataflowSection>();

        // Assert
        Assert.NotNull(component);
    }
}
