using System;
using System.Linq;
using Microsoft.AspNetCore.Components;
using ViciOne.Ui.Blazor.Components.Models;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Comparers;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models.Messages;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Services;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.TreeEditor.Templates;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Components;

public sealed partial class DataPortEditNodeTemplate : NodeTemplateBase, IDisposable
{
    private bool _anyErrors;
    private DataPortChildNodeEditContext? _editContext;
    private bool _renderPropertyGridAndFormButtons;

    [CascadingParameter]
    private DataPortEditTemplateContext EditTemplateContext { get; set; } = default!;

    [Inject] private IDatastore Datastore { get; set; } = default!;
    [Inject] private IInsertionOrderCategoryComparer InsertionOrderCategoryComparer { get; set; } = default!;
    [Inject] private IInsertionOrderPropertyComparer InsertionOrderPropertyComparer { get; set; } = default!;
    [Inject] private IPropertyGridController<DataPortChildNodeEditContext> PropertyGridController { get; set; } = default!;
    [Inject] private IPropertyGridEvents<DataPortChildNodeEditContext> PropertyGridEvents { get; set; } = default!;
    [Inject] private IPropertyGridMessageStore<DataPortChildNodeEditContext> PropertyGridMessageStore { get; set; } = default!;
    [Inject] private IPropertyGridState<DataPortChildNodeEditContext> PropertyGridState { get; set; } = default!;
    [Inject] private DataPortChildNodePropertyValueStore PropertyValueStore { get; set; } = default!;

    public void Dispose()
    {
        PropertyGridEvents.PropertyChanged -= OnPropertyGridPropertyChanged;
        PropertyGridMessageStore.Changed -= OnPropertyGridMessageStoreChangedAsync;
        PropertyGridState.PropertiesChanged -= OnPropertyGridStatePropertiesChanged;
    }

    private void OnCancelClicked()
    {
        if (Node.TreeNode is DataPortNodeModel dataPortNode)
            EditTemplateContext.InvokeCancel(dataPortNode);
    }

    private void OnConfirmClicked()
    {
        if (Node.TreeNode is DataPortChildNodeModel dataPortChildNodeModel)
            dataPortChildNodeModel.AssignValuesAndProperties(PropertyValueStore);

        if (Node.TreeNode is DataPortNodeModel dataPortNode)
            EditTemplateContext.InvokeConfirm(dataPortNode);
    }

    protected override void OnInitialized()
    {
        PropertyGridMessageStore.Changed += OnPropertyGridMessageStoreChangedAsync;
        PropertyGridEvents.PropertyChanged += OnPropertyGridPropertyChanged;
        PropertyGridState.PropertiesChanged += OnPropertyGridStatePropertiesChanged;

        PropertyGridState.CategoryComparer = InsertionOrderCategoryComparer;
        PropertyGridState.PropertyComparer = InsertionOrderPropertyComparer;
    }

    protected override void OnParametersSet()
    {
        if (Node.TreeNode is DataPortChildNodeModel node)
        {
            _editContext = new() { ClusterBuilder = Datastore.Builder, Node = node };

            PropertyGridController.SetInstances([node], _editContext);
        }
    }

    private async void OnPropertyGridMessageStoreChangedAsync(PropertyGridMessageStoreChangedEventArgs args)
    {
        var anyErrors = args.Sender.Contains<ErrorMessage>();
        if (anyErrors != _anyErrors)
        {
            _anyErrors = anyErrors;
            await InvokeAsync(StateHasChanged);
        }
        if (anyErrors)
            _editContext?.Node.HasChangedProperties = true;
    }

    private void OnPropertyGridPropertyChanged(PropertyGridPropertyChangedEventArgs _)
        => _editContext?.Node.HasChangedProperties = true;

    private void OnPropertyGridStatePropertiesChanged(PropertiesChangedEventArgs args)
    {
        if (args.PropertyNames.Contains(nameof(PropertyGridState.Items)))
        {
            PropertyGridState.GroupByCategory = PropertyGridState.Items.Select(p => p.Category).Distinct().Count() > 1;

            // We unlock property grid and form buttons here as items are now set and state is fresh,
            // otherwise we have a flicker effect as the property grid would be rendered with outdated state
            // from previous render cylces based on a different node
            _renderPropertyGridAndFormButtons = true;
            InvokeAsync(StateHasChanged);
        }
    }
}
