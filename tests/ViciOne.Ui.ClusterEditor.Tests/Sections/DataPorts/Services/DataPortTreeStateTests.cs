using System;
using System.Linq;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeIdentifier;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.DataPorts.Services;

public sealed class DataPortTreeStateTests
{
    private readonly DataPortTreeState _state = new();

    private static DataPortChildNodeModel CreateChild(DataPortRootNodeModel root, DataPortNodeModel parent, Guid? id = null)
        => new()
        {
            Id = id is null ? GuidNodeIdentifier.New() : new GuidNodeIdentifier(id.Value),
            LinkDirections = [],
            Name = "Child",
            Parent = parent,
            Properties = [],
            RootNode = root,
            TransferDirections = [],
        };

    private static DataPortRootNodeModel CreateRoot()
        => new()
        {
            Builder = new Tree.Builder.TreeBuilder(Resources.TestResources.MqttRuleset),
            Name = "Root",
        };

    [Fact]
    public void AddRootNode_AddsNodeToRootNodes()
    {
        // Arrange
        var root = CreateRoot();

        // Act
        _state.AddRootNode(root);

        // Assert
        Assert.Single(_state.RootNodes);
        Assert.Same(root, _state.RootNodes[0]);
    }

    [Fact]
    public void ClearRootNodes_RemovesAllRootNodes()
    {
        // Arrange
        _state.AddRootNode(CreateRoot());
        _state.AddRootNode(CreateRoot());

        // Act
        _state.ClearRootNodes();

        // Assert
        Assert.Empty(_state.RootNodes);
    }

    [Fact]
    public void FindNode_WhenNodeExists_ReturnsChildNode()
    {
        // Arrange
        var root = CreateRoot();
        var id = Guid.NewGuid();
        var child = CreateChild(root, root, id);
        root.Children.Add(child);
        _state.AddRootNode(root);

        // Act
        var result = _state.FindNode(id);

        // Assert
        Assert.Same(child, result);
    }

    [Fact]
    public void FindNode_WhenNodeMissing_ReturnsNull()
    {
        // Arrange
        _state.AddRootNode(CreateRoot());

        // Act
        var result = _state.FindNode(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FindRootNode_ReturnsMatchingRootNode()
    {
        // Arrange
        var root = CreateRoot();
        _state.AddRootNode(root);

        // Act
        var result = _state.FindRootNode(r => r.Name == "Root");

        // Assert
        Assert.Same(root, result);
    }

    [Fact]
    public void FindTreeNode_WhenIdMatchesDescendant_ReturnsNode()
    {
        // Arrange
        var root = CreateRoot();
        var id = Guid.NewGuid();
        var child = CreateChild(root, root, id);
        root.Children.Add(child);

        // Act
        var result = DataPortTreeState.FindTreeNode(root, new GuidNodeIdentifier(id));

        // Assert
        Assert.Same(child, result);
    }

    [Fact]
    public void FindTreeNode_WhenIdNotFound_ReturnsNull()
    {
        // Arrange
        var root = CreateRoot();

        // Act
        var result = DataPortTreeState.FindTreeNode(root, GuidNodeIdentifier.New());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetDataPortRootNode_WhenRulesetRootIdMatches_ReturnsRoot()
    {
        // Arrange
        var root = CreateRoot();
        _state.AddRootNode(root);
        var rulesetRootId = root.Builder.Ruleset.Root!.Id;

        // Act
        var result = _state.GetDataPortRootNode(rulesetRootId);

        // Assert
        Assert.Same(root, result);
    }

    [Fact]
    public void GetSelectedNodes_ReturnsSelectedRootAndChildNodes()
    {
        // Arrange
        // Build a multi-level tree to fully exercise the recursive descent:
        //
        // root (selected)
        //  ├─ selectedChild (selected)
        //  │   └─ level2Unselected (unselected)
        //  │       └─ level3Selected (selected)      <- deep selected under unselected parent
        //  ├─ unselectedChild (unselected)
        //  └─ level1Unselected (unselected)
        //      └─ level2Selected (selected)
        //          └─ level3Unselected (unselected)
        var root = CreateRoot();
        root.Selected = true;

        var selectedChild = CreateChild(root, root);
        selectedChild.Selected = true;
        var unselectedChild = CreateChild(root, root);
        unselectedChild.Selected = false;

        var level2Unselected = CreateChild(root, selectedChild);
        level2Unselected.Selected = false;
        var level3Selected = CreateChild(root, level2Unselected);
        level3Selected.Selected = true;

        var level1Unselected = CreateChild(root, root);
        level1Unselected.Selected = false;
        var level2Selected = CreateChild(root, level1Unselected);
        level2Selected.Selected = true;
        var level3Unselected = CreateChild(root, level2Selected);
        level3Unselected.Selected = false;

        level2Unselected.Children.Add(level3Selected);
        selectedChild.Children.Add(level2Unselected);
        level2Selected.Children.Add(level3Unselected);
        level1Unselected.Children.Add(level2Selected);
        root.Children.Add(selectedChild);
        root.Children.Add(unselectedChild);
        root.Children.Add(level1Unselected);
        _state.AddRootNode(root);

        // Act
        var result = _state.GetSelectedNodes().ToList();

        // Assert
        Assert.Contains(root, result);
        Assert.Contains(selectedChild, result);
        Assert.Contains(level2Selected, result);
        Assert.Contains(level3Selected, result);
        Assert.DoesNotContain(unselectedChild, result);
        Assert.DoesNotContain(level1Unselected, result);
        Assert.DoesNotContain(level2Unselected, result);
        Assert.DoesNotContain(level3Unselected, result);
        Assert.Equal(4, result.Count);
    }

    [Fact]
    public void GetTreeNode_WhenNodeExists_ReturnsNode()
    {
        // Arrange
        var root = CreateRoot();
        var id = Guid.NewGuid();
        var child = CreateChild(root, root, id);
        root.Children.Add(child);
        _state.AddRootNode(root);
        var dataPortTreeNode = new DataPortTreeNode { Id = id };

        // Act
        var result = _state.GetTreeNode(dataPortTreeNode);

        // Assert
        Assert.Same(child, result);
    }

    [Fact]
    public void RemoveRootNode_WhenNodeExists_RemovesAndReturnsTrue()
    {
        // Arrange
        var root = CreateRoot();
        _state.AddRootNode(root);

        // Act
        var removed = _state.RemoveRootNode(root);

        // Assert
        Assert.True(removed);
        Assert.Empty(_state.RootNodes);
    }
}
