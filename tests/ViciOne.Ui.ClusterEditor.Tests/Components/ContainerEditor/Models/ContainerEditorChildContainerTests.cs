using System;
using AwesomeAssertions;
using NSubstitute;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Components.ContainerEditor.Models;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components.ContainerEditor.Models;

public sealed class ContainerEditorChildContainerTests
{
    private readonly ChildContainer _container = new()
    {
        BackColor = "#FFF",
        Description = "Desc",
        ForeColor = "#000",
        Name = "Initial"
    };
    private readonly IContainerEditor _editor = Substitute.For<IContainerEditor>();

    [Theory]
    [InlineData("#000", null)]
    [InlineData(null, "#ABC")]
    [InlineData("#ABC", "#XYZ")]
    public void BackColor_setter_calls_editor_and_sets_Changed(string? initialColor, string? newColor)
    {
        // Arrange
        _container.BackColor = initialColor;
        var sut = CreateSut();

        // Act
        sut.BackColor = newColor;

        // Assert
        _editor.Received(1).SetBackColor(_container, newColor);
        sut.Changed.Should().BeTrue();
    }

    [Fact]
    public void BackColor_setter_with_same_value_does_not_call_editor_or_set_Changed()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.BackColor = _container.BackColor;

        // Assert
        _editor.DidNotReceive().SetBackColor(_container, Arg.Any<string?>());
        sut.Changed.Should().BeFalse();
    }

    [Fact]
    public void Changed_is_false_initially()
    {
        // Arrange & Act
        var sut = CreateSut();

        // Assert
        sut.Changed.Should().BeFalse();
    }

    private ContainerEditorChildContainer CreateSut() => new(_editor)
    {
        Backup = new ContainerEditorChildContainerBackup
        {
            BackColor = _container.BackColor,
            Container = _container,
            Description = _container.Description,
            ForeColor = _container.ForeColor,
            Name = _container.Name
        }
    };

    [Theory]
    [InlineData("Old desc", null)]
    [InlineData(null, "New desc")]
    [InlineData("Old desc", "New desc")]
    public void Description_setter_calls_editor_and_sets_Changed(string? initialDesc, string? newDesc)
    {
        // Arrange
        _container.Description = initialDesc;
        var sut = CreateSut();

        // Act
        sut.Description = newDesc;

        // Assert
        _editor.Received(1).SetDescription(_container, newDesc);
        sut.Changed.Should().BeTrue();
    }

    [Fact]
    public void Description_setter_with_same_value_does_not_call_editor_or_set_Changed()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.Description = _container.Description;

        // Assert
        _editor.DidNotReceive().SetDescription(_container, Arg.Any<string?>());
        sut.Changed.Should().BeFalse();
    }

    [Theory]
    [InlineData("#000", null)]
    [InlineData(null, "#ABC")]
    [InlineData("#ABC", "#XYZ")]
    public void ForeColor_setter_calls_editor_and_sets_Changed(string? initialColor, string? newColor)
    {
        // Arrange
        _container.ForeColor = initialColor;
        var sut = CreateSut();

        // Act
        sut.ForeColor = newColor;

        // Assert
        _editor.Received(1).SetForeColor(_container, newColor);
        sut.Changed.Should().BeTrue();
    }

    [Fact]
    public void ForeColor_setter_with_same_value_does_not_call_editor_or_set_Changed()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.ForeColor = _container.ForeColor;

        // Assert
        _editor.DidNotReceive().SetForeColor(_container, Arg.Any<string?>());
        sut.Changed.Should().BeFalse();
    }

    [Fact]
    public void Getters_delegate_to_Container()
    {
        // Arrange & Act
        var sut = CreateSut();

        // Assert
        sut.Name.Should().Be(_container.Name);
        sut.BackColor.Should().Be(_container.BackColor);
        sut.ForeColor.Should().Be(_container.ForeColor);
        sut.Description.Should().Be(_container.Description);
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
        _editor.Received(1).SetName(_container, newName);
        sut.Changed.Should().BeTrue();
    }

    [Fact]
    public void Name_setter_swallows_ArgumentException_and_does_not_set_Changed()
    {
        // Arrange
        var sut = CreateSut();
        _editor.When(e => e.SetName(_container, Arg.Any<string>()))
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
        sut.Name = _container.Name;

        // Assert
        _editor.DidNotReceive().SetName(_container, Arg.Any<string>());
        sut.Changed.Should().BeFalse();
    }
}
