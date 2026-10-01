using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using ViciOne.Ui.ClusterEditor.EndToEnd.Tests.Helper;
using ViciOne.Ui.ClusterEditor.EndToEnd.Tests.Infrastructure;
using ViciOne.Ui.Testing.Playwright.Infrastructure;
using Xunit;
using static Microsoft.Playwright.Assertions;
using static ViciOne.Ui.ClusterEditor.EndToEnd.Tests.Helper.ConnectorSelectionDialogPage;

namespace ViciOne.Ui.ClusterEditor.EndToEnd.Tests.Components.ConnectorDialogs;

/// <summary>
/// End-to-end tests of the Connector Selection dialog, run against two FunctionBlocks of one design.
/// </summary>
/// <remarks>
/// <para>
/// Row order on open: 0 <c>FunctionBlockEnabled</c> (input), 1 <c>Signal</c>, 2 <c>Reset</c>,
/// 3 <c>FunctionBlockEnabled</c> (output), 4 <c>Pieces</c>. Rows 1 and 2 are the non-system inputs.
/// </para>
/// <para>
/// Every test starts from the freshly opened dialog. Tests of that initial state have no Act section.
/// An Arrange section that clicks through the dialog ends by waiting for the state it set up,
/// so the Assert section cannot pass on a state from before the Act.
/// </para>
/// </remarks>
[Collection<ServerTestCollection>]
public class ConnectorSelectionDialogTests(ServerFixture fixture)
{
    private static readonly string[] s_bulkSelectTitles =
    [
        SelectInputConnectorsTitle,
        "Select All Input Connectors (including system connectors)",
        "Select Connectors",
        SelectAllConnectorsTitle,
        "Select Output Connectors",
        "Select All Output Connectors (including system connectors)",
        DeselectAllTitle,
    ];
    private static readonly string[] s_inputConnectors = ["FunctionBlockEnabled", "Reset", "Signal"];
    private static readonly string[] s_outputConnectors = ["FunctionBlockEnabled", "Pieces"];

    /// <summary>
    /// The arrow keys move the focus between rows.
    /// </summary>
    [Fact]
    public async Task Arrow_keys_should_move_the_focus_between_rows() =>
        await Run(async dialog =>
        {
            // Arrange
            await dialog.Cell(0, ElementColumnId).ClickAsync();

            // Act
            await dialog.Page.Keyboard.PressAsync("ArrowDown");
            await dialog.Page.Keyboard.PressAsync("ArrowDown");

            // Assert
            await Expect(dialog.Cell(2, ElementColumnId)).ToBeFocusedAsync();
        });

    /// <summary>
    /// The seven toolbar buttons select the inputs, all connectors or the outputs, or clear the selection,
    /// and each replaces the selection it finds. Each "All" variant also selects the system connector
    /// <c>FunctionBlockEnabled</c> of its direction, so it selects one row more than its counterpart.
    /// </summary>
    [Theory]
    [InlineData(SelectInputConnectorsTitle, 2)]
    [InlineData("Select All Input Connectors (including system connectors)", 3)]
    [InlineData("Select Connectors", 3)]
    [InlineData(SelectAllConnectorsTitle, 5)]
    [InlineData("Select Output Connectors", 1)]
    [InlineData("Select All Output Connectors (including system connectors)", 2)]
    [InlineData(DeselectAllTitle, 0)]
    public async Task Bulk_select_button_should_replace_the_selection(string title, int expectedCount) =>
        await Run(async dialog =>
        {
            // Arrange
            // No button selects four rows, so the Assert cannot pass on this selection.
            for (var row = 0; row < 4; row++)
                await dialog.CheckBox(row).ClickAsync();
            await dialog.ExpectSelectedCount(4);

            // Act
            await dialog.ToolbarButton(title).ClickAsync();

            // Assert
            await dialog.ExpectSelectedCount(expectedCount);
        });

    /// <summary>
    /// Closing with Cancel drops the search text and the column filters, so the dialog reopens with every row.
    /// </summary>
    [Fact]
    public async Task Cancel_should_reset_search_and_column_filter() =>
        await Run(async dialog =>
        {
            // Arrange
            await dialog.FilterColumn(ConnectorColumnId, "e");
            await dialog.Search("Bool");
            await Expect(dialog.Rows).ToHaveCountAsync(3);

            // Act
            await dialog.Close("Cancel");
            await dialog.Reopen();

            // Assert
            await Expect(dialog.SearchBox).ToHaveValueAsync(string.Empty);
            await Expect(dialog.Rows).ToHaveCountAsync(RowCount);
        });

