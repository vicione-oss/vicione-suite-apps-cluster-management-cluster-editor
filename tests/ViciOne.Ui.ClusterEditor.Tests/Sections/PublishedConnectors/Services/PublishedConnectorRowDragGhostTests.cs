using System.Linq;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using ViciOne.Ui.Blazor.Components.Draggable.Abstractions;
using ViciOne.Ui.Blazor.Components.Draggable.Models;
using ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Services;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.PublishedConnectors.Services;

public class PublishedConnectorRowDragGhostTests
{
    [Fact]
    public void GetJsModule_with_a_registered_markup_drag_ghost_returns_that_ghost_module()
    {
        // Arrange
        var expectedModule = new DragGhostJsModuleDescriptor
        {
            CreateFunction = new() { Name = "createDragGhost" },
            ModuleName = "/_content/Test/published-connector-drag-ghost.js"
        };

        var markupDragGhost = Substitute.For<IDragGhost>();
        markupDragGhost.GetJsModule().Returns(expectedModule);

        var logger = new FakeLogger<PublishedConnectorRowDragGhost>();
        var sut = new PublishedConnectorRowDragGhost(logger)
        {
            MarkupDragGhost = markupDragGhost
        };

        // Act
        var module = sut.GetJsModule();

        // Assert
        module.Should().BeSameAs(expectedModule);
        logger.Collector.GetSnapshot().Should().BeEmpty();
    }

    [Fact]
    public void GetJsModule_without_a_registered_markup_drag_ghost_falls_back_to_the_row_clone()
    {
        // Arrange
        var logger = new FakeLogger<PublishedConnectorRowDragGhost>();
        var sut = new PublishedConnectorRowDragGhost(logger);

        // Act
        var module = sut.GetJsModule();

        // Assert
        module.Should().NotBeNull();
        module.ModuleName.Should().Be("/_content/ViciOne.Ui.Blazor.Components/draggable/table-row-drag-ghost.js");
        module.CreateFunction.Name.Should().Be("createDragGhost");

        // The fallback ghost is degraded, so the error log is the only signal that registration broke.
        logger.Collector.GetSnapshot().Count(entry => entry.Level == LogLevel.Error).Should().Be(1);
    }
}
