using System.Collections.Generic;
using System.Linq;
using ViciOne.Tree.Builder.Extensions;
using ViciOne.Tree.Builder.NodeTypes;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using TreeBuilder = ViciOne.Tree.Builder.TreeBuilder;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;

internal static class NodeTypeExtensions
{
    /// <summary>
    /// The directions a node type declares it may be linked in, or <paramref name="transferDirections"/>
    /// when it declares none. <see cref="DataPortLinkDirection.None"/> is the ruleset's spelling for
    /// "never linked" and maps to no direction at all.
    /// </summary>
    private static IEnumerable<DataPortTransferDirection> GetDeclaredLinkDirections(this NodeType nodeType, IList<DataPortTransferDirection> transferDirections)
    {
        if (nodeType.LinkDirections is null or [])
            return transferDirections;

        return nodeType.LinkDirections
            .Where(direction => direction is DataPortLinkDirection.Inbound or DataPortLinkDirection.Outbound)
            .Select(direction => direction is DataPortLinkDirection.Inbound
                ? DataPortTransferDirection.Inbound
                : DataPortTransferDirection.Outbound);
    }

    /// <summary>
    /// The directions the engine may link this node type in under <paramref name="parentNode"/>.
    /// For an envelope child the ruleset decides per parent/child pair: a predefined child such as a
    /// validity the data port derives from the parent value is transferred with the parent's message
    /// but never linked. Everything else may be linked wherever it declares it may be, and wherever
    /// it is transferred if it declares nothing.
    /// </summary>
    internal static IList<DataPortTransferDirection> GetEffectiveLinkDirections(this NodeType nodeType, DataPortNodeModel parentNode, TreeBuilder builder)
    {
        var transferDirections = nodeType.GetInheritedTransferDirections(parentNode);

        if (parentNode is DataPortChildNodeModel { NodeReference.Id: { } parentNodeTypeId }
            && builder.Ruleset.IsEnvelopeRelation(parentNodeTypeId, nodeType.Id))
        {
            return [.. builder.Ruleset.GetEnvelopeLinkDirections(parentNodeTypeId, nodeType.Id).Intersect(transferDirections)];
        }

        return [.. nodeType.GetDeclaredLinkDirections(transferDirections).Intersect(transferDirections)];
    }

    /// <summary>
    /// The directions a node placed under <paramref name="parentNode"/> is effectively transferred in.
    /// </summary>
    /// <remarks>
    /// The ruleset answers the same question per node type pair through
    /// <see cref="EnvelopeExtensions.GetEnvelopeTransferDirections"/>,
    /// but only against the parent type's declaration. A parent that inherited a narrower set from
    /// its own ancestors is not covered by that, so the chain is walked here instead.
    /// </remarks>
    internal static IList<DataPortTransferDirection> GetInheritedTransferDirections(this NodeType nodeType, DataPortNodeModel parentNode)
    {
        if (parentNode is not DataPortChildNodeModel parentChildNode)
            return [.. nodeType.TransferDirections];

        if (nodeType.TransferDirections.Length == 0)
            return [.. parentChildNode.TransferDirections];

        // A node type that declares its own directions (e.g. an envelope child narrowed to
        // Outbound) may only use those it actually shares with its parent's effective
        // directions; a direction the parent does not have is never effective either.
        return [.. nodeType.TransferDirections.Intersect(parentChildNode.TransferDirections)];
    }

    internal static bool IsDataPoint(this NodeType nodeType)
        => nodeType.DataTypes.Length > 0;
}
