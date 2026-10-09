using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom;
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
using ViciOne.Ui.ClusterEditor.Services;
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

    [Fact]
    public async Task An_empty_table_shows_no_column()
    {
        // Arrange
        await using var ctx = CreateContext();

        // Act
        var component = ctx.Render<PublishedConnectorsSectionContent>();

        // Assert
        Columns(component).Should().HaveCount(6)
            .And.AllSatisfy(column => column.Instance.Visible.Should().BeFalse());
        component.FindAll("th").Should().BeEmpty();
        component.Find(".advanced-table").ClassList.Should().Contain("published-connectors-empty");
    }

    [Fact]
    public async Task Publishing_the_first_connector_shows_every_column()
    {
        // Arrange
        await using var ctx = CreateContext();
        using var builder = BuilderFactory.Create();
        var (component, connector) = await RenderEmpty(ctx, builder);

        // Act
        await SetPublished(component, builder, connector, true);

        // Assert
        component.WaitForAssertion(() => component.FindAll("th").Should().HaveCount(6));
        component.Find(".advanced-table").ClassList.Should().NotContain("published-connectors-empty");
    }

    [Theory]
    [InlineData(nameof(DataGridConnectorWrapper.Path), 90)]
    [InlineData(nameof(DataGridConnectorWrapper.DesignName), 140)]
    [InlineData(nameof(DataGridConnectorWrapper.FunctionBlockName), 140)]
    [InlineData(nameof(DataGridConnectorWrapper.ConnectorName), 125)]
    [InlineData(nameof(DataGridConnectorWrapper.Links), 100)]
    [InlineData(nameof(DataGridConnectorWrapper.Description), 80)]
    public async Task Every_column_grows_from_its_minimum_width(string columnId, int minimumWidth)
    {
        // Arrange
        await using var ctx = CreateContext();

        // Act
        var component = ctx.Render<PublishedConnectorsSectionContent>();

        // Assert
        var column = Column(component, columnId).Instance;

        column.MinimumWidth.Should().Be(minimumWidth);
        column.Width.Should().BeNull();
    }

    [Fact]
    public async Task The_column_chooser_cannot_be_opened_while_the_table_is_empty()
    {
        // Arrange
        await using var ctx = CreateContext();

        // Act
        var component = ctx.Render<PublishedConnectorsSectionContent>();

        // Assert
        var button = component.Find(".column-chooser-button");

        button.ClassList.Should().Contain("disabled");
        button.GetAttribute("aria-disabled").Should().Be("true");
        button.HasAttribute("blazor:onclick").Should().BeFalse();
    }

    [Fact]
    public async Task Publishing_the_first_connector_enables_the_column_chooser()
    {
        // Arrange
        await using var ctx = CreateContext();
        using var builder = BuilderFactory.Create();
        var (component, connector) = await RenderEmpty(ctx, builder);

        // Act
        await SetPublished(component, builder, connector, true);

        // Assert
        component.WaitForAssertion(() => component.Find(".column-chooser-button").HasAttribute("blazor:onclick").Should().BeTrue());
        component.Find(".column-chooser-button").ClassList.Should().NotContain("disabled");
    }

    [Fact]
    public async Task A_column_hidden_in_the_chooser_stays_hidden_when_the_section_renders_again()
    {
        // Arrange
        await using var ctx = CreateContext();
        using var builder = BuilderFactory.Create();
        var (component, connector) = await RenderEmpty(ctx, builder);
        await SetPublished(component, builder, connector, true);
        await HideInChooser(component, nameof(DataGridConnectorWrapper.Path));

        // Act
        component.Render();

        // Assert
        ShownColumnIds(component).Should().HaveCount(5).And.NotContain(nameof(DataGridConnectorWrapper.Path));
    }

    [Fact]
    public async Task A_column_hidden_in_the_chooser_stays_hidden_when_the_table_fills_again()
    {
        // Arrange
        await using var ctx = CreateContext();
        using var builder = BuilderFactory.Create();
        var (component, connector) = await RenderEmpty(ctx, builder);
        await SetPublished(component, builder, connector, true);
        await HideInChooser(component, nameof(DataGridConnectorWrapper.Path));
        await SetPublished(component, builder, connector, false);
        component.WaitForAssertion(() => component.FindAll("th").Should().BeEmpty());

        // Act
        await SetPublished(component, builder, connector, true);

        // Assert
        component.WaitForAssertion(() => ShownColumnIds(component)
            .Should().HaveCount(5).And.NotContain(nameof(DataGridConnectorWrapper.Path)));
    }

    [Fact]
    public async Task A_column_ticked_in_a_chooser_left_open_stays_hidden_while_the_table_is_empty()
    {
        // Arrange
        await using var ctx = CreateContext();
        using var builder = BuilderFactory.Create();
        var (component, connector) = await RenderEmpty(ctx, builder);
        await SetPublished(component, builder, connector, true);
        await HideInChooser(component, nameof(DataGridConnectorWrapper.Path));
        await component.Find(".column-chooser-button").ClickAsync(new());
        await SetPublished(component, builder, connector, false);
        component.WaitForAssertion(() => component.FindAll("th").Should().BeEmpty());

        // Act
        await ChooserCheckbox(component, nameof(DataGridConnectorWrapper.Path)).InputAsync(new() { Value = true });

        // Assert
        component.FindAll("th").Should().BeEmpty();
        component.Find(".advanced-table").ClassList.Should().Contain("published-connectors-empty");
    }

    [Fact]
    public async Task Publishing_the_first_connector_restores_the_initial_sorting()
    {
        // Arrange
        await using var ctx = CreateContext();
        using var builder = BuilderFactory.Create();
        var (component, connector) = await RenderEmpty(ctx, builder);

        // Act
        await SetPublished(component, builder, connector, true);

        // Assert
        component.WaitForAssertion(() => AscendingColumnIds(component).Should().BeEquivalentTo(
            nameof(DataGridConnectorWrapper.DesignName),
            nameof(DataGridConnectorWrapper.FunctionBlockName)));
    }

    [Fact]
    public async Task A_table_that_starts_filled_shows_every_column_sorted_initially()
    {
        // Arrange
        await using var ctx = CreateContext();
        using var builder = BuilderFactory.Create();
        var connector = await AddPublishableConnector(ctx, builder);
        var service = ctx.Services.GetRequiredService<PublishedConnectorsService>();

        await ctx.Renderer.Dispatcher.InvokeAsync(() => builder.Editors.Connector.SetPublished((Connector)connector, true));
        SpinWait.SpinUntil(() => service.PublishedConnectorWrappers.Count == 1, TimeSpan.FromSeconds(5)).Should().BeTrue();

        // Act
        var component = ctx.Render<PublishedConnectorsSectionContent>();

        // Assert
        component.FindAll("th").Should().HaveCount(6);
        AscendingColumnIds(component).Should().BeEquivalentTo(
            nameof(DataGridConnectorWrapper.DesignName),
            nameof(DataGridConnectorWrapper.FunctionBlockName));
    }

    /// <remarks>
    /// Reloading the dataflow that is already open hands out new connector instances under the ids the rows are
    /// matched by, so a reused row would keep acting on the cluster that was replaced.
    /// </remarks>
    [Fact]
    public async Task A_reloaded_cluster_rebuilds_the_rows_on_its_own_connectors()
    {
        // Arrange
        await using var ctx = CreateContext();
        using var builder = BuilderFactory.Create();
        var (component, service, _, _) = await RenderWithOnePublishedConnectorAsync(ctx, builder);
        using var reloaded = BuilderFactory.CreateReloaded(builder);

        // Act
        await component.InvokeAsync(() => LoadAsync(ctx, reloaded));

        // Assert
        service.PublishedConnectorWrappers.Should().ContainSingle()
            .Which.Connector.Should().BeSameAs(reloaded.Cache.PublishedConnectors.Single());
    }

    [Fact]
    public async Task A_reloaded_cluster_drops_the_selection()
    {
        // Arrange
        await using var ctx = CreateContext();
        using var builder = BuilderFactory.Create();
        var (component, service, published, _) = await RenderWithOnePublishedConnectorAsync(ctx, builder);
        using var reloaded = BuilderFactory.CreateReloaded(builder);

        var table = component.FindComponent<SimpleTable<DataGridConnectorWrapper>>();

        await component.InvokeAsync(() => service.RequestPublishedConnectorSelection(published));
        component.WaitForAssertion(() => table.Instance.SelectedItems.Should().HaveCount(1));

        // Act
        await component.InvokeAsync(() => LoadAsync(ctx, reloaded));

        // Assert
        component.WaitForAssertion(() => table.Instance.SelectedItems.Should().BeEmpty());
    }

    [Fact]
    public async Task Activating_a_row_after_a_reload_selects_the_published_connector_marker()
    {
        // Arrange
        await using var ctx = CreateContext();
        using var builder = BuilderFactory.Create();
        var (component, service, _, _) = await RenderWithOnePublishedConnectorAsync(ctx, builder);
        using var reloaded = BuilderFactory.CreateReloaded(builder);
        await component.InvokeAsync(() => LoadAsync(ctx, reloaded));

        var table = component.FindComponent<SimpleTable<DataGridConnectorWrapper>>();
        var wrapper = service.PublishedConnectorWrappers.Single();

        // Act
        await component.InvokeAsync(() => table.Instance.RowDoubleClick.InvokeAsync(wrapper));

        // Assert
        var marker = ctx.Services.GetRequiredService<IDatastore>()
            .DataflowDiagramMapping.GetDiagramModel(wrapper.Connector).PublishedConnectorMarker;

        ctx.Services.GetRequiredService<SelectionManager>().IsSelected(marker).Should().BeTrue();
    }

    private static async Task<IConnector> AddPublishableConnector(BunitContext ctx, IClusterBuilder builder)
    {
        var datastore = ctx.Services.GetRequiredService<IDatastore>();
        var diagramService = ctx.Services.GetRequiredService<DiagramService>();

        await datastore.Load(builder, diagramService, Ct);

        var node = await datastore.AddFunctionBlock(diagramService, BuilderFactory.FbDesignId, new Point(0, 0), Ct);
        var functionBlock = datastore.DataflowDiagramMapping.GetModel(node);

        return functionBlock.Outputs.First(c => c.Name == "Value");
    }

    private static IEnumerable<string?> AscendingColumnIds(IRenderedComponent<PublishedConnectorsSectionContent> component)
        => component.FindAll("th[aria-sort='ascending']").Select(header => header.GetAttribute("data-column-id"));

    private static IElement ChooserCheckbox(IRenderedComponent<PublishedConnectorsSectionContent> component, string columnId)
    {
        var title = Column(component, columnId).Instance.Title;

        return component.FindAll(".column-chooser-content input[type=checkbox]")
            .Single(checkbox => checkbox.ParentElement!.NextElementSibling!.TextContent == title);
    }

    private static IRenderedComponent<SimpleTableTemplateColumn<DataGridConnectorWrapper>> Column(
        IRenderedComponent<PublishedConnectorsSectionContent> component, string columnId)
        => Columns(component).Single(column => column.Instance.Id == columnId);

    private static IReadOnlyList<IRenderedComponent<SimpleTableTemplateColumn<DataGridConnectorWrapper>>> Columns(
        IRenderedComponent<PublishedConnectorsSectionContent> component)
        => component.FindComponents<SimpleTableTemplateColumn<DataGridConnectorWrapper>>();

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

    private static async Task HideInChooser(IRenderedComponent<PublishedConnectorsSectionContent> component, string columnId)
    {
        component.WaitForAssertion(() => component.FindAll("th").Should().HaveCount(6));

        var column = Column(component, columnId).Instance;

        await component.InvokeAsync(() => column.VisibleChanged.InvokeAsync(false));
    }

    private static Task LoadAsync(BunitContext ctx, IClusterBuilder builder)
        => ctx.Services.GetRequiredService<IDatastore>()
            .Load(builder, ctx.Services.GetRequiredService<DiagramService>(), Ct);

    private static async Task<(IRenderedComponent<PublishedConnectorsSectionContent> Component, IConnector Connector)>
        RenderEmpty(BunitContext ctx, IClusterBuilder builder)
    {
        var connector = await AddPublishableConnector(ctx, builder);

        return (ctx.Render<PublishedConnectorsSectionContent>(), connector);
    }

    private static async Task<(IRenderedComponent<PublishedConnectorsSectionContent> Component, PublishedConnectorsService Service, IConnector Published, IConnector Unpublished)>
        RenderWithOnePublishedConnectorAsync(BunitContext ctx, IClusterBuilder builder)
    {
        var service = ctx.Services.GetRequiredService<PublishedConnectorsService>();
        var (component, published) = await RenderEmpty(ctx, builder);
        var unpublished = published.FunctionBlock.Inputs.First(c => c.Name == "Increment");

        await SetPublished(component, builder, published, true);

        return (component, service, published, unpublished);
    }

    private static async Task SetPublished(
        IRenderedComponent<PublishedConnectorsSectionContent> component, IClusterBuilder builder, IConnector connector, bool published)
    {
        var service = component.Services.GetRequiredService<PublishedConnectorsService>();

        await component.InvokeAsync(() => builder.Editors.Connector.SetPublished((Connector)connector, published));
        component.WaitForState(() => service.PublishedConnectorWrappers.Count == (published ? 1 : 0));
    }

    private static IEnumerable<string?> ShownColumnIds(IRenderedComponent<PublishedConnectorsSectionContent> component)
        => component.FindAll("th").Select(header => header.GetAttribute("data-column-id"));

    private static DataGridConnectorWrapper Wrapper()
        => new(Substitute.For<IConnectorOutput>(), null!, null!);
}
