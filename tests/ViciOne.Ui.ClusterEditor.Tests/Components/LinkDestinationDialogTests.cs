using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Blazor.Diagrams;
using Blazor.Diagrams.Core.Geometry;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Ui.Blazor.Components.Dialog.Extensions;
using ViciOne.Ui.Blazor.Components.Popup.Components;
using ViciOne.Ui.ClusterEditor.Components;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.Comparer;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using ViciOne.Ui.ClusterEditor.Tests.TestHelpers;
using Xunit;
using FunctionBlock = ViciOne.Cluster.Model.FunctionBlock;

namespace ViciOne.Ui.ClusterEditor.Tests.Components;

public sealed class LinkDestinationDialogTests : IAsyncLifetime
{
    private const string SelectedRowSelector = ".advanced-table tbody td.active";
    private const string TableRowSelector = ".advanced-table tbody tr";

    private readonly IClusterBuilder _builder;
    private readonly BunitContext _ctx;
    private readonly IDatastore _datastore;
    private readonly DiagramService _diagramService;
    private readonly LinkDestinationDialogService _dialogService;
    private readonly ClusterEditService _editService;
    private readonly Guid _fbDesignId;
    private readonly DatastoreState _state;

    private static CancellationToken TestCancellationToken => Xunit.TestContext.Current.CancellationToken;

    public LinkDestinationDialogTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop
            .Setup<int[]>("ViciOne.Diagram.BlockNode.measureNameFieldHeights", _ => true)
            .SetResult([.. Enumerable.Repeat(0, 10)]);

        _ctx.Services.AddDialog();
        _ctx.SetupLinkDestinationDialogService();
        _ctx.SetupConnectorService();

        _datastore = _ctx.Services.GetRequiredService<IDatastore>();
        _diagramService = _ctx.Services.GetRequiredService<DiagramService>();
        _diagramService.Diagram = new BlazorDiagram();
        _state = _ctx.Services.GetRequiredService<DatastoreState>();
        _editService = _ctx.Services.GetRequiredService<ClusterEditService>();
        _dialogService = _ctx.Services.GetRequiredService<LinkDestinationDialogService>();

