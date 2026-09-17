using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Ui.ClusterEditor.Sections.Library.Services;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Services.ClusterEditorManagement;

public sealed class ClusterEditorManagementTests : IAsyncDisposable
{
    private readonly IDatastore _datastore;
    private readonly DiagramService _diagramService;
    private readonly ILibraryService _libraryService;
    private readonly ILogger<ClusterEditor.Services.ClusterEditorManagement> _logger;
    private readonly ClusterEditor.Services.ClusterEditorManagement _sut;

    public ClusterEditorManagementTests()
    {
        _datastore = Substitute.For<IDatastore>();
        _libraryService = Substitute.For<ILibraryService>();
        _logger = Substitute.For<ILogger<ClusterEditor.Services.ClusterEditorManagement>>();
        _diagramService = new DiagramService(
            _datastore,
            new DiagramEventService(Substitute.For<ILogger<DiagramEventService>>()),
            Substitute.For<ILogger<DiagramService>>());
        _sut = new ClusterEditor.Services.ClusterEditorManagement(_datastore, _diagramService, _libraryService, _logger);
    }

    public async ValueTask DisposeAsync()
    {
        await _datastore.DisposeAsync();
        _diagramService.Dispose();
    }

    [Fact]
    public void ActiveContainer_ReturnsActiveContainerOfDatastore()
    {
        // Arrange
        var expectedContainer = Substitute.For<Cluster.Model.Container>();
        _datastore.ActiveContainer.Returns(expectedContainer);

        // Act
        var activeContainer = _sut.ActiveContainer;

        // Assert
        activeContainer.Should().BeSameAs(expectedContainer);
    }

    [Fact]
    public async Task ForceRootContainerReload_CallsLoadContainerWithForceTrue_PassesDiagramService_PassesCancellationToken()
    {
        // Arrange
        SetUpBuilderWithRootContainer(out _, out var expectedRoot);
        var ct = TestContext.Current.CancellationToken;

        // Act
        await _sut.ForceRootContainerReload(ct);

        // Assert
        await _datastore.Received(1).LoadContainer(expectedRoot, _diagramService, ct, true);
    }

    [Fact]
    public async Task ForceRootContainerReload_WithAlreadyCancelledToken_ThrowsCanceledOperationException()
    {
        // Arrange
        SetUpBuilderWithRootContainer(out _, out var expectedRoot);
        using (var cts = new CancellationTokenSource())
        {
            await cts.CancelAsync();

            // Act
            var act = () => _sut.ForceRootContainerReload(cts.Token);

            // Assert
            await act.Should().ThrowAsync<OperationCanceledException>();
            await _datastore.Received(0).LoadContainer(expectedRoot, _diagramService, cts.Token, true);
        }
    }

    [Fact]
    public async Task LoadDataflow_CallsRequiredMethods()
    {
        // Arrange
        var builderSettingsAccessed = false;
        var builder = Substitute.For<IClusterBuilder>();

        // Capture when Settings is accessed (inside InitSettings method)
        builder.Settings.Returns(_ =>
        {
            builderSettingsAccessed = true;
            return new();
        });
        var cache = new ClusterCache();
        builder.Cache.Returns(cache);

        using (var cts = new CancellationTokenSource())
        {
            // Act
            await _sut.LoadDataflow(builder, cts.Token);

            //Assert
            builderSettingsAccessed.Should().BeTrue();
            _libraryService.Received(1).CreateLibraryEntries(Arg.Any<IEnumerable<FunctionBlockDesign>>());
            await _datastore.Received(1).Load(builder, _diagramService, cts.Token);
        }
    }

    [Fact]
    public async Task LoadDataflow_WithAlreadyCancelledToken_ThrowsCanceledOperationException()
    {
        // Arrange
        SetUpBuilderWithRootContainer(out var builder, out var expectedRoot);
        using (var cts = new CancellationTokenSource())
        {
            await cts.CancelAsync();

            // Act
            var act = () => _sut.LoadDataflow(builder, cts.Token);

            // Assert
            await act.Should().ThrowAsync<OperationCanceledException>();
            await _datastore.Received(0).LoadContainer(expectedRoot, _diagramService, cts.Token, true);
        }
        builder.Dispose();
    }

    [Fact]
    public void LoadFunctionBlockDesigns_WithMultipleDesigns_AddsEachDesignToBuilder_CallsCreateLibraryEntries()
    {
        // Arrange
        var designId1 = Guid.NewGuid();
        var designId2 = Guid.NewGuid();
        var builder = Substitute.For<IClusterBuilder>();
        var cache = new ClusterCache();
        builder.Cache.Returns(cache);
        _datastore.Builder.Returns(builder);

        // Act
        _sut.LoadFunctionBlockDesigns([designId1, designId2]);

        // Assert
        builder.Editors.FunctionBlockDesign.Received(1).AddFunctionBlockDesign(designId1);
        builder.Editors.FunctionBlockDesign.Received(1).AddFunctionBlockDesign(designId2);
        _libraryService.Received(1).CreateLibraryEntries(Arg.Any<IEnumerable<FunctionBlockDesign>>());
    }

    [Fact]
    public void PrepareClusterSerialization_CallsSaveViewportWithDiagramService()
    {
        // Act
        _sut.PrepareClusterSerialization();

        // Assert
        _datastore.Received(1).SaveViewport(_diagramService);
    }

    [Fact]
    public async Task ReloadActiveContainer_CallsLoadContainerWithForceTrue_PassesDiagramService_PassesCancellationToken()
    {
        // Arrange
        var expectedContainer = Substitute.For<Cluster.Model.Container>();
        _datastore.ActiveContainer.Returns(expectedContainer);
        var ct = TestContext.Current.CancellationToken;

        // Act
        await _sut.ReloadActiveContainer(ct);

        // Assert
        await _datastore.Received(1).LoadContainer(expectedContainer, _diagramService, ct, true);
    }

