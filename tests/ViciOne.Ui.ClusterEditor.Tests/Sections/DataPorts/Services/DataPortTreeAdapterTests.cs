using System;
using System.Linq;
using System.Threading.Tasks;
using Blazor.Diagrams;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.TreeBuilder.NodeTypes;
using ViciOne.Ui.Blazor.Components.ContextMenu.Services;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.ContextMenu;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.TreeEditor.Builder.Interface.Icons;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.DataPorts.Services;

public sealed class DataPortTreeAdapterTests : IAsyncDisposable
{
    private readonly DataPortTreeAdapter _adapter;
    private readonly ClusterBuilder _builder;
    private readonly IDatastore _datastore;
    private readonly DiagramService _diagramService;
    private readonly DragService _dragService;
    private readonly ClusterBuilderEventBuffer _eventBuffer;
    private readonly IContextMenuRequest<DataPortAddChildNodeContextMenuContext> _mockContextMenuRequest;
    private readonly IClusterEditorManagementInternal _mockDataManagementService;
    private readonly IDataPortTreeIconProvider _mockIconProvider;
    private readonly ILogger<DataPortTreeAdapter> _mockLogger;
    private readonly IRulesetProvider _mockRulesetProvider;

    public DataPortTreeAdapterTests()
    {
        _mockContextMenuRequest = Substitute.For<IContextMenuRequest<DataPortAddChildNodeContextMenuContext>>();
        _eventBuffer = new();
        _datastore = Substitute.For<IDatastore>();
        _diagramService = new(_datastore, new())
        {
            Diagram = new BlazorDiagram()
        };
        _dragService = new(new(), _diagramService, new());
        _mockRulesetProvider = Substitute.For<IRulesetProvider>();
        _mockLogger = Substitute.For<ILogger<DataPortTreeAdapter>>();
        _mockDataManagementService = Substitute.For<IClusterEditorManagementInternal>();
        _mockIconProvider = Substitute.For<IDataPortTreeIconProvider>();

        _adapter = new DataPortTreeAdapter(
            _mockContextMenuRequest,
            _eventBuffer,
            _datastore,
            _dragService,
            _mockRulesetProvider,
            _mockLogger,
            _mockDataManagementService,
            _mockIconProvider);

        var dependencyResolver = Substitute.For<IDependencyResolver>();
        _builder = new ClusterBuilder(dependencyResolver);

        _datastore.Builder.Returns(_builder);
    }

    public async ValueTask DisposeAsync()
    {
        _adapter.Dispose();
        _builder.Dispose();
        _eventBuffer.Dispose();
        _diagramService.Dispose();
        await _datastore.DisposeAsync();
    }

    [Fact]
    public void GetIcons_CalledMultipleTimesWithSameNode_ReturnsConsistentResults()
    {
        // Arrange
        var iconName = "test-icon";
        var iconMarkup = "<svg>test</svg>";
        var rootNode = DataPortNodeModelCreator.CreateDataPortRootNodeModel(iconName);

        _mockIconProvider.GetIconMarkupString(iconName).Returns(iconMarkup);

        // Act
        var result1 = _adapter.GetIcons(rootNode).ToList();
        var result2 = _adapter.GetIcons(rootNode).ToList();

        // Assert
        Assert.Single(result1);
        Assert.Equal(iconMarkup, ((SvgIcon)result1[0]).MarkupString);
        Assert.Single(result2);
        Assert.Equal(iconMarkup, ((SvgIcon)result2[0]).MarkupString);
        _mockIconProvider.Received(2).GetIconMarkupString(iconName);
    }

    [Fact]
    public void GetIcons_WithDataPortChildNodeModel_CustomIcon_EmptyMarkup_ReturnsEmpty()
    {
        // Arrange
        var customIcon = "custom-icon";
        var childNode = DataPortNodeModelCreator.CreateDataPortChildNodeModel(
            icon: customIcon,
            nodeReference: new NodeReference { Id = "test-id" });

        _mockIconProvider.GetIconMarkupString(customIcon).Returns(string.Empty);

        // Act
        var result = _adapter.GetIcons(childNode).ToList();

        // Assert
        Assert.Empty(result);
        _mockIconProvider.Received(1).GetIconMarkupString(customIcon);
    }

