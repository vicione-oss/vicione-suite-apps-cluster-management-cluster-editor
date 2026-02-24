using System;
using System.Linq;
using ViciOne.Cluster.Model;
using ViciOne.Cluster.Model.Extensions;

namespace ViciOne.Ui.ClusterEditor.Extensions;

internal static class IConnectorExtension
{
    public static string GetPath(this IConnector connector)
        => connector switch
        {
            Connector fbConnector => $"{GetPath(fbConnector.FunctionBlock.Container)}.{fbConnector.FunctionBlock.Name}.{fbConnector.Name}",
            ContainerConnector containerConnector => $"{GetPath(containerConnector.Container)}.{containerConnector.Connector.Name}",
            _ => throw new InvalidOperationException("Unknown connector type")
        };

    private static string GetPath(Container container)
        => container switch
        {
            ChildContainer childContainer => $"{(childContainer.Parent is not null ? GetPath(childContainer.Parent) + "." : string.Empty)}{childContainer.Name}",
            Container => container.Name,
            _ => throw new InvalidOperationException("Unknown container type")
        };

    public static bool HasUpstreamLinks(this IConnector connector)
    {
        ChildContainer? parentContainer = null;
        ContainerConnector? parentContainerConnector = null;

        if (connector is Connector fbConnector)
        {
            if (fbConnector.FunctionBlock.Container is ChildContainer container)
            {
                parentContainer = container;
                parentContainerConnector = fbConnector.GetAllUpstreamContainerConnectors().FirstOrDefault(cc => cc.Container == parentContainer);
            }
        }
        else if (connector is ContainerConnector containerConnector)
        {
            if (containerConnector.Container.Parent is ChildContainer container)
            {
                parentContainer = container;
                parentContainerConnector = containerConnector.Connector.GetAllUpstreamContainerConnectors().FirstOrDefault(cc => cc.Container == parentContainer);
            }
        }

        if (parentContainer is null || parentContainerConnector is null)
            return false;

        return parentContainerConnector.GetVisibleLinksConnectedToThis().Any(
            vl => vl.DestinationConnector is not null &&
                (vl.DestinationConnector.FunctionBlock.Container == parentContainer.Parent ||
                    vl.DestinationConnector.GetAllUpstreamContainerConnectors()
                        .Any(cc => parentContainer.Parent.Containers.Contains(cc.Container)))
            )
            || parentContainerConnector.HasUpstreamLinks();
    }
}
