using AwesomeAssertions;
using NSubstitute;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Models;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Models;

public class DataGridConnectorWrapperTests
{
    // Building a wrapper must not read the cluster. Callers build them in loops — 392 of them for one
    // ConnectorSelectionDialog over every design — and the sections keep instances that outlive the cluster they
    // were built from, so the section's selection survives a reload. Resolving Path in the constructor made
    // construction itself walk the upstream containers, which throws the moment that chain is incomplete.
    // See ../../.workflow/305-status/ClusterEditor/bugs.md -> CB10.
    [Fact]
    public void Building_a_wrapper_reads_nothing_from_the_cluster()
    {
        // Arrange
        var connector = Substitute.For<IConnectorOutput>();

        // Act
        var act = () => new DataGridConnectorWrapper(connector, null!, null!);

        // Assert
        act.Should().NotThrow();
    }

    // RefreshPath is how a dataflow, container or function-block rename reaches the Path column: the sections
    // never rebuild an existing wrapper, because the wrapper instances *are* the table's selection. It must
    // therefore stay callable on a wrapper whose path has never been read, and drop whatever was cached.
    [Fact]
    public void RefreshPath_is_safe_on_a_wrapper_whose_path_was_never_read()
    {
        // Arrange
        var wrapper = new DataGridConnectorWrapper(Substitute.For<IConnectorOutput>(), null!, null!);

        // Act
        var act = () => wrapper.RefreshPath("Dataflow");

        // Assert
        act.Should().NotThrow();
    }
}
