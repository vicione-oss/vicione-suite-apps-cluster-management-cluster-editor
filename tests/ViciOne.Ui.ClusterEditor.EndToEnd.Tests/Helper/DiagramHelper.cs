using Microsoft.Playwright;
using ViciOne.Ui.ClusterEditor.EndToEnd.Tests.Extensions;
using static Microsoft.Playwright.Assertions;

namespace ViciOne.Ui.ClusterEditor.EndToEnd.Tests.Helper;

internal static class DiagramHelper
{
    private const string DiagramSelector = ".diagram-canvas";

    public static async Task AddContainerAsync(IPage page, Position position)
    {
        var diagramLocator = page.Locator(DiagramSelector);
        await diagramLocator.ClickAsync(new() { Button = MouseButton.Right, Position = position });
        await page.GetByRole(AriaRole.Menuitem, new() { Name = "Add Container" }).ClickAsync();
    }

    /// <summary>
    /// Adds a FunctionBlock to the diagram by opening the library section and making a double click
    /// on an entry.
    /// </summary>
    public static async Task AddFunctionBlockAsync(IPage page, string name, int count = 1)
    {
        var libraryLocator = await page.ShowSectionAndGetLocator(SectionNames.Library);
        await Expect(libraryLocator).ToBeVisibleAsync();

        var fbLocator = libraryLocator.GetByTitle(name);

        for (var i = 0; i < count; i++)
            await fbLocator.DblClickAsync();
    }

    public static async Task AddLabelAsync(IPage page, Position position)
    {
        var diagramLocator = page.Locator(DiagramSelector);
        await diagramLocator.ClickAsync(new() { Button = MouseButton.Right, Position = position });
        await page.GetByRole(AriaRole.Menuitem, new() { Name = "Add Label" }).ClickAsync();
    }
}
