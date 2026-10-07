using System.Threading.Tasks;
using AwesomeAssertions;
using Bunit;
using ViciOne.Ui.Blazor.Components.TestingHelpers.SearchBox.Extensions;
using ViciOne.Ui.ClusterEditor.Components;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components;

public class SearchAndFilterComponentTests
{
    [Fact]
    public async Task Component_should_render()
    {
        // Arrange
        await using var ctx = new BunitContext();
        ctx.JSInterop.SetupForSearchBox();

        // Act
        var component = ctx.Render<SearchAndFilterComponent>();

        // Assert
        Assert.NotNull(component);
    }

    [Fact]
    public async Task Component_without_a_column_chooser_slot_renders_no_slot()
    {
        // Arrange
        await using var ctx = new BunitContext();
        ctx.JSInterop.SetupForSearchBox();

        // Act
        var component = ctx.Render<SearchAndFilterComponent>();

        // Assert
        component.FindAll(".column-chooser-slot").Should().BeEmpty();
    }

    [Fact]
    public async Task Component_with_a_column_chooser_slot_renders_its_content()
    {
        // Arrange
        await using var ctx = new BunitContext();
        ctx.JSInterop.SetupForSearchBox();

        // Act
        var component = ctx.Render<SearchAndFilterComponent>(parameters => parameters
            .Add(p => p.ColumnChooserSlot, "<span class=\"test-chooser\"></span>"));

        // Assert
        component.FindAll(".column-chooser-slot .test-chooser").Should().ContainSingle();
    }

    // A track count one column short leaves the row overflowing, which the slot rendering at all would not
    // catch.
    [Fact]
    public async Task Component_without_a_column_chooser_slot_reserves_two_system_button_columns()
    {
        // Arrange
        await using var ctx = new BunitContext();
        ctx.JSInterop.SetupForSearchBox();

        // Act
        var component = ctx.Render<SearchAndFilterComponent>();

        // Assert
        SystemButtonColumnCount(component).Should().Be("3");
    }

    [Fact]
    public async Task Component_with_a_column_chooser_slot_reserves_three_system_button_columns()
    {
        // Arrange
        await using var ctx = new BunitContext();
        ctx.JSInterop.SetupForSearchBox();

        // Act
        var component = ctx.Render<SearchAndFilterComponent>(parameters => parameters
            .Add(p => p.ColumnChooserSlot, "<span class=\"test-chooser\"></span>"));

        // Assert
        SystemButtonColumnCount(component).Should().Be("5");
    }

    // Two tracks per button minus the trailing gap, so 2 buttons render 3 tracks and 3 buttons render 5.
    private static string SystemButtonColumnCount(IRenderedComponent<SearchAndFilterComponent> component)
    {
        var style = component.Find(".command-buttons-container").GetAttribute("style");
        style.Should().NotBeNull();

        return style.Split("--system-button-column-count:")[1].Trim().TrimEnd(';');
    }
}
