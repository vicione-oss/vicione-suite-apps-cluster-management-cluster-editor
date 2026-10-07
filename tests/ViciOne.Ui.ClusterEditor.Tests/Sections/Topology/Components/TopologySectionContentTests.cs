using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.Ui.Blazor.Components.Dialog.Extensions;
using ViciOne.Ui.Blazor.Components.TestingHelpers.SearchBox.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.Topology.Components;
using ViciOne.Ui.ClusterEditor.Sections.Topology.Services;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.Topology.Components;

public class TopologySectionContentTests
{
    [Fact]
    public async Task Component_should_render()
    {
        // Arrange
        await using var ctx = new BunitContext();
        ctx.SetupTreeEditorJs();
        ctx.SetupClusterEditorManagement();
        ctx.Services.TryAddScoped<TopologyTreeAdapter>();
        ctx.Services.AddDialog();
        ctx.JSInterop.SetupForSearchBox();

        // Act
        var component = ctx.Render<TopologySectionContent>();

        // Assert
        Assert.NotNull(component);
    }
}
