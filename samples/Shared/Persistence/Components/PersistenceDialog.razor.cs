using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Shared.Localization;
using Shared.Persistence.Services;
using Shared.Services;
using ViciOne.Ui.Blazor.Components.Dialog.Components;
using ViciOne.Ui.Localization.Resources;

namespace Shared.Persistence.Components;

public sealed partial class PersistenceDialog : ComponentBase, IDisposable
{
    private const int StartUpSaveSlot = PersistenceService.StartUpSaveSlot;

    private Dialog? _refDialog;

    [Inject] private IndexService IndexService { get; set; } = default!;
    [Inject] private PersistenceService PersistenceService { get; set; } = default!;

    private static string ImportAndExportText
        => CompositeFormats.Format(CommonPatterns.ThisAndThat, CommonVocabulary.ImportVerb, CommonVocabulary.ExportVerb);

    private static string LoadAndSaveText
        => CompositeFormats.Format(CommonPatterns.ThisAndThat, Localization.PersistenceDialog.LoadVerb, CommonVocabulary.Save);

    private static IEnumerable<int> SaveSlots { get; } = Enumerable.Range(1, PersistenceService.SaveSlotCount);

    public void Dispose()
        => PersistenceService.SaveSlotSizesChanged -= OnSaveSlotSizesChanged;

    private string GetSaveSlotSizeText(int saveSlot)
    {
        var saveSlotSize = PersistenceService.GetSaveSlotSize(saveSlot);

        return IsSaveSlotOccupied(saveSlot)
            ? CompositeFormats.Format(Localization.PersistenceDialog.SaveSlotSize, saveSlotSize)
            : Localization.PersistenceDialog.SaveSlotEmpty;
    }

    private static string GetSaveSlotText(int saveSlot)
        => CompositeFormats.Format(Localization.PersistenceDialog.SaveSlotWithNumber, saveSlot);

    private bool IsSaveSlotOccupied(int saveSlot)
        => PersistenceService.GetSaveSlotSize(saveSlot) >= 0;

    private Task OnDialogClose()
    {
        if (_refDialog is not null)
            return _refDialog.CloseAsync();

        return Task.CompletedTask;
    }

    private Task OnDialogShowing()
        => PersistenceService.RefreshSaveSlotSizes();

    private Task OnFileUpload(InputFileChangeEventArgs e)
        => PersistenceService.ImportCluster(e.File);

    protected override void OnInitialized()
        => PersistenceService.SaveSlotSizesChanged += OnSaveSlotSizesChanged;

    private Task OnSaveSlotSizesChanged()
        => InvokeAsync(StateHasChanged);

    internal Task ShowDialog()
    {
        if (_refDialog is not null)
            return _refDialog.ShowAsync();

        return Task.CompletedTask;
    }
}
