// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_renaming_with_a_pre_materialization_catalog_revision : given.an_unbound_container_workspace
{
    WorkspaceAuthoringResult _result = null!;

    void Because()
    {
        var target = WorkspaceSyntaxIndex.Create(Workspace).Entries.Single(entry => entry.Address?.Kind == SemanticKind.Slice);
        _result = Workspace.ProposeRename(new()
        {
            ExpectedRevision = Workspace.Revision,
            ExpectedCatalogRevision = SemanticIdentityCatalog.Create(Application, Workspace.IdentityCatalog.Documents, [], []).Revision,
            Target = target.Handle,
            ExpectedName = "PlaceOrder",
            NewName = "Renamed"
        });
    }

    // Decision 0093 item 5: pre-materialization catalog revisions are stale, not accepted aliases.
    [Fact] void should_refuse_the_stale_catalog_revision() => _result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.StaleCatalogRevision);
    [Fact] void should_not_return_a_candidate() => _result.Workspace.ShouldBeNull();
}
