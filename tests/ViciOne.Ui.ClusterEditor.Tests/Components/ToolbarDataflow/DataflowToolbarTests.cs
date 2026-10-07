using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using ViciOne.Ui.Blazor.Components.SectionRail.Extensions;
using ViciOne.Ui.Blazor.Components.TestingHelpers.SearchBox.Extensions;
using ViciOne.Ui.ClusterEditor.Components.ToolbarDataflow;
using ViciOne.Ui.ClusterEditor.Components.ToolbarDataflow.Extensions;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;

using ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Components;
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
    public async Task Component_should_render()
    {
        // Arrange
        await using var ctx = new BunitContext();
        // The Published Connectors section's table loads its own JS module, which this toolbar test does not set up.
        ctx.ComponentFactories.AddStub<PublishedConnectorsSectionContent>();
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
        ctx.SetupPublishedConnectorsSection();
        ctx.SetupResizeObserver();
        ctx.SetupDropDown();
        ctx.SetupComboBox();
        ctx.JSInterop.SetupForSearchBox();

        ctx.CreateDiagramInstance();

        // Act
        var component = ctx.Render<DataflowToolbar>();

        // Assert
        Assert.NotNull(component);
    }
}
