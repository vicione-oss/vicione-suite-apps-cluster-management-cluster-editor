using Microsoft.AspNetCore.Components;
using Shared.Debugging.Models;
using Shared.Debugging.Services;
using Shared.Localization;
using ViciOne.Ui.Blazor.Components.Dialog.Components;
using ViciOne.Ui.Localization.Resources;
using LocalTechnicalTerms = Shared.Localization.Resources.TechnicalTerms;

namespace Shared.Debugging.Components;

public sealed partial class DebugDialog : ComponentBase
{
    private int _dataPointAmount = 1;
    private int _functionBlockAmount = 25;
    private string _functionBlockDesignName = "TwoWaySelector";
    private int _linkAmount = 1;
    private Dialog? _refDialog;

    [Inject] private ClusterGeneratorService ClusterGeneratorService { get; set; } = default!;

    private static string GenerateAllFunctionBlocksText
        => CompositeFormats.Format(UserActions.GenerateSomething, $"{CommonVocabulary.All} {LocalTechnicalTerms.FunctionBlockPlural}");

    private static string GenerateDataPortsText
        => CompositeFormats.Format(UserActions.GenerateSomething, LocalTechnicalTerms.DataPortPlural);

    private static string GenerateFunctionBlocksText
        => CompositeFormats.Format(UserActions.GenerateSomething, LocalTechnicalTerms.FunctionBlockPlural);

    private static string GenerateLinksText
        => CompositeFormats.Format(UserActions.GenerateSomething, LocalTechnicalTerms.LinkPlural);

    private Task OnDialogClose()
    {
        if (_refDialog is not null)
            return _refDialog.CloseAsync();

        return Task.CompletedTask;
    }

    private Task OnGenerateAllFunctionBlocksClick()
        => ClusterGeneratorService.GenerateAllLibraryFunctionBlocks();

    private Task OnGenerateDataPointsClick()
        => ClusterGeneratorService.GenerateDataPoints(_dataPointAmount);

    private Task OnGenerateFunctionBlocksClick()
        => ClusterGeneratorService.GenerateFunctionBlocks(_functionBlockDesignName, _functionBlockAmount);

    private Task OnGenerateLinksClick(GenerateLinksType type)
        => ClusterGeneratorService.GenerateLinks(type, _linkAmount);

    internal Task ShowDialog()
    {
        if (_refDialog is not null)
            return _refDialog.ShowAsync();

        return Task.CompletedTask;
    }
}
