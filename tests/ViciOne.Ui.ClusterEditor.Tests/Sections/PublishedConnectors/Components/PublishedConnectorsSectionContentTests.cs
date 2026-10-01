using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Blazor.Diagrams.Core.Geometry;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.ContextMenu.Services;
using ViciOne.Ui.Blazor.Components.Tables.SimpleTable.Components;
using ViciOne.Ui.Blazor.Components.Tables.SimpleTable.Components.Columns;
using ViciOne.Ui.ClusterEditor.Components;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Components;
using ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Services;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using ViciOne.Ui.ClusterEditor.Tests.TestHelpers;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.PublishedConnectors.Components;

public class PublishedConnectorsSectionContentTests
{
    private static CancellationToken Ct => Xunit.TestContext.Current.CancellationToken;

    [Fact]
    public async Task Component_should_render()
    {
        // Arrange
        await using var ctx = CreateContext();

        // Act
        var component = ctx.Render<PublishedConnectorsSectionContent>();

        // Assert
        Assert.NotNull(component);
    }

    // Enter is a per-column callback, so a column added without a handler is silent: Enter stops working
    // while the lead sits in it, with nothing in the UI to explain it.
    [Fact]
    public async Task Every_column_activates_the_row_so_enter_is_never_silent()
    {
        // Arrange
        await using var ctx = CreateContext();

        // Act
        var component = ctx.Render<PublishedConnectorsSectionContent>();

        // Assert
        var columns = component.FindComponents<SimpleTableTemplateColumn<DataGridConnectorWrapper>>();

        columns.Should().HaveCount(6);
        columns.Should().AllSatisfy(column => column.Instance.CellActivated.HasDelegate.Should().BeTrue());
    }

    [Fact]
    public async Task The_markup_drag_ghost_is_registered_while_the_section_lives()
    {
        // Arrange
        await using var ctx = CreateContext();
        var rowDragGhost = ctx.Services.GetRequiredService<PublishedConnectorRowDragGhost>();

        // Act
        ctx.Render<PublishedConnectorsSectionContent>();

        // Assert
        rowDragGhost.MarkupDragGhost.Should().NotBeNull();
    }

    // The container holds one ghost slot for the whole application, so a section that does not clear its own
    // on the way out leaves a dangling one behind.
    [Fact]
    public async Task The_markup_drag_ghost_is_cleared_with_the_section()
    {
        // Arrange
        await using var ctx = CreateContext();
        var rowDragGhost = ctx.Services.GetRequiredService<PublishedConnectorRowDragGhost>();
        ctx.Render<PublishedConnectorsSectionContent>();

        // Act
        await ctx.DisposeComponentsAsync();

        // Assert
        rowDragGhost.MarkupDragGhost.Should().BeNull();
    }

    // The table suppresses the browser's own menu whenever a handler is wired, so wiring it unconditionally
    // would leave a right-click opening nothing at all.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task The_row_context_menu_is_wired_only_when_the_custom_menu_would_open(bool useCustomMenu)
    {
        // Arrange
        await using var ctx = CreateContext(useCustomMenu);

        // Act
        var component = ctx.Render<PublishedConnectorsSectionContent>();

        // Assert
        component.FindComponent<SimpleTable<DataGridConnectorWrapper>>()
            .Instance.RowContextMenuRequested.HasDelegate
            .Should().Be(useCustomMenu);
    }

    // The table resolves the payload from the whole selection, so a row the active filter hides would be
    // dragged along with nothing on screen to say so.
    [Fact]
    public async Task A_drag_carries_the_visible_selection_and_not_the_rows_the_filter_hides()
    {
        // Arrange
        await using var ctx = CreateContext();
        var component = ctx.Render<PublishedConnectorsSectionContent>();
        var table = component.FindComponent<SimpleTable<DataGridConnectorWrapper>>().Instance;

        var visible = Wrapper();
        var hidden = Wrapper();

        await component.InvokeAsync(() => table.VisibleSelectionChanged.InvokeAsync([visible]));

        // Act
        var payload = table.DragPayloadProvider!.GetPayload(visible, [visible, hidden]);

        // Assert
        payload.Should().Equal(visible);
    }

    // The direction buttons are FilterButton models rendered by SearchAndFilterComponent, so the @onclick that
    // reaches them belongs to that component's render tree and marks it dirty, not this section. Without an
    // explicit render the table never receives the new FilterState and the rows keep whichever filter was
    // applied last. The handler is invoked directly rather than through InvokeOnFilterClickedFn because the
    // buttons are disabled while the service has no published connectors, which no test can seed.
    [Fact]
    public async Task Pressing_a_direction_filter_hands_the_new_filter_state_to_the_table()
    {
        // Arrange
        await using var ctx = CreateContext();
        var component = ctx.Render<PublishedConnectorsSectionContent>();
        var table = component.FindComponent<SimpleTable<DataGridConnectorWrapper>>();
        var filterButtons = component.FindComponent<SearchAndFilterComponent>().Instance.FilterButtons;

        var stateBeforeClick = table.Instance.FilterState;

        // Act
        await component.InvokeAsync(filterButtons[0].OnFilterClickedFn!);

        // Assert
        table.Instance.FilterState.Should().NotBe(stateBeforeClick);
    }