    /// <summary>
    /// The check box adds exactly its own row to the selection. With row clicks off, it is the only way to select a single row.
    /// </summary>
    [Fact]
    public async Task Check_box_should_add_its_row_to_the_selection() =>
        await Run(async dialog =>
        {
            // Arrange
            await dialog.ToolbarButton(SelectInputConnectorsTitle).ClickAsync();
            await dialog.ExpectSelectedCount(2);

            // Act
            await dialog.CheckBox(0).ClickAsync();

            // Assert
            await dialog.ExpectSelectedCount(3);
            await Expect(dialog.CheckBox(0).Locator("input")).ToBeCheckedAsync();
        });

    /// <summary>
    /// Clicking a checked check box removes exactly its own row and leaves the rest of the selection alone.
    /// </summary>
    [Fact]
    public async Task Check_box_should_remove_only_its_row_from_the_selection() =>
        await Run(async dialog =>
        {
            // Arrange
            await dialog.ToolbarButton(SelectInputConnectorsTitle).ClickAsync();
            await dialog.CheckBox(0).ClickAsync();
            await dialog.ExpectSelectedCount(3);

            // Act
            await dialog.CheckBox(0).ClickAsync();

            // Assert
            await dialog.ExpectSelectedCount(2);
            await Expect(dialog.CheckBox(0).Locator("input")).Not.ToBeCheckedAsync();
            await Expect(dialog.CheckBox(1).Locator("input")).ToBeCheckedAsync();
            await Expect(dialog.CheckBox(2).Locator("input")).ToBeCheckedAsync();
        });

    /// <summary>
    /// The column chooser hides each data column, and header and body stay aligned.
    /// </summary>
    [Theory]
    [InlineData(ElementColumnId, "Element")]
    [InlineData(ConnectorColumnId, "Connector")]
    [InlineData(DataTypeColumnId, "Data type")]
    [InlineData(DescriptionColumnId, "Description")]
    public async Task Column_chooser_should_hide_a_data_column(string columnId, string columnTitle) =>
        await Run(async dialog =>
        {
            // Arrange
            await dialog.ToolbarButton(ColumnChooserTitle).ClickAsync();

            // Act
            await dialog.ChooserToggle(columnTitle);

            // Assert
            await Expect(dialog.Header(columnId)).ToHaveCountAsync(0);
            await Expect(dialog.Headers).ToHaveCountAsync(4);
            Assert.True(await dialog.HeaderBodyMisalignment() < 2, $"Header and body misaligned with {columnTitle} hidden.");
        });

    /// <summary>
    /// Nothing in the dialog paints over the open column chooser.
    /// </summary>
    [Fact]
    public async Task Column_chooser_should_not_be_painted_over() =>
        await Run(async dialog =>
        {
            // Act
            await dialog.ToolbarButton(ColumnChooserTitle).ClickAsync();

            // Assert
            await Expect(dialog.ColumnChooserPopup).ToBeVisibleAsync();
            var occludedPoints = await dialog.ColumnChooserPopup.EvaluateAsync<int>(
                """
                popup => {
                    const r = popup.getBoundingClientRect();
                    let occluded = 0;
                    for (let x = r.left + 4; x < r.right - 2; x += 15)
                        for (let y = r.top + 4; y < r.bottom - 2; y += 12)
                            if (!popup.contains(document.elementFromPoint(x, y))) occluded++;
                    return occluded;
                }
                """);
            Assert.Equal(0, occludedPoints);
        });

    /// <summary>
    /// The column chooser offers the four data columns. The select column is deliberately not offered.
    /// </summary>
    [Fact]
    public async Task Column_chooser_should_offer_the_data_columns_only() =>
        await Run(async dialog =>
        {
            // Act
            await dialog.ToolbarButton(ColumnChooserTitle).ClickAsync();

            // Assert
            await Expect(dialog.ColumnChooserPopup.Locator(".column-chooser-content span"))
                .ToHaveTextAsync(["Element", "Connector", "Data type", "Description"]);
        });

