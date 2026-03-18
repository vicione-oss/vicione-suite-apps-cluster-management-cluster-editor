using System;
using System.Globalization;
using Blazor.Diagrams.Core.Geometry;
using ViciOne.Cluster.Model;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Ui.ClusterEditor.Models.DiagramModels;

public sealed class FunctionBlockNode : BlockNode
{
    internal string CycleFrequency { get; private set; } = string.Empty;
    internal bool Enabled { get; set; } = true;
    internal string RunModeText { get; private set; } = string.Empty;

    internal FunctionBlockNode(Point? point = null) : base(point) { }

    private static string GetCycleString(uint n)
        => n is >= 0 and < 100 ? n.ToString(CultureInfo.InvariantCulture) : "*";

    internal void SetCycleFrequency(uint cycleFrequency)
        => CycleFrequency = GetCycleString(cycleFrequency);

    internal void SetRunMode(RunMode runMode)
        => RunModeText = runMode switch
        {
            RunMode.Change => "C",
            RunMode.Cyclic => "Y",
            _ => throw new NotImplementedException()
        };

    internal void SetRunMode(FunctionBlockRunMode runMode)
        => RunModeText = runMode switch
        {
            FunctionBlockRunMode.Change => "C",
            FunctionBlockRunMode.Cyclic => "Y",
            _ => throw new NotImplementedException()
        };
}
