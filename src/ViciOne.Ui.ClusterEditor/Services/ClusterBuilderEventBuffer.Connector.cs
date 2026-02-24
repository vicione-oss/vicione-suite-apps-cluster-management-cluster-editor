using System;
using System.Collections.Generic;
using ViciOne.Cluster.Model;

namespace ViciOne.Ui.ClusterEditor.Services;

public sealed partial class ClusterBuilderEventBuffer
{
    private readonly List<Link> _connectorLinkAddedBuffer = [];
    private readonly List<Link> _connectorLinkRemovedBuffer = [];
    private readonly List<(object? sender, System.ComponentModel.PropertyChangedEventArgs e)> _connectorPropertyChangedBuffer = [];

    public event Action<IEnumerable<Link>>? ConnectorLinksAdded;
    public event Action<IEnumerable<Link>>? ConnectorLinksRemoved;
    public event Action<IEnumerable<(object? Sender, System.ComponentModel.PropertyChangedEventArgs EventArgs)>>? ConnectorPropertiesChanged;

    private void AttachConnectorEvents()
    {
        Builder.Editors.Connector.LinkAdded += OnConnectorLinkAdded;
        Builder.Editors.Connector.LinkRemoved += OnConnectorLinkRemoved;
        Builder.Editors.Connector.PropertyChanged += OnConnectorPropertyChanged;
    }

    private void DetachConnectorEvents()
    {
        Builder.Editors.Connector.LinkAdded -= OnConnectorLinkAdded;
        Builder.Editors.Connector.LinkRemoved -= OnConnectorLinkRemoved;
        Builder.Editors.Connector.PropertyChanged -= OnConnectorPropertyChanged;
    }

    private void FireConnectorEvents()
    {
        if (_connectorLinkAddedBuffer.Count > 0)
        {
            ConnectorLinksAdded?.Invoke([.. _connectorLinkAddedBuffer]);
            _connectorLinkAddedBuffer.Clear();
        }

        if (_connectorLinkRemovedBuffer.Count > 0)
        {
            ConnectorLinksRemoved?.Invoke([.. _connectorLinkRemovedBuffer]);
            _connectorLinkRemovedBuffer.Clear();
        }

        if (_connectorPropertyChangedBuffer.Count > 0)
        {
            ConnectorPropertiesChanged?.Invoke([.. _connectorPropertyChangedBuffer]);
            _connectorPropertyChangedBuffer.Clear();
        }
    }

    private void OnConnectorLinkAdded(Link link)
    {
        if (_connectorLinkAddedBuffer.Contains(link))
            return;

        _connectorLinkAddedBuffer.Add(link);
        StartBufferTimer();
    }

    private void OnConnectorLinkRemoved(Link link)
    {
        if (_connectorLinkRemovedBuffer.Contains(link))
            return;

        _connectorLinkRemovedBuffer.Add(link);
        StartBufferTimer();
    }

    private void OnConnectorPropertyChanged(object? s, System.ComponentModel.PropertyChangedEventArgs e)
    {
        _connectorPropertyChangedBuffer.Add((s, e));
        StartBufferTimer();
    }
}
