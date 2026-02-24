using ViciOne.Ui.ClusterEditor.Sections.Information.Components;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.Information.Components;

public class InformationSectionTests
{
    [Fact]
    public void Component_should_render()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.SetupDatastore();
        ctx.SetupStatisticService();

        // Act
        var component = ctx.RenderComponent<InformationSection>();

        // Assert
        Assert.NotNull(component);
    }
}