    /// <summary>
    /// The column chooser opens to the left of its toolbar button (<c>OpenPopupToLeft</c>) and stays inside the viewport.
    /// </summary>
    [Fact]
    public async Task Column_chooser_should_open_to_the_left_inside_the_viewport() =>
        await Run(async dialog =>
        {
            // Arrange
            var toggle = await Box(dialog.ToolbarButton(ColumnChooserTitle));
            var viewport = dialog.Page.ViewportSize!;

            // Act
            await dialog.ToolbarButton(ColumnChooserTitle).ClickAsync();

            // Assert
            await Expect(dialog.ColumnChooserPopup).ToBeVisibleAsync();
            var popup = await Box(dialog.ColumnChooserPopup);
            Assert.True(popup.X < toggle.X, "The chooser does not extend to the left of its toggle.");
            Assert.True(popup.X + popup.Width <= toggle.X + toggle.Width + 1, "The chooser extends to the right of its toggle.");
            Assert.True(popup.X >= 0 && popup.Y >= 0, "The chooser leaves the viewport at the top or left.");
            Assert.True(popup.X + popup.Width <= viewport.Width && popup.Y + popup.Height <= viewport.Height, "The chooser leaves the viewport at the bottom or right.");
        });

    /// <summary>
    /// The column chooser still opens after it has been opened, closed, used to hide and show a column, and closed again.
    /// </summary>
    [Fact]
    public async Task Column_chooser_should_reopen_after_an_open_close_hide_show_cycle() =>
        await Run(async dialog =>
        {
            // Arrange
            await dialog.ToolbarButton(ColumnChooserTitle).ClickAsync();
            await Expect(dialog.ColumnChooserPopup).ToBeVisibleAsync();
            await dialog.CloseColumnChooser();
            await dialog.ToolbarButton(ColumnChooserTitle).ClickAsync();
            await dialog.ChooserToggle("Data type");
            await Expect(dialog.Headers).ToHaveCountAsync(4);
            await dialog.ChooserToggle("Data type");
            await Expect(dialog.Headers).ToHaveCountAsync(5);
            await dialog.CloseColumnChooser();

            // Act
            await dialog.ToolbarButton(ColumnChooserTitle).ClickAsync();

            // Assert
            await Expect(dialog.ColumnChooserPopup).ToBeVisibleAsync();
        });

    /// <summary>
    /// The column chooser shows each hidden data column again, and header and body stay aligned.
    /// </summary>
    [Theory]
    [InlineData(ElementColumnId, "Element")]
    [InlineData(ConnectorColumnId, "Connector")]
    [InlineData(DataTypeColumnId, "Data type")]
    [InlineData(DescriptionColumnId, "Description")]
    public async Task Column_chooser_should_show_a_hidden_data_column_again(string columnId, string columnTitle) =>
        await Run(async dialog =>
        {
            // Arrange
            await dialog.ToolbarButton(ColumnChooserTitle).ClickAsync();
            await dialog.ChooserToggle(columnTitle);
            await Expect(dialog.Header(columnId)).ToHaveCountAsync(0);

            // Act
            await dialog.ChooserToggle(columnTitle);

            // Assert
            await Expect(dialog.Header(columnId)).ToHaveCountAsync(1);
            Assert.True(await dialog.HeaderBodyMisalignment() < 2, $"Header and body misaligned after showing {columnTitle} again.");
        });

    /// <summary>
    /// A column's filter panel opens anchored to the bottom-left corner of its filter button.
    /// An offset of roughly the dialog's own position on screen means the panel's containing block is wrong again.
    /// </summary>
    [Fact]
    public async Task Column_filter_panel_should_anchor_to_its_button() =>
        await Run(async dialog =>
        {
            // Arrange
            var filterButton = dialog.Header(ElementColumnId).Locator("button.column-filter-button");

            // Act
            await filterButton.ClickAsync();

            // Assert
            await Expect(dialog.ColumnFilterPanel).ToBeVisibleAsync();
            var button = await Box(filterButton);
            var panel = await Box(dialog.ColumnFilterPanel);
            Assert.InRange(panel.X, button.X - 1.5, button.X + 1.5);
            Assert.InRange(panel.Y, button.Y + button.Height - 1.5, button.Y + button.Height + 1.5);
        });

    /// <summary>
    /// The Connector column sorts by direction, inputs before outputs, not alphabetically by connector name.
    /// Deliberate: it is the same sort expression the initial sort uses.
    /// </summary>
    [Fact]
    public async Task Connector_column_should_sort_by_direction_not_by_name() =>
        await Run(async dialog =>
        {
            // Arrange
            await dialog.HeaderContent(ConnectorColumnId).ClickAsync();
            await Expect(dialog.Header(ConnectorColumnId)).ToHaveAttributeAsync("aria-sort", "descending");

            // Act
            await dialog.HeaderContent(ConnectorColumnId).ClickAsync();

            // Assert
            await Expect(dialog.Header(ConnectorColumnId)).ToHaveAttributeAsync("aria-sort", "ascending");
            await ExpectConnectorOrder(dialog, inputsFirst: true);
        });

