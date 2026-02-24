using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.Topology.Components;
using ViciOne.Ui.ClusterEditor.Sections.Topology.Services;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.Topology.Components;

public class TopologySectionContentTests
{
    [Fact]
    public void Component_should_render()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.SetupDevExpressBlazor();
        ctx.SetupTreeEditorJs();
        ctx.SetupDataManagementService();
        ctx.Services.TryAddScoped<TopologyTreeAdapter>();

        // Act
        var component = ctx.RenderComponent<TopologySectionContent>();

        // Assert
        Assert.NotNull(component);
    }
}
