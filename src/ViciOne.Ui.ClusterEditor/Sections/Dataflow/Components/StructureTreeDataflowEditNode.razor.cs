using System;
using Microsoft.AspNetCore.Components;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models.Messages;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Services;
using ViciOne.Ui.ClusterEditor.Sections.Dataflow.Models;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.TreeEditor.Templates;

namespace ViciOne.Ui.ClusterEditor.Sections.Dataflow.Components;

public sealed partial class StructureTreeDataflowEditNode : NodeTemplateBase, IDisposable
{
    private bool _anyErrors;
    private DataflowEditModel? _dataflowEditModel;

    [Inject] private IDatastore Datastore { get; set; } = default!;
    [Inject] private IPropertyGridController<DataflowStructureTreeNode> PropertyGridController { get; set; } = default!;
    [Inject] private IPropertyGridMessageStore<DataflowStructureTreeNode> PropertyGridMessageStore { get; set; } = default!;

    public void Dispose()
        => UnsubscribeFromErrorMessageStoreChanged();

    private void OnCancelClicked()
    {
        if (Node.TreeNode is not DataflowStructureTreeNode dataflowNode)
            return;

        dataflowNode.Editing = false;
        Builder.Notifications.NotifyNodeChanged(dataflowNode);
    }

    private void OnConfirmClicked()
    {
        if (Node.TreeNode is not DataflowStructureTreeNode dataflowNode)
            return;

        if (_dataflowEditModel is not null)
            Datastore.SetDataflowName(dataflowNode.Dataflow, _dataflowEditModel.Name);

        dataflowNode.Editing = false;
        Builder.Notifications.NotifyNodeChanged(dataflowNode);
    }

    protected override void OnInitialized()
        => SubscribeToErrorMessageStoreChanged();

    protected override void OnParametersSet()
    {
        if (Node.TreeNode is not DataflowStructureTreeNode dataflowNode)
            return;

        _dataflowEditModel = new DataflowEditModel { Name = dataflowNode.Dataflow.Name };

        PropertyGridController.SetInstances([_dataflowEditModel], dataflowNode);
    }

    private async void OnPropertyGridMessageStoreChangedAsync(PropertyGridMessageStoreChangedEventArgs args)
    {
        var anyErrors = args.Sender.Contains<ErrorMessage>();
        if (anyErrors != _anyErrors)
        {
            _anyErrors = anyErrors;
            await InvokeAsync(StateHasChanged);
        }
    }

    private void SubscribeToErrorMessageStoreChanged()
        => PropertyGridMessageStore.Changed += OnPropertyGridMessageStoreChangedAsync;

    private void UnsubscribeFromErrorMessageStoreChanged()
        => PropertyGridMessageStore.Changed -= OnPropertyGridMessageStoreChangedAsync;
}
