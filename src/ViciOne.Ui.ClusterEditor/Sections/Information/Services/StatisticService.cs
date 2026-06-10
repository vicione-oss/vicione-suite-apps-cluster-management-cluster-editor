using System;
using System.Collections.Generic;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Builder.Extensions;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Sections.Information.Models;
using ViciOne.Ui.ClusterEditor.Services;

namespace ViciOne.Ui.ClusterEditor.Sections.Information.Services;

internal sealed class StatisticService : IDisposable
{
    private IClusterBuilder? _clusterBuilder;
    private readonly ClusterBuilderEventBuffer _clusterBuilderEventBuffer;
    private readonly IDatastore _datastore;

    public event Action<Statistic>? StatisticChanged;

    public StatisticService(ClusterBuilderEventBuffer clusterBuilderEventBuffer, IDatastore datastore)
    {
        _clusterBuilderEventBuffer = clusterBuilderEventBuffer;
        _datastore = datastore;
        _datastore.BuilderChanged += OnBuilderChanged;
    }

    public void Dispose()
    {
        _datastore.BuilderChanged -= OnBuilderChanged;
        UnsubscribeBuilderEvents();
    }

    public Statistic GetStatistic()
    {
        if (_clusterBuilder is null)
            return new();

        var driverNames = new HashSet<string>();
        foreach (var kvp in _clusterBuilder.Cache.FunctionBlockDesigns)
        {
            var fbd = _clusterBuilder.ResolveFunctionBlockDesign(kvp.Key);
            if (!string.IsNullOrEmpty(fbd.RuntimeDriverTypeName))
                driverNames.Add(fbd.RuntimeDriverTypeName);
        }

        var driverCount = driverNames.Count;

        int links = 0, visibleLinks = 0;
        foreach (var link in _clusterBuilder.Cache.Links)
        {
            if (link.DestinationDataPortTreeNode is null && link.SourceDataPortTreeNode is null)
                links++;
            if (link.Visible)
                visibleLinks++;
        }

        int dataPortTreeNodes = 0, dataPortTreeNodesConnected = 0;
        foreach (var node in _clusterBuilder.Cache.DataPortTreeNodes)
        {
            if (node.ValueType is not null)
                dataPortTreeNodes++;

            foreach (var _ in node.Links)
            {
                dataPortTreeNodesConnected++;
                break;
            }
        }

        int connectors = 0, connectorsEventEnabled = 0;
        foreach (var connector in _clusterBuilder.Cache.Connectors)
        {
            if (connector.Type is ConnectorType.System or ConnectorType.ProcessData)
                connectors++;
            if (connector.EventEnabled)
                connectorsEventEnabled++;
        }

        var statistic = new Statistic()
        {
            Connectors = connectors,
            ConnectorsEventEnabled = connectorsEventEnabled,
            ConnectorsPublished = _clusterBuilder.Cache.PublishedConnectors.Count,
            Containers = _clusterBuilder.Cache.Containers.Count,
            DataPorts = _clusterBuilder.Cache.DataPorts.Count,
            DataPortTreeNodes = dataPortTreeNodes,
            DataPortTreeNodesConnected = dataPortTreeNodesConnected,
            DataPortTreeNodesNotConnected = dataPortTreeNodes - dataPortTreeNodesConnected,
            Drivers = driverCount,
            Engines = _clusterBuilder.Cache.EngineIds.Count,
            FbDesigns = _clusterBuilder.Cache.FunctionBlockDesigns.Count,
            Fbs = _clusterBuilder.Cache.FunctionBlocks.Count,
            Labels = _clusterBuilder.Cache.Labels.Count,
            Links = links,
            LinksHidden = links - visibleLinks,
            LinksVisible = visibleLinks,
        };

        return statistic;
    }

    private void InvokeStatisticChanged()
        => StatisticChanged?.Invoke(GetStatistic());

