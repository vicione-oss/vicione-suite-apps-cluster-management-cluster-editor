using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Services;
using ViciOne.Ui.Blazor.Components.Sidebar.Enums;
using ViciOne.Ui.ClusterEditor.Components.ToolbarDataflow.Models;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.MonochromeIcons.Core.Enums;
using ViciOne.Ui.MonochromeIcons.Core.Extensions;

namespace ViciOne.Ui.ClusterEditor.Components.ToolbarDataflow;

public sealed partial class DataflowToolbar : ComponentBase, IDisposable
{
    private static readonly string s_clusterIconCssClass = MonochromeIconName.Cluster2.GetCssClasses().ToSpaceSeparated();
    private static readonly string s_dataPortsIconCssClass = MonochromeIconName.DataSwitch.GetCssClasses().ToSpaceSeparated();
#if DEBUG
    private static readonly string s_debugIconCssClass = MonochromeIconName.BugLight.GetCssClasses().ToSpaceSeparated();
#endif
    private static readonly string s_libraryIconCssClass = MonochromeIconName.Library.GetCssClasses().ToSpaceSeparated();
    private static readonly string s_propertiesIconCssClass = MonochromeIconName.Properties.GetCssClasses().ToSpaceSeparated();
    private static readonly string s_publishedConnectorsIconCssClass = MonochromeIconName.PublishedConnectorFull.GetCssClasses().ToSpaceSeparated();
    private static readonly string s_searchAndToolsIconCssClass = MonochromeIconName.SearchAndTools.GetCssClasses().ToSpaceSeparated();

    private DataflowToolbarSection _activeSectionId = DataflowToolbarSection.Properties;
    private bool _expanded;
    private int? _sidebarFluidWidth;

    [Inject] private IDatastore Datastore { get; set; } = default!;
    [Inject] private IPropertyGridController<DataflowToolbarPropertyGridContext> PropertyGridController { get; set; } = default!;
    [Inject] private SelectionManager SelectionManager { get; set; } = default!;
    [Inject] private ToolbarService ToolbarService { get; set; } = default!;

    public void Dispose()
    {
        SelectionManager.ConnectorSelectionChanged -= OnConnectorSelectionChanged;
        ToolbarService.DataflowToolbarSectionRequested -= OnDataflowToolbarSectionRequestedAsync;
    }

    private SidebarMode GetSidebarMode()
        => _expanded ? SidebarMode.Fluid : SidebarMode.Compact;

    private void OnConnectorSelectionChanged(IEnumerable<BlockNodeConnector> selection)
    {
        var connectors = Datastore.DataflowDiagramMapping.GetModels(selection).ToList();

        PropertyGridController.SetInstances(connectors,
            new DataflowToolbarPropertyGridContext { SelectedConnectors = connectors });
    }

    private async void OnDataflowToolbarSectionRequestedAsync(DataflowToolbarSection requestedSection)
    {
        _expanded = true;
        _activeSectionId = requestedSection;

        await InvokeAsync(StateHasChanged);
    }

    protected override void OnInitialized()
    {
        SelectionManager.ConnectorSelectionChanged += OnConnectorSelectionChanged;
        ToolbarService.DataflowToolbarSectionRequested += OnDataflowToolbarSectionRequestedAsync;
    }
}
