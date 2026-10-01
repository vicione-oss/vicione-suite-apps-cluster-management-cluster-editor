using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using ViciOne.Ui.ClusterEditor.Models.Comparer;
using ViciOne.Ui.ClusterEditor.Services;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Services;

public class ComparerServiceTests
{
    private readonly ILogger<ComparerService> _logger = NSubstitute.Substitute.For<ILogger<ComparerService>>();

    [Fact]
    public void Service_should_find_inherited_comparers()
    {
        // Arrange
        var service = new ComparerService(new List<KnownComparerType>()
        {
            new () { Type = typeof(InheritTestCompareItem) },
        }, _logger);
        service.InitCache();

        // Act
        var comparer = service.GetComparer(typeof(InheritTestCompareItem));

        // Assert
        Assert.NotNull(comparer);
    }

    [Fact]
    public void Service_should_not_find_interface_comparers()
    {
        // Arrange
        var service = new ComparerService(new List<KnownComparerType>()
        {
            new () { Type = typeof(InterfaceTestCompareItem) },
        }, _logger);
        service.InitCache();

        // Act
        var comparer = service.GetComparer(typeof(InterfaceTestCompareItem));

        // Assert
        Assert.Null(comparer);
    }

    [Fact]
    public void Service_should_not_find_nested_comparers()
    {
        // Arrange
        var service = new ComparerService(new List<KnownComparerType>()
        {
            new () { Type = typeof(NestedTestCompareItem) },
        }, _logger);
        service.InitCache();

        // Act
        var comparer = service.GetComparer(typeof(NestedTestCompareItem));

        // Assert
        Assert.Null(comparer);
    }

    [SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "ComparerService only finds exported comparers, and a public comparer needs a public item type.")]
    public class NestedTestCompareItem
    {
        public required int CompareValue { get; init; }
    }

    [SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "ComparerService only finds exported comparers, and a public comparer needs a public item type.")]
    public class NestedTestComparer : IComparer<InterfaceTestCompareItem>, IComparer
    {
        public int Compare(InterfaceTestCompareItem? x, InterfaceTestCompareItem? y) => x is not null ? x.CompareValue.CompareTo(y?.CompareValue) : -1;
        public int Compare(object? x, object? y) => Compare(x as InterfaceTestCompareItem, y as InterfaceTestCompareItem);
    }
}


[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "ComparerService only finds exported comparers, and a public comparer needs a public item type.")]
public class InheritTestCompareItem
{
    public required int CompareValue { get; init; }
}

[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "ComparerService only finds exported comparers, and a public comparer needs a public item type.")]
public class InheritTestComparer : Comparer<InheritTestCompareItem>
{
    public override int Compare(InheritTestCompareItem? x, InheritTestCompareItem? y) => x is not null ? x.CompareValue.CompareTo(y?.CompareValue) : -1;
}

[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "ComparerService only finds exported comparers, and a public comparer needs a public item type.")]
public class InterfaceTestCompareItem
{
    public required int CompareValue { get; init; }
}

[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "ComparerService only finds exported comparers, and a public comparer needs a public item type.")]
public class InterfaceTestComparer : IComparer<InterfaceTestCompareItem>, IComparer
{
    public int Compare(InterfaceTestCompareItem? x, InterfaceTestCompareItem? y) => x is not null ? x.CompareValue.CompareTo(y?.CompareValue) : -1;
    public int Compare(object? x, object? y) => Compare(x as InterfaceTestCompareItem, y as InterfaceTestCompareItem);
}
