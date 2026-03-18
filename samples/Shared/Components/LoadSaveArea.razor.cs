using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Shared.ClusterSerialization;
using Shared.Services;
using ViciOne.Cluster.Builder.Abstractions;

namespace Shared.Components;

public sealed partial class LoadSaveArea : ComponentBase, IDisposable
{
    private const int MaxUploadFileSize = 1024 * 1024 * 1024;

    private int[] _fileSizes = [-1, -1, -1, -1];

    [Inject] private IndexService IndexService { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private ILogger<LoadSaveArea> Logger { get; set; } = default!;

    public void Dispose()
        => IndexService.ClusterLoaded -= OnClusterLoaded;

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to import cluster from {File}.")]
    private static partial void LogFailedImport(ILogger logger, Exception ex, string file);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _fileSizes = await JSRuntime.InvokeAsync<int[]>("ViciOne.File.getSizes");
            await InvokeAsync(StateHasChanged);
        }
    }

    private Task OnClusterLoaded(IClusterBuilder builder)
    {
        IndexService.LoadCluster(builder);
        return Task.CompletedTask;
    }

    private async Task OnFileClearClick(int saveSlot)
    {
        await JSRuntime.InvokeVoidAsync("ViciOne.File.clear", saveSlot);
        if (saveSlot < 4)
            _fileSizes[saveSlot - 1] = -1;
    }

    private async Task OnFileLoadClick(int saveSlot)
    {
        var json = await JSRuntime.InvokeAsync<string>("ViciOne.File.load", saveSlot);

        if (json is null)
          return;

        await IndexService.LoadClusterJson(json);
    }

    private async Task OnFileSaveClick(int saveSlot)
    {
        if (IndexService.Builder is null)
            return;

        IndexService.PrepareClusterSerialization();
        var json = ClusterSerializer.Serialize(IndexService.Builder.Cluster);
        var success = await JSRuntime.InvokeAsync<bool>("ViciOne.File.save", saveSlot, json);

        if (!success)
        {
            await IndexService.InvokeSaveFailed();
            return;
        }

        _fileSizes = await JSRuntime.InvokeAsync<int[]>("ViciOne.File.getSizes");
    }

    private async Task OnFileUpload(InputFileChangeEventArgs e)
    {
        try
        {
            await using MemoryStream ms = new();
            await e.File.OpenReadStream(MaxUploadFileSize).CopyToAsync(ms);
            var json = Encoding.UTF8.GetString(ms.ToArray());

            await IndexService.LoadClusterJson(json);
        }
        catch (Exception ex)
        {
            LogFailedImport(Logger, ex, e.File.Name);
        }
    }

    protected override void OnInitialized()
        => IndexService.ClusterLoaded += OnClusterLoaded;
}
