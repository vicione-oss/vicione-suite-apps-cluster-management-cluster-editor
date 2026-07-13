using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Services;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.MonochromeIcons.Core.Enums;

namespace ViciOne.Ui.ClusterEditor.Sections.Property.Components;

public sealed partial class PropertySectionContent<TPropertyGridContext> : ComponentBase, IDisposable
{
    private readonly List<FilterButton> _filterButtons = [];

    [Inject] private IDatastore Datastore { get; set; } = default!;
    [Inject] private DiagramEventService DiagramEventService { get; set; } = default!;
    [Inject] private IPropertyGridEvents<TPropertyGridContext> PropertyGridEvents { get; set; } = default!;
    [Inject] private IPropertyGridState<TPropertyGridContext> PropertyGridState { get; set; } = default!;

    [Parameter, EditorRequired] public IPropertyGridController<TPropertyGridContext> PropertyGridController { get; set; } = default!;

    public void ContextMenuVisibilityChanged(PropertyGridContextMenuVisibilityChangedEventArgs args)
    {
        if (!args.Visible)
            DiagramEventService.RequestDiagramFocus();
    }

    public void Dispose()
    {
        Datastore.PropertyChanged -= OnDatastorePropertyChanged;
        PropertyGridEvents.ContextMenuVisibilityChanged -= ContextMenuVisibilityChanged;
        GC.SuppressFinalize(this);
    }

    private void OnDatastorePropertyChanged(string property)
        => PropertyGridController.UpdateProperty(property);

    protected override void OnInitialized()
    {
        PropertyGridEvents.ContextMenuVisibilityChanged += ContextMenuVisibilityChanged;
        Datastore.PropertyChanged += OnDatastorePropertyChanged;

        var groupByCategoryButton = new FilterButton()
        {
            IsActive = PropertyGridState.GroupByCategory,
            MonochromeIconName = MonochromeIconName.GroupByCategory,
            Title = Localization.PropertySection.EntriesGroupTooltip
        };
        groupByCategoryButton.OnFilterClickedFn = () =>
        {
            groupByCategoryButton.IsActive = !groupByCategoryButton.IsActive;
            PropertyGridState.GroupByCategory = !PropertyGridState.GroupByCategory;
            InvokeAsync(StateHasChanged);
        };
        _filterButtons.Add(groupByCategoryButton);
    }
}
