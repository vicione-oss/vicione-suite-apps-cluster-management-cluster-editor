using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Helpers;
using ViciOne.Ui.ClusterEditor.Localization;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.Comparer;
using ViciOne.Ui.ClusterEditor.Resources;
using ViciOne.Ui.ClusterEditor.Sections.Dataflow.Components;
using ViciOne.Ui.ClusterEditor.Sections.Dataflow.Models;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.Localization.Resources;
using ViciOne.Ui.MonochromeIcons.Core.Enums;
using ViciOne.Ui.TreeEditor.Builder.Interface;
using ViciOne.Ui.TreeEditor.Builder.Interface.Enums;
using ViciOne.Ui.TreeEditor.Builder.Interface.Icons;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeActions;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeIdentifier;
using ViciOne.Ui.TreeEditor.Builder.Interface.Nodes;
using TechnicalTerms = ViciOne.Ui.ClusterEditor.Localization.Resources.TechnicalTerms;

namespace ViciOne.Ui.ClusterEditor.Sections.Dataflow.Services;

internal sealed partial class DataflowStructureTreeAdapter : TreeAdapter, IDisposable
{
#pragma warning disable CA2213 // Disposable fields should be disposed
    private IClusterBuilder _clusterBuilder = default!;
#pragma warning restore CA2213 // Disposable fields should be disposed
    private readonly ClusterBuilderEventBuffer _clusterBuilderEventBuffer;
    private readonly Dictionary<ChildContainer, ContainerStructureTreeNode> _containerMap = [];
    private readonly Dictionary<Cluster.Model.Dataflow, DataflowStructureTreeNode> _dataflowMap = [];
    private readonly IDatastore _datastore;
    private readonly DiagramEventService _diagramEventService;
    private readonly DiagramService _diagramService;
    private readonly Dictionary<FunctionBlock, FunctionBlockStructureTreeNode> _functionBlockMap = [];
    private readonly SelectionManager _selectionManager;

    public event Action<DataflowStructureTreeNode>? DeleteStarted;

    public DataflowStructureTreeAdapter(
        ClusterBuilderEventBuffer clusterBuilderEventBuffer,
        IDatastore datastore,
        DiagramService diagramService,
        DiagramEventService diagramEventService,
        SelectionManager selectionManager)
    {
        _clusterBuilderEventBuffer = clusterBuilderEventBuffer;
        _datastore = datastore;
        _diagramService = diagramService;
        _diagramEventService = diagramEventService;
        _selectionManager = selectionManager;

        _datastore.ActiveDataflowChanged += UpdateDataflowActiveState;
        _diagramEventService.ContainerLoaded += OnContainerLoaded;
    }

    public override void Dismantle()
    {
        Builder.Expansion.ExpansionChanged -= OnExpansionChanged;
        Builder.Selection.SelectionChanged -= OnSelectionChanged;
    }

    public void FilterNodes(string filterText)
        => TreeAdapterHelper.FilterNodesByDisplayText(Builder, GetParent, filterText);

    public override IEnumerable<INodeAction> GetActions(ITreeNode node)
    {
        if (node is ContainerStructureTreeNode containerNode)
        {
            return
            [
                new NodeButton()
                {
                    Action = async (s, e) => await LoadContainerAsync(e.Node),
                    Description = CompositeFormats.LoadSomething(TechnicalTerms.Container),
                    EnabledFunc = (_) => _datastore.ActiveContainer != containerNode.ChildContainer,
                    Icon = new SvgIcon(SvgIcons.dataflow_structure_block_jump_into),
                },
                new NodeButton()
                {
                    Action = async (s, e) => await LoadContainerContainerAsync(e.Node),
                    Description = Localization.DataflowStructureTreeAdapter.JumpToContainer,
                    EnabledFunc = (_) => true,
                    Icon = new SvgIcon(SvgIcons.dataflow_structure_block_jump_to),
                },
            ];
        }

        if (node is DataflowStructureTreeNode dataflowNode)
        {
            return
            [
                new NodeButton()
                {
                    Action = async (s, e) => await LoadDataflowAsync(dataflowNode),
                    Description = CompositeFormats.LoadSomething(TechnicalTerms.Dataflow),
                    EnabledFunc = (_) => _datastore.ActiveDataflow != dataflowNode.Dataflow || _datastore.ActiveContainer != dataflowNode.Dataflow.Root,
                    Icon = new SvgIcon(SvgIcons.dataflow_structure_block_jump_into),
                },
                new NodeButton()
                {
                    Action = (s, e) => StartEditDataflow(dataflowNode),
                    Description = CommonVocabulary.RenameVerb,
                    EnabledFunc = (e) =>
                        !((DataflowStructureTreeNode)e).Editing
                        && !((DataflowStructureTreeNode)e).Deleting,
                    Icon = new TreeEditorMonochromeIcon(MonochromeIconName.Edit, MonochromeIconSize.Small),
                },
                new NodeButton()
                {
                    Action = (s, e) => DeleteStarted?.Invoke(dataflowNode),
                    Description = CommonVocabulary.RemoveVerb,
                    EnabledFunc = (e) =>
                        _datastore.ActiveDataflow != dataflowNode.Dataflow
                        && !((DataflowStructureTreeNode)e).Editing
                        && !((DataflowStructureTreeNode)e).Deleting,
                    Icon = new TreeEditorMonochromeIcon(MonochromeIconName.Delete, MonochromeIconSize.Small),
                },
            ];
        }

        if (node is FunctionBlockStructureTreeNode fbNode)
        {
            return
            [
                new NodeButton()
                {
                    Action = async(s, e) => await LoadFunctionBlockContainerAsync(e.Node),
                    Description = Localization.DataflowStructureTreeAdapter.JumpToFunctionBlock,
                    EnabledFunc = (_) => true,
                    Icon = new SvgIcon(SvgIcons.dataflow_structure_block_jump_to),
                },
            ];
        }

        return [];
    }