    /// <summary>
    /// Clicking the Connector header of the freshly opened dialog, where Connector is the second sort level,
    /// makes it the only level and sorts it descending, outputs first.
    /// </summary>
    [Fact]
    public async Task Connector_column_should_sort_outputs_first_on_first_click() =>
        await Run(async dialog =>
        {
            // Act
            await dialog.HeaderContent(ConnectorColumnId).ClickAsync();

            // Assert
            await Expect(dialog.Header(ConnectorColumnId)).ToHaveAttributeAsync("aria-sort", "descending");
            await ExpectConnectorOrder(dialog, inputsFirst: false);
        });

    /// <summary>
    /// Ctrl+click on a row does not toggle its selection. Row clicks are off in this dialog
    /// (<c>RowClickSelectionEnabled="false"</c>), and Ctrl+click is part of the row click handling.
    /// </summary>
    [Fact]
    public async Task Ctrl_click_on_a_row_should_not_change_the_selection() =>
        await Run(async dialog =>
        {
            // Arrange
            await dialog.ToolbarButton(SelectInputConnectorsTitle).ClickAsync();
            await dialog.ExpectSelectedCount(2);

            // Act
            await dialog.Cell(4, ElementColumnId).ClickAsync(new() { Modifiers = [KeyboardModifier.Control] });

            // Assert
            await dialog.ExpectSelectionUnchanged(2, uncheckedRow: 3);
        });

    /// <summary>
    /// All four data columns are resizable, the select column is not.
    /// </summary>
    [Fact]
    public async Task Data_columns_should_be_resizable() =>
        await Run(async dialog =>
        {
            // Assert
            await Expect(dialog.Headers.First.Locator(".resize-handle")).ToHaveCountAsync(0);
            foreach (var id in new[] { ElementColumnId, ConnectorColumnId, DataTypeColumnId, DescriptionColumnId })
                await Expect(dialog.Header(id).Locator(".resize-handle")).ToHaveCountAsync(1);
        });

    /// <summary>
    /// The Data type column sorts ascending on the first header click.
    /// </summary>
    [Fact]
    public async Task Data_type_column_should_sort_ascending_on_first_click() =>
        await Run(async dialog =>
        {
            // Act
            await dialog.HeaderContent(DataTypeColumnId).ClickAsync();

            // Assert
            await Expect(dialog.Header(DataTypeColumnId)).ToHaveAttributeAsync("aria-sort", "ascending");
            await Expect(dialog.Cells(DataTypeColumnId)).ToHaveTextAsync(["Boolean", "Boolean", "Boolean", "Boolean", "Double"]);
        });

    /// <summary>
    /// The Data type column sorts descending on the second header click.
    /// </summary>
    [Fact]
    public async Task Data_type_column_should_sort_descending_on_second_click() =>
        await Run(async dialog =>
        {
            // Arrange
            await dialog.HeaderContent(DataTypeColumnId).ClickAsync();
            await Expect(dialog.Header(DataTypeColumnId)).ToHaveAttributeAsync("aria-sort", "ascending");

            // Act
            await dialog.HeaderContent(DataTypeColumnId).ClickAsync();

            // Assert
            await Expect(dialog.Header(DataTypeColumnId)).ToHaveAttributeAsync("aria-sort", "descending");
            await Expect(dialog.Cells(DataTypeColumnId)).ToHaveTextAsync(["Double", "Boolean", "Boolean", "Boolean", "Boolean"]);
        });

    /// <summary>
    /// The Description column deliberately does not sort, as it declares no sort expression: header clicks
    /// leave the sort indicators and the row order as they are.
    /// </summary>
    [Fact]
    public async Task Description_column_should_not_sort() =>
        await Run(async dialog =>
        {
            // Arrange
            var orderBefore = await dialog.Cells(ConnectorColumnId).AllInnerTextsAsync();

            // Act
            await dialog.HeaderContent(DescriptionColumnId).ClickAsync();
            await dialog.HeaderContent(DescriptionColumnId).ClickAsync();

            // Assert
            await dialog.ExpectSelectionUnchanged(0, uncheckedRow: 0);
            await Expect(dialog.Header(DescriptionColumnId)).ToHaveAttributeAsync("aria-sort", "none");
            await Expect(dialog.Header(ElementColumnId)).ToHaveAttributeAsync("aria-sort", "ascending");
            await Expect(dialog.Cells(ConnectorColumnId)).ToHaveTextAsync(orderBefore);
        });

