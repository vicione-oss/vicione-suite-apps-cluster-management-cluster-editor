using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using Shared.ClusterManagement.Services;
using Shared.Debugging.Extensions;
using Shared.Debugging.Models;
using Shared.Designs;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Builder.Extensions;
using ViciOne.Cluster.Model;
using ViciOne.Cluster.Model.Extensions;
using ViciOne.Tree.Builder.NodeTypes;
using ViciOne.Tree.Builder.Rules;
using ViciOne.Ui.ClusterEditor.Services;
using DataPortTransferMode = ViciOne.Cluster.Model.DataPortTransferMode;
using FunctionBlockDesign = ViciOne.Core.Dataflow.DataModel.FunctionBlockDesign;

namespace Shared.Debugging.Services;

/// <summary>
/// Fills the current cluster with generated test data - function blocks, links and data ports - to get a
/// dataflow of realistic size without clicking it together. Every mutation goes through the
/// <see cref="IClusterBuilder" />; the editor is only asked for the container the user is looking at and
/// to re-project itself afterwards.
/// </summary>
public sealed class ClusterGeneratorService(
    IClusterEditorManagement clusterEditorManagement,
    ClusterManagementService clusterManagementService,
    IRulesetSource rulesetSource)
{
    private const int MaxConsecutiveFailures = 20;

    private readonly Random _rnd = new();

    /// <summary>
    /// Always read through, never cache: <see cref="ClusterManagementService" /> replaces the builder on
    /// every load, and a cached one would already be disposed.
    /// </summary>
    private IClusterBuilder Builder => clusterManagementService.Builder;

    private void AddFunctionBlocks(IReadOnlyCollection<Guid> designIds, int amount)
    {
        var builder = Builder;
        var containerEditor = builder.Editors.Container;
        var functionBlockEditor = builder.Editors.FunctionBlock;
        var activeContainer = clusterEditorManagement.ActiveContainer;
        var engine = GetFirstValidEngine(builder, activeContainer);

        foreach (var (designId, index) in designIds.Select((designId, index) => (designId, index)))
        {
            for (var i = 0; i < amount; i++)
            {
                var position = (index * amount) + i;
                var functionBlock = containerEditor.AddFunctionBlock(
                    activeContainer,
                    designId,
                    location: new Point(position % 5 * 320, position / 5 * 280));

                if (engine is not null)
                    functionBlockEditor.AssignEngine(engine, functionBlock);
            }
        }
    }

    [SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "This is only debug data")]
    private List<NodeReference>? FindPathToDataPoint(NodeReference[] candidates, IReadOnlyDictionary<string, NodeType> nodeTypes)
    {
        foreach (var candidate in candidates.OrderBy(_ => _rnd.Next()))
        {
            if (!nodeTypes.TryGetValue(candidate.Id, out var nodeType))
                continue;

            if (nodeType.IsDataPoint())
                return [candidate];

            var childPath = FindPathToDataPoint(nodeType.ChildNodes, nodeTypes);
            if (childPath is not null)
                return [candidate, .. childPath];
        }

        return null;
    }

    /// <summary>
    /// Adds one function block per function block design known to the cluster.
    /// </summary>
    public Task GenerateAllLibraryFunctionBlocks(CancellationToken cancellationToken = default)
    {
        AddFunctionBlocks([.. GetFunctionBlockDesigns().Select(design => design.Id)], 1);

        return clusterEditorManagement.ReloadActiveContainer(cancellationToken);
    }

    [SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "This is only debug data")]
    private bool GenerateDataPoint()
    {
        var builder = Builder;

        var rulesets = GetRulesets();
        if (rulesets.Count == 0)
            return false;

        var ruleset = rulesets[_rnd.Next(rulesets.Count)];
        if (ruleset.Root is null)
            return false;

        var nodeTypes = ruleset.NodeTypes.ToDictionary(nodeType => nodeType.Id);

        // Pick a random DataPort type that can reach a leaf DataPoint and build the path of tree nodes leading to it.
        List<NodeReference>? treeNodePath = null;
        NodeType? dataPortNodeType = null;
        foreach (var dataPortRef in ruleset.Root.ChildNodes.OrderBy(_ => _rnd.Next()))
        {
            if (!nodeTypes.TryGetValue(dataPortRef.Id, out var candidate))
                continue;

            treeNodePath = FindPathToDataPoint(candidate.ChildNodes, nodeTypes);
            if (treeNodePath is not null)
            {
                dataPortNodeType = candidate;
                break;
            }
        }

        if (dataPortNodeType is null || treeNodePath is null)
            return false;

        var dataflow = GetActiveDataflow(builder, clusterEditorManagement.ActiveContainer);
        if (dataflow is null)
            return false;

        builder.EnsureSystemDataPortDependencyExists(rulesetSource);

        var dataPort = builder.Editors.Dataflow.AddDataPort(
            dataflow,
            dataPortNodeType.Id,
            dataPortNodeType.Name,
            PickDirection(dataPortNodeType),
            ruleset.Root.Id);

        DataPortTreeNode? current = null;
        foreach (var nodeRef in treeNodePath)
        {
            var nodeType = nodeTypes[nodeRef.Id];
            var valueType = nodeType.IsDataPoint() ? PickRandomValueType(nodeType, ruleset) : null;
            var transferMode = PickTransferMode(nodeType);

            current = current is null
                ? builder.Editors.DataPort.AddTreeNode(nodeType.Id, dataPort, nodeType.Name, valueType, transferMode)
                : builder.Editors.DataPortTreeNode.AddTreeNode(nodeType.Id, current, nodeType.Name, valueType, transferMode);
        }

        return true;
    }

    /// <summary>
    /// Adds up to <paramref name="amount" /> data ports, each with a random tree path down to a data point.
    /// </summary>
    public async Task GenerateDataPoints(int amount, CancellationToken cancellationToken = default)
    {
        if (amount < 1)
            return;

        var created = 0;
        var availableTries = amount + 20;

        while (created < amount && availableTries > 0)
        {
            if (GenerateDataPoint())
                created++;
            else
                availableTries--;
        }

        if (created > 0)
            await clusterEditorManagement.ReloadActiveContainer(cancellationToken);
    }

    [SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "This is only debug data")]
    private void GenerateDataPortLinks(int amount)
    {
        var builder = Builder;
        var dataPortEditor = builder.Editors.DataPortTreeNode;
        var cache = builder.Cache;

        var dataPortTreeNodes = cache.DataPortTreeNodes.Where(node => node.ValueType is not null).ToList();
        var candidateConnectors = cache.Connectors.Where(connector => connector.Type != ConnectorType.Setting).ToList();
        if (dataPortTreeNodes.Count == 0 || candidateConnectors.Count == 0)
            return;

        var created = 0;
        var consecutiveFailures = 0;
        while (created < amount && consecutiveFailures < MaxConsecutiveFailures)
        {
            var dataPortTreeNode = dataPortTreeNodes[_rnd.Next(dataPortTreeNodes.Count)];

            var validConnectors = candidateConnectors
                .Where(connector => dataPortEditor.CanAssignConnector(dataPortTreeNode, connector))
                .ToList();
            if (validConnectors.Count == 0)
            {
                consecutiveFailures++;
                continue;
            }

            dataPortEditor.AssignConnector(dataPortTreeNode, validConnectors[_rnd.Next(validConnectors.Count)]);

            consecutiveFailures = 0;
            created++;
        }
    }

    /// <summary>
    /// Adds <paramref name="amount" /> function blocks of the design with the given name.
    /// </summary>
    public Task GenerateFunctionBlocks(string designName, int amount, CancellationToken cancellationToken = default)
    {
        var design = GetFunctionBlockDesigns()
            .Find(candidate => string.Equals(candidate.Name, designName, StringComparison.OrdinalIgnoreCase));
        if (design is null)
            return Task.CompletedTask;

        AddFunctionBlocks([design.Id], amount);

        return clusterEditorManagement.ReloadActiveContainer(cancellationToken);
    }

    [SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "This is only debug data")]
    private void GenerateHiddenLinks(int amount)
    {
        var builder = Builder;
        var connectorEditor = builder.Editors.Connector;
        var allConnectors = builder.Cache.Connectors.Where(connector => connector.Type != ConnectorType.Setting).ToList();

        var outputConnectors = allConnectors.OfType<IConnectorOutput>().ToList();
        var inputConnectors = allConnectors.OfType<IConnectorInput>().ToList();
        if (outputConnectors.Count == 0 || inputConnectors.Count == 0)
            return;

        var created = 0;
        var consecutiveFailures = 0;
        while (created < amount && consecutiveFailures < MaxConsecutiveFailures)
        {
            var sourceConnector = outputConnectors[_rnd.Next(outputConnectors.Count)];

            var validDestinationConnectors = inputConnectors
                .Where(connector => connectorEditor.CanCreateLink(sourceConnector, connector, false))
                .ToList();
            if (validDestinationConnectors.Count == 0)
            {
                consecutiveFailures++;
                continue;
            }

            var destinationConnector = validDestinationConnectors[_rnd.Next(validDestinationConnectors.Count)];

            connectorEditor.SetPublished(_rnd.NextDouble() < 0.5 ? sourceConnector : destinationConnector, true);
            connectorEditor.AddLink(sourceConnector, destinationConnector, false);

            consecutiveFailures = 0;
            created++;
        }
    }

    /// <summary>
    /// Adds up to <paramref name="amount" /> links of the requested kind between randomly picked connectors.
    /// </summary>
    public async Task GenerateLinks(GenerateLinksType type, int amount, CancellationToken cancellationToken = default)
    {
        if (amount < 1)
            return;

        switch (type)
        {
            case GenerateLinksType.Visible:
                GenerateVisibleLinks(amount);
                break;
            case GenerateLinksType.Hidden:
                GenerateHiddenLinks(amount);
                break;
            case GenerateLinksType.DataPort:
                GenerateDataPortLinks(amount);
                break;
        }

        await clusterEditorManagement.ReloadActiveContainer(cancellationToken);
    }

    [SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "This is only debug data")]
    private void GenerateVisibleLinks(int amount)
    {
        var builder = Builder;
        var connectorEditor = builder.Editors.Connector;
        var activeContainer = clusterEditorManagement.ActiveContainer;

        var outputConnectors = activeContainer.FunctionBlocks.SelectMany(block => block.Outputs)
            .Union(activeContainer.Containers.SelectMany(container => container.GetConnectors().OfType<IConnectorOutput>()))
            .ToList();
        var inputConnectors = activeContainer.FunctionBlocks.SelectMany(block => block.Inputs)
            .Union(activeContainer.Containers.SelectMany(container => container.GetConnectors().OfType<IConnectorInput>()))
            .ToList();
        if (outputConnectors.Count == 0 || inputConnectors.Count == 0)
            return;

        var created = 0;
        var consecutiveFailures = 0;
        while (created < amount && consecutiveFailures < MaxConsecutiveFailures)
        {
            var sourceConnector = outputConnectors[_rnd.Next(outputConnectors.Count)];

            var validDestinationConnectors = inputConnectors
                .Where(connector => connectorEditor.CanCreateLink(sourceConnector, connector, true))
                .ToList();
            if (validDestinationConnectors.Count == 0)
            {
                consecutiveFailures++;
                continue;
            }

            connectorEditor.AddLink(sourceConnector, validDestinationConnectors[_rnd.Next(validDestinationConnectors.Count)]);

            consecutiveFailures = 0;
            created++;
        }
    }

    private static Dataflow? GetActiveDataflow(IClusterBuilder builder, Container activeContainer)
        => activeContainer is ChildContainer childContainer
            ? builder.Cache.GetDataflow(childContainer)
            : builder.Cache.Dataflows.FirstOrDefault(dataflow => dataflow.Root == activeContainer);

    private static Engine? GetFirstValidEngine(IClusterBuilder builder, Container activeContainer)
    {
        var dataflow = GetActiveDataflow(builder, activeContainer);
        if (dataflow is null)
            return null;

        return builder.Cache.GetUsedEngines(dataflow)
            .Concat(builder.Cache.GetUnusedEngines())
            .FirstOrDefault();
    }

    private List<FunctionBlockDesign> GetFunctionBlockDesigns()
    {
        var builder = Builder;

        return [.. builder.Cache.FunctionBlockDesigns.Keys.Select(builder.ResolveFunctionBlockDesign)];
    }

    private List<Ruleset> GetRulesets()
        => [.. rulesetSource.GetAllRulesets()];

    [SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "This is only debug data")]
    private DataPortDirection PickDirection(NodeType dataPortNodeType)
    {
        var hasInbound = dataPortNodeType.TransferDirections.Contains(DataPortTransferDirection.Inbound);
        var hasOutbound = dataPortNodeType.TransferDirections.Contains(DataPortTransferDirection.Outbound);

        if (hasInbound && hasOutbound)
            return (DataPortDirection)_rnd.Next(3);
        if (hasInbound)
            return DataPortDirection.In;

        return DataPortDirection.Out;
    }

    [SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "This is only debug data")]
    private Type? PickRandomValueType(NodeType leafNodeType, Ruleset ruleset)
    {
        var runtimeTypes = leafNodeType.DataTypes
            .Select(name => ruleset.DataTypes.FirstOrDefault(dataType => dataType.Name == name)?.RuntimeType)
            .Where(type => type is not null)
            .ToList();

        return runtimeTypes.Count == 0 ? null : runtimeTypes[_rnd.Next(runtimeTypes.Count)];
    }

    [SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "This is only debug data")]
    private DataPortTransferMode PickTransferMode(NodeType nodeType)
    {
        if (nodeType is DataPortTreeNodeType treeNodeType && treeNodeType.TransferModes.Length > 0)
        {
            var mode = treeNodeType.TransferModes[_rnd.Next(treeNodeType.TransferModes.Length)];
            if (Enum.TryParse<DataPortTransferMode>(mode.ToString(), out var parsed))
                return parsed;
        }

        return DataPortTransferMode.None;
    }
}
