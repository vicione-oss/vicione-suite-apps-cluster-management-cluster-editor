using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Components;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Components;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.ClusterEditor.Tests.Sections.DataPorts.Services;
using ViciOne.Ui.TreeEditor.Builder.Interface;
using ViciOne.Ui.TreeEditor.Components;
using Xunit;
using CommonVocabulary = ViciOne.Ui.Localization.Resources.CommonVocabulary;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.DataPorts.Components;

public sealed class DataPortEditNodeTemplateTests : IAsyncDisposable
{
    private readonly BunitContext _ctx = new();
    private readonly IDatastore _datastore = Substitute.For<IDatastore>();
    private readonly TreeEditor.Builder.TreeBuilder _treeBuilder = new();

    public DataPortEditNodeTemplateTests()
    {
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.ComponentFactories.AddStub<DefaultNodeDisplay>();
        _ctx.ComponentFactories.AddStub<PropertyGrid<DataPortChildNodeEditContext>>();

        var rulesetProvider = Substitute.For<IRulesetProvider>();
        _ctx.Services.AddSingleton(_datastore);
        _ctx.Services.AddSingleton(rulesetProvider);
        _ctx.Services.AddSingleton(Substitute.For<IClusterEditorManagementInternal>());
        _ctx.Services.AddSingleton(Substitute.For<ILogger<DataPortTreeMutator>>());
        _ctx.Services.AddSingleton<ILogger<DataPortTreeBuilderRegistry>>(new FakeLogger<DataPortTreeBuilderRegistry>());
        _ctx.Services.AddScoped<DataPortTreeState>();
        _ctx.Services.AddScoped<DataPortTreeBuilderRegistry>();
        _ctx.Services.AddScoped<DataPortTreeMutator>();
        _ctx.Services.AddScoped<DataPortEditingCoordinator>();
        _ctx.Services.AddScoped<NumericPropertyDescriptorBuilderProvider>();
        _ctx.Services.AddDataPortPropertyGrid();
    }

    public async ValueTask DisposeAsync()
    {
        await _ctx.DisposeAsync();
        _treeBuilder.Dispose();
        await _datastore.DisposeAsync();
    }

    // Creates a DataPort node in edit mode whose pending direction change (Out -> In) the cluster accepts or rejects.
    private DataPortChildNodeModel CreateDataPortNodeWithPendingDirectionChange(bool canSetDirection)
    {
        var root = new DataPortRootNodeModel
        {
            Builder = new Tree.Builder.TreeBuilder(Resources.TestResources.MqttRuleset),
            Name = "Root",
        };
        var node = DataPortNodeModelCreator.CreateDataPortChildNodeModel(parent: root, rootNode: root, dataPortDirection: DataPortDirection.Out);
        node.IsEditModeActive = true;

        var dataPort = new DataPort { Direction = DataPortDirection.Out, Id = node.Id.Value, Name = node.Name };
        _datastore.HasBuilder.Returns(true);
        _datastore.Builder.Cache.DataPortIds.Returns(new Dictionary<Guid, DataPort> { [dataPort.Id] = dataPort });
        _datastore.Builder.Editors.DataPort.CanSetDirection(dataPort, DataPortDirection.In).Returns(canSetDirection);

        return node;
    }

    // Renders the edit template for the node, clicks Save and returns the nodes the template confirmed.
    private async Task<List<DataPortNodeModel>> RenderAndClickSaveAsync(DataPortChildNodeModel node)
    {
        var adapter = Substitute.For<TreeAdapter>();
        adapter.GetRootNodes().Returns([node]);
        _treeBuilder.SetAdapter(adapter);
        var displayNode = Assert.Single(_treeBuilder.DisplayedNodes);

        var confirmedNodes = new List<DataPortNodeModel>();
        var editTemplateContext = new DataPortEditTemplateContext();
        editTemplateContext.Confirm += confirmedNodes.Add;

        var component = _ctx.Render<DataPortEditNodeTemplate>(parameters => parameters
            .AddCascadingValue(displayNode)
            .AddCascadingValue(editTemplateContext));
        component.WaitForElement("button");

        // Set after rendering, because the property grid fills the value store when it receives the node.
        _ctx.Services.GetRequiredService<DataPortChildNodePropertyValueStore>().Set(nameof(DataPort.Direction), DataPortDirection.In);

        var saveButton = component.FindAll("button").Single(button => button.TextContent.Contains(CommonVocabulary.Save, StringComparison.Ordinal));
        await saveButton.ClickAsync(new());

        return confirmedNodes;
    }

    [Fact]
    public async Task SaveClick_WhenTheChangesAreAccepted_Confirms()
    {
        // Arrange
        var node = CreateDataPortNodeWithPendingDirectionChange(canSetDirection: true);

        // Act
        var confirmedNodes = await RenderAndClickSaveAsync(node);

        // Assert
        var confirmedNode = Assert.Single(confirmedNodes);
        Assert.Same(node, confirmedNode);
    }

    [Fact]
    public async Task SaveClick_WhenTheChangesAreRejected_DoesNotConfirm()
    {
        // Arrange
        var node = CreateDataPortNodeWithPendingDirectionChange(canSetDirection: false);

        // Act
        var confirmedNodes = await RenderAndClickSaveAsync(node);

        // Assert
        Assert.Empty(confirmedNodes);
    }
}
