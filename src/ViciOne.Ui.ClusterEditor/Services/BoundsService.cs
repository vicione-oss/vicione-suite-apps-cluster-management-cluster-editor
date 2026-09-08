using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Services;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated through dependency injection")]
internal sealed class BoundsService(DiagramService diagramService, IJSRuntime jsRuntime)
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

    public async Task<Rectangle> GetWindowBoundsAsync()
    {
        var windowWidth = await jsRuntime.InvokeAsync<int>("eval", "window.innerWidth");
        var windowHeight = await jsRuntime.InvokeAsync<int>("eval", "window.innerHeight");
        return new Rectangle(0, 0, windowWidth, windowHeight);
    }
}
