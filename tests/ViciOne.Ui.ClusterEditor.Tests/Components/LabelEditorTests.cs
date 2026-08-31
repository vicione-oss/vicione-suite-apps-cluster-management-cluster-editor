using ViciOne.Ui.ClusterEditor.Components;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components;

public class LabelEditorTests
{
    [Fact]
    public void Component_should_render()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.SetupLabelEditor();

        // Act
        var component = ctx.RenderComponent<LabelEditor>();

        // Assert
        Assert.NotNull(component);
    }
}
