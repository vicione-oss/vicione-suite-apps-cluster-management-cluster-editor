using System.Linq;
using AwesomeAssertions;
using NSubstitute;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Components.FbSettingsEditor.Models;
using ViciOne.Ui.ClusterEditor.Models;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components.FbSettingsEditor;

public class FbSettingSearchFilterTests
{
    [Fact]
    public void Matches_on_the_setting_name_the_row_is_grouped_by()
    {
        // Arrange
        var filter = new FbSettingSearchFilter("spe");
        var row = Row("Speed", "10");

        // Act
        var matches = filter.Matches(row);

        // Assert
        matches.Should().BeTrue();
    }

    // A row is one setting across every selected function block, so a hit in any block's value keeps it.
    [Fact]
    public void Matches_on_a_value_held_by_any_of_the_rows_function_blocks()
    {
        // Arrange
        var filter = new FbSettingSearchFilter("42");
        var row = Row("Speed", "10", "42");

        // Act
        var matches = filter.Matches(row);

        // Assert
        matches.Should().BeTrue();
    }

    [Fact]
    public void Matches_nothing_when_neither_the_name_nor_any_value_contains_the_text()
    {
        // Arrange
        var filter = new FbSettingSearchFilter("torque");
        var row = Row("Speed", "10");

        // Act
        var matches = filter.Matches(row);

        // Assert
        matches.Should().BeFalse();
    }

    [Fact]
    public void Matches_ignores_case()
    {
        // Arrange
        var filter = new FbSettingSearchFilter("SPEED");
        var row = Row("Speed", "10");

        // Act
        var matches = filter.Matches(row);

        // Assert
        matches.Should().BeTrue();
    }

    // The filter reads only the group key and each setting's value; FbName is never touched.
    private static IGrouping<string, FbSetting> Row(string name, params string[] values)
        => values
            .Select(value => new FbSetting(value, null!, Substitute.For<Setting>(), typeof(string)))
            .GroupBy(_ => name)
            .Single();
}
