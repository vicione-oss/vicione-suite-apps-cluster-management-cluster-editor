using System;
using System.Collections.Generic;
using ViciOne.Ui.ClusterEditor.Constants;

namespace ViciOne.Ui.ClusterEditor.Models.Data;

internal static class ConnectorColor
{
    private static readonly Dictionary<Type, string> s_colorMapping = new()
    {
        { typeof(bool), BlockNodeConnectorColors.TypeBool },

        { typeof(float), BlockNodeConnectorColors.TypeDouble },
        { typeof(double), BlockNodeConnectorColors.TypeDouble },
        { typeof(decimal), BlockNodeConnectorColors.TypeDouble },

        { typeof(long), BlockNodeConnectorColors.TypeLong },
        { typeof(int), BlockNodeConnectorColors.TypeLong },
        { typeof(short), BlockNodeConnectorColors.TypeLong },
        { typeof(sbyte), BlockNodeConnectorColors.TypeLong },
        { typeof(ulong), BlockNodeConnectorColors.TypeLong },
        { typeof(uint), BlockNodeConnectorColors.TypeLong },
        { typeof(ushort), BlockNodeConnectorColors.TypeLong },
        { typeof(byte), BlockNodeConnectorColors.TypeLong },

        { typeof(string), BlockNodeConnectorColors.TypeString },
        { typeof(char), BlockNodeConnectorColors.TypeString }
    };

    internal static string Get(Type connectorType)
        => s_colorMapping.GetValueOrDefault(connectorType, BlockNodeConnectorColors.TypeDefault);
}
