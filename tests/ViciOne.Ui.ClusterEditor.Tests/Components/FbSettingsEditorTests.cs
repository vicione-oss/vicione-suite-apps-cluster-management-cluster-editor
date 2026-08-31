using ViciOne.Ui.ClusterEditor.Components.FbSettingsEditor;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components;

public class FbSettingsEditorTests
{
    [Fact]
    public void Component_should_render()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.SetupFbSettingsEditor();

        ctx.CreateDiagramInstance();

        // Act
        var component = ctx.RenderComponent<FbSettingsEditor>();

        // Assert
        Assert.NotNull(component);
    }
}
