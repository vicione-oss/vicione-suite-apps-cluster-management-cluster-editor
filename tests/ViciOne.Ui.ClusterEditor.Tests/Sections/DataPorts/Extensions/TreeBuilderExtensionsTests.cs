using System.Collections.Generic;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.DataPorts.Extensions;

public class TreeBuilderExtensions_GetDefaultDirection
{
    public static TheoryData<List<DataPortDirection>, DataPortDirection> GetDataPortDirections()
        => new()
        {
            { [DataPortDirection.In, DataPortDirection.Out], DataPortDirection.Out },
            { [DataPortDirection.In], DataPortDirection.In },
            { [DataPortDirection.Out], DataPortDirection.Out },
            { [DataPortDirection.In, DataPortDirection.Out, DataPortDirection.InOut], DataPortDirection.InOut },
        };

    [Theory]
    [MemberData(nameof(GetDataPortDirections))]
    public void Should_return_default_direction(List<DataPortDirection> dataPortDirections, DataPortDirection @default)
        => Assert.Equal(@default, TreeBuilderExtensions.GetDefaultDirection(dataPortDirections));
}
