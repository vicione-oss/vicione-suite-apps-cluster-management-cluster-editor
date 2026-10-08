using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Services.ComponentServices;

public sealed class DiagramServiceTests
{
    private static DiagramService CreateSut()
        => new(
            Substitute.For<IDatastore>(),
            new DiagramEventService(Substitute.For<ILogger<DiagramEventService>>()),
            Substitute.For<ILogger<DiagramService>>());

    [Fact]
    public void RequestSimplifiedViewChange_WithoutDiagram_DoesNotThrow()
    {
        // Arrange
        using var sut = CreateSut();

        // Act
        var act = () => sut.RequestSimplifiedViewChange(true);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RequestSimplifiedViewChange_WithoutDiagram_UpdatesDiagramState()
    {
        // Arrange
        using var sut = CreateSut();

        // Act
        sut.RequestSimplifiedViewChange(true);

        // Assert
        sut.DiagramState.SimplifiedView.Should().BeTrue();
    }
}
