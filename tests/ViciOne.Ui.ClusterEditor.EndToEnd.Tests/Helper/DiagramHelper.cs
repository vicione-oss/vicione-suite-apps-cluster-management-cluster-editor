using Microsoft.Playwright;
using ViciOne.Ui.ClusterEditor.EndToEnd.Tests.Extensions;
using static Microsoft.Playwright.Assertions;

namespace ViciOne.Ui.ClusterEditor.EndToEnd.Tests.Helper;

internal static class DiagramHelper
{
    private const string ContextMenuItemSelector = "div.context-menu-item";
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
    /// <remarks>
    /// The library tree is expanded first, as its entries are not rendered while their folder is collapsed.
    /// </remarks>
    public static async Task AddFunctionBlockAsync(IPage page, string name, int count = 1)
    {
        var libraryLocator = await page.ShowSectionAndGetLocator(SectionNames.Library);
        await Expect(libraryLocator).ToBeVisibleAsync();

        await page.Locator(".sidebar--right .flyout > .section:not(.hidden)")
            .GetByTitle("Expand All", new() { Exact = true })
            .First
            .ClickAsync();

        var fbLocator = libraryLocator.GetByText(name, new() { Exact = true });
        await Expect(fbLocator).ToBeVisibleAsync();

        for (var i = 0; i < count; i++)
            await fbLocator.DblClickAsync();
    }

    public static async Task AddLabelAsync(IPage page, Position position)
    {
        var diagramLocator = page.Locator(DiagramSelector);
        await diagramLocator.ClickAsync(new() { Button = MouseButton.Right, Position = position });
        await page.GetByRole(AriaRole.Menuitem, new() { Name = "Add Label" }).ClickAsync();
    }

    /// <summary>
    /// Opens the context menu of the first FunctionBlock and clicks the item with the given text.
    /// The current block selection is kept.
    /// </summary>
    public static async Task ClickFunctionBlockContextMenuItem(IPage page, string itemText)
    {
        var nodeLocator = page.Locator(".diagram-node").First;
        var box = await nodeLocator.BoundingBoxAsync() ?? throw new InvalidOperationException("No FunctionBlock on the diagram.");

        // The node header, clear of the connectors and markers.
        await page.Mouse.ClickAsync(box.X + (box.Width / 2), box.Y + 12, new() { Button = MouseButton.Right });
        await page.Locator(ContextMenuItemSelector, new() { HasText = itemText }).First.ClickAsync();
    }

    /// <remarks>
    /// Returns once the context menu has closed; a right click arriving earlier only closes the menu.
    /// </remarks>
    public static async Task SelectAllBlocks(IPage page, Position position)
    {
        var diagramLocator = page.Locator(DiagramSelector);
        await diagramLocator.ClickAsync(new() { Button = MouseButton.Right, Position = position });
        await page.Locator(ContextMenuItemSelector, new() { HasText = "Select all blocks" }).ClickAsync();

        // Closed menus stay in the DOM, hidden.
        await Expect(page.Locator($"{ContextMenuItemSelector}:visible")).ToHaveCountAsync(0);
    }
}
