using System.Threading.Tasks;
using Bunit;
using ViciOne.Ui.ClusterEditor.Sections.Information.Components;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.Information.Components;

public class InformationSectionTests
{
    [Fact]
    public async Task Component_should_render()
    {
        // Arrange
        await using var ctx = new BunitContext();
        ctx.SetupDatastore();
        ctx.SetupStatisticService();

        // Act
        var component = ctx.Render<InformationSection>();

        // Assert
        Assert.NotNull(component);
    }
}