    public override IEnumerable<ITreeNode> GetChildren(ITreeNode node)
    {
        if (node is FunctionBlockStructureTreeNode)
            return [];

        Container? parentContainer = null;

        if (node is ContainerStructureTreeNode containerNode)
            parentContainer = containerNode.ChildContainer;

        if (node is DataflowStructureTreeNode dataflowNode)
            parentContainer = dataflowNode.Dataflow.Root;

        if (parentContainer is null)
            return [];

        var containerResult = new List<ITreeNode>();
        foreach (var container in parentContainer.Containers)
        {
            if (_containerMap.TryGetValue(container, out var mappedChild))
            {
                containerResult.Add(mappedChild);
                continue;
            }

            var containerStructureNode = new ContainerStructureTreeNode()
            {
                ChildContainer = container,
                Id = new GuidNodeIdentifier(container.Id),
                Name = container.Name,
            };

            _containerMap[container] = containerStructureNode;
            containerResult.Add(containerStructureNode);
        }

        var fbResult = new List<ITreeNode>();
        foreach (var fb in parentContainer.FunctionBlocks)
        {
            if (_functionBlockMap.TryGetValue(fb, out var mappedChild))
            {
                fbResult.Add(mappedChild);
                continue;
            }

            var fbStructureNode = new FunctionBlockStructureTreeNode()
            {
                FunctionBlock = fb,
                Id = new GuidNodeIdentifier(fb.Id),
                Name = fb.Name,
            };

            _functionBlockMap[fb] = fbStructureNode;
            fbResult.Add(fbStructureNode);
        }

        return
        [
            .. containerResult.OrderBy(n => ((ContainerStructureTreeNode)n).Name, AlphaNumericComparer<string>.Default),
            .. fbResult.OrderBy(n => ((FunctionBlockStructureTreeNode)n).Name, AlphaNumericComparer<string>.Default),
        ];
    }

    public override IEnumerable<string> GetCssClasses(ITreeNode node, TemplateType templateType)
    {
        if (templateType != TemplateType.Node)
            return [];

        if (node is DataflowStructureTreeNode dataflowNode && dataflowNode.Active)
            return ["dataflow-active"];

        return [];
    }

    public override Action<ITreeNode>? GetDblClickAction(ITreeNode node)
    {
        if (node is ContainerStructureTreeNode containerNode)
            return async n => await LoadContainerAsync(containerNode);

        if (node is DataflowStructureTreeNode dataflowNode)
            return async n => await LoadDataflowAsync(dataflowNode);

        if (node is FunctionBlockStructureTreeNode fbNode)
            return async n => await LoadFunctionBlockContainerAsync(fbNode);

        return null;
    }

    public override string GetDisplayText(ITreeNode node)
        => node switch
        {
            ContainerStructureTreeNode cNode => cNode.Name,
            DataflowStructureTreeNode dfNode => dfNode.Name,
            FunctionBlockStructureTreeNode fbNode => fbNode.Name,
            _ => string.Empty
        };

    private static Type? GetNodeTemplate(ITreeNode node, TemplateType templateType)
    {
        if (node is DataflowStructureTreeNode dataflowNode && templateType == TemplateType.NodeDisplay)
        {
            if (dataflowNode.Editing)
                return typeof(StructureTreeDataflowEditNode);
        }

        return null;
    }

    public override ITreeNode? GetParent(ITreeNode node)
    {
        if (node is ContainerStructureTreeNode containerNode)
        {
            if (containerNode.ChildContainer.Parent is not ChildContainer parentContainer)
                return null;

            if (_containerMap.TryGetValue(parentContainer, out var mappedChild))
                return mappedChild;
        }
        else if (node is FunctionBlockStructureTreeNode fbNode)
        {
            if (fbNode.FunctionBlock.Container is not ChildContainer parentContainer)
                return null;

            if (_containerMap.TryGetValue(parentContainer, out var mappedChild))
                return mappedChild;
        }

        return null;
    }

    public override IEnumerable<ITreeNode> GetRootNodes()
        => _dataflowMap
            .Select(dstn => dstn.Value)
            .OrderBy(n => n.Name, AlphaNumericComparer<string>.Default);

