// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_manually_renaming_an_unbound_container_without_migrations : given.an_unbound_container_workspace
{
    WorkspaceAuthoringResult _result = null!;

    void Because()
    {
        var target = WorkspaceSyntaxIndex.Create(Workspace).Entries.Single(entry => entry.Address?.Kind == SemanticKind.Slice);
        _result = Workspace.ProposeAuthoring(new()
        {
            ExpectedRevision = Workspace.Revision,
            ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Operations = [new ReplaceWorkspaceNode(target.Handle, target.Node, ((SliceSyntax)target.Node) with { Name = "Renamed" })]
        });
    }

    // Decision 0093 item 10: manual AST rename must explicitly migrate the assigned pre-binding identity.
    [Fact] void should_require_an_explicit_identity_migration() => _result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.InvalidIdentityMigration);
    [Fact] void should_refuse_the_rename() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_not_return_a_candidate() => _result.Workspace.ShouldBeNull();
}
