// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Indexing;

sealed class WorkspaceAuthoringAnalysis(ScreenplayWorkspace workspace)
{
    static readonly ConditionalWeakTable<ScreenplayWorkspace, WorkspaceAuthoringAnalysis> _analyses = [];
    readonly Lazy<AuthoringSnapshot> _source = new(() => new(workspace.Documents));
    readonly Lazy<WorkspaceSyntaxIndex> _syntax = new(() => WorkspaceSyntaxIndex.Create(workspace));

    internal AuthoringSnapshot Source => _source.Value;
    internal WorkspaceSyntaxIndex Syntax => _syntax.Value;

    internal static WorkspaceAuthoringAnalysis For(ScreenplayWorkspace workspace) => _analyses.GetValue(workspace, value => new(value));
}
