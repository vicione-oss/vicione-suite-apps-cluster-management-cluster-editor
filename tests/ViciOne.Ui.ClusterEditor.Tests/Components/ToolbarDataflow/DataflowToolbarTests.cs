using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using ViciOne.Ui.Blazor.Components.SectionRail.Extensions;
using ViciOne.Ui.ClusterEditor.Components.ToolbarDataflow;
using ViciOne.Ui.ClusterEditor.Components.ToolbarDataflow.Extensions;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;
#if DEBUG
using ViciOne.Ui.ClusterEditor.Sections.Debugging.Services;
#endif
using ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Services;
using ViciOne.Ui.ClusterEditor.Sections.Topology.Services;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components.ToolbarDataflow;

public class DataflowToolbarTests
{
    [Fact]
    public void Component_should_render()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.SetupDevExpressBlazor();
        ctx.SetupTreeEditorJs();
        ctx.Services.AddSectionRail<DataflowToolbarSection>();
        ctx.Services.TryAddScoped<NumericPropertyDescriptorBuilderProvider>();
        ctx.Services.AddToolbarDataflow();
        ctx.Services.TryAddScoped<ToolbarService>();
        ctx.SetupDatastore();
        ctx.SetupDiagramService();
        ctx.SetupConnectorService();
        ctx.Services.TryAddScoped<LabelOrderService>();
        ctx.SetupSelectionManager();
        ctx.Services.TryAddScoped<TraceService>();
        ctx.Services.TryAddScoped<TopologyTreeAdapter>();
        ctx.Services.AddDataPortContextMenu();
        ctx.SetupDragService();
        ctx.SetupLinkDestinationDialogService();
        ctx.Services.TryAddScoped(_ => Substitute.For<IRulesetProvider>());
        ctx.SetupDataPortTreeAdapter();
        ctx.Services.AddPublishedConnectorsSectionContextMenu();
        ctx.Services.TryAddScoped<PublishedConnectorsService>();
        ctx.SetupResizeObserver();
#if DEBUG
        ctx.Services.TryAddScoped<DebugService>();
#endif
        ctx.CreateDiagramInstance();

        // Act
        var component = ctx.RenderComponent<DataflowToolbar>();

        // Assert
        Assert.NotNull(component);
    }
}