    [Fact]
    public void GetIcons_WithDataPortChildNodeModel_CustomIcon_NullMarkup_ReturnsEmpty()
    {
        // Arrange
        var customIcon = "custom-icon";
        var childNode = DataPortNodeModelCreator.CreateDataPortChildNodeModel(
            icon: customIcon,
            nodeReference: new NodeReference { Id = "test-id" });

        _mockIconProvider.GetIconMarkupString(customIcon).Returns((string?)null);

        // Act
        var result = _adapter.GetIcons(childNode).ToList();

        // Assert
        Assert.Empty(result);
        _mockIconProvider.Received(1).GetIconMarkupString(customIcon);
    }

    [Fact]
    public void GetIcons_WithDataPortChildNodeModel_CustomIcon_ValidMarkup_ReturnsSvgIcon()
    {
        // Arrange
        var customIcon = "custom-icon";
        var iconMarkup = "<svg>custom</svg>";
        var childNode = DataPortNodeModelCreator.CreateDataPortChildNodeModel(
            icon: customIcon,
            nodeReference: new NodeReference { Id = "test-id" });

        _mockIconProvider.GetIconMarkupString(customIcon).Returns(iconMarkup);

        // Act
        var result = _adapter.GetIcons(childNode).ToList();

        // Assert
        Assert.Single(result);
        Assert.IsType<SvgIcon>(result[0]);
        _mockIconProvider.Received(1).GetIconMarkupString(customIcon);
    }

    [Theory]
    [InlineData("datapoint")]
    [InlineData("DATAPOINT")]
    [InlineData("DataPoint")]
    [InlineData("DaTaPoInT")]
    public async Task GetIcons_WithDataPortChildNodeModel_DataPointIcon_CaseInsensitive_CallsGetDataPointIcon(string iconValue)
    {
        // Arrange
        var childNode = DataPortNodeModelCreator.CreateDataPortChildNodeModel(
            icon: iconValue,
            nodeReference: new NodeReference { Id = "test-id" });

        _mockIconProvider.GetDataPointIcon(childNode, 24, Arg.Any<IClusterCache>()).Returns((IIcon?)null);

        // Act
        var result = _adapter.GetIcons(childNode).ToList();

        // Assert
        Assert.Empty(result);
        _mockIconProvider.Received(1).GetDataPointIcon(childNode, 24, Arg.Any<IClusterCache>());
    }

    [Fact]
    public async Task GetIcons_WithDataPortChildNodeModel_DataPointIcon_ReturnsDataPointIcon()
    {
        // Arrange

        var childNode = DataPortNodeModelCreator.CreateDataPortChildNodeModel(
            icon: "datapoint",
            nodeReference: new NodeReference { Id = "test-id" });

        var expectedIcon = new SvgIcon("<svg>datapoint</svg>");
        _mockIconProvider.GetDataPointIcon(childNode, 24, Arg.Any<IClusterCache>()).Returns(expectedIcon);

        // Act
        var result = _adapter.GetIcons(childNode).ToList();

        // Assert
        Assert.Single(result);
        Assert.Same(expectedIcon, result[0]);
        _mockIconProvider.Received(1).GetDataPointIcon(childNode, 24, Arg.Any<IClusterCache>());
    }

    [Fact]
    public void GetIcons_WithDataPortChildNodeModel_NullIcon_NodeTypeIconWithNullMarkup_ReturnsEmpty()
    {
        // Arrange
        var nodeTypeId = "Folder";
        var nodeTypeIconName = "folder";

        var childNode = DataPortNodeModelCreator.CreateDataPortChildNodeModel(
            icon: null,
            nodeReference: new NodeReference { Id = nodeTypeId });

        _mockIconProvider.GetIconMarkupString(nodeTypeIconName).Returns((string?)null);

        // Act
        var result = _adapter.GetIcons(childNode).ToList();

        // Assert
        Assert.Empty(result);
        _mockIconProvider.Received(1).GetIconMarkupString(nodeTypeIconName);
    }

