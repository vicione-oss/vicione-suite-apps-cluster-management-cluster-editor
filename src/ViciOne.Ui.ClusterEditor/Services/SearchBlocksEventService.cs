using System;

namespace ViciOne.Ui.ClusterEditor.Services;

internal static class SearchBlocksEventService
{
    public static event Action? ResetFindResultRequested;

    public static void RequestResetFindResult()
        => ResetFindResultRequested?.Invoke();
}
