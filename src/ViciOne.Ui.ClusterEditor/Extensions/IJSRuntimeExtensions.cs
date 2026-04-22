using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using ViciOne.Ui.ClusterEditor.Constants;

namespace ViciOne.Ui.ClusterEditor.Extensions;

internal static class IJSRuntimeExtensions
{
    internal static async Task<int[]> MeasureNameFieldHeights(this IJSRuntime jsRuntime, List<string> names, CancellationToken cancellationToken)
    {
        if (names.Count == 0)
            return [];

        try
        {
            return await jsRuntime.InvokeAsync<int[]>(
                "ViciOne.Diagram.BlockNode.measureNameFieldHeights",
                cancellationToken,
                names,
                BlockNodeLayout.Width,
                DiagramSettings.DefaultGridSize
            );
        }
        catch (JSDisconnectedException)
        {
            // Circuit already gone – ignore
        }
        catch (ObjectDisposedException)
        {
            // JSRuntime already disposed – ignore
        }
        catch (TaskCanceledException)
        {
            // Task already canceled – ignore
        }

        var fallback = new int[names.Count];
        Array.Fill(fallback, 2 * DiagramSettings.DefaultGridSize);
        return fallback;
    }
}
