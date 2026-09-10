using System.Threading.Tasks;
using Bunit;
using ViciOne.Ui.ClusterEditor.Sections.File.Components;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.File.Components;

public class FileSectionTests
{
    [Fact]
    public async Task Component_should_render()
    {
        // Arrange
        await using var ctx = new BunitContext();
        ctx.SetupClusterEditorManagement();

        // Act
        var component = ctx.Render<FileSection>();

        // Assert
        Assert.NotNull(component);
    }
}
