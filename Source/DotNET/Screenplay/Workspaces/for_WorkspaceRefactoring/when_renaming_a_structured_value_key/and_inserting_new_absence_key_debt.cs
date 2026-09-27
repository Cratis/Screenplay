// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_renaming_a_structured_value_key;

public class and_inserting_new_absence_key_debt : given.a_workspace_with_an_absent_read_model_key
{
    WorkspaceAuthoringResult _safe = null!;
    WorkspaceAuthoringResult _draft = null!;
    WorkspaceAuthoringResult _stale = null!;

    void Because()
    {
        var entry = WorkspaceSyntaxIndex.Create(Workspace).Entries.Single(candidate => candidate.Node is ObjectMemberSyntax member && member.Name == "part");
        var request = new WorkspaceAuthoringRequest
        {
            ExpectedRevision = Workspace.Revision,
            ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Operations = [new ReplaceWorkspaceNode(entry.Handle, entry.Node, ((ObjectMemberSyntax)entry.Node) with { Name = "missing" })]
        };
        _safe = Workspace.ProposeAuthoring(request);
        _draft = Workspace.ProposeAuthoring(request with { ReferencePolicy = WorkspaceAuthoringReferencePolicy.Draft });
        _stale = Workspace.ProposeAuthoring(request with { ExpectedRevision = default });
    }

    [Fact] void should_refuse_safe_new_debt() => _safe.Accepted.ShouldBeFalse();
    [Fact] void should_refuse_unproved_draft_new_debt() => _draft.Accepted.ShouldBeFalse();
    [Fact] void should_not_return_write_plans()
    {
        _safe.WritePlan.ShouldBeNull();
        _draft.WritePlan.ShouldBeNull();
        _stale.WritePlan.ShouldBeNull();
    }
    [Fact] void should_refuse_stale_revision_first() => _stale.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.StaleWorkspaceRevision);
}
