using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace ViciOne.Ui.ClusterEditor.EndToEnd.Tests.Helper;

/// <summary>
/// Drives the Connector Selection dialog, opened for two FunctionBlocks of the <see cref="DesignName"/> design.
/// </summary>
/// <remarks>
/// The dialog de-duplicates by design, so the table always shows the five connectors of that design:
/// the inputs <c>FunctionBlockEnabled</c>, <c>Signal</c> and <c>Reset</c>, and the outputs
/// <c>FunctionBlockEnabled</c> and <c>Pieces</c>. <c>FunctionBlockEnabled</c> is the system connector.
/// </remarks>
internal sealed class ConnectorSelectionDialogPage
{
    public const string ColumnChooserTitle = "Column Chooser";
    public const string ConnectorColumnId = "ConnectorName";
    public const string DataTypeColumnId = "ConnectorTypeName";
    public const string DescriptionColumnId = "Description";
    public const string DeselectAllTitle = "Deselect all Connectors";
    public const string DesignName = "Pieces";
    public const string ElementColumnId = "ParentName";
    public const int RowCount = 5;
    public const string SelectAllConnectorsTitle = "Select All Connectors (including system connectors)";
    public const string SelectInputConnectorsTitle = "Select Input Connectors";

    private static readonly Position s_emptyDiagramPosition = new() { X = 40, Y = 40 };

    public ILocator ColumnChooserPopup => Page.Locator(".column-chooser-popup");

    public ILocator ColumnFilterPanel => Page.Locator(".column-filter-panel");

    public ILocator Dialog { get; }

    /// <summary>
    /// The table footer with the element count and the selection count, not the dialog's button footer.
    /// </summary>
    public ILocator Footer => Dialog.Locator("div.footer").Filter(new() { HasNot = Page.Locator("button") });

    public ILocator Headers => Table.Locator("thead th");

    public IPage Page { get; }

    /// <summary>
    /// The rendered data rows; the virtualization spacer rows carry no cell with a row index.
    /// </summary>
    public ILocator Rows => Table.Locator("tbody tr").Filter(new() { Has = Page.Locator("td[data-row-index]") });

    public ILocator SearchBox => Dialog.Locator("input.search-box-input");

    public ILocator Table { get; }

    public ILocator VisibleToolbarButtons => Dialog.Locator(".toolbar-button:not(.hidden)");

    private ConnectorSelectionDialogPage(IPage page)
    {
        Page = page;
        Dialog = page.Locator(".modal-dialog");
        Table = Dialog.Locator("table.inner-table");
    }

    public ILocator Cell(int row, string columnId) => Rows.Nth(row).Locator($"td[data-column-id=\"{columnId}\"]");

    public ILocator Cells(string columnId) => Rows.Locator($"td[data-column-id=\"{columnId}\"]");

    /// <summary>
    /// Clicks the chooser entry of the column, which toggles its visibility. The chooser must be open.
    /// </summary>
    public async Task ChooserToggle(string columnTitle)
    {
        // The entry label is not clickable, only the check box in front of it.
        await ColumnChooserPopup
            .Locator(".column-chooser-content span")
            .Filter(new() { HasTextRegex = new Regex($"^{Regex.Escape(columnTitle)}$") })
            .Locator("xpath=preceding-sibling::div[contains(@class,'check-box')][1]")
            .ClickAsync();
    }

    public ILocator CheckBox(int row) => Rows.Nth(row).Locator(".check-box").First;

    public async Task Close(string buttonText)
    {
        await Dialog.GetByRole(AriaRole.Button, new() { Exact = true, Name = buttonText }).ClickAsync();
        await Expect(Dialog).ToHaveCountAsync(0);
    }

    /// <summary>
    /// Closes the column chooser through its own close button, which covers the toolbar toggle while the chooser is open.
    /// </summary>
    public async Task CloseColumnChooser()
    {
        await ColumnChooserPopup.Locator(".popup-header-action-button").ClickAsync();
        await Expect(ColumnChooserPopup).ToHaveCountAsync(0);
    }

    /// <summary>
    /// Waits until the footer reports <paramref name="count"/> selected rows; a count of zero means no selection part at all.
    /// </summary>
    public async Task ExpectSelectedCount(int count)
    {
        if (count == 0)
            await Expect(Footer).Not.ToContainTextAsync("Selected");
        else
            await Expect(Footer).ToHaveTextAsync(new Regex($@"\|\s*{count} Selected\s*$"));
    }

