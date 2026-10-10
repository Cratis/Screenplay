// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

sealed class McpWorkspaceAnalysis
{
    static readonly ConditionalWeakTable<ScreenplayWorkspace, McpWorkspaceAnalysis> _analyses = [];
    readonly WorkspaceAuthoringAnalysis _authoring;
    readonly Lazy<McpSnapshot> _source;
    readonly Lazy<WorkspacePhysicalReadView> _physical;
    readonly Lazy<WorkspaceImplementationInventory> _handlerIntents;
    readonly Lazy<WorkspaceNamedRuleIntentInventory> _namedRuleIntents;
    readonly Lazy<McpOperationInventory> _operationIntents;
    readonly Lazy<McpEventSourceInventory> _eventSources;
    readonly Lazy<byte[]> _export;
    readonly Lazy<Dictionary<WorkspaceNodeHandle, int>> _childCounts;

    McpWorkspaceAnalysis(ScreenplayWorkspace workspace)
    {
        _authoring = WorkspaceAuthoringAnalysis.For(workspace);
        _source = new(() => new McpSnapshot(_authoring.Source));
        _physical = new(() => WorkspacePhysicalReadView.Create(workspace));
        _handlerIntents = new(() => WorkspaceImplementationInventory.Create(Syntax));
        _namedRuleIntents = new(() => WorkspaceNamedRuleIntentInventory.Create(Syntax));
        _operationIntents = new(() => new(workspace, Syntax));
        _eventSources = new(() => new(workspace, _physical.Value));
        _export = new(() => ScreenplayWorkspaceSerializer.Serialize(workspace));
        _childCounts = new(() => Syntax.Entries.Where(entry => entry.Parent is not null).GroupBy(entry => entry.Parent!).ToDictionary(group => group.Key, group => group.Count()));
    }

    internal McpSnapshot Source => _source.Value;
    internal WorkspaceSyntaxIndex Syntax => _authoring.Syntax;
    internal WorkspaceImplementationInventory HandlerIntents => _handlerIntents.Value;
    internal WorkspaceNamedRuleIntentInventory NamedRuleIntents => _namedRuleIntents.Value;
    internal McpOperationInventory OperationIntents => _operationIntents.Value;
    internal McpEventSourceInventory EventSources => _eventSources.Value;
    internal byte[] ExportBytes => _export.Value;

    internal static McpWorkspaceAnalysis For(ScreenplayWorkspace workspace) => _analyses.GetValue(workspace, value => new(value));

    internal int ChildCount(WorkspaceNodeHandle handle) => _childCounts.Value.GetValueOrDefault(handle);
}