    [Fact]
    public void GetIcons_WithDataPortChildNodeModel_NullIcon_NodeTypeIconWithValidMarkup_ReturnsSvgIcon()
    {
        // Arrange
        var nodeTypeId = "Folder";
        var nodeTypeIconName = "folder";
        var iconMarkup = "<svg>node-type</svg>";


        var childNode = DataPortNodeModelCreator.CreateDataPortChildNodeModel(
            icon: null,
            nodeReference: new NodeReference { Id = nodeTypeId });

        _mockIconProvider.GetIconMarkupString(nodeTypeIconName).Returns(iconMarkup);

        // Act
        var result = _adapter.GetIcons(childNode).ToList();

        // Assert
        Assert.Single(result);
        Assert.IsType<SvgIcon>(result[0]);
        _mockIconProvider.Received(1).GetIconMarkupString(nodeTypeIconName);
    }

    [Fact]
    public void GetIcons_WithDataPortChildNodeModel_NullNodeReference_ReturnsEmpty()
    {
        // Arrange
        var childNode = DataPortNodeModelCreator.CreateDataPortChildNodeModel(
            icon: "child-icon",
            nodeReference: null);

        // Act
        var result = _adapter.GetIcons(childNode).ToList();

        // Assert
        Assert.Empty(result);
        _mockIconProvider.DidNotReceive().GetIconMarkupString(Arg.Any<string>());
        _mockIconProvider.DidNotReceive().GetDataPointIcon(Arg.Any<DataPortChildNodeModel>(), Arg.Any<int>(), Arg.Any<IClusterCache>());
    }

    [Fact]
    public void GetIcons_WithDataPortRootNodeModel_EmptyIcon_AvailableIconsWithEmptyMarkup_ReturnsEmpty()
    {
        // Arrange
        var iconName = "test-icon";
        var rootNode = DataPortNodeModelCreator.CreateDataPortRootNodeModel(string.Empty, [iconName]);

        _mockIconProvider.GetIconMarkupString(iconName).Returns((string?)null);

        // Act
        var result = _adapter.GetIcons(rootNode).ToList();

        // Assert
        Assert.Empty(result);
        _mockIconProvider.Received(1).GetIconMarkupString(iconName);
    }

    [Fact]
    public void GetIcons_WithDataPortRootNodeModel_EmptyIcon_AvailableIconsWithEmptyStringMarkup_ReturnsEmpty()
    {
        // Arrange
        var iconName = "test-icon";
        var rootNode = DataPortNodeModelCreator.CreateDataPortRootNodeModel(string.Empty, [iconName]);

        _mockIconProvider.GetIconMarkupString(iconName).Returns(string.Empty);

        // Act
        var result = _adapter.GetIcons(rootNode).ToList();

        // Assert
        Assert.Empty(result);
        _mockIconProvider.Received(1).GetIconMarkupString(iconName);
    }

    [Fact]
    public void GetIcons_WithDataPortRootNodeModel_EmptyIcon_AvailableIconsWithValidMarkup_ReturnsSvgIcon()
    {
        // Arrange
        var iconName = "test-icon";
        var iconMarkup = "<svg>test</svg>";
        var rootNode = DataPortNodeModelCreator.CreateDataPortRootNodeModel(string.Empty, [iconName]);

        _mockIconProvider.GetIconMarkupString(iconName).Returns(iconMarkup);

        // Act
        var result = _adapter.GetIcons(rootNode).ToList();

        // Assert
        Assert.Single(result);
        Assert.IsType<SvgIcon>(result[0]);
        _mockIconProvider.Received(1).GetIconMarkupString(iconName);
    }

    [Fact]
    public void GetIcons_WithDataPortRootNodeModel_EmptyIcon_NoAvailableIcons_ReturnsEmpty()
    {
        // Arrange
        var rootNode = DataPortNodeModelCreator.CreateDataPortRootNodeModel(string.Empty);

        // Act
        var result = _adapter.GetIcons(rootNode).ToList();

        // Assert
        Assert.Empty(result);
        _mockIconProvider.DidNotReceive().GetIconMarkupString(Arg.Any<string>());
        _mockIconProvider.DidNotReceive().GetDataPointIcon(Arg.Any<DataPortChildNodeModel>(), Arg.Any<int>(), Arg.Any<IClusterCache>());
    }

