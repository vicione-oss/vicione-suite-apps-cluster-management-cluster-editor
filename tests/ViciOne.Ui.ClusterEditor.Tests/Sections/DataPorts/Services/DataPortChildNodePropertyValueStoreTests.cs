using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.DataPorts.Services;

public sealed class DataPortChildNodePropertyValueStoreTests
{
    private const string PropertyName = "MyProperty";

    private readonly DataPortChildNodePropertyValueStore _store = new();

    [Fact]
    public void Clear_RemovesAllStoredValues()
    {
        // Arrange
        _store.Set(PropertyName, 42);
        _store.Set("Other", "value");

        // Act
        _store.Clear();

        // Assert
        Assert.False(_store.TryGet<int>(PropertyName, out _));
        Assert.False(_store.TryGet<string>("Other", out _));
    }

    [Fact]
    public void Get_WhenValueExists_ReturnsStoredValue()
    {
        // Arrange
        _store.Set(PropertyName, 42);

        // Act
        var result = _store.Get(PropertyName, defaultValue: 0);

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public void Get_WhenValueMissing_ReturnsDefaultValue()
    {
        // Act
        var result = _store.Get(PropertyName, defaultValue: 99);

        // Assert
        Assert.Equal(99, result);
    }

    [Fact]
    public void Get_WhenStoredValueTypeMismatches_ReturnsDefaultValue()
    {
        // Arrange
        _store.Set(PropertyName, "not-an-int");

        // Act
        var result = _store.Get(PropertyName, defaultValue: 7);

        // Assert
        Assert.Equal(7, result);
    }

    [Fact]
    public void Set_OverwritesExistingValue()
    {
        // Arrange
        _store.Set(PropertyName, 1);

        // Act
        _store.Set(PropertyName, 2);

        // Assert
        Assert.Equal(2, _store.Get(PropertyName, defaultValue: 0));
    }

    [Fact]
    public void SetIfAbsent_WhenValueExists_KeepsExistingValue()
    {
        // Arrange
        _store.Set(PropertyName, 1);

        // Act
        _store.SetIfAbsent(PropertyName, 2);

        // Assert
        Assert.Equal(1, _store.Get(PropertyName, defaultValue: 0));
    }

    [Fact]
    public void SetIfAbsent_WhenValueMissing_StoresValue()
    {
        // Act
        _store.SetIfAbsent(PropertyName, 2);

        // Assert
        Assert.Equal(2, _store.Get(PropertyName, defaultValue: 0));
    }

    [Fact]
    public void TryGet_WhenNullValueStored_ReturnsFalse()
    {
        // Arrange
        _store.Set(PropertyName, null);

        // Act
        var found = _store.TryGet<string>(PropertyName, out var value);

        // Assert
        Assert.False(found);
        Assert.Null(value);
    }

    [Fact]
    public void TryGet_WhenValueExists_ReturnsTrueWithValue()
    {
        // Arrange
        _store.Set(PropertyName, "hello");

        // Act
        var found = _store.TryGet<string>(PropertyName, out var value);

        // Assert
        Assert.True(found);
        Assert.Equal("hello", value);
    }

    [Fact]
    public void TryGet_WhenValueMissing_ReturnsFalse()
    {
        // Act
        var found = _store.TryGet<int>(PropertyName, out var value);

        // Assert
        Assert.False(found);
        Assert.Equal(0, value);
    }
}
