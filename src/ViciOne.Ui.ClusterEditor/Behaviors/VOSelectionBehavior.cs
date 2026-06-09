using System;
using Blazor.Diagrams.Core;
using Blazor.Diagrams.Core.Events;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models.Base;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services;

namespace ViciOne.Ui.ClusterEditor.Behaviors;

// Der Code basiert auf Blazor.Diagrams.Core.Behaviors.SelectionBehavior
internal sealed class VOSelectionBehavior : Behavior
{
    private bool? _selectingConnectors;
    private readonly SelectionManager _selectionManager;

    public VOSelectionBehavior(Diagram diagram, SelectionManager selectionManager) : base(diagram)
    {
        Diagram.PointerDown += OnPointerDown;
        _selectionManager = selectionManager;
    }

    public override void Dispose()
    {
        Diagram.PointerDown -= OnPointerDown;
        GC.SuppressFinalize(this);
    }

    private void OnPointerDown(Model? model, PointerEventArgs e)
    {
        if (e.Button == (int)MouseEventButton.Wheel || Diagram.Container is null)
            return;

        Process(model, e.Button == (int)MouseEventButton.Right, e.CtrlKey, Diagram.GetRelativeMousePoint(e.ClientX, e.ClientY));
    }

    private void Process(Model? model, bool cmButtonPressed, bool ctrlKey, Point diagramMousePoint)
    {
        if (model is null)
        {
            if (ctrlKey && _selectingConnectors.HasValue)
                return;

            _selectingConnectors = null;

            if (!ctrlKey)
                _selectionManager.DeselectAll();
        }
        else if (model is IDiagramModel dm)
        {
            if (ctrlKey && _selectionManager.SelectedConnectorMarker.Count > 0)
                return;

            if (ctrlKey && _selectingConnectors.HasValue && _selectingConnectors.Value != (model is BlockNodeConnector))
                return;

            var isSelected = _selectionManager.IsSelected(dm);
            _selectingConnectors ??= model is BlockNodeConnector;

            if (cmButtonPressed && isSelected)
                return;

            // Test whether a block node is hit inside it's actual bounds.
            // The diagram delivers also a block model if the user click
            // inside the alignment borders but outside the actual node borders.
            if (model is BlockNode blockNode)
            {
                var bounds = blockNode.GetBounds();
                if (bounds is null || !bounds.ContainsPoint(diagramMousePoint))
                {
                    // If the node was selected then deselect it because we are outside
                    // the actual node borders.
                    if (isSelected)
                        _selectionManager.Deselect(dm);

                    return;
                }
            }

            if (ctrlKey && Diagram.Options.AllowMultiSelection)
            {
                if (!isSelected)
                    _selectionManager.Select(dm);
                else
                    _selectionManager.Deselect(dm);
            }
            else
            {
                if (!isSelected || _selectionManager.SelectedModels.Count >= 2)
                {
                    if (!_selectingConnectors.Value && isSelected)
                        return;

                    _selectingConnectors = model is BlockNodeConnector;
                    _selectionManager.SetSelection(dm);
                }
            }
        }
    }
}
