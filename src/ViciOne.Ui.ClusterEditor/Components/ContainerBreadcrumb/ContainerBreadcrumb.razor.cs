using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.Breadcrumb.Models;
using ViciOne.Ui.ClusterEditor.Components.ContainerBreadcrumb.Extensions;
using ViciOne.Ui.ClusterEditor.Components.ContainerBreadcrumb.Models;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Components.ContainerBreadcrumb;

public sealed partial class ContainerBreadcrumb : ComponentBase, IDisposable
{
    private Container? _bufferedContainer;
    private BreadcrumbContainerItem? _currentItem;

    private bool _shouldRecalcCurrentItem;
    private bool _shouldRender = true;

    [Inject] private ClusterBuilderEventBuffer ClusterBuilderEventBuffer { get; set; } = default!;
    [Inject] private Datastore Datastore { get; set; } = default!;
    [Inject] private DiagramEventService DiagramEventService { get; set; } = default!;
    [Inject] private DiagramService DiagramService { get; set; } = default!;

    public void Dispose()
    {
        ClusterBuilderEventBuffer.ContainersAdded -= OnContainersAdded;
        ClusterBuilderEventBuffer.ContainersRemoved -= OnContainersRemoved;
        ClusterBuilderEventBuffer.ContainerPropertiesChanged -= OnContainerPropertiesChanged;

        DiagramEventService.ContainerLoaded -= OnContainerLoaded;
    }

    private Container GetCurrentContainer()
        => _bufferedContainer ??= Datastore.ActiveContainer;

    private Task HandleClickAsync(BreadcrumbItem item)
        => Datastore.LoadContainer(((BreadcrumbContainerItem)item).Container, DiagramService);

    private async void OnContainerLoaded(Container container)
    {
        _bufferedContainer = container;
        _shouldRecalcCurrentItem = true;

        await RefreshAsync();
    }

    private async void OnContainerPropertiesChanged(IEnumerable<(object? sender, System.ComponentModel.PropertyChangedEventArgs e)> changedProperties)
    {
        var shouldRefresh = false;
        foreach (var (sender, e) in changedProperties)
        {
            if (sender is Container container && e.PropertyName == nameof(Container.Name) && TryFind(container, _currentItem?.Children, out var item))
            {
                item.Name = container.Name;
                shouldRefresh = true;
            }
        }

        if (shouldRefresh)
            await RefreshAsync();
    }

    private async void OnContainersAdded(IEnumerable<(Container Parent, Container Container)> events)
    {
        foreach (var e in events)
        {
            if (_currentItem?.Container == e.Parent)
            {
                _currentItem.AddChild(new BreadcrumbContainerItem { Container = e.Container, Name = e.Container.Name });
            }
            else
            {
                _bufferedContainer = null;
                _shouldRecalcCurrentItem = true;
            }
        }

        await RefreshAsync();
    }

    private async void OnContainersRemoved(IEnumerable<(Container Parent, Container Container)> events)
    {
        foreach (var e in events)
        {
            if (_currentItem != null && TryFind(e.Container, _currentItem.Children, out var item))
            {
                _currentItem.RemoveChild(item);
            }
            else
            {
                _bufferedContainer = null;
                _shouldRecalcCurrentItem = true;
            }
        }

        await RefreshAsync();
    }

    protected override void OnInitialized()
    {
        _currentItem = GetCurrentContainer().ToBreadcrumbContainerItem();

        ClusterBuilderEventBuffer.ContainersAdded += OnContainersAdded;
        ClusterBuilderEventBuffer.ContainersRemoved += OnContainersRemoved;
        ClusterBuilderEventBuffer.ContainerPropertiesChanged += OnContainerPropertiesChanged;

        DiagramEventService.ContainerLoaded += OnContainerLoaded;
    }

    private async Task RefreshAsync()
    {
        if (_shouldRecalcCurrentItem)
        {
            _currentItem = GetCurrentContainer().ToBreadcrumbContainerItem();
            _shouldRecalcCurrentItem = false;
        }

        _shouldRender = true;
        await InvokeAsync(StateHasChanged);
    }

    protected override bool ShouldRender()
    {
        if (_shouldRender)
        {
            _shouldRender = false;
            return true;
        }

        return false;
    }

    private static bool TryFind(Container target, IReadOnlyCollection<BreadcrumbItem>? list, [MaybeNullWhen(false)] out BreadcrumbContainerItem item)
    {
        item = list?
            .OfType<BreadcrumbContainerItem>()
            .FirstOrDefault(b => b.Container == target);

        return item != null;
    }
}
