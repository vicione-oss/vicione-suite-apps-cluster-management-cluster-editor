using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Components.Web;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Constants;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;

namespace ViciOne.Ui.ClusterEditor.Components.NodeEditorServices;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated through dependency injection")]
internal sealed class ConnectorMarkerDeletionController(
    SelectionManager selectionManager,
    IDatastore datastore,
    ConnectorService connectorService,
    InputEventService inputEventService,
    LinkDestinationDialogService linkDestinationDialogService) : IDisposable
{
    private bool _initialized;

    private void DeleteSelectedConnectorMarkerLinks()
    {
        if (selectionManager.SelectedConnectorMarker.Count == 0)
            return;

        if (selectionManager.SelectedConnectorMarker.Count == 1)
        {
            var selectedMarker = selectionManager.SelectedConnectorMarker[0];

            if (selectedMarker.Links.Count == 0 && selectedMarker.Connector.Connector.Published)
            {
                datastore.Builder.Editors.Connector.SetPublished(selectedMarker.Connector.Connector, false);
                selectionManager.DeselectAll(SelectionMode.ConnectorMarker);
                return;
            }

            if (selectedMarker.Links.Count == 1)
            {
                connectorService.DeleteInvisibleLinks(selectedMarker.Links);
                selectionManager.DeselectAll(SelectionMode.ConnectorMarker);
            }
            else
            {
                linkDestinationDialogService.IsDeletionMode = true;
                linkDestinationDialogService.SetSourceConnectorMarker(selectedMarker);
                linkDestinationDialogService.SetVisibility(true);
                linkDestinationDialogService.LinksToDeleteSelected += OnDetailDialogServiceLinksToDeleteSelected;
            }
        }
        else
        {
            if (selectionManager.SelectedConnectorMarker.All(cm => cm.Links.Count == 0))
            {
                foreach (var connectorMarker in selectionManager.SelectedConnectorMarker)
                    datastore.Builder.Editors.Connector.SetPublished(connectorMarker.Connector.Connector, false);
            }
            else
            {
                foreach (var connectorMarker in selectionManager.SelectedConnectorMarker)
                    connectorService.DeleteInvisibleLinks(connectorMarker.Links);
            }

            selectionManager.DeselectAll(SelectionMode.ConnectorMarker);
        }
    }

    public void Dispose()
        => Teardown();

    public void Initialize()
    {
        if (_initialized)
            Teardown();

        inputEventService.KeyDown += OnKeyDown;

        _initialized = true;
    }

    private void OnDetailDialogServiceLinksToDeleteSelected(IReadOnlyList<Link> links)
    {
        connectorService.DeleteInvisibleLinks(links);

        var pubLinkCount = linkDestinationDialogService.SourceConnectorMarker?.Links.Count;
        if (pubLinkCount < 1)
            selectionManager.DeselectAll(SelectionMode.ConnectorMarker);
    }

    private void OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Code == KeyboardCodes.Delete)
            DeleteSelectedConnectorMarkerLinks();
    }

    private void Teardown()
    {
        inputEventService.KeyDown -= OnKeyDown;
        linkDestinationDialogService.LinksToDeleteSelected -= OnDetailDialogServiceLinksToDeleteSelected;

        _initialized = false;
    }
}
