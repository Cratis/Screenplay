// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_a_mapping_candidate_changes_other_semantics : given.a_workspace_with_a_produced_mapping
{
    ScreenplayWorkspace _candidate = null!;
    WorkspaceConflict? _conflict;

    void Establish()
    {
        var source = _document.Text.Replace("name = name  //", "name = displayName  //", StringComparison.Ordinal)
            .Replace("count = 42", "count = 43", StringComparison.Ordinal);
        _candidate = _workspace.Propose(Request(new ReplaceWorkspaceDocument { Document = _document.Id, Bytes = [.. Encoding.UTF8.GetBytes(source)] })).Workspace!;
    }

    void Because() => _conflict = ProducedEventMappingPatch.Verify(_operation, _workspace, _candidate);

    [Fact] void should_detect_an_unrequested_specification_change() => _conflict!.Kind.ShouldEqual(WorkspaceConflictKind.UnsupportedSemanticField);
}
#endif
