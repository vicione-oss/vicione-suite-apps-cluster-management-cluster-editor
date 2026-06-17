using System;
using AwesomeAssertions;
using NSubstitute;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Components.ContainerEditor.Models;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components.ContainerEditor.Models;

public sealed class ContainerEditorConnectorTests
{
    private readonly ContainerConnectorInput _connector = new()
    {
        Description = "Desc",
        Index = 1,
        Name = "InitialName",
        ShortName = "IN"
    };
    private readonly IConnectorEditor _editor = Substitute.For<IConnectorEditor>();

    [Fact]
    public void Changed_is_false_initially()
    {
        // Arrange & Act
        var sut = CreateSut();

        // Assert
        sut.Changed.Should().BeFalse();
    }

    private ContainerEditorConnector CreateSut() => new(_editor)
    {
        Backup = new ContainerEditorConnectorBackup
        {
            Connector = _connector,
            Description = _connector.Description,
            Index = (int)_connector.Index,
            Name = _connector.Name,
            ShortName = _connector.ShortName
        }
    };

    [Theory]
    [InlineData("OldDesc", null)]
    [InlineData(null, "NewDesc")]
    [InlineData("OldDesc", "NewDesc")]
    public void Description_setter_calls_editor_and_sets_Changed(string? initialDesc, string? newDesc)
    {
        // Arrange
        _connector.Description = initialDesc;
        var sut = CreateSut();

        // Act
        sut.Description = newDesc;

        // Assert
        _editor.Received(1).SetDescription(_connector, newDesc ?? string.Empty);
        sut.Changed.Should().BeTrue();
    }

    [Fact]
    public void Description_setter_with_same_value_does_not_call_editor_or_set_Changed()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.Description = _connector.Description;

        // Assert
        _editor.DidNotReceive().SetDescription(_connector, Arg.Any<string?>());
        sut.Changed.Should().BeFalse();
    }

    [Fact]
    public void Getters_delegate_to_Connector()
    {
        // Arrange & Act
        var sut = CreateSut();

        // Assert
        sut.Name.Should().Be(_connector.Name);
        sut.ShortName.Should().Be(_connector.ShortName);
        sut.Index.Should().Be((int)_connector.Index);
        sut.Description.Should().Be(_connector.Description);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void Index_setter_calls_editor_and_sets_Changed(int newIndex)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.Index = newIndex;

        // Assert
        _editor.Received(1).SetIndex(_connector, (uint)newIndex);
        sut.Changed.Should().BeTrue();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-99)]
    public void Index_setter_with_negative_value_does_not_call_editor_or_set_Changed(int negativeIndex)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.Index = negativeIndex;

        // Assert
        _editor.DidNotReceive().SetIndex(_connector, Arg.Any<uint>());
        sut.Changed.Should().BeFalse();
    }

    [Fact]
    public void Index_setter_with_same_value_does_not_call_editor_or_set_Changed()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.Index = (int)_connector.Index;

        // Assert
        _editor.DidNotReceive().SetIndex(_connector, Arg.Any<uint>());
        sut.Changed.Should().BeFalse();
    }

    [Theory]
    [InlineData("NewName")]
    [InlineData("AnotherName")]
    public void Name_setter_calls_editor_and_sets_Changed(string newName)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.Name = newName;

        // Assert
        _editor.Received(1).SetName(_connector, newName);
        sut.Changed.Should().BeTrue();
    }

    [Fact]
    public void Name_setter_swallows_ArgumentException_and_does_not_set_Changed()
    {
        // Arrange
        var sut = CreateSut();
        _editor.When(e => e.SetName(_connector, Arg.Any<string>()))
               .Throw(new ArgumentException("Duplicate name"));

        // Act
        sut.Name = "Duplicate";

        // Assert
        sut.Changed.Should().BeFalse();
    }

    [Fact]
    public void Name_setter_with_same_value_does_not_call_editor_or_set_Changed()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.Name = _connector.Name;

        // Assert
        _editor.DidNotReceive().SetName(_connector, Arg.Any<string>());
        sut.Changed.Should().BeFalse();
    }

    [Theory]
    [InlineData("SN")]
    [InlineData("XY")]
    public void ShortName_setter_calls_editor_and_sets_Changed(string newShortName)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.ShortName = newShortName;

        // Assert
        _editor.Received(1).SetShortName(_connector, newShortName);
        sut.Changed.Should().BeTrue();
    }

    [Fact]
    public void ShortName_setter_swallows_ArgumentException_and_does_not_set_Changed()
    {
        // Arrange
        var sut = CreateSut();
        _editor.When(e => e.SetShortName(_connector, Arg.Any<string>()))
               .Throw(new ArgumentException("Invalid short name"));

        // Act
        sut.ShortName = "BAD";

        // Assert
        sut.Changed.Should().BeFalse();
    }

    [Fact]
    public void ShortName_setter_with_same_value_does_not_call_editor_or_set_Changed()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.ShortName = _connector.ShortName;

        // Assert
        _editor.DidNotReceive().SetShortName(_connector, Arg.Any<string>());
        sut.Changed.Should().BeFalse();
    }
}