        _builder = BuilderFactory.Create();
        _fbDesignId = BuilderFactory.FbDesignId;
    }

    public ValueTask InitializeAsync()
        => new(_datastore.Load(_builder, _diagramService, CancellationToken.None));

    public async ValueTask DisposeAsync()
    {
        _builder.Dispose();
        await _ctx.DisposeAsync();
        await _datastore.DisposeAsync();
        _diagramService.Dispose();
    }

    [Fact]
    public void Component_should_render()
    {
        // Act
        var component = _ctx.Render<LinkDestinationDialog>();

        // Assert
        Assert.NotNull(component);
    }

    [Fact(Skip = "Blocked: Popup renders its body into a SectionContent whose SectionOutlet lives in PopupRoot, and hosting both in bUnit throws inside Blazor's RenderTreeDiffBuilder. No dialog-body assertion is possible until that harness exists.")]
    public async Task Deletion_mode_opens_with_every_link_selected()
    {
        // Act
        var component = await ShowDialogAsync(deletionMode: true, destinationCount: 4);

        // Assert
        component.FindAll(SelectedRowSelector).Should().NotBeEmpty();
        SelectedRowCount(component).Should().Be(4);
    }

    [Fact(Skip = "Blocked: Popup renders its body into a SectionContent whose SectionOutlet lives in PopupRoot, and hosting both in bUnit throws inside Blazor's RenderTreeDiffBuilder. No dialog-body assertion is possible until that harness exists.")]
    public async Task Searching_in_deletion_mode_leaves_the_rows_it_still_shows_selected()
    {
        // Arrange
        var component = await ShowDialogAsync(deletionMode: true, destinationCount: 4);

        // Act
        Search(component, UniqueTextOfFirstDestination());

        // Assert
        // A hidden row renders nothing, so what must hold here is only that nothing visible was deselected.
        component.WaitForAssertion(() =>
        {
            var visibleRows = component.FindAll(TableRowSelector).Count;
            visibleRows.Should().BeLessThan(4);
            SelectedRowCount(component).Should().Be(visibleRows);
        });
    }

    [Fact(Skip = "Blocked: Popup renders its body into a SectionContent whose SectionOutlet lives in PopupRoot, and hosting both in bUnit throws inside Blazor's RenderTreeDiffBuilder. No dialog-body assertion is possible until that harness exists.")]
    public async Task Clearing_the_search_restores_every_row_with_the_selection_intact()
    {
        // Arrange
        var component = await ShowDialogAsync(deletionMode: true, destinationCount: 4);
        Search(component, UniqueTextOfFirstDestination());
        component.WaitForAssertion(() =>
            component.FindAll(TableRowSelector).Count.Should().BeLessThan(4));

        // Act
        Search(component, string.Empty);

        // Assert
        component.WaitForAssertion(() =>
        {
            component.FindAll(TableRowSelector).Count.Should().Be(4);
            SelectedRowCount(component).Should().Be(4);
        });
    }

    [Fact(Skip = "Blocked: Popup renders its body into a SectionContent whose SectionOutlet lives in PopupRoot, and hosting both in bUnit throws inside Blazor's RenderTreeDiffBuilder. No dialog-body assertion is possible until that harness exists.")]
    public async Task Pick_one_mode_seeds_the_first_rendered_row()
    {
        // Act
        var component = await ShowDialogAsync(deletionMode: false, destinationCount: 4);

        // Assert
        component.WaitForAssertion(() => component.FindAll(SelectedRowSelector).Should().NotBeEmpty());
        var rows = component.FindAll(TableRowSelector);
        var selectedRowIndex = rows.ToList().FindIndex(row => row.QuerySelector("td.active") is not null);
        selectedRowIndex.Should().Be(0);
    }

    [Fact(Skip = "Blocked: Popup renders its body into a SectionContent whose SectionOutlet lives in PopupRoot, and hosting both in bUnit throws inside Blazor's RenderTreeDiffBuilder. No dialog-body assertion is possible until that harness exists.")]
    public async Task Pick_one_seed_follows_the_tables_sort_not_the_link_order()
    {
        // Act
        var component = await ShowDialogAsync(deletionMode: false, destinationCount: 4);

        // Assert
        component.WaitForAssertion(() => component.FindAll(SelectedRowSelector).Should().NotBeEmpty());
        var expected = _dialogService.ConnectorWrappers
            .OrderBy(wrapper => wrapper.FunctionBlockName,
                Comparer<string>.Create(AlphaNumericComparer.Default.Compare))
            .First();
        var selectedRow = component.Find(SelectedRowSelector).ParentElement;
        selectedRow.Should().NotBeNull();
        selectedRow.TextContent.Should().Contain(expected.FunctionBlockName);
    }

    /// <summary>
    /// Renders the dialog inside a <see cref="PopupRoot"/> and shows it for a marker with
    /// <paramref name="destinationCount"/> link destinations.
    /// </summary>
    /// <remarks>
    /// A popup renders into a section owned by <see cref="PopupRoot"/>, so the dialog body only reaches the render
    /// tree when one is hosted.
    /// </remarks>
    private async Task<IRenderedComponent<PopupRoot>> ShowDialogAsync(bool deletionMode, int destinationCount)
    {
        var marker = await CreateMarkerWithDestinations(destinationCount);

        var popupHost = _ctx.Render<PopupRoot>();
        _ctx.Render<LinkDestinationDialog>();
        var component = popupHost;

        _dialogService.IsDeletionMode = deletionMode;
        _dialogService.SetSourceConnectorMarker(marker);
        _dialogService.ConnectorWrappers.Should().HaveCount(destinationCount);

        await component.InvokeAsync(() => _dialogService.SetVisibility(true));

        // Showing is dispatched onto the renderer without being awaited, so the rows are not there on return.
        component.WaitForAssertion(() =>
            component.FindAll(TableRowSelector).Count.Should().Be(destinationCount));

        return component;
    }

    /// <summary>
    /// Creates a marker on one source connector with <paramref name="destinationCount"/> outgoing links.
    /// </summary>
    private async Task<ConnectorMarker> CreateMarkerWithDestinations(int destinationCount)
    {
        var source = await AddFunctionBlock(new Point(50, 50));
        var sourceConnector = source.SystemOutputs.First();

        var marker = new ConnectorMarker(_state.DataflowDiagramMapping.GetDiagramModel(sourceConnector));

        for (var index = 0; index < destinationCount; index++)
        {
            var destination = await AddFunctionBlock(new Point(300, 50 + (index * 100)));
            marker.Links.Add(_builder.Editors.Connector.AddLink(sourceConnector,
                destination.SystemInputs.First()));
        }

        return marker;
    }

    private async Task<FunctionBlock> AddFunctionBlock(Point position)
    {
        var node = await _editService.AddFunctionBlock(_diagramService, _fbDesignId, position, TestCancellationToken);

        return _state.DataflowDiagramMapping.GetModel(node);
    }

    private static void Search(IRenderedComponent<PopupRoot> component, string text)
        => component.Find(".search-container input").Input(text);

    private static int SelectedRowCount(IRenderedComponent<PopupRoot> component)
        => component.FindAll(SelectedRowSelector)
            .Select(cell => cell.ParentElement)
            .Distinct()
            .Count();

    private string UniqueTextOfFirstDestination()
        => _dialogService.ConnectorWrappers[0].FunctionBlockName;
}
