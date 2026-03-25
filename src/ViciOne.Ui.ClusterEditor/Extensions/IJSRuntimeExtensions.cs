using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using ViciOne.Ui.ClusterEditor.Constants;

namespace ViciOne.Ui.ClusterEditor.Extensions;

internal static class IJSRuntimeExtensions
{
    /// <summary>
    ///     Invokes a JavaScript function to measure the height of a BlockNode name field.
    /// </summary>
    /// <param name="jsRuntime">The JavaScript runtime instance.</param>
    /// <param name="name">The name to measure.</param>
    /// <returns>The measured height as an integer.</returns>
    internal static async Task<int> MeasureNameFieldHeightAsync(this IJSRuntime jsRuntime, string name, CancellationToken cancellationToken)
    {
        var height = 2 * DiagramSettings.DefaultGridSize; // Default height if measurement fails

        try
        {
            height = await jsRuntime.InvokeAsync<int>(
                "ViciOne.Diagram.BlockNode.measureNameFieldHeight",
                cancellationToken,
                name,
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

        return height;
    }
}