    /// <summary>
    /// The dialog opens with a select column without a select-all check box, the data columns Element, Connector,
    /// Data type and Description, and a toolbar with the seven bulk-select buttons followed by the column chooser.
    /// </summary>
    [Fact]
    public async Task Dialog_should_open_with_select_column_four_data_columns_and_toolbar() =>
        await Run(async dialog =>
        {
            // Assert
            await Expect(dialog.Headers).ToHaveTextAsync(["", "Element", "Connector", "Data type", "Description"]);
            await Expect(dialog.Headers.First.Locator("input")).ToHaveCountAsync(0);
            await Expect(dialog.VisibleToolbarButtons).ToHaveCountAsync(s_bulkSelectTitles.Length + 1);
            var titles = await dialog.VisibleToolbarButtons.EvaluateAllAsync<string[]>("buttons => buttons.map(b => b.title)");
            Assert.Equal([.. s_bulkSelectTitles, ColumnChooserTitle], titles);
        });

    /// <summary>
    /// The Element column, the initial first sort level, sorts ascending on its first click once another column
    /// has replaced the initial sort.
    /// </summary>
    [Fact]
    public async Task Element_column_should_sort_ascending_on_first_click() =>
        await Run(async dialog =>
        {
            // Arrange
            await dialog.HeaderContent(DataTypeColumnId).ClickAsync();
            await Expect(dialog.Header(ElementColumnId)).ToHaveAttributeAsync("aria-sort", "none");

            // Act
            await dialog.HeaderContent(ElementColumnId).ClickAsync();

            // Assert
            await Expect(dialog.Header(ElementColumnId)).ToHaveAttributeAsync("aria-sort", "ascending");
        });

    /// <summary>
    /// The Element column sorts descending on its second click.
    /// </summary>
    [Fact]
    public async Task Element_column_should_sort_descending_on_second_click() =>
        await Run(async dialog =>
        {
            // Arrange
            await dialog.HeaderContent(DataTypeColumnId).ClickAsync();
            await dialog.HeaderContent(ElementColumnId).ClickAsync();
            await Expect(dialog.Header(DataTypeColumnId)).ToHaveAttributeAsync("aria-sort", "none");
            await Expect(dialog.Header(ElementColumnId)).ToHaveAttributeAsync("aria-sort", "ascending");

            // Act
            await dialog.HeaderContent(ElementColumnId).ClickAsync();

            // Assert
            await Expect(dialog.Header(ElementColumnId)).ToHaveAttributeAsync("aria-sort", "descending");
        });

    /// <summary>
    /// Enter on the focused row does nothing, as the dialog wires no per-column activation.
    /// </summary>
    [Fact]
    public async Task Enter_should_neither_select_nor_close() =>
        await Run(async dialog =>
        {
            // Arrange
            await dialog.Cell(0, ElementColumnId).ClickAsync();
            await dialog.Page.Keyboard.PressAsync("ArrowDown");
            await dialog.Page.Keyboard.PressAsync("ArrowDown");
            await Expect(dialog.Cell(2, ElementColumnId)).ToBeFocusedAsync();

            // Act
            await dialog.Page.Keyboard.PressAsync("Enter");

            // Assert
            await dialog.ExpectSelectionUnchanged(0, uncheckedRow: 4);
            await Expect(dialog.Dialog).ToBeVisibleAsync();
        });

    /// <summary>
    /// The table footer shows the number of rows.
    /// </summary>
    [Fact]
    public async Task Footer_should_show_the_element_count() =>
        await Run(async dialog =>
        {
            // Assert
            await Expect(dialog.Footer).ToHaveTextAsync(new Regex($@"^\s*{RowCount} Elements\s*$"));
        });

    /// <summary>
    /// Once rows are selected, the footer adds the selected count after the row count, as in "5 Elements | 3 Selected".
    /// </summary>
    [Fact]
    public async Task Footer_should_show_the_selected_count_after_the_element_count() =>
        await Run(async dialog =>
        {
            // Act
            for (var row = 0; row < 3; row++)
                await dialog.CheckBox(row).ClickAsync();

            // Assert
            await Expect(dialog.Footer).ToHaveTextAsync(new Regex($@"^\s*{RowCount} Elements\s*\|\s*3 Selected\s*$"));
        });

    /// <summary>
    /// A plain header click replaces both levels of the initial sort.
    /// </summary>
    [Fact]
    public async Task Header_click_should_replace_the_initial_sort() =>
        await Run(async dialog =>
        {
            // Act
            await dialog.HeaderContent(DataTypeColumnId).ClickAsync();

            // Assert
            await Expect(dialog.Header(DataTypeColumnId)).ToHaveAttributeAsync("aria-sort", "ascending");
            await Expect(dialog.Header(ElementColumnId)).ToHaveAttributeAsync("aria-sort", "none");
            await Expect(dialog.Header(ConnectorColumnId)).ToHaveAttributeAsync("aria-sort", "none");
        });

