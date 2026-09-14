using System;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Ui.ClusterEditor.Components.TreeNodes;
using ViciOne.Ui.ClusterEditor.Localization;
using ViciOne.Ui.ClusterEditor.Sections.Topology.Components;
using ViciOne.Ui.ClusterEditor.Sections.Topology.Models;
using ViciOne.Ui.ClusterEditor.Sections.Topology.Services;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Tests.TestHelpers;
using ViciOne.Ui.TreeEditor.Builder.Interface.Enums;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeActions;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeActions.Arguments;
using ViciOne.Ui.TreeEditor.Builder.Interface.Nodes;
using Xunit;
using TechnicalTerms = ViciOne.Ui.ClusterEditor.Localization.Resources.TechnicalTerms;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.Topology.Services;

public sealed class TopologyTreeAdapterTests : IDisposable
{
    private readonly TopologyTreeAdapter _adapter;
    private readonly IClusterBuilder _clusterBuilder;
    private readonly ClusterBuilderEventBuffer _eventBuffer = new();
    private readonly TreeEditor.Builder.TreeBuilder _treeBuilder;

    public TopologyTreeAdapterTests()
    {
        _adapter = new TopologyTreeAdapter(_eventBuffer, Substitute.For<IClusterEditorManagementInternal>());

        _clusterBuilder = BuilderFactory.Create();
        _clusterBuilder.Editors.Cluster.AddNodeGroup();

        _treeBuilder = new TreeEditor.Builder.TreeBuilder();
        _treeBuilder.SetAdapter(_adapter);
        _adapter.BuildClusterTree(_clusterBuilder);
    }

    public void Dispose()
    {
        _adapter.Dispose();
        _clusterBuilder.Dispose();
        _eventBuffer.Dispose();
        _treeBuilder.Dispose();
    }

    private IEnumerable<TopologyTreeViewModel> GetAllNodes()
    {
        return _adapter.GetRootNodes().Cast<TopologyTreeViewModel>().SelectMany(Flatten);

        static IEnumerable<TopologyTreeViewModel> Flatten(TopologyTreeViewModel node)
            => node.Children.SelectMany(Flatten).Prepend(node);
    }

    [Fact]
    public void TemplateMapping_ReturnsExpectedTemplateTypes()
    {
        // Arrange
        var node = GetAllNodes().First();
        var mapping = _treeBuilder.Template.Mapping!;

        // Act & Assert
        Assert.Equal(typeof(CustomTooltipTreeNode), mapping(node, TemplateType.Node));
        Assert.Null(mapping(node, TemplateType.NodeDisplay));

        node.IsEditModeActive = true;
        Assert.Equal(typeof(TopologyNodeEditTemplate), mapping(node, TemplateType.NodeDisplay));
        Assert.Null(mapping(Substitute.For<ITreeNode>(), TemplateType.NodeDisplay));
        Assert.Null(mapping(node, (TemplateType)(-1)));
    }

    [Fact]
    public void EditAction_ShowsEditTemplateForEveryNode()
    {
        // Arrange
        var mapping = _treeBuilder.Template.Mapping!;
        var editDescription = CompositeFormats.EditSomething(TechnicalTerms.Node);
        var nodes = GetAllNodes().ToList();
        Assert.NotEmpty(nodes);

        foreach (var node in nodes)
        {
            var editButton = _adapter.GetActions(node).OfType<NodeButton>().Single(b => b.Description == editDescription);

            // Act
            editButton.Action(editButton, new VisibleActionArguments { Builder = _treeBuilder, Node = node });

            // Assert
            Assert.True(node.IsEditModeActive);
            Assert.Equal(typeof(TopologyNodeEditTemplate), mapping(node, TemplateType.NodeDisplay));

            node.IsEditModeActive = false;
        }
    }
}
