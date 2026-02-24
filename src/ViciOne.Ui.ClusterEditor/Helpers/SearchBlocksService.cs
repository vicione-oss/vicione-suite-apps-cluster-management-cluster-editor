using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Helpers;

internal class SearchBlocksService(Datastore datastore, DiagramService diagramService)
{
    private int _currentIndex = -1;
    private List<BlockNode>? _result;

    public bool HasResult => _result is not null;

    public IEnumerable<BlockNode>? GetAll()
        => _result?.ToArray();

    public BlockNode? GetNext()
    {
        if (_result is null || _result.Count == 0)
            return null;

        if (++_currentIndex >= _result.Count)
            _currentIndex = 0;

        return _result[_currentIndex];
    }

    public BlockNode? GetPrevious()
    {
        if (_result is null || _result.Count == 0)
            return null;

        if (--_currentIndex < 0)
            _currentIndex = _result.Count - 1;

        return _result[_currentIndex];
    }

    public void Reset()
    {
        _currentIndex = -1;
        _result = null;
    }

    public void Search(string searchString)
    {
        if (string.IsNullOrEmpty(searchString))
        {
            _result = [];
        }
        else
        {
            var searchTerms = searchString.Split(';').Select(s => s.Trim());

            var functionBlockModels = datastore.DataflowDiagramMapping
                .GetModels(diagramService.Diagram.Nodes.OfType<FunctionBlockNode>().Where(n => n.Visible))
                .Where(fbm => searchTerms.Any(st => fbm.Name.Contains(st, StringComparison.CurrentCultureIgnoreCase)));
            var functionBlockNodes = datastore.DataflowDiagramMapping
                .GetDiagramModels(functionBlockModels);

            var containerModels = datastore.DataflowDiagramMapping
                .GetModels(diagramService.Diagram.Nodes.OfType<ChildContainerNode>().Where(n => n.Visible))
                .Where(cm => searchTerms.Any(st => cm.Name.Contains(st, StringComparison.CurrentCultureIgnoreCase)));
            var containerNodes = datastore.DataflowDiagramMapping
                .GetDiagramModels(containerModels);

            _result = [.. functionBlockNodes, .. containerNodes];
        }
    }
}
