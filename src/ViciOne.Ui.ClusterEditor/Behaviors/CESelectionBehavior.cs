using System;
using Blazor.Diagrams.Core;
using Blazor.Diagrams.Core.Events;
using Blazor.Diagrams.Core.Models.Base;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;

namespace ViciOne.Ui.ClusterEditor.Behaviors;

internal sealed class CESelectionBehavior : Behavior
{
    public event Action<BlockNodeConnector, bool>? SelectedConnectorChanged;

    public CESelectionBehavior(Diagram diagram) : base(diagram)
        => Diagram.PointerDown += OnPointerDown;

    public override void Dispose()
    {
        Diagram.PointerDown -= OnPointerDown;
        GC.SuppressFinalize(this);
    }

    private void OnPointerDown(Model? model, PointerEventArgs e)
    {
        if (e.Button == (int)MouseEventButton.Wheel)
            return;

        Process(model, e.Button == (int)MouseEventButton.Left, e.CtrlKey);
    }

    private void Process(Model? model, bool cmButtonPressed, bool ctrlKey)
    {
        if ((model is null or not BlockNodeConnector) || !cmButtonPressed)
            return;

        SelectedConnectorChanged?.Invoke((BlockNodeConnector)model, ctrlKey);
    }
}
