using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ViciOne.Ui.ClusterEditor.Helpers;
using ViciOne.Ui.ClusterEditor.Sections.Library.Models;
using ViciOne.Ui.ClusterEditor.Sections.Library.Services;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.MonochromeIcons.Core.Enums;
using ViciOne.Ui.MonochromeIcons.Core.Extensions;
using ViciOne.Ui.TreeEditor.Builder.Interface.Nodes;

namespace ViciOne.Ui.ClusterEditor.Sections.Library.Components;

public sealed partial class LibrarySectionContent : ComponentBase, IDisposable
{
    private readonly string _openIconCssClass = MonochromeIconName.Open.GetCssClasses(MonochromeIconSize.Small).ToSpaceSeparated();
    private readonly TreeEditor.Builder.TreeBuilder _treeBuilder = new();

    [Inject] private DataManagementService DataManagementService { get; set; } = default!;
    [Inject] private ILibraryService LibraryService { get; set; } = default!;

    public void Dispose()
    {
        LibraryService.EntriesChanged -= OnLibraryEntriesChangedAsync;

        _treeBuilder.DragAndDrop.DragEnded -= LibraryService.InvokeDragEnded;
        _treeBuilder.DragAndDrop.DragStarted -= OnTreeDragStarted;

        ((LibraryTreeAdapter)_treeBuilder.Adapter).DblClick -= OnTreeDblClick;

        _treeBuilder.Dispose();
    }

    private void FilterNodes(string filterText)
        => TreeAdapterHelper.FilterNodesByDisplayText(_treeBuilder, (node) => ((LibraryTreeNode)node).Parent, filterText);

    private void OnCollapseAllGroups()
        => _treeBuilder.Expansion.ChangeExpansionForLayers(false);

    private void OnExpandAllGroups()
        => _treeBuilder.Expansion.ChangeExpansionForLayers(true);

    private async Task OnImportFbDesignsClicked()
        => await DataManagementService.RequestLoadFbDesigns();

    protected override void OnInitialized()
    {
        LibraryService.EntriesChanged += OnLibraryEntriesChangedAsync;

        _treeBuilder.SetAdapter<LibraryTreeAdapter>();
        _treeBuilder.DragAndDrop.DisplayElementShadow = false;
        ((LibraryTreeAdapter)_treeBuilder.Adapter).DblClick += OnTreeDblClick;
        ((LibraryTreeAdapter)_treeBuilder.Adapter).SetEntries(LibraryService.LibraryEntries);
        _treeBuilder.Expansion.ChangeExpansionForLayers(true);

        _treeBuilder.DragAndDrop.DragEnded += LibraryService.InvokeDragEnded;
        _treeBuilder.DragAndDrop.DragStarted += OnTreeDragStarted;
    }

    private async void OnLibraryEntriesChangedAsync()
    {
        ((LibraryTreeAdapter)_treeBuilder.Adapter).SetEntries(LibraryService.LibraryEntries);

        await InvokeAsync(StateHasChanged);
    }

    private void OnTreeDblClick(LibraryTreeNode tNode)
        => LibraryService.RequestFunctionBlockCreation(tNode.LibraryEntry.UniqueId);

    private void OnTreeDragStarted(IEnumerable<ITreeNode> treeNodes)
    {
        LibraryService.DraggingEntries = treeNodes.Select(tn => ((LibraryTreeNode)tn).LibraryEntry);
        LibraryService.InvokeDragStarted();
    }
}