    private void OnBuilderChanged()
    {
        _clusterBuilder = _datastore.Builder;

        _clusterBuilderEventBuffer.ConnectorLinksAdded += OnClusterStateChanged;
        _clusterBuilderEventBuffer.ConnectorLinksRemoved += OnClusterStateChanged;
        _clusterBuilderEventBuffer.ConnectorPropertiesChanged += OnConnectorPropertiesChanged;
        _clusterBuilderEventBuffer.ContainersAdded += OnClusterStateChanged;
        _clusterBuilderEventBuffer.ContainersRemoved += OnClusterStateChanged;
        _clusterBuilderEventBuffer.DataflowsAdded += OnClusterStateChanged;
        _clusterBuilderEventBuffer.DataflowsRemoved += OnClusterStateChanged;
        _clusterBuilderEventBuffer.DataPortsAdded += OnClusterStateChanged;
        _clusterBuilderEventBuffer.DataPortsRemoved += OnClusterStateChanged;
        _clusterBuilderEventBuffer.EnginesAdded += OnClusterStateChanged;
        _clusterBuilderEventBuffer.EnginesRemoved += OnClusterStateChanged;
        _clusterBuilderEventBuffer.FunctionBlocksAdded += OnClusterStateChanged;
        _clusterBuilderEventBuffer.FunctionBlockDesignsAdded += OnClusterStateChanged;
        _clusterBuilderEventBuffer.FunctionBlockDesignsRemoved += OnClusterStateChanged;
        _clusterBuilderEventBuffer.FunctionBlocksRemoved += OnClusterStateChanged;
        _clusterBuilderEventBuffer.LabelsAdded += OnClusterStateChanged;
        _clusterBuilderEventBuffer.LabelsRemoved += OnClusterStateChanged;
        _clusterBuilderEventBuffer.TreeNodesAdded += OnClusterStateChanged;
        _clusterBuilderEventBuffer.TreeNodesRemoved += OnClusterStateChanged;

        InvokeStatisticChanged();
    }

    private void OnClusterStateChanged<T>(IEnumerable<T> _)
        => InvokeStatisticChanged();

    private void OnConnectorPropertiesChanged(IEnumerable<(object? sender, System.ComponentModel.PropertyChangedEventArgs e)> changedProperties)
    {
        foreach (var (_, e) in changedProperties)
        {
            if (e.PropertyName is (nameof(Connector.Published)) or (nameof(Connector.EventEnabled)))
                InvokeStatisticChanged();
        }
    }

    private void UnsubscribeBuilderEvents()
    {
        if (_clusterBuilderEventBuffer is not null)
        {
            _clusterBuilderEventBuffer.ConnectorLinksAdded -= OnClusterStateChanged;
            _clusterBuilderEventBuffer.ConnectorLinksRemoved -= OnClusterStateChanged;
            _clusterBuilderEventBuffer.ConnectorPropertiesChanged -= OnConnectorPropertiesChanged;
            _clusterBuilderEventBuffer.ContainersAdded -= OnClusterStateChanged;
            _clusterBuilderEventBuffer.ContainersRemoved -= OnClusterStateChanged;
            _clusterBuilderEventBuffer.DataflowsAdded -= OnClusterStateChanged;
            _clusterBuilderEventBuffer.DataflowsRemoved -= OnClusterStateChanged;
            _clusterBuilderEventBuffer.DataPortsAdded -= OnClusterStateChanged;
            _clusterBuilderEventBuffer.DataPortsRemoved -= OnClusterStateChanged;
            _clusterBuilderEventBuffer.EnginesAdded -= OnClusterStateChanged;
            _clusterBuilderEventBuffer.EnginesRemoved -= OnClusterStateChanged;
            _clusterBuilderEventBuffer.FunctionBlocksAdded -= OnClusterStateChanged;
            _clusterBuilderEventBuffer.FunctionBlockDesignsAdded -= OnClusterStateChanged;
            _clusterBuilderEventBuffer.FunctionBlockDesignsRemoved -= OnClusterStateChanged;
            _clusterBuilderEventBuffer.FunctionBlocksRemoved -= OnClusterStateChanged;
            _clusterBuilderEventBuffer.LabelsAdded -= OnClusterStateChanged;
            _clusterBuilderEventBuffer.LabelsRemoved -= OnClusterStateChanged;
            _clusterBuilderEventBuffer.TreeNodesAdded -= OnClusterStateChanged;
            _clusterBuilderEventBuffer.TreeNodesRemoved -= OnClusterStateChanged;
        }
    }
}
