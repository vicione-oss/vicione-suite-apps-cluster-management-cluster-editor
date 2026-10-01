namespace ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Models;

/// <summary>
/// Which way a connector points. The filter is switched off with <see langword="null"/> rather than by a
/// third member.
/// </summary>
internal enum ConnectorDirection
{
    Input,
    Output
}
