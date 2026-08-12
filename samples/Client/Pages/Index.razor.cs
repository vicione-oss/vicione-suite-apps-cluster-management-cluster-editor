using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Designs;
using Shared.Services;
using ViciOne.Ui.Blazor.Components.LoadingSpinner.Models;
using ViciOne.Ui.ClusterEditor.Services;

namespace BlazorWasm.Client.Pages;

[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Components cannot be internal")]
public sealed partial class Index : ComponentBase, IDisposable
{
    private Task? _designLoadingTask = Task.CompletedTask;
    private bool _errorVisible;
    private bool _loaderVisible = true;
    private readonly IEnumerable<TimedMessage> _messages = [new TimedMessage() { DisplayDuration = -1, Message = Localization.Index.LoadingDependencies }];
    private Action _messageToastCallback = () => { };
    private string _messageToastText = string.Empty;
    private string _messageToastTitle = string.Empty;
    private bool _messageToastVisible;

    [Inject] private IClusterEditorManagement? DataManagementService { get; set; }
    [Inject] private IndexService IndexService { get; set; } = default!;
    [Inject] private IServiceProvider ServiceProvider { get; set; } = default!;

    public void Dispose()
    {
        IndexService.SaveFailed -= OnSaveFailed;

        DataManagementService?.MessageToastRequested -= MessageToastRequested;
    }

    private async Task HideLoader()
    {
        _loaderVisible = false;
        await IndexService.InitCluster();
        await InvokeAsync(StateHasChanged);
    }

    private async Task MessageToastRequested(LogLevel logLevel, string message, Action clickCallback)
    {
        _messageToastText = message;
        _messageToastTitle = logLevel.ToString();
        _messageToastCallback = clickCallback;
        _messageToastVisible = true;
        await InvokeAsync(StateHasChanged);
    }

    private void MessageToastVisibilityChanged()
    {
        if (!_messageToastVisible)
            _messageToastCallback();
    }

    protected override void OnInitialized()
    {
        IndexService.SaveFailed += OnSaveFailed;

        if (DataManagementService is null)
            throw new TypeInitializationException(nameof(DataManagementService), null);

        DataManagementService.MessageToastRequested += MessageToastRequested;

        _designLoadingTask = ServiceProvider.GetServices<IHostedService>().OfType<IDownloader>().Single().Task;
        _designLoadingTask.ContinueWith(t => HideLoader(), TaskScheduler.Default);
    }

    private async Task OnSaveFailed()
    {
        _errorVisible = true;
        await InvokeAsync(StateHasChanged);
    }
}