    /// <summary>
    /// The dialog opens sorted by Element, then by Connector, which puts each element's inputs before its outputs.
    /// </summary>
    [Fact]
    public async Task Initial_sort_should_be_element_then_connector_direction() =>
        await Run(async dialog =>
        {
            // Assert
            await ExpectInitialSort(dialog);
        });

    /// <summary>
    /// The sort indicators of a multi-column sort show each level's direction but not its priority. Deliberate.
    /// </summary>
    [Fact]
    public async Task Multi_column_sort_should_not_show_priorities() =>
        await Run(async dialog =>
        {
            // Arrange
            await dialog.HeaderContent(DataTypeColumnId).ClickAsync();

            // Act
            await dialog.HeaderContent(ElementColumnId).ClickAsync(new() { Modifiers = [KeyboardModifier.Shift] });

            // Assert
            await Expect(dialog.Header(ElementColumnId)).ToHaveAttributeAsync("aria-sort", "ascending");
            await Expect(dialog.Header(DataTypeColumnId)).ToHaveAttributeAsync("aria-sort", "ascending");
            var indicators = await dialog.Table.Locator("thead .sorting-indicator").AllInnerTextsAsync();
            Assert.All(indicators, text => Assert.DoesNotMatch(@"\d", text));
        });

    /// <summary>
    /// A plain header click collapses a multi-column sort to that one column; there is no gesture that removes a single
    /// level. Deliberate.
    /// </summary>
    [Fact]
    public async Task Plain_click_should_collapse_a_multi_column_sort() =>
        await Run(async dialog =>
        {
            // Arrange
            await dialog.HeaderContent(DataTypeColumnId).ClickAsync();
            await dialog.HeaderContent(ElementColumnId).ClickAsync(new() { Modifiers = [KeyboardModifier.Shift] });
            await Expect(dialog.Header(ElementColumnId)).ToHaveAttributeAsync("aria-sort", "ascending");
            await Expect(dialog.Header(DataTypeColumnId)).ToHaveAttributeAsync("aria-sort", "ascending");

            // Act
            await dialog.HeaderContent(ConnectorColumnId).ClickAsync();

            // Assert
            await Expect(dialog.Header(ConnectorColumnId)).ToHaveAttributeAsync("aria-sort", "ascending");
            await Expect(dialog.Header(ElementColumnId)).ToHaveAttributeAsync("aria-sort", "none");
            await Expect(dialog.Header(DataTypeColumnId)).ToHaveAttributeAsync("aria-sort", "none");
        });

    /// <summary>
    /// Reopening the dialog after a header click restores the initial sort.
    /// </summary>
    [Fact]
    public async Task Reopen_should_restore_the_initial_sort() =>
        await Run(async dialog =>
        {
            // Arrange
            await dialog.HeaderContent(DataTypeColumnId).ClickAsync();
            await Expect(dialog.Header(ElementColumnId)).ToHaveAttributeAsync("aria-sort", "none");

            // Act
            await dialog.Close("Cancel");
            await dialog.Reopen();

            // Assert
            await ExpectInitialSort(dialog);
        });

    /// <summary>
    /// A plain row click neither selects the row nor replaces the selection. Row clicks are off in this dialog
    /// (<c>RowClickSelectionEnabled="false"</c>), so a stray click cannot lose a bulk selection.
    /// </summary>
    [Fact]
    public async Task Row_click_should_not_change_the_selection() =>
        await Run(async dialog =>
        {
            // Arrange
            await dialog.ToolbarButton(SelectInputConnectorsTitle).ClickAsync();
            await dialog.ExpectSelectedCount(2);

            // Act
            await dialog.Cell(4, ElementColumnId).ClickAsync();

            // Assert
            await dialog.ExpectSelectionUnchanged(2, uncheckedRow: 3);
            await Expect(dialog.CheckBox(4).Locator("input")).Not.ToBeCheckedAsync();
        });

    /// <summary>
    /// The dialog lists the connectors of a design once, however many selected blocks share that design:
    /// two blocks of one design give five rows, not ten.
    /// </summary>
    [Fact]
    public async Task Rows_should_be_deduplicated_by_design() =>
        await Run(async dialog =>
        {
            // Assert
            await Expect(dialog.Rows).ToHaveCountAsync(RowCount);
            await Expect(dialog.Cells(ElementColumnId)).ToHaveTextAsync(Enumerable.Repeat(DesignName, RowCount));
        });

