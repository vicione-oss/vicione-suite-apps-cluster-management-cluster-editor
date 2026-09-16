using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Services;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated through dependency injection")]
internal sealed class BoundsService(DiagramService diagramService, IJSRuntime jsRuntime, ILogger<BoundsService> logger)
{
    public Rectangle GetDiagramBounds()
    {
        var diagram = diagramService.Diagram;
        var diagramContainer = diagram.Container;

        if (diagramContainer is null)
            return Rectangle.Empty;

        var diagramContainerBounds = new Rectangle(
            Convert.ToInt32(diagramContainer.Left),
            Convert.ToInt32(diagramContainer.Top),
            Convert.ToInt32(diagramContainer.Width),
            Convert.ToInt32(diagramContainer.Height));

        return diagramContainerBounds;
    }

    public async Task<Rectangle?> GetWindowBoundsAsync()
    {
        var (widthSuccess, windowWidth) = await jsRuntime.TryInvoke<int>(logger, "eval", "window.innerWidth");
        var (heightSuccess, windowHeight) = await jsRuntime.TryInvoke<int>(logger, "eval", "window.innerHeight");

        if (!widthSuccess || !heightSuccess)
            return null;

        return new Rectangle(0, 0, windowWidth, windowHeight);
    }
}