    public override bool HasChildren(ITreeNode node)
        => node switch
        {
            ContainerStructureTreeNode containerNode when containerNode.ChildContainer.Containers.Count > 0 || containerNode.ChildContainer.FunctionBlocks.Count > 0 => true,
            DataflowStructureTreeNode dataflowNode when dataflowNode.Dataflow.Root.Containers.Count > 0 || dataflowNode.Dataflow.Root.FunctionBlocks.Count > 0 => true,
            _ => false,
        };

    public override bool IsExpanded(ITreeNode treeNode)
        => treeNode switch
        {
            ContainerStructureTreeNode cNode => cNode.Expanded,
            DataflowStructureTreeNode dfNode => dfNode.Expanded,
            FunctionBlockStructureTreeNode fbNode => fbNode.Expanded,
            _ => false
        };

    public override bool IsSelected(ITreeNode treeNode)
        => treeNode switch
        {
            ContainerStructureTreeNode cNode => cNode.Selected,
            DataflowStructureTreeNode dfNode => dfNode.Selected,
            FunctionBlockStructureTreeNode fbNode => fbNode.Selected,
            _ => false
        };

    private async Task LoadContainerAsync(ITreeNode node)
    {
        if (node is not ContainerStructureTreeNode containerNode)
            return;

        await _datastore.LoadContainer(containerNode.ChildContainer, _diagramService);
        Builder.Notifications.NotifyNodeChanged(containerNode, ChangedNodeDetail.None);
    }

    private async Task LoadContainerContainerAsync(ITreeNode node)
    {
        if (node is not ContainerStructureTreeNode containerStructureNode)
            return;

        await _datastore.LoadContainer(containerStructureNode.ChildContainer.Parent, _diagramService);

        var containerNode = _datastore.DataflowDiagramMapping.GetDiagramModel(containerStructureNode.ChildContainer);
        _selectionManager.SetSelection(containerNode);

        if (!_diagramService.Diagram.IsNodeInViewport(containerNode))
            _diagramService.Diagram.PanToNode(containerNode);

        Builder.Notifications.NotifyNodeChanged(containerStructureNode, ChangedNodeDetail.None);
    }

    private async Task LoadDataflowAsync(DataflowStructureTreeNode dataflowNode)
    {
        await _datastore.LoadContainer(dataflowNode.Dataflow.Root, _diagramService);
        Builder.Notifications.NotifyNodeChanged(dataflowNode, ChangedNodeDetail.None);
    }

    private async Task LoadFunctionBlockContainerAsync(ITreeNode node)
    {
        if (node is not FunctionBlockStructureTreeNode fbStructureNode)
            return;

        await _datastore.LoadContainer(fbStructureNode.FunctionBlock.Container, _diagramService);

        var fbNode = _datastore.DataflowDiagramMapping.GetDiagramModel(fbStructureNode.FunctionBlock);
        _selectionManager.SetSelection(fbNode);

        if (!_diagramService.Diagram.IsNodeInViewport(fbNode))
            _diagramService.Diagram.PanToNode(fbNode);
    }

    private void OnExpansionChanged(ITreeNode node, bool expanded)
    {
        switch (node)
        {
            case ContainerStructureTreeNode cNode:
                cNode.Expanded = expanded;
                break;
            case DataflowStructureTreeNode dfNode:
                dfNode.Expanded = expanded;
                break;
            case FunctionBlockStructureTreeNode fbNode:
                fbNode.Expanded = expanded;
                break;
        }
    }

    private void OnSelectionChanged(ITreeNode node, bool selected)
    {
        switch (node)
        {
            case ContainerStructureTreeNode cNode:
                cNode.Selected = selected;
                break;
            case DataflowStructureTreeNode dfNode:
                dfNode.Selected = selected;
                break;
            case FunctionBlockStructureTreeNode fbNode:
                fbNode.Selected = selected;
                break;
        }
    }

    public override void Setup()
    {
        Builder.Expansion.ExpansionChanged += OnExpansionChanged;
        Builder.Selection.SelectionChanged += OnSelectionChanged;

        Builder.Guidelines.Show = true;
        Builder.Template.Mapping = GetNodeTemplate;
    }

    private void StartEditDataflow(DataflowStructureTreeNode dataflowNode)
    {
        var rootDataflowNodes = Builder.RootNodes.Select(rn => rn.TreeNode).OfType<DataflowStructureTreeNode>().Where(dn => dn.Editing).ToArray();
        foreach (var rdn in rootDataflowNodes)
        {
            if (rdn.Editing)
            {
                rdn.Editing = false;
                Builder.Notifications.NotifyNodeChanged(rdn);
            }
        }

        dataflowNode.Editing = true;
        Builder.Notifications.NotifyNodeChanged(dataflowNode);
    }

    private void UpdateDataflowActiveState()
    {
        if (_dataflowMap.TryGetValue(_datastore.ActiveDataflow, out var treeNode) && !treeNode.Active)
        {
            foreach (var dataflow in _dataflowMap.Values.Where(d => d.Active).ToArray())
            {
                dataflow.Active = false;
                Builder.Notifications.NotifyNodeChanged(dataflow);
            }

            treeNode.Active = true;
            Builder.Notifications.NotifyNodeChanged(treeNode);
        }
    }
}