    /// <summary>
    /// Clearing the search restores the rows the column filter lets through, not every row.
    /// </summary>
    [Fact]
    public async Task Search_clear_should_keep_the_column_filter() =>
        await Run(async dialog =>
        {
            // Arrange
            // Every connector but Signal contains an "e"; Pieces is the only non-Boolean.
            await dialog.FilterColumn(ConnectorColumnId, "e");
            await dialog.Search("Bool");
            await Expect(dialog.Rows).ToHaveCountAsync(3);

            // Act
            await dialog.Search(string.Empty);

            // Assert
            await Expect(dialog.Rows).ToHaveCountAsync(4);
        });

    /// <summary>
    /// Clearing the search restores every row.
    /// </summary>
    [Fact]
    public async Task Search_clear_should_restore_all_rows() =>
        await Run(async dialog =>
        {
            // Arrange
            await dialog.Search("Sig");
            await Expect(dialog.Rows).ToHaveCountAsync(1);

            // Act
            await dialog.Search(string.Empty);

            // Assert
            await Expect(dialog.Rows).ToHaveCountAsync(RowCount);
        });

    /// <summary>
    /// The search combines with a column filter instead of replacing it, and highlights the matching text.
    /// </summary>
    [Fact]
    public async Task Search_should_compose_with_a_column_filter() =>
        await Run(async dialog =>
        {
            // Arrange
            // Every connector but Signal contains an "e"; Pieces is the only non-Boolean.
            await dialog.FilterColumn(ConnectorColumnId, "e");
            await Expect(dialog.Rows).ToHaveCountAsync(4);

            // Act
            await dialog.Search("Bool");

            // Assert
            await Expect(dialog.Rows).ToHaveCountAsync(3);
            await Expect(dialog.Cells(DataTypeColumnId).Locator(".highlightedText")).ToHaveCountAsync(3);
        });

    /// <summary>
    /// The search narrows the rows and highlights the matching text.
    /// </summary>
    [Fact]
    public async Task Search_should_narrow_the_rows_and_highlight_matches() =>
        await Run(async dialog =>
        {
            // Act
            await dialog.Search("Sig");

            // Assert
            await Expect(dialog.Rows).ToHaveCountAsync(1);
            await Expect(dialog.Cell(0, ConnectorColumnId).Locator(".highlightedText")).ToHaveTextAsync("Sig");
        });

    /// <summary>
    /// Shift+click on rows does not select a range. Row clicks are off in this dialog, so a larger selection takes
    /// one check box click per row; accepted as the price for not losing a selection to a stray click.
    /// </summary>
    [Fact]
    public async Task Shift_click_on_rows_should_not_select_a_range() =>
        await Run(async dialog =>
        {
            // Arrange
            await dialog.Cell(0, ElementColumnId).ClickAsync();

            // Act
            await dialog.Cell(3, ElementColumnId).ClickAsync(new() { Modifiers = [KeyboardModifier.Shift] });

            // Assert
            await dialog.ExpectSelectionUnchanged(0, uncheckedRow: 4);
        });

    /// <summary>
    /// Shift+click on a header adds that column as a lower sort level.
    /// </summary>
    [Fact]
    public async Task Shift_click_should_add_a_lower_sort_level() =>
        await Run(async dialog =>
        {
            // Arrange
            await dialog.HeaderContent(DataTypeColumnId).ClickAsync();
            await Expect(dialog.Header(DataTypeColumnId)).ToHaveAttributeAsync("aria-sort", "ascending");
            await Expect(dialog.Header(ElementColumnId)).ToHaveAttributeAsync("aria-sort", "none");

            // Act
            await dialog.HeaderContent(ElementColumnId).ClickAsync(new() { Modifiers = [KeyboardModifier.Shift] });

            // Assert
            await Expect(dialog.Header(ElementColumnId)).ToHaveAttributeAsync("aria-sort", "ascending");
            await Expect(dialog.Header(DataTypeColumnId)).ToHaveAttributeAsync("aria-sort", "ascending");
        });