    [Fact]
    public async Task ReloadActiveContainer_WithAlreadyCancelledToken_ThrowsCanceledOperationException()
    {
        // Arrange
        var expectedContainer = Substitute.For<Cluster.Model.Container>();
        _datastore.ActiveContainer.Returns(expectedContainer);
        using (var cts = new CancellationTokenSource())
        {
            await cts.CancelAsync();

            // Act
            var act = () => _sut.ReloadActiveContainer(cts.Token);

            // Assert
            await act.Should().ThrowAsync<OperationCanceledException>();
            await _datastore.Received(0).LoadContainer(expectedContainer, _diagramService, cts.Token, true);
        }
    }

    [Fact]
    public async Task RequestLoadFbDesigns_InvokesAllHandlers_DoesNotThrowEvenIfHandlerThrows()
    {
        // Arrange
        var invocationCount = 0;
        _sut.LoadFunctionBlockDesignsRequested += () => { invocationCount++; throw new InvalidOperationException("First handler fails"); };
        _sut.LoadFunctionBlockDesignsRequested += () => { invocationCount++; return Task.CompletedTask; };

        // Act
        var act = () => _sut.RequestLoadFbDesigns();

        // Assert
        await act.Should().NotThrowAsync();
        invocationCount.Should().Be(2);
    }

    [Fact]
    public async Task RequestLoadFbDesigns_WithNoHandlers_DoesNotThrow()
    {
        // Act
        var act = () => _sut.RequestLoadFbDesigns();

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RequestSave_InvokesAllHandlers_DoesNotThrowEvenIfHandlerThrows_InvokesHandlerWithCurrentBuilder()
    {
        // Arrange
        var mockBuilder = Substitute.For<IClusterBuilder>();
        _datastore.Builder.Returns(mockBuilder);

        var invocationCount = 0;
        IClusterBuilder? receivedBuilder = null;
        _sut.SaveRequested += _ => { invocationCount++; throw new InvalidOperationException("First handler fails"); };
        _sut.SaveRequested += builder => { receivedBuilder = builder; invocationCount++; return Task.CompletedTask; }; ;

        // Act
        var act = () => _sut.RequestSave();

        // Assert
        await act.Should().NotThrowAsync();
        invocationCount.Should().Be(2);
        receivedBuilder.Should().BeSameAs(mockBuilder);
    }

    [Fact]
    public async Task RequestSave_WhenCalled_SavesViewportBeforeInvokingSaveRequested()
    {
        // Arrange
        var callOrder = new List<string>();
        _datastore.When(d => d.SaveViewport(Arg.Any<DiagramService>()))
                  .Do(_ => callOrder.Add("saveViewport"));
        _sut.SaveRequested += _ => { callOrder.Add("saveRequested"); return Task.CompletedTask; };

        // Act
        await _sut.RequestSave();

        // Assert
        callOrder.Should().ContainInOrder("saveViewport", "saveRequested");
    }

    [Fact]
    public async Task RequestSave_WithNoHandlers_DoesNotThrow()
    {
        // Act
        var act = () => _sut.RequestSave();

        // Assert
        await act.Should().NotThrowAsync();
    }

    private void SetUpBuilderWithRootContainer(out IClusterBuilder builder, out Cluster.Model.Container root)
    {
        builder = Substitute.For<IClusterBuilder>();
        root = Substitute.For<Cluster.Model.Container>();

        var dataflow = new Cluster.Model.Dataflow
        {
            Root = root
        };
        var cluster = new Cluster.Model.Cluster
        {
            Dataflows = [dataflow]
        };

        builder.Cluster.Returns(cluster);

        _datastore.Builder.Returns(builder);
    }

    [Fact]
    public async Task ShowMessageToast_InvokesAllHandlers_DoesNotThrowEvenIfHandlerThrows()
    {
        // Arrange
        var invocationCount = 0;
        _sut.MessageToastRequested += (_, _, _) => { invocationCount++; throw new InvalidOperationException("First handler fails"); };
        _sut.MessageToastRequested += (_, _, _) => { invocationCount++; return Task.CompletedTask; };

        // Act
        var act = () => _sut.ShowMessageToast(LogLevel.Error, "message", () => { }); ;

        // Assert
        await act.Should().NotThrowAsync();
        invocationCount.Should().Be(2);
    }

    [Fact]
    public async Task ShowMessageToast_InvokesHandlerWithCorrectParameters()
    {
        // Arrange
        const LogLevel ExpectedLogLevel = LogLevel.Error;
        const string ExpectedMessage = "Test toast message";
        static void ExpectedCallback() { }

        LogLevel? receivedLevel = null;
        string? receivedMessage = null;
        Action? receivedCallback = null;

        _sut.MessageToastRequested += (level, msg, callback)
            =>
            {
                receivedLevel = level;
                receivedMessage = msg;
                receivedCallback = callback;
                return Task.CompletedTask;
            };

        // Act
        await _sut.ShowMessageToast(ExpectedLogLevel, ExpectedMessage, ExpectedCallback);

        // Assert
        receivedLevel.Should().Be(ExpectedLogLevel);
        receivedMessage.Should().Be(ExpectedMessage);
        receivedCallback.Should().BeSameAs(ExpectedCallback);
    }

    [Fact]
    public async Task ShowMessageToast_WithNoHandlers_DoesNotThrow()
    {
        // Act
        var act = () => _sut.ShowMessageToast(LogLevel.Information, "message", () => { });

        // Assert
        await act.Should().NotThrowAsync();
    }
}
