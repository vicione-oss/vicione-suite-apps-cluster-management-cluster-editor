using System.Collections.Generic;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;

namespace ViciOne.Ui.ClusterEditor.Models;

internal sealed class TraceOptions
{
    public IReadOnlyList<BlockNode> BlockNodes { get; set; } = [];

    public ConnectionDirection ConnectionDirection { get; set; }

    public int? Depth { get; set; }
}