    [Fact]
    public void GetIcons_WithDataPortRootNodeModel_NullIcon_NoAvailableIcons_ReturnsEmpty()
    {
        // Arrange
        var rootNode = DataPortNodeModelCreator.CreateDataPortRootNodeModel(null);

        // Act
        var result = _adapter.GetIcons(rootNode).ToList();

        // Assert
        Assert.Empty(result);
        _mockIconProvider.DidNotReceive().GetIconMarkupString(Arg.Any<string>());
        _mockIconProvider.DidNotReceive().GetDataPointIcon(Arg.Any<DataPortChildNodeModel>(), Arg.Any<int>(), Arg.Any<IClusterCache>());
    }

    [Fact]
    public void GetIcons_WithDataPortRootNodeModel_WithValidIcon_EmptyMarkup_ReturnsEmpty()
    {
        // Arrange
        var iconName = "test-icon";
        var rootNode = DataPortNodeModelCreator.CreateDataPortRootNodeModel(iconName);

        _mockIconProvider.GetIconMarkupString(iconName).Returns(string.Empty);

        // Act
        var result = _adapter.GetIcons(rootNode).ToList();

        // Assert
        Assert.Empty(result);
        _mockIconProvider.Received(1).GetIconMarkupString(iconName);
    }

    [Fact]
    public void GetIcons_WithDataPortRootNodeModel_WithValidIcon_NullMarkup_ReturnsEmpty()
    {
        // Arrange
        var iconName = "valid-icon";
        var rootNode = DataPortNodeModelCreator.CreateDataPortRootNodeModel(iconName);

        _mockIconProvider.GetIconMarkupString(iconName).Returns((string?)null);

        // Act
        var result = _adapter.GetIcons(rootNode).ToList();

        // Assert
        Assert.Empty(result);
        _mockIconProvider.Received(1).GetIconMarkupString(iconName);
    }

    [Fact]
    public void GetIcons_WithDataPortRootNodeModel_WithValidIcon_ValidMarkup_ReturnsSvgIcon()
    {
        // Arrange
        var iconName = "valid-icon";
        var iconMarkup = "<svg>valid</svg>";
        var rootNode = DataPortNodeModelCreator.CreateDataPortRootNodeModel(iconName);

        _mockIconProvider.GetIconMarkupString(iconName).Returns(iconMarkup);

        // Act
        var result = _adapter.GetIcons(rootNode).ToList();

        // Assert
        Assert.Single(result);
        Assert.IsType<SvgIcon>(result[0]);
        _mockIconProvider.Received(1).GetIconMarkupString(iconName);
    }

    [Fact]
    public void GetIcons_WithDifferentNodes_CallsProviderWithCorrectParameters()
    {
        // Arrange
        var rootIconName = "root-icon";
        var rootIconMarkup = "<svg>root</svg>";
        var childIconName = "child-icon";
        var childIconMarkup = "<svg>child</svg>";

        var rootNode = DataPortNodeModelCreator.CreateDataPortRootNodeModel(rootIconName);
        var childNode = DataPortNodeModelCreator.CreateDataPortChildNodeModel(
            parent: rootNode,
            rootNode: rootNode,
            icon: childIconName,
            nodeReference: new NodeReference { Id = "test-id" });

        _mockIconProvider.GetIconMarkupString(rootIconName).Returns(rootIconMarkup);
        _mockIconProvider.GetIconMarkupString(childIconName).Returns(childIconMarkup);

        // Act
        var rootResult = _adapter.GetIcons(rootNode).ToList();
        var childResult = _adapter.GetIcons(childNode).ToList();

        // Assert
        Assert.Single(rootResult);
        Assert.Single(childResult);
        _mockIconProvider.Received(1).GetIconMarkupString(rootIconName);
        _mockIconProvider.Received(1).GetIconMarkupString(childIconName);
    }

    [Fact]
    public void GetIcons_WithNonDataPortNodeModel_ReturnsEmpty()
    {
        // Arrange
        var mockNode = Substitute.For<TreeEditor.Builder.Interface.Nodes.ITreeNode>();

        // Act
        var result = _adapter.GetIcons(mockNode).ToList();

        // Assert
        Assert.Empty(result);
        _mockIconProvider.DidNotReceive().GetIconMarkupString(Arg.Any<string>());
        _mockIconProvider.DidNotReceive().GetDataPointIcon(Arg.Any<DataPortChildNodeModel>(), Arg.Any<int>(), Arg.Any<IClusterCache>());
    }
}