    // The other half of the diagram's "double click a published marker" gesture: BlockComponent asks the
    // service, and this is the end that has to turn the connector into the row the user sees selected.
    [Fact]
    public async Task A_requested_published_connector_becomes_the_tables_selection()
    {
        // Arrange
        await using var ctx = CreateContext();
        using var builder = BuilderFactory.Create();
        var (component, service, published, _) = await RenderWithOnePublishedConnectorAsync(ctx, builder);

        var table = component.FindComponent<SimpleTable<DataGridConnectorWrapper>>();
        var wrapper = service.PublishedConnectorWrappers.Single(w => w.Connector.Id == published.Id);

        // Act
        await component.InvokeAsync(() => service.RequestPublishedConnectorSelection(published));

        // Assert
        component.WaitForAssertion(() => table.Instance.SelectedItems.Should().Equal(wrapper));
    }

    // Unpublishing races the gesture: the marker is double clicked on a diagram that has not caught up yet.
    // Collapsing the selection to nothing would be a silent, unexplained change to what the footer counts.
    [Fact]
    public async Task Requesting_a_connector_that_has_no_row_leaves_the_selection_alone()
    {
        // Arrange
        await using var ctx = CreateContext();
        using var builder = BuilderFactory.Create();
        var (component, service, published, unpublished) = await RenderWithOnePublishedConnectorAsync(ctx, builder);

        var table = component.FindComponent<SimpleTable<DataGridConnectorWrapper>>();
        var wrapper = service.PublishedConnectorWrappers.Single(w => w.Connector.Id == published.Id);

        await component.InvokeAsync(() => service.RequestPublishedConnectorSelection(published));
        component.WaitForAssertion(() => table.Instance.SelectedItems.Should().Equal(wrapper));

        // Act
        await component.InvokeAsync(() => service.RequestPublishedConnectorSelection(unpublished));

        // Assert
        table.Instance.SelectedItems.Should().Equal(wrapper);
    }

    // Publishing has to go through the builder: that is what fills the service's wrapper list. Publishing on
    // the diagram model instead only moves the marker and leaves the section empty.
    private static async Task<(IRenderedComponent<PublishedConnectorsSectionContent> Component, PublishedConnectorsService Service, IConnector Published, IConnector Unpublished)>
        RenderWithOnePublishedConnectorAsync(BunitContext ctx, IClusterBuilder builder)
    {
        var datastore = ctx.Services.GetRequiredService<IDatastore>();
        var diagramService = ctx.Services.GetRequiredService<DiagramService>();
        var service = ctx.Services.GetRequiredService<PublishedConnectorsService>();

        await datastore.Load(builder, diagramService, Ct);

        var node = await datastore.AddFunctionBlock(diagramService, BuilderFactory.FbDesignId, new Point(0, 0), Ct);
        var functionBlock = datastore.DataflowDiagramMapping.GetModel(node);

        var published = functionBlock.Outputs.First(c => c.Name == "Value");
        var unpublished = functionBlock.Inputs.First(c => c.Name == "Increment");

        var component = ctx.Render<PublishedConnectorsSectionContent>();

        builder.Editors.Connector.SetPublished((Connector)published, true);
        component.WaitForState(() => service.PublishedConnectorWrappers.Count == 1);

        return (component, service, published, unpublished);
    }

    // ConnectorDesign is abstract with an inaccessible member and no public implementation outside its own
    // assembly. The policy sorts rows by identity, so neither design is read.
    private static DataGridConnectorWrapper Wrapper()
        => new(Substitute.For<IConnectorOutput>(), null!, null!);

    private static BunitContext CreateContext(bool useCustomMenu = true)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var contextMenuSettings = Substitute.For<IContextMenuSettings>();
        contextMenuSettings.UseCustomMenu.Returns(useCustomMenu);
        ctx.Services.TryAddScoped(_ => contextMenuSettings);

        ctx.Services.AddPublishedConnectorsSectionContextMenu();
        ctx.SetupPublishedConnectorsSection();

        // Projecting a function block onto the diagram measures its name field, and the loose JS runtime
        // would answer that call with null rather than the heights the mapper indexes into.
        ctx.JSInterop
            .Setup<int[]>("ViciOne.Diagram.BlockNode.measureNameFieldHeights", _ => true)
            .SetResult([.. Enumerable.Repeat(0, 10)]);

        ctx.CreateDiagramInstance();

        return ctx;
    }
}
