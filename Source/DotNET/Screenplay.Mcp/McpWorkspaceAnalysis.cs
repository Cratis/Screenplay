// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

sealed class McpWorkspaceAnalysis
{
    static readonly ConditionalWeakTable<ScreenplayWorkspace, McpWorkspaceAnalysis> _analyses = [];
    readonly Lazy<McpSnapshot> _source;
    readonly Lazy<WorkspaceSyntaxIndex> _syntax;
    readonly Lazy<byte[]> _export;
    readonly Lazy<Dictionary<WorkspaceNodeHandle, int>> _childCounts;

    McpWorkspaceAnalysis(ScreenplayWorkspace workspace)
    {
        _source = new(() => new McpSnapshot(workspace.Documents));
        _syntax = new(() => WorkspaceSyntaxIndex.Create(workspace));
        _export = new(() => ScreenplayWorkspaceSerializer.Serialize(workspace));
        _childCounts = new(() => Syntax.Entries.Where(entry => entry.Parent is not null).GroupBy(entry => entry.Parent!).ToDictionary(group => group.Key, group => group.Count()));
    }

    internal McpSnapshot Source => _source.Value;
    internal WorkspaceSyntaxIndex Syntax => _syntax.Value;
    internal byte[] ExportBytes => _export.Value;

    internal static McpWorkspaceAnalysis For(ScreenplayWorkspace workspace) => _analyses.GetValue(workspace, value => new(value));

    internal int ChildCount(WorkspaceNodeHandle handle) => _childCounts.Value.GetValueOrDefault(handle);
}