    /// <summary>
    /// Proves that an interaction without visible effect has been processed, then checks the selection count.
    /// </summary>
    /// <remarks>
    /// Checks and unchecks <paramref name="uncheckedRow"/>. Blazor Server handles the events of a circuit in order,
    /// so once the check box round trip has rendered, so has every interaction sent before it.
    /// </remarks>
    public async Task ExpectSelectionUnchanged(int count, int uncheckedRow)
    {
        await CheckBox(uncheckedRow).ClickAsync();
        await ExpectSelectedCount(count + 1);
        await CheckBox(uncheckedRow).ClickAsync();
        await ExpectSelectedCount(count);
    }

    /// <summary>
    /// Applies a "contains" column filter; an empty <paramref name="value"/> clears it.
    /// </summary>
    public async Task FilterColumn(string columnId, string value)
    {
        await Header(columnId).Locator("button.column-filter-button").ClickAsync();

        var input = ColumnFilterPanel.Locator("input");
        await input.ClickAsync();
        await Page.Keyboard.PressAsync("Control+A");
        await Page.Keyboard.PressAsync("Backspace");
        if (value.Length > 0)
            await input.PressSequentiallyAsync(value);

        await ColumnFilterPanel.GetByRole(AriaRole.Button, new() { Exact = true, Name = "Apply" }).ClickAsync();
        await Expect(ColumnFilterPanel).ToHaveCountAsync(0);
    }

    public ILocator Header(string columnId) => Table.Locator($"thead th[data-column-id=\"{columnId}\"]");

    /// <summary>
    /// The clickable part of a header, which sorts; the rest of the header holds the filter button and the resize handle.
    /// </summary>
    public ILocator HeaderContent(string columnId) => Header(columnId).Locator(".header-content");

    /// <summary>
    /// The largest horizontal offset, in pixels, between a header cell and the cell below it in the first row.
    /// </summary>
    public Task<double> HeaderBodyMisalignment() =>
        Table.EvaluateAsync<double>(
            """
            table => {
                const headers = [...table.querySelectorAll('thead th')];
                const row = [...table.querySelectorAll('tbody tr')].find(r => r.querySelector('td[data-row-index]'));
                const cells = [...row.querySelectorAll(':scope > td')];
                return Math.max(...headers.map((th, i) => {
                    if (!cells[i]) return 999;
                    const a = th.getBoundingClientRect(), b = cells[i].getBoundingClientRect();
                    return Math.max(Math.abs(a.left - b.left), Math.abs(a.width - b.width));
                }));
            }
            """);

    /// <summary>
    /// Opens the dialog for two FunctionBlocks of the <see cref="DesignName"/> design on an empty diagram.
    /// </summary>
    /// <remarks>
    /// The viewport is large enough to keep every toolbar button inline; at Playwright's default size
    /// half of them fold into the toolbar's overflow menu.
    /// </remarks>
    public static async Task<ConnectorSelectionDialogPage> Open(IPage page, string serverAddress)
    {
        await page.SetViewportSizeAsync(1600, 1000);
        await page.GotoAsync(serverAddress);

        await DiagramHelper.AddFunctionBlockAsync(page, DesignName, 2);
        await Expect(page.Locator(".diagram-node")).ToHaveCountAsync(2);

        var dialogPage = new ConnectorSelectionDialogPage(page);
        await dialogPage.Reopen();
        return dialogPage;
    }

    /// <summary>
    /// Opens the dialog again for every block on the diagram. The dialog must be closed.
    /// </summary>
    public async Task Reopen()
    {
        await DiagramHelper.SelectAllBlocks(Page, s_emptyDiagramPosition);
        await DiagramHelper.ClickFunctionBlockContextMenuItem(Page, "Connectors Wizard");
        await Expect(Rows).ToHaveCountAsync(RowCount);
    }

    /// <summary>
    /// Replaces the search text by typing it; an empty <paramref name="text"/> clears the search.
    /// </summary>
    /// <remarks>
    /// Clears with Backspace, as a forward Delete can reach the diagram's global Delete handler.
    /// </remarks>
    public async Task Search(string text)
    {
        await SearchBox.ClickAsync();
        await Page.Keyboard.PressAsync("Control+A");
        await Page.Keyboard.PressAsync("Backspace");
        if (text.Length > 0)
            await SearchBox.PressSequentiallyAsync(text);
    }

    public ILocator ToolbarButton(string title) => Dialog.Locator($".toolbar-button[title=\"{title}\"]:not(.hidden)");
}