    /// <summary>
    /// A second Shift+click on the lower sort level inverts only that level.
    /// </summary>
    [Fact]
    public async Task Shift_click_should_invert_only_the_lower_sort_level() =>
        await Run(async dialog =>
        {
            // Arrange
            await dialog.HeaderContent(DataTypeColumnId).ClickAsync();
            await dialog.HeaderContent(ElementColumnId).ClickAsync(new() { Modifiers = [KeyboardModifier.Shift] });
            await Expect(dialog.Header(ElementColumnId)).ToHaveAttributeAsync("aria-sort", "ascending");

            // Act
            await dialog.HeaderContent(ElementColumnId).ClickAsync(new() { Modifiers = [KeyboardModifier.Shift] });

            // Assert
            await Expect(dialog.Header(ElementColumnId)).ToHaveAttributeAsync("aria-sort", "descending");
            await Expect(dialog.Header(DataTypeColumnId)).ToHaveAttributeAsync("aria-sort", "ascending");
            // Data type still wins over the lower level.
            await Expect(dialog.Cells(DataTypeColumnId)).ToHaveTextAsync(["Boolean", "Boolean", "Boolean", "Boolean", "Double"]);
        });

    /// <summary>
    /// Shift+Enter on a focused header does the same as Shift+click: it adds that column as a lower sort level.
    /// </summary>
    [Fact]
    public async Task Shift_enter_on_a_header_should_add_a_lower_sort_level() =>
        await Run(async dialog =>
        {
            // Arrange
            await dialog.HeaderContent(DataTypeColumnId).ClickAsync();
            await Expect(dialog.Header(ElementColumnId)).ToHaveAttributeAsync("aria-sort", "none");
            await dialog.Header(ElementColumnId).FocusAsync();

            // Act
            await dialog.Page.Keyboard.PressAsync("Shift+Enter");

            // Assert
            await Expect(dialog.Header(ElementColumnId)).ToHaveAttributeAsync("aria-sort", "ascending");
            await Expect(dialog.Header(DataTypeColumnId)).ToHaveAttributeAsync("aria-sort", "ascending");
        });

    /// <summary>
    /// Space on the focused row selects it, as the test protocol expects. Skipped: since row clicks are off
    /// (<c>RowClickSelectionEnabled="false"</c>), Space no longer selects.
    /// </summary>
    [Fact(Skip = "The protocol expects Space to select the lead row. Since RowClickSelectionEnabled=\"false\" it does not; awaiting the owner's decision.")]
    public async Task Space_should_select_the_lead_row() =>
        await Run(async dialog =>
        {
            // Arrange
            await dialog.Cell(0, ElementColumnId).ClickAsync();
            await dialog.Page.Keyboard.PressAsync("ArrowDown");
            await dialog.Page.Keyboard.PressAsync("ArrowDown");
            await Expect(dialog.Cell(2, ElementColumnId)).ToBeFocusedAsync();

            // Act
            await dialog.Page.Keyboard.PressAsync("Space");

            // Assert
            await dialog.ExpectSelectedCount(1);
            await Expect(dialog.CheckBox(2).Locator("input")).ToBeCheckedAsync();
        });

    private static async Task<LocatorBoundingBoxResult> Box(ILocator locator) =>
        await locator.BoundingBoxAsync() ?? throw new InvalidOperationException($"{locator} is not visible.");

    /// <summary>
    /// Checks that the rows hold the inputs and the outputs of the design as two blocks, in the given order.
    /// </summary>
    private static async Task ExpectConnectorOrder(ConnectorSelectionDialogPage dialog, bool inputsFirst)
    {
        var names = await dialog.Cells(ConnectorColumnId).AllInnerTextsAsync();
        var (first, second) = inputsFirst ? (s_inputConnectors, s_outputConnectors) : (s_outputConnectors, s_inputConnectors);

        Assert.Equal(first, names.Take(first.Length).Order(StringComparer.Ordinal));
        Assert.Equal(second, names.Skip(first.Length).Order(StringComparer.Ordinal));
    }

    private static async Task ExpectInitialSort(ConnectorSelectionDialogPage dialog)
    {
        await Expect(dialog.Header(ElementColumnId)).ToHaveAttributeAsync("aria-sort", "ascending");
        await Expect(dialog.Header(ConnectorColumnId)).ToHaveAttributeAsync("aria-sort", "ascending");
        await Expect(dialog.Header(DataTypeColumnId)).ToHaveAttributeAsync("aria-sort", "none");
        await Expect(dialog.Header(DescriptionColumnId)).ToHaveAttributeAsync("aria-sort", "none");
        await ExpectConnectorOrder(dialog, inputsFirst: true);
    }

    private Task Run(Func<ConnectorSelectionDialogPage, Task> test, [CallerMemberName] string testName = "") =>
        new Browser().LaunchAsync(
            async page => await test(await Open(page, fixture.ServerAddress)),
            testName);
}
