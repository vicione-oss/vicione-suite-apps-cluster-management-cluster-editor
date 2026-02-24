using ViciOne.Cluster.Model;

namespace ViciOne.Ui.ClusterEditor.Tests;

internal static class TestData
{
    internal static ConnectorInput GetConnector()
        => new()
        {
            Description = "description",
            MarkAsChangedOnlyIfNotEqual = true,
            Published = true
        };

    internal static FunctionBlock GetFunctionBlock()
        => new()
        {
            BackColor = "rgb(0, 0, 0)",
            CycleFrequency = 1,
            Description = "description",
            Engine = new Cluster.Model.Engine() { Name = "engine" },
            ForeColor = "rgb(255, 255, 255)",
            Name = "name",
            RunMode = FunctionBlockRunMode.Cyclic,
            X = 2,
            Y = null
        };

    internal static Label GetLabel()
        => new()
        {
            BackColor = "rgb(0, 0, 0)",
            BorderColor = "rgb(128, 128, 128)",
            Content = "content",
            Height = 1,
            Width = 2,
            X = 3,
            Y = 4,
            ZIndex = -1
        };
}
