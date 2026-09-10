using System.Threading.Tasks;
using Bunit;
using ViciOne.Ui.ClusterEditor.Components;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components;

public class LabelEditorTests
{
    [Fact]
    public async Task Component_should_render()
    {
        // Arrange
        await using var ctx = new BunitContext();
        ctx.SetupLabelEditor();

        // Act
        var component = ctx.Render<LabelEditor>();

        // Assert
        Assert.NotNull(component);
    }
}
