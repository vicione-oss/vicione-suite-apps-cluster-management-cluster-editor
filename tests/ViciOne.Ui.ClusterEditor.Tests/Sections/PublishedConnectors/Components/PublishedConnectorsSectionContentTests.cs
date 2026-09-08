using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Bunit;
using DevExpress.Blazor;
using DevExpress.Data.Filtering;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Components;
using ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Services;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using ViciOne.Ui.ClusterEditor.Tests.TestHelpers;
using Xunit;
using TestContext = Bunit.TestContext;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.PublishedConnectors.Components;

public class PublishedConnectorsSectionContentTests
{
    private static CancellationToken Ct => Xunit.TestContext.Current.CancellationToken;

    [Fact]
    public void Component_should_render()
    {
        // Arrange
        using var ctx = CreateContext();

        ctx.CreateDiagramInstance();

        // Act
        var component = ctx.RenderComponent<PublishedConnectorsSectionContent>();

        // Assert
        Assert.NotNull(component);
    }

    private static TestContext CreateContext()
    {
        var ctx = new TestContext();
        ctx.SetupDevExpressBlazor();
        ctx.SetupDiagramService();
        ctx.Services.AddPublishedConnectorsSectionContextMenu();
        ctx.SetupPublishedConnectorsService();
        ctx.SetupSelectionManager();
        ctx.SetupConnectorService();
        ctx.SetupDragService();

        ctx.JSInterop
            .Setup<int[]>("ViciOne.Diagram.BlockNode.measureNameFieldHeights", _ => true)
            .SetResult([.. Enumerable.Repeat(0, 10)]);

        return ctx;
    }

    private static Dictionary<string, bool> GetGroupExpansionStateByConnectorName(IGrid grid)
    {
        var expansionStates = new Dictionary<string, bool>();

        for (var rowIndex = 0; rowIndex < grid.GetVisibleRowCount(); rowIndex++)
        {
            if (!grid.IsGroupRow(rowIndex) || grid.GetRowLevel(rowIndex) != 1)
                continue;

            var connectorName = (string)grid.GetRowValue(rowIndex, nameof(DataGridConnectorWrapper.ConnectorName));
            expansionStates[connectorName] = grid.IsGroupRowExpanded(rowIndex);
        }

        return expansionStates;
    }

    [Fact]
    public async Task Requested_published_connector_is_selected_in_the_grid()
    {
        // Arrange
        using var ctx = CreateContext();
        using var builder = BuilderFactory.Create();
        var (component, service, grid, output, _) = await SetupPublishedConnectorsAsync(ctx, builder);

        var wrapper = service.PublishedConnectorWrappers.Single(w => w.Connector.Id == output.Id);

        // Act
        await component.InvokeAsync(() => service.RequestPublishedConnectorSelection(output));

        // Assert
        component.WaitForAssertion(() => grid.IsDataItemSelected(wrapper).Should().BeTrue());
    }

    [Fact]
    public async Task Requested_published_connector_stays_selected_when_a_filter_hides_it()
    {
        // Arrange
        using var ctx = CreateContext();
        using var builder = BuilderFactory.Create();
        var (component, service, grid, output, _) = await SetupPublishedConnectorsAsync(ctx, builder);

        var wrapper = service.PublishedConnectorWrappers.Single(w => w.Connector.Id == output.Id);

        // Show inputs only, which excludes the output connector we are about to request.
        await component.InvokeAsync(() => grid.SetFilterCriteria(CriteriaOperator.FromLambda<DataGridConnectorWrapper>(c => c.IsInput)));

        // Act
        await component.InvokeAsync(() => service.RequestPublishedConnectorSelection(output));

        // Assert
        component.WaitForAssertion(() => grid.IsDataItemSelected(wrapper).Should().BeTrue());

        // Clearing the filter brings the row back, still selected.
        await component.InvokeAsync(grid.ClearFilter);
        component.WaitForAssertion(() => grid.IsDataItemSelected(wrapper).Should().BeTrue());
    }

    [Fact]
    public async Task Requesting_a_published_connector_expands_only_its_own_group()
    {
        // Arrange
        using var ctx = CreateContext();
        using var builder = BuilderFactory.Create();
        var (component, service, grid, output, input) = await SetupPublishedConnectorsAsync(ctx, builder);

        // Group by connector name as well, so the two connectors end up in separate sub groups.
        await component.InvokeAsync(() => grid.GroupBy(nameof(DataGridConnectorWrapper.ConnectorName)));
        await component.InvokeAsync(grid.CollapseAllGroupRows);

        // Act
        await component.InvokeAsync(() => service.RequestPublishedConnectorSelection(output));

        // Assert
        component.WaitForAssertion(() =>
        {
            var expansionStates = GetGroupExpansionStateByConnectorName(grid);
            expansionStates[output.Name].Should().BeTrue();
            expansionStates[input.Name].Should().BeFalse();
        });
    }

    private static async Task<(IRenderedComponent<PublishedConnectorsSectionContent> Component, PublishedConnectorsService Service, IGrid Grid, IConnector Output, IConnector Input)>
        SetupPublishedConnectorsAsync(TestContext ctx, IClusterBuilder builder)
    {
        ctx.CreateDiagramInstance();

        var datastore = ctx.Services.GetRequiredService<IDatastore>();
        var diagramService = ctx.Services.GetRequiredService<DiagramService>();
        var service = ctx.Services.GetRequiredService<PublishedConnectorsService>();

        await datastore.Load(builder, diagramService, Ct);

        var node = await datastore.AddFunctionBlock(diagramService, BuilderFactory.FbDesignId, new(0, 0), Ct);
        var functionBlock = datastore.DataflowDiagramMapping.GetModel(node);

        // Deliberately two differently named connectors: grouping by connector name has to put
        // them into separate groups.
        var output = functionBlock.Outputs.First(c => c.Name == "Value");
        var input = functionBlock.Inputs.First(c => c.Name == "Increment");

        var component = ctx.RenderComponent<PublishedConnectorsSectionContent>();

        builder.Editors.Connector.SetPublished(output, true);
        builder.Editors.Connector.SetPublished(input, true);

        component.WaitForState(() => service.PublishedConnectorWrappers.Count() == 2);

        return (component, service, component.FindComponent<DxGrid>().Instance, output, input);
    }
}
