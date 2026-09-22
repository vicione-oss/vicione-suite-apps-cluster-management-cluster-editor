using System.Collections.Generic;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeIdentifier;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;

/// <summary>
/// An open edit form of the node with <paramref name="NodeId"/>, with the values the user changed by property name.
/// </summary>
internal sealed record DataPortPendingEdit(GuidNodeIdentifier NodeId, IReadOnlyDictionary<string, object?> ChangedValues);
