using ViciOne.Ui.ClusterEditor.Sections.File.Components;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.File.Components;

public class FileSectionTests
{
    [Fact]
    public void Component_should_render()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.SetupClusterEditorManagement();

        // Act
        var component = ctx.RenderComponent<FileSection>();

        // Assert
        Assert.NotNull(component);
    }
}
