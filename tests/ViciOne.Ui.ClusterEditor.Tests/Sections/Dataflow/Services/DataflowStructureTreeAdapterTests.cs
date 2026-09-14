using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using ViciOne.Ui.ClusterEditor.Components.TreeNodes;
using ViciOne.Ui.ClusterEditor.Sections.Dataflow.Components;
using ViciOne.Ui.ClusterEditor.Sections.Dataflow.Models;
using ViciOne.Ui.ClusterEditor.Sections.Dataflow.Services;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using ViciOne.Ui.Localization.Resources;
using ViciOne.Ui.TreeEditor.Builder.Interface.Enums;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeActions;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeActions.Arguments;
using ViciOne.Ui.TreeEditor.Builder.Interface.Nodes;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.Dataflow.Services;

public sealed class DataflowStructureTreeAdapterTests
{
    [Fact]
    public async Task TemplateMapping_ReturnsExpectedTemplateTypes()
    {
        // Arrange
        await using var ctx = new BunitContext();
        ctx.SetupDataflowStructureTreeAdapter();
        ctx.CreateDiagramInstance();

        using var treeBuilder = new TreeEditor.Builder.TreeBuilder();
        treeBuilder.SetAdapter(ctx.Services.GetRequiredService<DataflowStructureTreeAdapter>());

        var node = new DataflowStructureTreeNode { Dataflow = new Cluster.Model.Dataflow() };
        var mapping = treeBuilder.Template.Mapping!;

        // Act & Assert
        Assert.Equal(typeof(CustomTooltipTreeNode), mapping(node, TemplateType.Node));
        Assert.Null(mapping(node, TemplateType.NodeDisplay));

        node.Editing = true;
        Assert.Equal(typeof(StructureTreeDataflowEditNode), mapping(node, TemplateType.NodeDisplay));
        Assert.Null(mapping(Substitute.For<ITreeNode>(), TemplateType.NodeDisplay));
        Assert.Null(mapping(node, (TemplateType)(-1)));
    }

    [Fact]
    public async Task RenameAction_ShowsEditTemplate()
    {
        // Arrange
        await using var ctx = new BunitContext();
        ctx.SetupDataflowStructureTreeAdapter();
        ctx.CreateDiagramInstance();

        var adapter = ctx.Services.GetRequiredService<DataflowStructureTreeAdapter>();
        using var treeBuilder = new TreeEditor.Builder.TreeBuilder();
        treeBuilder.SetAdapter(adapter);

        var node = new DataflowStructureTreeNode { Dataflow = new Cluster.Model.Dataflow() };
        var renameButton = adapter.GetActions(node).OfType<NodeButton>().Single(b => b.Description == CommonVocabulary.RenameVerb);

        // Act
        renameButton.Action(renameButton, new VisibleActionArguments { Builder = treeBuilder, Node = node });

        // Assert
        Assert.True(node.Editing);
        Assert.Equal(typeof(StructureTreeDataflowEditNode), treeBuilder.Template.Mapping!(node, TemplateType.NodeDisplay));
    }
}
