using System;
using System.Collections.Generic;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Helpers;

internal class SearchBlocksService(DiagramService diagramService)
{
    private int _currentIndex = -1;
    private List<BlockNode>? _result;

    public bool HasResult => _result is not null;

    public IReadOnlyList<BlockNode>? GetAll()
        => _result;

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

    private static bool MatchesAnyTerm(string name, string[] searchTerms)
    {
        foreach (var term in searchTerms)
        {
            if (name.Contains(term, StringComparison.CurrentCultureIgnoreCase))
                return true;
        }

        return false;
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
            var searchTerms = searchString.Split(';', StringSplitOptions.TrimEntries);
            _result = [];

            foreach (var node in diagramService.Diagram.Nodes)
            {
                if (node is BlockNode blockNode && blockNode.Visible && MatchesAnyTerm(blockNode.Name, searchTerms))
                    _result.Add(blockNode);
            }
        }
    }
}
