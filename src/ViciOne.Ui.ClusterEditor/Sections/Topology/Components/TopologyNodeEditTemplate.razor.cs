using System;
using Microsoft.AspNetCore.Components;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Services;
using ViciOne.Ui.ClusterEditor.Models.Contexts;
using ViciOne.Ui.ClusterEditor.Sections.Topology.Factories;
using ViciOne.Ui.ClusterEditor.Sections.Topology.Models;
using ViciOne.Ui.TreeEditor.Templates;

namespace ViciOne.Ui.ClusterEditor.Sections.Topology.Components;

public sealed partial class TopologyNodeEditTemplate : NodeTemplateBase, IDisposable
{
    private object? _editItem;

    [CascadingParameter] private TopologyEditTemplateContext EditTemplateContext { get; set; } = default!;

    [Inject] private IPropertyGridController<TopologyNodeEditContext> PropertyGridController { get; set; } = default!;
    [Inject] private IPropertyGridEvents<TopologyNodeEditContext> PropertyGridEvents { get; set; } = default!;

    public void Dispose()
        => PropertyGridEvents.PropertyChanged -= OnPropertyGridPropertyChanged;

    private void OnCancelClicked()
    {
        if (Node.TreeNode is TopologyTreeViewModel node)
            EditTemplateContext.InvokeCancel(node);
    }

    private void OnConfirmClicked()
    {
        if (Node.TreeNode is TopologyTreeViewModel node)
            EditTemplateContext.InvokeConfirm(node, _editItem);
    }

    protected override void OnInitialized()
        => PropertyGridEvents.PropertyChanged += OnPropertyGridPropertyChanged;

    protected override void OnParametersSet()
    {
        if (Node.TreeNode is TopologyTreeViewModel node)
        {
            _editItem = DataItemCloneFactory.CreateClone(node.DataItem);
            PropertyGridController.SetInstances([_editItem], new TopologyNodeEditContext());
        }
    }

    private void OnPropertyGridPropertyChanged(PropertyGridPropertyChangedEventArgs args)
    {
        if (Node.TreeNode is TopologyTreeViewModel node)
            node.HasChangedProperties = true;
    }
}
