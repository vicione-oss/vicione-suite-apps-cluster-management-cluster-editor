using System;
using System.Globalization;
using ViciOne.Cluster.Model;

namespace ViciOne.Ui.ColorableIcons;

public static class ColoredIconFactory
{
    public static string GetConnectorIcon(string htmlColor, bool isInput, int? size = default)
    {
        var icon = isInput ? IconParts.ConnectorInput : IconParts.ConnectorOutput;
        return icon.SetSize(size).SetColor(htmlColor);
    }

    public static string GetDataPortIcon(string htmlColor, DataPortDirection dataPortDirection, bool isConnectedToOutputConnectors = false, bool isConnectedToInputConnectors = false, int? size = default)
    {
        var body = IconParts.DataPortIconBody.SetSize(size);

        var iconPath = dataPortDirection switch
        {
            DataPortDirection.In => isConnectedToInputConnectors ? IconParts.DataPortIconFilled : IconParts.DataPortIconEmpty,
            DataPortDirection.Out => isConnectedToOutputConnectors ? IconParts.DataPortIconFilled : IconParts.DataPortIconEmpty,
            DataPortDirection.InOut => isConnectedToInputConnectors
                ? isConnectedToOutputConnectors ? IconParts.DataPortIconFilled : IconParts.DataPortIconInputFilled
                : isConnectedToOutputConnectors ? IconParts.DataPortIconOutputFilled : IconParts.DataPortIconEmpty,
            _ => throw new NotSupportedException("Unsupported DataPort direction.")
        };

        iconPath = iconPath.SetColor(htmlColor);

        var arrows = dataPortDirection switch
        {
            DataPortDirection.In => IconParts.DataPortArrowInPath,
            DataPortDirection.Out => IconParts.DataPortArrowOutPath,
            DataPortDirection.InOut => IconParts.DataPortArrowInPath + IconParts.DataPortArrowOutPath,
            _ => throw new NotSupportedException("Unsupported DataPort direction.")
        };

        var paths = iconPath + arrows;

        return string.Format(CultureInfo.InvariantCulture, body, paths);
    }

    private static string SetColor(this string iconPath, string htmlColor)
        => iconPath.Replace("fill=\"#F0F\"", $"fill=\"{htmlColor}\"", StringComparison.InvariantCultureIgnoreCase);

    private static string SetSize(this string input, int? size)
        => size is not null
            ? input.Replace("<svg", $"<svg height=\"{size}px\" width=\"{size}px\"", StringComparison.InvariantCultureIgnoreCase)
            : input;
}
