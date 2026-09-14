using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Shared.ClusterManagement.Services;
using Shared.Localization;
using Shared.Persistence.Services;
using ViciOne.Ui.Blazor.Components.Dialog.Components;
using ViciOne.Ui.Localization.Resources;

namespace Shared.Persistence.Components;

public sealed partial class PersistenceDialog : ComponentBase
{
    private const int EmptySaveSlotSize = -1;
    private const int StartUpSaveSlot = PersistenceService.StartUpSaveSlot;

    private Dialog? _refDialog;
    private IReadOnlyDictionary<int, int> _saveSlotSizes = new Dictionary<int, int>();

    [Inject] private ClusterManagementService ClusterManagementService { get; set; } = default!;
    [Inject] private PersistenceService PersistenceService { get; set; } = default!;

    private static string ImportAndExportText
        => CompositeFormats.Format(CommonPatterns.ThisAndThat, CommonVocabulary.ImportVerb, CommonVocabulary.ExportVerb);

    private static string LoadAndSaveText
        => CompositeFormats.Format(CommonPatterns.ThisAndThat, Localization.PersistenceDialog.LoadVerb, CommonVocabulary.Save);

    private static IEnumerable<int> SaveSlots { get; } = Enumerable.Range(1, PersistenceService.SaveSlotCount);

    private int GetSaveSlotSize(int saveSlot)
        => _saveSlotSizes.TryGetValue(saveSlot, out var saveSlotSize) ? saveSlotSize : EmptySaveSlotSize;

    private string GetSaveSlotSizeText(int saveSlot)
    {
        var saveSlotSize = GetSaveSlotSize(saveSlot);

        return IsSaveSlotOccupied(saveSlot)
            ? CompositeFormats.Format(Localization.PersistenceDialog.SaveSlotSize, saveSlotSize)
            : Localization.PersistenceDialog.SaveSlotEmpty;
    }

    private static string GetSaveSlotText(int saveSlot)
        => CompositeFormats.Format(Localization.PersistenceDialog.SaveSlotWithNumber, saveSlot);

    private bool IsSaveSlotOccupied(int saveSlot)
        => GetSaveSlotSize(saveSlot) >= 0;

    private async Task OnClearSaveSlot(int saveSlot)
    {
        await PersistenceService.ClearSaveSlot(saveSlot);
        await RefreshSaveSlotSizes();
    }

    private Task OnDialogClose()
    {
        if (_refDialog is not null)
            return _refDialog.CloseAsync();

        return Task.CompletedTask;
    }

    private Task OnDialogShowing()
        => RefreshSaveSlotSizes();

    private Task OnFileUpload(InputFileChangeEventArgs e)
        => PersistenceService.ImportCluster(e.File);

    private async Task OnSaveToSaveSlot(int saveSlot)
    {
        await PersistenceService.SaveToSaveSlot(saveSlot);
        await RefreshSaveSlotSizes();
    }

    private async Task RefreshSaveSlotSizes()
    {
        var saveSlotSizes = await PersistenceService.GetSaveSlotSizes();

        if (saveSlotSizes is null)
            return;

        _saveSlotSizes = saveSlotSizes;

        await InvokeAsync(StateHasChanged);
    }

    internal Task ShowDialog()
    {
        if (_refDialog is not null)
            return _refDialog.ShowAsync();

        return Task.CompletedTask;
    }
}
